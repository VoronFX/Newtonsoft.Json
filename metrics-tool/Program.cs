using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Xml.Linq;

// relative to metrics-tool/, since that's the cwd for dotnet run
var src = "../Src/Newtonsoft.Json";
var scopeFiles = new[]
{
    "JsonSerializerInternalReader.cs",
    "JsonSerializerInternalWriter.cs",
    "DefaultContractResolver.cs",
    "JsonSerializerInternalBase.cs",
    "JsonTypeReflector.cs",
    "JsonContract.cs",
    "JsonProperty.cs"
};

// no defines.txt lying around, so just pull the same constants the analysis project uses
var defines = XDocument.Load("../analysis/NJ.Analysis.csproj")
    .Descendants("DefineConstants").First().Value
    .Split(';', StringSplitOptions.RemoveEmptyEntries);

var opts = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: defines);

var files = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Replace('\\', '/').Contains("/obj/") && !f.Replace('\\', '/').Contains("/bin/"));
var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), opts, f)).ToList();

// use whatever assemblies this runtime already has loaded instead of a hardcoded SDK path
var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(p => MetadataReference.CreateFromFile(p));

var comp = CSharpCompilation.Create("NJ", trees, refs,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

var errs = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
Console.Error.WriteLine($"errors: {errs.Count}");
foreach (var e in errs.Take(5))
    Console.Error.WriteLine(e);

// map: type -> set of referenced types (own assembly, excluding self/nested-of-self)
var asm = comp.Assembly;
INamedTypeSymbol Top(INamedTypeSymbol t) { t = t.OriginalDefinition; return t; }
var efferent = new Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

void AddRef(INamedTypeSymbol from, ITypeSymbol to)
{
    if (to == null) return;
    if (to is IArrayTypeSymbol at) { AddRef(from, at.ElementType); return; }
    if (to is not INamedTypeSymbol nt) return;

    nt = nt.OriginalDefinition;
    foreach (var ta in (to as INamedTypeSymbol).TypeArguments)
        AddRef(from, ta);

    if (SymbolEqualityComparer.Default.Equals(nt, from)) return;
    if (nt.SpecialType != SpecialType.None && nt.SpecialType != SpecialType.System_Object) return; // skip primitives
    if (nt.SpecialType == SpecialType.System_Object) return;

    if (!efferent.TryGetValue(from, out var s))
        efferent[from] = s = new(SymbolEqualityComparer.Default);
    s.Add(nt);
}

foreach (var tree in trees)
{
    var model = comp.GetSemanticModel(tree);
    var root = tree.GetRoot();
    foreach (var node in root.DescendantNodes())
    {
        if (node is not (IdentifierNameSyntax or GenericNameSyntax or ObjectCreationExpressionSyntax or PredefinedTypeSyntax)) continue;

        var td = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        if (td == null) continue;
        var from = model.GetDeclaredSymbol(td) as INamedTypeSymbol;
        if (from == null) continue;

        var si = model.GetSymbolInfo(node).Symbol;
        ITypeSymbol t = si switch
        {
            ITypeSymbol ts => ts,
            IMethodSymbol ms => ms.ContainingType,
            IPropertySymbol ps => ps.Type,
            IFieldSymbol fs => fs.Type,
            _ => null
        };

        if (si is IMethodSymbol m2) AddRef(from, m2.ReturnType);
        if (si is IPropertySymbol || si is IFieldSymbol) AddRef(from, si.ContainingType);
        AddRef(from, t);
    }
}

var result = new List<object>();
foreach (var tree in trees.Where(t => scopeFiles.Contains(Path.GetFileName(t.FilePath))))
{
    var model = comp.GetSemanticModel(tree);

    foreach (var cd in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
    {
        var sym = model.GetDeclaredSymbol(cd) as INamedTypeSymbol;

        var fields = sym.GetMembers().OfType<IFieldSymbol>()
            .Where(f => !f.IsConst && !f.IsImplicitlyDeclared)
            .Cast<ISymbol>().ToList();

        // auto-properties count as fields
        var autoProps = sym.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.DeclaringSyntaxReferences.Any(r =>
                r.GetSyntax() is PropertyDeclarationSyntax pds &&
                pds.AccessorList != null &&
                pds.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null)))
            .Cast<ISymbol>().ToList();

        var attrs = fields.Concat(autoProps).ToList();

        var inherited = new List<ISymbol>();
        for (var b = sym.BaseType; b != null && SymbolEqualityComparer.Default.Equals(b.ContainingAssembly, asm); b = b.BaseType)
        {
            inherited.AddRange(b.GetMembers()
                .Where(x => (x is IFieldSymbol f && !f.IsConst && !f.IsImplicitlyDeclared) || x is IPropertySymbol)
                .Where(x => x.DeclaredAccessibility != Accessibility.Private));
        }
        attrs.AddRange(inherited);

        var methodNodes = cd.Members.Where(m =>
            m is MethodDeclarationSyntax ||
            (m is PropertyDeclarationSyntax p && !autoProps.Any(a => a.DeclaringSyntaxReferences.Any(r => r.GetSyntax() == p))) ||
            (m is ConstructorDeclarationSyntax cds && !cds.Modifiers.Any(SyntaxKind.StaticKeyword) && false)
        ).ToList();
        var methods = methodNodes.Select(m => (node: m, sym: model.GetDeclaredSymbol(m))).ToList();

        // which fields/props each method touches, and which sibling methods it calls
        var uses = new List<HashSet<ISymbol>>();
        var calls = new List<HashSet<ISymbol>>();
        foreach (var (node, ms) in methods)
        {
            var u = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var c = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var id in node.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                var s = model.GetSymbolInfo(id).Symbol;
                if (s == null) continue;
                s = s.OriginalDefinition;
                if (attrs.Contains(s, SymbolEqualityComparer.Default)) u.Add(s);
                if (methods.Any(mm => SymbolEqualityComparer.Default.Equals(mm.sym, s))) c.Add(s);
            }
            uses.Add(u);
            calls.Add(c);
        }

        int m = methods.Count, a = attrs.Count;

        // LCOM-HS (Henderson-Sellers)
        double lcomhs = double.NaN;
        if (m > 1 && a > 0)
        {
            double sum = attrs.Sum(f => uses.Count(u => u.Contains(f)));
            lcomhs = ((sum / a) - m) / (1.0 - m);
        }

        // LCOM4: connected components over the uses/calls graph
        var parent = Enumerable.Range(0, m).ToArray();
        int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
        for (int i = 0; i < m; i++)
        {
            for (int j = i + 1; j < m; j++)
            {
                bool link = uses[i].Overlaps(uses[j]) || calls[i].Contains(methods[j].sym) || calls[j].Contains(methods[i].sym);
                if (link) parent[Find(i)] = Find(j);
            }
        }
        int lcom4 = Enumerable.Range(0, m).Select(Find).Distinct().Count();
        var comps = Enumerable.Range(0, m)
            .GroupBy(Find)
            .Select(g => g.Select(i => methods[i].sym?.Name).ToList())
            .ToList();

        // Ce/Ca: efferent/afferent coupling
        efferent.TryGetValue(sym.OriginalDefinition, out var eff);
        eff ??= new(SymbolEqualityComparer.Default);

        // exclude own nested types from Ce
        var effList = eff.Where(t => !(t.ContainingType != null && SymbolEqualityComparer.Default.Equals(t.ContainingType, sym))).ToList();
        int ceInternal = effList.Count(t => SymbolEqualityComparer.Default.Equals(t.ContainingAssembly, asm));
        int ceExternal = effList.Count - ceInternal;

        var afferent = efferent
            .Where(kv => !SymbolEqualityComparer.Default.Equals(kv.Key, sym) &&
                         !(kv.Key.ContainingType != null && SymbolEqualityComparer.Default.Equals(kv.Key.ContainingType, sym)) &&
                         kv.Value.Contains(sym.OriginalDefinition))
            .Select(kv => kv.Key.ToDisplayString()).ToList();

        var pubMethods = sym.GetMembers().OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary &&
                        (x.DeclaredAccessibility == Accessibility.Public ||
                         x.DeclaredAccessibility == Accessibility.Internal ||
                         x.DeclaredAccessibility == Accessibility.Protected ||
                         x.DeclaredAccessibility == Accessibility.ProtectedOrInternal))
            .Select(x => x.Name).Distinct().ToList();
        var allMethods = sym.GetMembers().OfType<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary).Count();
        var props = sym.GetMembers().OfType<IPropertySymbol>().Count();

        result.Add(new
        {
            type = sym.ToDisplayString(),
            file = Path.GetFileName(tree.FilePath),
            kind = cd.Keyword.Text,
            baseType = sym.BaseType?.ToDisplayString(),
            methodsAnalysed = m,
            ordinaryMethods = allMethods,
            properties = props,
            fields = fields.Select(f => f.Name).ToList(),
            autoProps = autoProps.Select(p => p.Name).ToList(),
            inheritedAttrs = inherited.Select(p => p.Name).ToList(),
            lcomHS = double.IsNaN(lcomhs) ? (double?)null : Math.Round(lcomhs, 3),
            lcom4,
            comps,
            ceInternal,
            ceExternal,
            ceTypes = effList.Select(t => t.ToDisplayString()).OrderBy(x => x).ToList(),
            ca = afferent.Count,
            caTypes = afferent,
            nonPrivateMethods = pubMethods,
            unusedAttrs = attrs.Where(f => !uses.Any(u => u.Contains(f))).Select(f => f.Name).ToList()
        });
    }
}

File.WriteAllText("../results/types.json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("ok " + result.Count);

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

// Граф залежностей між типами бібліотеки Newtonsoft.Json (Roslyn Semantic API)
var src = args.Length > 0 ? args[0] : "Src/Newtonsoft.Json";
var outPath = args.Length > 1 ? args[1] : "pr2-analysis/data/graph.json";
var defines = File.ReadAllText("defines.txt").Trim().Split(';', StringSplitOptions.RemoveEmptyEntries);
var opts = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: defines);
var files = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"));
var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), opts, f)).ToList();
var refs = Directory.GetFiles("/usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.31/ref/net8.0", "*.dll").Select(p => MetadataReference.CreateFromFile(p));
var comp = CSharpCompilation.Create("NJ", trees, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
Console.Error.WriteLine("errors: " + comp.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error));
var asm = comp.Assembly;
string Name(INamedTypeSymbol t) => t.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");

var edges = new Dictionary<(string, string, string), int>();
var types = new Dictionary<string, object>();
void Add(string from, ITypeSymbol to, string kind) {
  if (to == null) return;
  if (to is IArrayTypeSymbol at) { Add(from, at.ElementType, kind); return; }
  if (to is IPointerTypeSymbol pt) { Add(from, pt.PointedAtType, kind); return; }
  if (to is not INamedTypeSymbol nt) return;
  foreach (var ta in nt.TypeArguments) Add(from, ta, kind == "inherit" ? "typeref" : kind);
  var od = nt.OriginalDefinition;
  if (!SymbolEqualityComparer.Default.Equals(od.ContainingAssembly, asm)) return;
  var tn = Name(od);
  if (tn == from) return;
  var k = (from, tn, kind); edges[k] = edges.TryGetValue(k, out var c) ? c + 1 : 1;
}
foreach (var tree in trees) {
  var model = comp.GetSemanticModel(tree);
  var rel = Path.GetRelativePath(src, tree.FilePath);
  foreach (var td in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Cast<SyntaxNode>().Concat(tree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>())) {
    var sym = model.GetDeclaredSymbol(td) as INamedTypeSymbol; if (sym == null) continue;
    var name = Name(sym);
    var span = td.GetLocation().GetLineSpan();
    if (!types.ContainsKey(name)) {
      var methods = sym.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToList();
      types[name] = new {
        name, ns = sym.ContainingNamespace.ToDisplayString(), file = rel, folder = Path.GetDirectoryName(rel) ?? "",
        kind = sym.TypeKind.ToString(), access = sym.DeclaredAccessibility.ToString(), isStatic = sym.IsStatic, isAbstract = sym.IsAbstract,
        nested = sym.ContainingType != null, container = sym.ContainingType != null ? Name(sym.ContainingType) : null,
        obsolete = sym.GetAttributes().Any(a => a.AttributeClass?.Name == "ObsoleteAttribute"),
        obsoleteMembers = sym.GetMembers().Count(m => m.GetAttributes().Any(a => a.AttributeClass?.Name == "ObsoleteAttribute")),
        methods = methods.Count, publicMethods = methods.Count(m => m.DeclaredAccessibility == Accessibility.Public || m.DeclaredAccessibility == Accessibility.Protected || m.DeclaredAccessibility == Accessibility.ProtectedOrInternal),
        fields = sym.GetMembers().OfType<IFieldSymbol>().Count(f => !f.IsImplicitlyDeclared && !f.IsConst),
        properties = sym.GetMembers().OfType<IPropertySymbol>().Count(),
        lines = sym.DeclaringSyntaxReferences.Sum(r => { var ls = r.GetSyntax().GetLocation().GetLineSpan(); return ls.EndLinePosition.Line - ls.StartLinePosition.Line + 1; }),
        ops = sym.GetMembers().OfType<IMethodSymbol>().Count(m => m.MethodKind == MethodKind.Conversion || m.MethodKind == MethodKind.UserDefinedOperator),
        partial = sym.DeclaringSyntaxReferences.Length
      };
    }
    // base types
    if (td is BaseTypeDeclarationSyntax btd && btd.BaseList != null)
      foreach (var bt in btd.BaseList.Types) Add(name, model.GetTypeInfo(bt.Type).Type, "inherit");
    // body: attribute each node to innermost type declaration
    foreach (var node in td.DescendantNodes()) {
      var owner = node.Ancestors().FirstOrDefault(a => a is BaseTypeDeclarationSyntax || a is DelegateDeclarationSyntax);
      if (owner != td) continue;
      if (node.Ancestors().Any(a => a is BaseListSyntax)) continue;
      switch (node) {
        case ObjectCreationExpressionSyntax oc: Add(name, model.GetTypeInfo(oc).Type, "create"); break;
        case InvocationExpressionSyntax inv: {
          if (model.GetSymbolInfo(inv).Symbol is IMethodSymbol ms) { Add(name, ms.ContainingType, ms.IsExtensionMethod ? "call" : "call"); }
          break; }
        case MemberAccessExpressionSyntax ma: {
          var s = model.GetSymbolInfo(ma).Symbol;
          if (s is IFieldSymbol || s is IPropertySymbol || s is IEventSymbol) Add(name, s.ContainingType, "access");
          break; }
        case IdentifierNameSyntax id: {
          var s = model.GetSymbolInfo(id).Symbol;
          if (s is ITypeSymbol ts) Add(name, ts, node.Parent is VariableDeclarationSyntax || node.Parent is ParameterSyntax || node.Parent is PropertyDeclarationSyntax || node.Parent is MethodDeclarationSyntax ? "member" : "typeref");
          else if ((s is IFieldSymbol || s is IPropertySymbol) && node.Parent is not MemberAccessExpressionSyntax) Add(name, s.ContainingType, "access");
          break; }
        case GenericNameSyntax gn: {
          var s = model.GetSymbolInfo(gn).Symbol;
          if (s is ITypeSymbol ts) Add(name, ts, "typeref");
          break; }
      }
    }
  }
}
// внутрішні члени, що не використовуються (для Lava Flow): private/internal методи без посилань
var allNodes = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>().Select(n => (t, n))).ToList();
var usedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
foreach (var g in allNodes.GroupBy(x => x.t)) {
  var model = comp.GetSemanticModel(g.Key);
  foreach (var (_, n) in g) { var s = model.GetSymbolInfo(n).Symbol; if (s != null) { usedSymbols.Add(s.OriginalDefinition); if (s is IMethodSymbol m && m.ReducedFrom != null) usedSymbols.Add(m.ReducedFrom.OriginalDefinition); } var ti = model.GetTypeInfo(n).Type; }
}
var unused = new List<object>();
foreach (var tree in trees) {
  var model = comp.GetSemanticModel(tree);
  foreach (var md in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()) {
    var ms = model.GetDeclaredSymbol(md);
    if (ms == null || ms.IsOverride || ms.IsVirtual || ms.IsAbstract || ms.ExplicitInterfaceImplementations.Length > 0) continue;
    if (ms.DeclaredAccessibility != Accessibility.Private && ms.DeclaredAccessibility != Accessibility.Internal) continue;
    if (ms.ContainingType.AllInterfaces.Any(i => i.GetMembers().Any(im => SymbolEqualityComparer.Default.Equals(ms.ContainingType.FindImplementationForInterfaceMember(im), ms)))) continue;
    if (!usedSymbols.Contains(ms.OriginalDefinition))
      unused.Add(new { type = Name(ms.ContainingType), method = ms.Name, access = ms.DeclaredAccessibility.ToString(), file = Path.GetRelativePath(src, tree.FilePath), line = md.GetLocation().GetLineSpan().StartLinePosition.Line + 1 });
  }
}

// Структура керування методів: глибина вкладеності, goto, return, break/continue
int Depth(SyntaxNode n) {
  int best = 0;
  foreach (var ch in n.ChildNodes()) {
    int d = Depth(ch);
    bool ctl = ch is IfStatementSyntax || ch is ForStatementSyntax || ch is ForEachStatementSyntax || ch is WhileStatementSyntax || ch is DoStatementSyntax || ch is SwitchStatementSyntax || ch is TryStatementSyntax || ch is LockStatementSyntax || ch is UsingStatementSyntax;
    // else-if не додає рівня
    if (ch is IfStatementSyntax && ch.Parent is ElseClauseSyntax) ctl = false;
    best = Math.Max(best, d + (ctl ? 1 : 0));
  }
  return best;
}
var flow = new List<object>();
foreach (var tree in trees) {
  foreach (var md in tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>()) {
    var body = (SyntaxNode)md.Body ?? md.ExpressionBody; if (body == null) continue;
    var ls = md.GetLocation().GetLineSpan();
    flow.Add(new { file = Path.GetRelativePath(src, tree.FilePath), line = ls.StartLinePosition.Line + 1,
      name = md is MethodDeclarationSyntax m ? m.Identifier.Text : md is ConstructorDeclarationSyntax c ? c.Identifier.Text : md.Kind().ToString(),
      depth = Depth(body), gotos = body.DescendantNodes().Count(x => x is GotoStatementSyntax),
      returns = body.DescendantNodes().Count(x => x is ReturnStatementSyntax), breaks = body.DescendantNodes().Count(x => x is BreakStatementSyntax || x is ContinueStatementSyntax),
      switches = body.DescendantNodes().Count(x => x is SwitchStatementSyntax), loops = body.DescendantNodes().Count(x => x is ForStatementSyntax || x is ForEachStatementSyntax || x is WhileStatementSyntax || x is DoStatementSyntax),
      lines = ls.EndLinePosition.Line - ls.StartLinePosition.Line + 1 });
  }
}
File.WriteAllText(Path.Combine(Path.GetDirectoryName(outPath), "flow.json"), JsonSerializer.Serialize(flow, new JsonSerializerOptions { WriteIndented = false }));
Directory.CreateDirectory(Path.GetDirectoryName(outPath));
File.WriteAllText(outPath, JsonSerializer.Serialize(new {
  types = types.Values,
  edges = edges.Select(e => new { from = e.Key.Item1, to = e.Key.Item2, kind = e.Key.Item3, count = e.Value }),
  unusedMethods = unused
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"types {types.Count}, edges {edges.Count}, unused {unused.Count}");

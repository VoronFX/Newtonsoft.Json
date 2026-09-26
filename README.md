# Analysis Configuration — Practical Work 1, Variant 2 (Newtonsoft.Json)

Scope: Src/Newtonsoft.Json/Serialization — JsonSerializerInternalReader, JsonSerializerInternalWriter,
DefaultContractResolver, JsonSerializerInternalBase, JsonTypeReflector, JsonContract, JsonProperty.

## Steps

1. Roslyn analysis (CA*/IDE*/CS*, metrics CA1502/1505/1506) → `results/analysis.sarif`
2. Cyclomatic complexity (lizard) → `results/lizard.csv`
3. Duplicate code detection (PMD CPD, minimum-tokens 50) → `results/cpd_50.xml`
4. Cohesion/coupling metrics (LCOM-HS, LCOM4, Ce, Ca) → `results/types.json`
5. Post-processing and chart generation (`scripts/*.py`) → `results/*.json`, `results/*.png`

## Commands (Windows PowerShell)


```powershell
New-Item -ItemType Directory -Force -Path results | Out-Null

$files = @(
    "Src/Newtonsoft.Json/Serialization/JsonSerializerInternalReader.cs",
    "Src/Newtonsoft.Json/Serialization/JsonSerializerInternalWriter.cs",
    "Src/Newtonsoft.Json/Serialization/DefaultContractResolver.cs",
    "Src/Newtonsoft.Json/Serialization/JsonSerializerInternalBase.cs",
    "Src/Newtonsoft.Json/Serialization/JsonTypeReflector.cs",
    "Src/Newtonsoft.Json/Serialization/JsonContract.cs",
    "Src/Newtonsoft.Json/Serialization/JsonProperty.cs"
)

# 1. Roslyn analyzers -> results/analysis.sarif
cd analysis
dotnet build -c Release
cd ..

# 2. lizard -> results/lizard.csv
lizard -l csharp --csv $files > results/lizard.csv

# 3. PMD CPD -> results/cpd_50.xml
pmd cpd --minimum-tokens 50 --language cs --format xml -d ($files -join ",") > results/cpd_50.xml

# 4. custom metrics tool -> results/types.json
cd metrics-tool
dotnet run
cd ..

# 5. post-processing / charts (reads and writes under results/)
python scripts/extract.py
python scripts/metrics.py
python scripts/sqale.py
python scripts/charts.py
```

# PR4 — рефакторинг JsonTypeReflector: вимірювання і перевірка

- `before/`, `after/` — метрики до (тег `pr4-baseline`) і після рефакторингу тими самими інструментами, що в ПР1:
  `scope_results.json` (діагностики Roslyn з `analysis/NJ.Analysis.csproj`), `types.json` (`metrics-tool`: LCOM-HS, LCOM4, Ce, Ca),
  `lizard.csv` (Lizard 1.24.0), `cpd_50.xml` / `cpd_30.xml` (PMD CPD 7.7.0). Для «після» в область включено нові файли
  `Serialization/AssociatedMetadataTypeResolver.cs` і `Utilities/ReflectionEnvironment.cs`.
- `dependencies.txt` — підсумок графа залежностей (`pr2-analysis/depgraph`) до і після.
- `verify-commits.ps1` — скрипт перевірки: для кожного коміту від `pr4-baseline` до `master` збирає бібліотеку під усі платформи
  і запускає характеризаційні тести; наприкінці — повний набір тестів бібліотеки до/після і покриття.
- `test-log.md`, `logs/` — результати останнього прогону скрипта; `coverage/` — покриття модуля після рефакторингу.

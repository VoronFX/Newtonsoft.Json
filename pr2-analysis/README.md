# Практична робота 2 — архітектурні антипатерни (варіант 2, Newtonsoft.Json)

Базовий коміт: 52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa. Область: Src/Newtonsoft.Json у цілому.

- `diagrams/*.puml` — діаграми компонентів у редагованому форматі PlantUML (+ PNG);
  `diagrams/fragments/` — фрагменти діаграми для кожного антипатерну.
- `depgraph/` — утиліта на Roslyn: граф залежностей між типами (`dotnet run -c Release -- <шлях до Src/Newtonsoft.Json> <вихід/graph.json>`;
  потрібен файл символів умовної компіляції, як у ПР №1 — шлях задано в Program.cs).
- `data/` — graph.json (типи та ребра), flow.json (вкладеність методів), cc_all.json (M усіх методів), cross.json (міжпакетні залежності).
- `scripts/` — analyze.py → backedges.py → whatif.py → spaghetti.py → gen_puml.py / gen_puml2.py → charts2.py.

Рендер діаграм: `java -jar plantuml.jar -tpng -charset UTF-8 diagrams/components_packages.puml` (потрібен Graphviz).

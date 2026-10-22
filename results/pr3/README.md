# Практична робота 3 — характеризаційні тести для JsonTypeReflector

Модуль: `Src/Newtonsoft.Json/Serialization/JsonTypeReflector.cs` (577 рядків, 259 рядків, що покриваються).
Тести: `Src/Newtonsoft.Json.CharacterizationTests/JsonTypeReflector/` — 106 тестів NUnit, 12 із категорією `Suspicious`.

| Ітерація | Тестів | Рядки | Гілки |
|---|---|---|---|
| baseline | 1 (службовий) | 0 % (0/259) | 0 % (0/98) |
| iter1 — типові сценарії | 27 | 65,2 % | 47,9 % |
| iter2a — межові випадки конвертерів | 50 | 69,1 % | 53,0 % |
| final | 106 | 99,6 % (258/259) | 97,9 % (96/98) |

Запуск (з кореня репозиторію):

    dotnet test Src/Newtonsoft.Json.CharacterizationTests -c Debug --collect:"XPlat Code Coverage" --settings Src/Newtonsoft.Json.CharacterizationTests/coverlet.runsettings

Те саме разом зі звітом ReportGenerator: `Src/Newtonsoft.Json.CharacterizationTests/run-coverage.ps1`.
Лише підозрілі результати: додайте `--filter TestCategory=Suspicious`.
Проміжні ітерації (iter1, iter2a) виміряно під час написання тестів; відтоді ці тестові класи доповнювалися.

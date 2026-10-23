# PR4: перевірка після кожного рефакторингу

Дата: 2026-10-24 02:25, .NET SDK 10.0.402

| Коміт | Збірка (усі TFM) | Попереджень | Характеризаційні тести | Результат |
|---|---|---|---|---|
| 33a03441 Practical Work 3 | OK | 0 | total 106, failed 0, passed 106 | OK |
| d7f03398 PR4 1/6 Rename Method: GetAssociateMetadataTypeFromAttribute -> FindMetadataClassType | OK | 0 | total 106, failed 0, passed 106 | OK |
| 0a063556 PR4 2/6 Extract Variable: name the converter checks in CanTypeDescriptorConvertString | OK | 0 | total 106, failed 0, passed 106 | OK |
| 9bb17936 PR4 3/6 Extract Method: split constructor lookup out of the GetCreator lambda | OK | 0 | total 106, failed 0, passed 106 | OK |
| 5f3b158a PR4 4/6 Replace Inline Code with Function Call: GetJsonConverter reuses CreateJsonConverterInstance | OK | 0 | total 106, failed 0, passed 106 | OK |
| ceadf266 PR4 5/6 Extract Class: move MetadataTypeAttribute lookup into AssociatedMetadataTypeResolver | OK | 0 | total 106, failed 0, passed 106 | OK |
| b4c12092 PR4 6/6 Move Method: move DynamicCodeGeneration, FullyTrusted and ReflectionDelegateFactory to Utilities.ReflectionEnvironment | OK | 0 | total 106, failed 0, passed 106 | OK |

Повний набір тестів бібліотеки (Src/Newtonsoft.Json.Tests, net8.0):

| Стан | Тести | Результат | Тести, що падають |
|---|---|---|---|
| pr4-baseline (33a03441) | total 3617, failed 4, passed 3613 | є падіння | Newtonsoft.Json.Tests.Issues.Issue2768.ParseJsonDecimal, Newtonsoft.Json.Tests.Issues.Issue2768.Test_Deserialize, Newtonsoft.Json.Tests.Issues.Issue2768.Test_Deserialize_MultipleTrailingZeroes, Newtonsoft.Json.Tests.Issues.Issue2768.Test_Deserialize_Negative |
| master (b4c12092) | total 3617, failed 4, passed 3613 | є падіння | Newtonsoft.Json.Tests.Issues.Issue2768.ParseJsonDecimal, Newtonsoft.Json.Tests.Issues.Issue2768.Test_Deserialize, Newtonsoft.Json.Tests.Issues.Issue2768.Test_Deserialize_MultipleTrailingZeroes, Newtonsoft.Json.Tests.Issues.Issue2768.Test_Deserialize_Negative |

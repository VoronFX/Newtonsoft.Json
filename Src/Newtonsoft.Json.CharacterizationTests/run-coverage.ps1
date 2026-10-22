# Запуск характеризаційних тестів JsonTypeReflector з вимірюванням покриття і звітом ReportGenerator.
# Запускати з кореня репозиторію:  powershell -ExecutionPolicy Bypass -File Src/Newtonsoft.Json.CharacterizationTests/run-coverage.ps1
# Потрібен ReportGenerator:        dotnet tool install -g dotnet-reportgenerator-globaltool
$ErrorActionPreference = 'Stop'
$proj = 'Src/Newtonsoft.Json.CharacterizationTests'
$out  = if ($args.Count -gt 0) { $args[0] } else { 'results/pr3/final' }
$tmp  = "$out/_testresults"

if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
dotnet test $proj -c Debug --collect:"XPlat Code Coverage" --settings "$proj/coverlet.runsettings" --results-directory $tmp
if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed' }

$cov = Get-ChildItem $tmp -Recurse -Filter coverage.cobertura.xml | Select-Object -First 1
Copy-Item $cov.FullName "$out/coverage.cobertura.xml" -Force
Remove-Item $tmp -Recurse -Force

$html = "$out/html"
if (Test-Path $html) { Remove-Item $html -Recurse -Force }
reportgenerator "-reports:$out/coverage.cobertura.xml" "-targetdir:$html" "-reporttypes:Html;TextSummary" "-title:JsonTypeReflector"
Copy-Item "$html/Summary.txt" "$out/Summary.txt" -Force
Write-Host "HTML: $html/index.html"

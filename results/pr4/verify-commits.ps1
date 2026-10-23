# Перевірка рефакторингів ПР4: для кожного коміту від pr4-baseline до master
#  1) збирає бібліотеку під усі цільові платформи (net20 ... net10.0),
#  2) запускає характеризаційні тести ПР3.
# Наприкінці: повний набір тестів бібліотеки (net8.0) до і після та покриття модуля після.
# Запуск:  powershell -ExecutionPolicy Bypass -File verify-commits.ps1 -Repo C:\Users\Voron\Source\Repos\Newtonsoft.Json
param([string]$Repo = 'C:\Users\Voron\Source\Repos\Newtonsoft.Json')

Set-Location $Repo
if (git status --porcelain --untracked-files=no) { throw 'Є незакомічені зміни в репозиторії.' }
$outDir = Join-Path $Repo 'results\pr4'
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir 'test-log.md'
$logs = Join-Path $outDir 'logs'
New-Item -ItemType Directory -Force $logs | Out-Null
$rows = @()

function Get-TestSummary($lines) {
    $m = $lines | Select-String -Pattern 'total:\s*(\d+);\s*failed:\s*(\d+);\s*succeeded:\s*(\d+)' | Select-Object -Last 1
    if ($m) { return 'total {0}, failed {1}, passed {2}' -f $m.Matches[0].Groups[1].Value, $m.Matches[0].Groups[2].Value, $m.Matches[0].Groups[3].Value }
    $m = $lines | Select-String -Pattern '(Passed|Failed)!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)' | Select-Object -Last 1
    if ($m) { $g = $m.Matches[0].Groups; return 'total {0}, failed {1}, passed {2}' -f $g[5].Value, $g[2].Value, $g[3].Value }
    return 'n/a (див. вивід)'
}

$commits = @(git rev-parse --short pr4-baseline) + @(git rev-list --reverse --abbrev-commit pr4-baseline..master)
try {
    foreach ($c in $commits) {
        git -c advice.detachedHead=false checkout -q --detach $c
        $title = git log -1 --format='%h %s'
        Write-Host "=== $title" -ForegroundColor Cyan

        $short = git rev-parse --short HEAD
        $build = dotnet build Src/Newtonsoft.Json/Newtonsoft.Json.csproj -c Debug -nologo --no-incremental -tl:off 2>&1
        $buildOk = ($LASTEXITCODE -eq 0)
        $build | Set-Content (Join-Path $logs "$short-build.txt") -Encoding UTF8
        $warnings = @($build | Select-String -Pattern 'warning [A-Z]+[0-9]+' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique).Count

        $test = dotnet test Src/Newtonsoft.Json.CharacterizationTests -c Debug -nologo -tl:off 2>&1
        $testOk = ($LASTEXITCODE -eq 0)
        $test | Set-Content (Join-Path $logs "$short-tests.txt") -Encoding UTF8
        $summary = Get-TestSummary $test
        Write-Host "build: $buildOk ($warnings warnings), tests: $summary"
        if (-not $buildOk) { $build | Select-String -Pattern 'error [A-Z]+[0-9]+' | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red } }
        $rows += "| $title | $(if ($buildOk) {'OK'} else {'FAILED'}) | $warnings | $summary | $(if ($testOk) {'OK'} else {'FAILED'}) |"
        if (-not ($buildOk -and $testOk)) { Write-Warning "Помилка на коміті $c"; break }
    }

    $full = @()
    foreach ($c in @('pr4-baseline', 'master')) {
        git -c advice.detachedHead=false checkout -q --detach $c
        Write-Host "=== full test suite (net8.0) at $c" -ForegroundColor Cyan
        $t = dotnet test Src/Newtonsoft.Json.Tests -c Debug -f net8.0 -nologo -tl:off 2>&1
        $ok = ($LASTEXITCODE -eq 0)
        $short = git rev-parse --short HEAD
        $t | Set-Content (Join-Path $logs "$short-full-tests.txt") -Encoding UTF8
        $failedNames = ($t | Select-String -Pattern '^\s*Failed\s+(\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique) -join ', '
        $full += "| $c ($short) | $(Get-TestSummary $t) | $(if ($ok) {'OK'} else {'є падіння'}) | $failedNames |"
    }
}
finally {
    git checkout -q master
}

Write-Host '=== coverage at master' -ForegroundColor Cyan
& powershell -ExecutionPolicy Bypass -File Src/Newtonsoft.Json.CharacterizationTests/run-coverage.ps1 results/pr4/coverage

$header = @(
    "# PR4: перевірка після кожного рефакторингу",
    "",
    "Дата: $(Get-Date -Format 'yyyy-MM-dd HH:mm'), .NET SDK $(dotnet --version)",
    "",
    "| Коміт | Збірка (усі TFM) | Попереджень | Характеризаційні тести | Результат |",
    "|---|---|---|---|---|"
)
$fullHeader = @("", "Повний набір тестів бібліотеки (Src/Newtonsoft.Json.Tests, net8.0):", "", "| Стан | Тести | Результат | Тести, що падають |", "|---|---|---|---|")
($header + $rows + $fullHeader + $full) | Set-Content -Path $log -Encoding UTF8
Write-Host "Log: $log"

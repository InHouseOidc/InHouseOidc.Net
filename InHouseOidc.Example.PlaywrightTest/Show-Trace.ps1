# Show-Trace.ps1
# Interactive Playwright trace viewer for InHouseOidc.Example.PlaywrightTest
# Run from the project directory: .\Show-Trace.ps1

$projectDir = $PSScriptRoot
$testResultsDir = Join-Path $projectDir "TestResults"
$playwrightPs1 = Join-Path $projectDir "bin\Debug\net10.0\playwright.ps1"

if (-not (Test-Path $testResultsDir)) {
    Write-Host "No TestResults folder found. Run the tests first." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $playwrightPs1)) {
    Write-Host "playwright.ps1 not found at '$playwrightPs1'. Build the project first." -ForegroundColor Yellow
    exit 1
}

function Show-Menu {
    param(
        [string]$Title,
        [string[]]$Items
    )
    Write-Host ""
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("-" * $Title.Length) -ForegroundColor Cyan
    for ($i = 0; $i -lt $Items.Count; $i++) {
        Write-Host ("  [{0}] {1}" -f ($i + 1), $Items[$i])
    }
    Write-Host "  [0] Quit"
    Write-Host ""

    while ($true) {
        $input = Read-Host "Select"
        if ($input -eq "0") { return $null }
        $n = $input -as [int]
        if ($n -ge 1 -and $n -le $Items.Count) {
            return $Items[$n - 1]
        }
        Write-Host "  Invalid selection, try again." -ForegroundColor Red
    }
}

while ($true) {
    # List class folders
    $classFolders = @(Get-ChildItem $testResultsDir -Directory | Select-Object -ExpandProperty Name)
    if ($classFolders.Count -eq 0) {
        Write-Host "No trace folders found under TestResults. Run the tests first." -ForegroundColor Yellow
        exit 1
    }

    if ($classFolders.Count -eq 1) {
        $selectedClass = $classFolders[0]
        Write-Host ""
        Write-Host "  Auto-selected class: $selectedClass" -ForegroundColor DarkCyan
    } else {
        $selectedClass = Show-Menu -Title "Test class" -Items $classFolders
        if ($null -eq $selectedClass) { exit 0 }
    }

    # List trace zips in the chosen class folder
    $classDir = Join-Path $testResultsDir $selectedClass
    while ($true) {
        $traceFiles = @(Get-ChildItem $classDir -Filter "*.zip" | Select-Object -ExpandProperty BaseName)
        if ($traceFiles.Count -eq 0) {
            Write-Host "  No trace files found in '$selectedClass'." -ForegroundColor Yellow
            break
        }

        if ($traceFiles.Count -eq 1) {
            $selectedTest = $traceFiles[0]
            Write-Host ""
            Write-Host "  Auto-selected test: $selectedTest" -ForegroundColor DarkCyan
        } else {
            $selectedTest = Show-Menu -Title "Test ($selectedClass)" -Items $traceFiles
            if ($null -eq $selectedTest) { break }
        }

        $tracePath = Join-Path $classDir "$selectedTest.zip"
        Write-Host ""
        Write-Host "  Opening trace: $tracePath" -ForegroundColor Green
        & $playwrightPs1 show-trace $tracePath

        if ($traceFiles.Count -eq 1) { break }
    }
}

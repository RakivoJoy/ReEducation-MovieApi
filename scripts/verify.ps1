param([switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solutions = @(Get-ChildItem -LiteralPath $repoRoot -File | Where-Object { $_.Extension -in '.sln', '.slnx' })

if ($solutions.Count -eq 0) {
    Write-Warning 'No .NET solution found at the repository root. Skipping build and tests.'
    exit 0
}

if ($solutions.Count -gt 1) {
    throw 'Verification expects exactly one .NET solution at the repository root.'
}

$solution = $solutions[0].FullName

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solution --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($SkipTests) {
    Write-Host 'Build completed. Tests were skipped.'
    exit 0
}

dotnet test $solution --configuration Release --no-build --no-restore
exit $LASTEXITCODE

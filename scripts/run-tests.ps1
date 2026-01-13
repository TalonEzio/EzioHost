# Run all tests and generate coverage reports
param(
    [string]$Configuration = "Debug"
)

# Get the script directory and find project root (where .sln file is)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = $scriptDir

# Try to find project root by looking for .sln file
while ($projectRoot -ne $null -and -not (Test-Path (Join-Path $projectRoot "*.sln"))) {
    $parent = Split-Path -Parent $projectRoot
    if ($parent -eq $projectRoot) {
        # Reached root, use script directory's parent
        $projectRoot = Split-Path -Parent $scriptDir
        break
    }
    $projectRoot = $parent
}

# If still not found, assume parent of scripts folder
if (-not (Test-Path (Join-Path $projectRoot "*.sln"))) {
    $projectRoot = Split-Path -Parent $scriptDir
}

# Change to project root directory
Push-Location $projectRoot
Write-Host "Changed to project root: $projectRoot" -ForegroundColor Gray

try {
    Write-Host "Running all tests..." -ForegroundColor Green

    # Clean previous test results
    if (Test-Path "TestResults") {
        Remove-Item -Recurse -Force "TestResults"
    }

    # Run Unit Tests
    Write-Host "`nRunning Unit Tests..." -ForegroundColor Cyan
    dotnet test Test/EzioHost.UnitTests/EzioHost.UnitTests.csproj --configuration $Configuration --collect:"XPlat Code Coverage" --results-directory:TestResults

    # Run Integration Tests
    Write-Host "`nRunning Integration Tests..." -ForegroundColor Cyan
    dotnet test Test/EzioHost.IntegrationTests/EzioHost.IntegrationTests.csproj --configuration $Configuration --collect:"XPlat Code Coverage" --results-directory:TestResults

    Write-Host "`nTests completed!" -ForegroundColor Green
    Write-Host "Coverage data saved to TestResults directory" -ForegroundColor Yellow

    # Generate coverage report
    & "$scriptDir\generate-coverage-report.ps1"
}
finally {
    # Restore original directory
    Pop-Location
}

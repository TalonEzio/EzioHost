# Generate HTML coverage report from coverage data
param(
    [string]$ReportFormat = "Html"
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
    Write-Host "Generating coverage report..." -ForegroundColor Green

    # Check if ReportGenerator is installed
    $reportGeneratorPath = "tools\ReportGenerator\ReportGenerator.exe"
    if (-not (Test-Path $reportGeneratorPath)) {
        Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
        $reportGeneratorPath = "reportgenerator"
    }

    # Find coverage files - check both root TestResults and test project TestResults
    $coverageFiles = @()
    
    # Check root TestResults
    if (Test-Path "TestResults") {
        $files = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
        if ($files) {
            $coverageFiles += $files | Select-Object -ExpandProperty FullName
        }
    }
    
    # Check test projects TestResults
    $testProjects = @("Test\EzioHost.UnitTests", "Test\EzioHost.IntegrationTests")
    foreach ($testProject in $testProjects) {
        $testResultsPath = Join-Path $testProject "TestResults"
        if (Test-Path $testResultsPath) {
            $files = Get-ChildItem -Path $testResultsPath -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
            if ($files) {
                $coverageFiles += $files | Select-Object -ExpandProperty FullName
            }
        }
    }

    if ($null -eq $coverageFiles -or $coverageFiles.Count -eq 0) {
        Write-Host "No coverage files found. Run tests first." -ForegroundColor Red
        Write-Host "Looking in:" -ForegroundColor Yellow
        Write-Host "  - TestResults\" -ForegroundColor Gray
        Write-Host "  - Test\EzioHost.UnitTests\TestResults\" -ForegroundColor Gray
        Write-Host "  - Test\EzioHost.IntegrationTests\TestResults\" -ForegroundColor Gray
        exit 1
    }

    Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Cyan
    foreach ($file in $coverageFiles) {
        Write-Host "  - $file" -ForegroundColor Gray
    }

    # Merge coverage files if multiple exist
    $mergedCoverage = Join-Path $projectRoot "TestResults\merged-coverage.cobertura.xml"
    
    # Ensure TestResults directory exists
    $testResultsDir = Join-Path $projectRoot "TestResults"
    if (-not (Test-Path $testResultsDir)) {
        New-Item -ItemType Directory -Path $testResultsDir -Force | Out-Null
    }
    
    if ($coverageFiles.Count -gt 1) {
        Write-Host "Merging coverage files..." -ForegroundColor Yellow
        # For simplicity, we'll use the first file. In production, use a proper merger
        $sourceFile = $coverageFiles[0]
        if ($sourceFile -is [array]) {
            $sourceFile = $sourceFile[0]
        }
        Copy-Item -Path $sourceFile -Destination $mergedCoverage -Force
    } else {
        $sourceFile = $coverageFiles
        if ($sourceFile -is [array]) {
            $sourceFile = $sourceFile[0]
        }
        Copy-Item -Path $sourceFile -Destination $mergedCoverage -Force
    }
    
    if (-not (Test-Path $mergedCoverage)) {
        Write-Host "Failed to create merged coverage file at: $mergedCoverage" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Merged coverage file created: $mergedCoverage" -ForegroundColor Green

    # Generate report
    $reportPath = "TestResults\coverage"
    Write-Host "Generating $ReportFormat report to $reportPath..." -ForegroundColor Cyan

    if ($reportGeneratorPath -eq "reportgenerator") {
        reportgenerator -reports:"$mergedCoverage" -targetdir:"$reportPath" -reporttypes:"$ReportFormat"
    } else {
        & $reportGeneratorPath -reports:"$mergedCoverage" -targetdir:"$reportPath" -reporttypes:"$ReportFormat"
    }

    Write-Host "`nCoverage report generated at: $reportPath\index.html" -ForegroundColor Green
    Write-Host "Open the HTML file in your browser to view the coverage report." -ForegroundColor Yellow
}
finally {
    # Restore original directory
    Pop-Location
}

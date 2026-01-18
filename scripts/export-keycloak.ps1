# Export Keycloak Docker Container và Data Volume
# Script này export Keycloak image và data volume (chứa realm đã cấu hình) để chuyển sang máy khác

param(
    [string]$OutputPath = "keycloak-backup",
    [switch]$StopContainer = $false
)

# Get the script directory and find project root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = $scriptDir

# Try to find project root by looking for .sln file
while ($projectRoot -ne $null -and -not (Test-Path (Join-Path $projectRoot "*.sln"))) {
    $parent = Split-Path -Parent $projectRoot
    if ($parent -eq $projectRoot) {
        $projectRoot = Split-Path -Parent $scriptDir
        break
    }
    $projectRoot = $parent
}

if (-not (Test-Path (Join-Path $projectRoot "*.sln"))) {
    $projectRoot = Split-Path -Parent $scriptDir
}

# Change to project root directory
Push-Location $projectRoot
Write-Host "Changed to project root: $projectRoot" -ForegroundColor Gray

try {
    Write-Host "`n=== Keycloak Export Script ===" -ForegroundColor Green
    Write-Host ""

    # Check if Docker is running
    try {
        docker version | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Docker is not running"
        }
    } catch {
        Write-Host "Error: Docker is not running or not installed." -ForegroundColor Red
        Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
        exit 1
    }

    # Find Keycloak volume
    Write-Host "Searching for Keycloak volume..." -ForegroundColor Cyan
    $volumes = docker volume ls --format "{{.Name}}" | Where-Object { $_ -like "*keycloak*" -or $_ -like "*Keycloak*" }
    
    if ($null -eq $volumes -or $volumes.Count -eq 0) {
        Write-Host "No Keycloak volume found. Trying to find Aspire volumes..." -ForegroundColor Yellow
        $volumes = docker volume ls --format "{{.Name}}" | Where-Object { $_ -like "*aspire*" -and ($_ -like "*keycloak*" -or $_ -like "*Keycloak*") }
    }

    if ($null -eq $volumes -or $volumes.Count -eq 0) {
        Write-Host "Error: Could not find Keycloak volume." -ForegroundColor Red
        Write-Host "Available volumes:" -ForegroundColor Yellow
        docker volume ls
        Write-Host "`nPlease ensure Keycloak container has been run at least once via Aspire." -ForegroundColor Yellow
        exit 1
    }

    $keycloakVolume = $volumes[0]
    if ($volumes.Count -gt 1) {
        Write-Host "Multiple Keycloak volumes found:" -ForegroundColor Yellow
        for ($i = 0; $i -lt $volumes.Count; $i++) {
            Write-Host "  [$i] $($volumes[$i])" -ForegroundColor Gray
        }
        $selection = Read-Host "Select volume number (default: 0)"
        if ($selection -ne "") {
            $keycloakVolume = $volumes[[int]$selection]
        }
    }

    Write-Host "Found Keycloak volume: $keycloakVolume" -ForegroundColor Green

    # Find Keycloak container
    Write-Host "`nSearching for Keycloak container..." -ForegroundColor Cyan
    $containers = docker ps -a --format "{{.Names}}" | Where-Object { $_ -like "*keycloak*" -or $_ -like "*Keycloak*" }
    
    if ($null -ne $containers -and $containers.Count -gt 0) {
        $keycloakContainer = $containers[0]
        Write-Host "Found Keycloak container: $keycloakContainer" -ForegroundColor Green
        
        # Check if container is running
        $containerStatus = docker inspect -f '{{.State.Running}}' $keycloakContainer 2>$null
        if ($containerStatus -eq "true") {
            if ($StopContainer) {
                Write-Host "Stopping Keycloak container..." -ForegroundColor Yellow
                docker stop $keycloakContainer
                Write-Host "Container stopped." -ForegroundColor Green
            } else {
                Write-Host "Warning: Keycloak container is running. It's recommended to stop it before export." -ForegroundColor Yellow
                Write-Host "Use -StopContainer switch to stop it automatically, or stop manually: docker stop $keycloakContainer" -ForegroundColor Yellow
                $continue = Read-Host "Continue anyway? (y/N)"
                if ($continue -ne "y" -and $continue -ne "Y") {
                    Write-Host "Export cancelled." -ForegroundColor Red
                    exit 0
                }
            }
        }
    } else {
        Write-Host "No Keycloak container found (this is OK if using Aspire)." -ForegroundColor Gray
    }

    # Find Keycloak image
    Write-Host "`nSearching for Keycloak image..." -ForegroundColor Cyan
    $images = docker images --format "{{.Repository}}:{{.Tag}}" | Where-Object { $_ -like "*keycloak*" -or $_ -like "*Keycloak*" }
    
    $keycloakImage = $null
    if ($null -ne $images -and $images.Count -gt 0) {
        $keycloakImage = $images[0]
        if ($images.Count -gt 1) {
            Write-Host "Multiple Keycloak images found:" -ForegroundColor Yellow
            for ($i = 0; $i -lt $images.Count; $i++) {
                Write-Host "  [$i] $($images[$i])" -ForegroundColor Gray
            }
            $selection = Read-Host "Select image number (default: 0)"
            if ($selection -ne "") {
                $keycloakImage = $images[[int]$selection]
            }
        }
        Write-Host "Found Keycloak image: $keycloakImage" -ForegroundColor Green
    } else {
        Write-Host "No custom Keycloak image found. Will use base image info from Aspire." -ForegroundColor Yellow
        Write-Host "Note: Keycloak base image is typically 'quay.io/keycloak/keycloak:latest'" -ForegroundColor Gray
    }

    # Create output directory
    $outputDir = Join-Path $projectRoot $OutputPath
    if (Test-Path $outputDir) {
        Write-Host "`nOutput directory already exists: $outputDir" -ForegroundColor Yellow
        $overwrite = Read-Host "Overwrite? (y/N)"
        if ($overwrite -ne "y" -and $overwrite -ne "Y") {
            Write-Host "Export cancelled." -ForegroundColor Red
            exit 0
        }
        Remove-Item -Recurse -Force $outputDir
    }
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "Created output directory: $outputDir" -ForegroundColor Green

    # Export volume
    Write-Host "`nExporting Keycloak data volume..." -ForegroundColor Cyan
    $volumeBackupFile = Join-Path $outputDir "keycloak-data-backup.tar.gz"
    
    Write-Host "This may take a few minutes depending on volume size..." -ForegroundColor Yellow
    docker run --rm -v "${keycloakVolume}:/data" -v "${outputDir}:/backup" alpine tar czf /backup/keycloak-data-backup.tar.gz -C /data .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to export volume." -ForegroundColor Red
        exit 1
    }

    if (Test-Path $volumeBackupFile) {
        $fileSize = (Get-Item $volumeBackupFile).Length / 1MB
        Write-Host "Volume exported successfully: $volumeBackupFile ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Host "Error: Backup file was not created." -ForegroundColor Red
        exit 1
    }

    # Export image (if found)
    if ($null -ne $keycloakImage) {
        Write-Host "`nExporting Keycloak image..." -ForegroundColor Cyan
        $imageBackupFile = Join-Path $outputDir "keycloak-image.tar"
        
        Write-Host "This may take a few minutes depending on image size..." -ForegroundColor Yellow
        docker save $keycloakImage -o $imageBackupFile
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Warning: Failed to export image. Continuing anyway..." -ForegroundColor Yellow
        } else {
            if (Test-Path $imageBackupFile) {
                $fileSize = (Get-Item $imageBackupFile).Length / 1MB
                Write-Host "Image exported successfully: $imageBackupFile ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
            }
        }
    }

    # Create info file
    Write-Host "`nCreating export info file..." -ForegroundColor Cyan
    $infoFile = Join-Path $outputDir "keycloak-export-info.txt"
    $infoContent = @"
Keycloak Export Information
============================
Export Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Export Location: $outputDir

Volume Information:
- Volume Name: $keycloakVolume
- Backup File: keycloak-data-backup.tar.gz

Image Information:
- Image: $(if ($keycloakImage) { $keycloakImage } else { "quay.io/keycloak/keycloak:latest (base image)" })
- Backup File: $(if ($keycloakImage) { "keycloak-image.tar" } else { "N/A - using base image" })

Keycloak Configuration:
- Port: 18080
- Realm: EzioHost
- Client ID: Ezio-Host-Client

Import Instructions:
1. Copy the entire '$OutputPath' folder to the target machine
2. Run: .\scripts\import-keycloak.ps1 -BackupPath '$OutputPath'
3. Or follow the detailed guide in docs/KEYCLOAK_EXPORT_IMPORT.md

"@
    Set-Content -Path $infoFile -Value $infoContent
    Write-Host "Info file created: $infoFile" -ForegroundColor Green

    Write-Host "`n=== Export Completed Successfully ===" -ForegroundColor Green
    Write-Host "Backup location: $outputDir" -ForegroundColor Cyan
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Copy the '$OutputPath' folder to the target machine" -ForegroundColor White
    Write-Host "2. Run the import script on the target machine" -ForegroundColor White
    Write-Host "3. See docs/KEYCLOAK_EXPORT_IMPORT.md for detailed instructions" -ForegroundColor White
}
finally {
    Pop-Location
}

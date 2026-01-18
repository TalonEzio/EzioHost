# Import Keycloak Docker Container và Data Volume
# Script này import Keycloak image và data volume từ backup để chạy trên máy mới

param(
    [string]$BackupPath = "keycloak-backup",
    [string]$VolumeName = "",
    [int]$Port = 18080,
    [switch]$StartContainer = $false
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
    Write-Host "`n=== Keycloak Import Script ===" -ForegroundColor Green
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

    # Check backup path
    $backupDir = if ([System.IO.Path]::IsPathRooted($BackupPath)) {
        $BackupPath
    } else {
        Join-Path $projectRoot $BackupPath
    }

    if (-not (Test-Path $backupDir)) {
        Write-Host "Error: Backup directory not found: $backupDir" -ForegroundColor Red
        Write-Host "Please ensure the backup folder exists or specify the correct path with -BackupPath parameter." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Backup directory: $backupDir" -ForegroundColor Cyan

    # Check for backup files
    $volumeBackupFile = Join-Path $backupDir "keycloak-data-backup.tar.gz"
    $imageBackupFile = Join-Path $backupDir "keycloak-image.tar"
    $infoFile = Join-Path $backupDir "keycloak-export-info.txt"

    if (-not (Test-Path $volumeBackupFile)) {
        Write-Host "Error: Volume backup file not found: $volumeBackupFile" -ForegroundColor Red
        exit 1
    }

    Write-Host "Found volume backup: $volumeBackupFile" -ForegroundColor Green

    # Read info file if exists
    if (Test-Path $infoFile) {
        Write-Host "`nReading export information..." -ForegroundColor Cyan
        Get-Content $infoFile | Write-Host
    }

    # Determine volume name
    if ([string]::IsNullOrWhiteSpace($VolumeName)) {
        # Try to get from info file
        if (Test-Path $infoFile) {
            $infoContent = Get-Content $infoFile -Raw
            if ($infoContent -match "Volume Name:\s*(.+)") {
                $originalVolumeName = $matches[1].Trim()
                $VolumeName = "keycloak-data-restored"
                Write-Host "Original volume name: $originalVolumeName" -ForegroundColor Gray
                Write-Host "Using new volume name: $VolumeName" -ForegroundColor Cyan
            } else {
                $VolumeName = "keycloak-data-restored"
            }
        } else {
            $VolumeName = "keycloak-data-restored"
        }
    }

    Write-Host "`nTarget volume name: $VolumeName" -ForegroundColor Cyan

    # Check if volume already exists
    $existingVolumes = docker volume ls --format "{{.Name}}" | Where-Object { $_ -eq $VolumeName }
    if ($null -ne $existingVolumes -and $existingVolumes.Count -gt 0) {
        Write-Host "Warning: Volume '$VolumeName' already exists." -ForegroundColor Yellow
        $overwrite = Read-Host "Remove and recreate? (y/N)"
        if ($overwrite -eq "y" -or $overwrite -eq "Y") {
            # Check if volume is in use
            $containersUsingVolume = docker ps -a --filter "volume=$VolumeName" --format "{{.Names}}"
            if ($null -ne $containersUsingVolume -and $containersUsingVolume.Count -gt 0) {
                Write-Host "Stopping containers using this volume..." -ForegroundColor Yellow
                foreach ($container in $containersUsingVolume) {
                    docker stop $container 2>$null
                    docker rm $container 2>$null
                }
            }
            docker volume rm $VolumeName 2>$null
            Write-Host "Volume removed." -ForegroundColor Green
        } else {
            Write-Host "Import cancelled." -ForegroundColor Red
            exit 0
        }
    }

    # Create new volume
    Write-Host "`nCreating new volume: $VolumeName" -ForegroundColor Cyan
    docker volume create $VolumeName
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to create volume." -ForegroundColor Red
        exit 1
    }
    Write-Host "Volume created successfully." -ForegroundColor Green

    # Import volume data
    Write-Host "`nImporting volume data..." -ForegroundColor Cyan
    Write-Host "This may take a few minutes depending on backup size..." -ForegroundColor Yellow
    
    # Extract the tar.gz file to the volume
    docker run --rm -v "${VolumeName}:/data" -v "${backupDir}:/backup" alpine sh -c "cd /data && tar xzf /backup/keycloak-data-backup.tar.gz"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to import volume data." -ForegroundColor Red
        Write-Host "Cleaning up volume..." -ForegroundColor Yellow
        docker volume rm $VolumeName 2>$null
        exit 1
    }
    Write-Host "Volume data imported successfully." -ForegroundColor Green

    # Import image (if exists)
    if (Test-Path $imageBackupFile) {
        Write-Host "`nImporting Keycloak image..." -ForegroundColor Cyan
        Write-Host "This may take a few minutes depending on image size..." -ForegroundColor Yellow
        docker load -i $imageBackupFile
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Warning: Failed to import image. You may need to pull the base image manually." -ForegroundColor Yellow
        } else {
            Write-Host "Image imported successfully." -ForegroundColor Green
        }
    } else {
        Write-Host "`nNo custom image backup found. Using base Keycloak image." -ForegroundColor Yellow
        Write-Host "If needed, pull the image: docker pull quay.io/keycloak/keycloak:latest" -ForegroundColor Gray
    }

    # Create run script
    Write-Host "`nCreating run script..." -ForegroundColor Cyan
    $runScript = Join-Path $backupDir "run-keycloak.ps1"
    $runScriptContent = @"
# Run Keycloak with imported volume
# This script starts Keycloak container with the restored data volume

`$volumeName = "$VolumeName"
`$port = $Port

Write-Host "Starting Keycloak container..." -ForegroundColor Green
Write-Host "Volume: `$volumeName" -ForegroundColor Cyan
Write-Host "Port: `$port" -ForegroundColor Cyan

docker run -d `
    --name keycloak-restored `
    -p `$port:8080 `
    -e KEYCLOAK_ADMIN=admin `
    -e KEYCLOAK_ADMIN_PASSWORD=admin `
    -v `$volumeName:/opt/keycloak/data `
    quay.io/keycloak/keycloak:latest `
    start-dev

if (`$LASTEXITCODE -eq 0) {
    Write-Host "`nKeycloak started successfully!" -ForegroundColor Green
    Write-Host "Access Keycloak at: http://localhost:`$port" -ForegroundColor Cyan
    Write-Host "Admin console: http://localhost:`$port/admin" -ForegroundColor Cyan
    Write-Host "`nTo stop: docker stop keycloak-restored" -ForegroundColor Yellow
    Write-Host "To remove: docker stop keycloak-restored && docker rm keycloak-restored" -ForegroundColor Yellow
} else {
    Write-Host "Failed to start Keycloak container." -ForegroundColor Red
}
"@
    Set-Content -Path $runScript -Value $runScriptContent
    Write-Host "Run script created: $runScript" -ForegroundColor Green

    Write-Host "`n=== Import Completed Successfully ===" -ForegroundColor Green
    Write-Host "Volume name: $VolumeName" -ForegroundColor Cyan
    Write-Host "Port: $Port" -ForegroundColor Cyan
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Run Keycloak using the generated script:" -ForegroundColor White
    Write-Host "   .\$runScript" -ForegroundColor Cyan
    Write-Host "`n2. Or run manually with Docker:" -ForegroundColor White
    Write-Host "   docker run -d --name keycloak-restored -p $Port`:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin -v ${VolumeName}:/opt/keycloak/data quay.io/keycloak/keycloak:latest start-dev" -ForegroundColor Cyan
    Write-Host "`n3. Or use Aspire - the volume will be automatically used if named correctly" -ForegroundColor White
    Write-Host "`n4. Update appsettings.json to point to the correct Keycloak URL" -ForegroundColor White
    Write-Host "`nSee docs/KEYCLOAK_EXPORT_IMPORT.md for detailed instructions." -ForegroundColor Gray

    # Optionally start container
    if ($StartContainer) {
        Write-Host "`nStarting Keycloak container..." -ForegroundColor Cyan
        & $runScript
    }
}
finally {
    Pop-Location
}

# Test Scripts Hướng Dẫn

## Cách chạy tests và generate coverage report

### Cách 1: Chạy từ thư mục scripts (đã được sửa, tự động tìm project root)
```powershell
cd scripts
.\run-tests.ps1
```

### Cách 2: Chạy từ thư mục root của project
```powershell
cd D:\Sources\CSharp\Project\EzioHost
.\scripts\run-tests.ps1
```

### Cách 3: Chạy trực tiếp từ bất kỳ đâu
```powershell
pwsh -ExecutionPolicy Bypass -File "D:\Sources\CSharp\Project\EzioHost\scripts\run-tests.ps1"
```

## Generate coverage report riêng

Nếu bạn đã chạy tests trước đó và chỉ muốn generate report:

```powershell
cd scripts
.\generate-coverage-report.ps1
```

Hoặc từ root:
```powershell
.\scripts\generate-coverage-report.ps1
```

## Xem coverage report

Sau khi chạy xong, mở file:
```
TestResults\coverage\index.html
```

## Export/Import Keycloak Docker Container

Scripts để export và import Keycloak container cùng data volume (chứa realm đã cấu hình) giữa các máy.

### Export Keycloak (Máy cũ)

```powershell
# Export Keycloak volume và image
.\scripts\export-keycloak.ps1

# Tự động dừng container trước khi export
.\scripts\export-keycloak.ps1 -StopContainer

# Chỉ định thư mục output
.\scripts\export-keycloak.ps1 -OutputPath "my-backup"
```

Script sẽ tạo thư mục backup chứa:
- `keycloak-data-backup.tar.gz` - Backup của data volume
- `keycloak-image.tar` - Backup của Docker image (nếu có)
- `keycloak-export-info.txt` - Thông tin export

### Import Keycloak (Máy mới)

```powershell
# Import từ backup mặc định
.\scripts\import-keycloak.ps1

# Chỉ định đường dẫn backup
.\scripts\import-keycloak.ps1 -BackupPath "keycloak-backup"

# Tự động start container sau khi import
.\scripts\import-keycloak.ps1 -StartContainer

# Chỉ định port khác
.\scripts\import-keycloak.ps1 -Port 18081
```

Xem hướng dẫn chi tiết tại: `docs/KEYCLOAK_EXPORT_IMPORT.md`

## Lưu ý

- Scripts tự động tìm project root (nơi có file .sln)
- Coverage data được lưu trong thư mục `TestResults`
- Nếu gặp lỗi về "XPlat Code Coverage", đảm bảo package `coverlet.msbuild` đã được cài trong test projects
- Keycloak export/import scripts yêu cầu Docker đang chạy
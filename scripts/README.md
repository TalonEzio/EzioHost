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

## Lưu ý

- Scripts tự động tìm project root (nơi có file .sln)
- Coverage data được lưu trong thư mục `TestResults`
- Nếu gặp lỗi về "XPlat Code Coverage", đảm bảo package `coverlet.msbuild` đã được cài trong test projects

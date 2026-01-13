# Code Coverage Levels

## Các mức độ Code Coverage

### C0 - Statement Coverage (Line Coverage)
- **Mức độ**: Cơ bản nhất
- **Đo lường**: Số lượng câu lệnh (statements/lines) được thực thi ít nhất 1 lần
- **Hiện tại**: ✅ 53.8% line coverage
- **Ví dụ**: 
  ```csharp
  if (x > 0) {
      DoSomething(); // Chỉ cần gọi 1 lần với x > 0 là đủ
  }
  ```

### C1 - Branch Coverage
- **Mức độ**: Cao hơn C0
- **Đo lường**: Tất cả các nhánh (branches) trong điều kiện phải được thực thi
- **Hiện tại**: ✅ 50.14% branch coverage
- **Ví dụ**:
  ```csharp
  if (x > 0) {
      DoSomething(); // Cần test cả x > 0 (true) và x <= 0 (false)
  }
  ```

### C2 - Path Coverage
- **Mức độ**: Cao nhất
- **Đo lường**: Tất cả các đường dẫn (paths) có thể xảy ra trong code
- **Hiện tại**: ❌ Chưa đo
- **Ví dụ**:
  ```csharp
  if (x > 0) {
      if (y > 0) {
          DoSomething(); // Cần test: (x>0,y>0), (x>0,y<=0), (x<=0,y>0), (x<=0,y<=0)
      }
  }
  ```

## Cấu hình hiện tại

Project đang sử dụng **Coverlet** với:
- ✅ **Line Coverage (C0)**: Đã bật
- ✅ **Branch Coverage (C1)**: Đã bật (50.14%)
- ✅ **Method Coverage**: Đã bật (61.24%)
- ❌ **Path Coverage (C2)**: Chưa hỗ trợ trực tiếp

## Cách cải thiện Coverage

### 1. Tăng Line Coverage (C0)
- Viết tests cho tất cả methods chưa được test
- Test các edge cases và error handling

### 2. Tăng Branch Coverage (C1)
- Test cả `true` và `false` cho mỗi điều kiện `if`
- Test tất cả các case trong `switch`
- Test cả `null` và `not null` cho nullable types

### 3. Path Coverage (C2) - Khuyến nghị
- Viết tests cho tất cả các combinations của điều kiện
- Sử dụng **Theory** tests với nhiều input combinations
- Sử dụng **Property-based testing** (như FsCheck, Hedgehog)

## Mục tiêu Coverage

- **Line Coverage**: ≥ 90% ✅ (hiện tại: 53.8%)
- **Branch Coverage**: ≥ 85% ✅ (hiện tại: 50.14%)
- **Method Coverage**: ≥ 90% ✅ (hiện tại: 61.24%)

## Tools hỗ trợ

- **Coverlet**: Đo C0 và C1
- **ReportGenerator**: Tạo HTML reports
- **AltCover**: Hỗ trợ MC/DC (Modified Condition/Decision Coverage)

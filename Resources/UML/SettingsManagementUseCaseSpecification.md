# Đặc tả Use Case: Quản lý cài đặt

## Thông tin cơ bản
- **Tên Use case**: Quản lý cài đặt
- **Tên Actor**: Administrator
- **Mức**: 1
- **Tiền điều kiện**: Đã đăng nhập vào hệ thống

## Mô tả
Use case này cho phép Administrator quản lý các cài đặt hệ thống, bao gồm cài đặt chất lượng encoding, cài đặt transcribe phụ đề, và cài đặt lưu trữ Cloudflare R2. Mỗi loại cài đặt được quản lý riêng biệt và áp dụng cho từng người dùng.

## Kích hoạt
Tác nhân yêu cầu chức năng quản lý cài đặt hệ thống.

## Luồng chính

### 1. Quản lý cài đặt Encoding

#### 1.1. Xem cài đặt Encoding

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Truy cập vào trang "Cài đặt" | 1.1. Hệ thống hiển thị danh sách các loại cài đặt |
| 2. Chọn "Chất lượng Encoding" | 2.1. Hệ thống kiểm tra cài đặt của người dùng |
| | 2.2. Nếu chưa có cài đặt, hệ thống tạo cài đặt mặc định (360p, 480p, 720p) |
| | 2.3. Hệ thống hiển thị danh sách cài đặt encoding (Resolution, Bitrate, IsEnabled) |

#### 1.2. Cập nhật cài đặt Encoding

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn "Chất lượng Encoding" | 1.1. Hệ thống hiển thị danh sách cài đặt hiện tại |
| 2. Điều chỉnh bitrate và bật/tắt resolution | 2.1. Hệ thống kiểm tra request không rỗng |
| | 2.2. Hệ thống kiểm tra ít nhất một resolution được bật |
| 3. Bấm "Lưu" | 3.1. Hệ thống cập nhật hoặc tạo mới EncodingQualitySetting cho từng resolution |
| | 3.2. Hệ thống cập nhật BitrateKbps và IsEnabled |
| | 3.3. Hệ thống cập nhật ModifiedBy và ModifiedAt |
| | 3.4. Hệ thống lưu vào database |
| | 3.5. Hệ thống trả về danh sách cài đặt đã cập nhật |

### 2. Quản lý cài đặt Transcribe

#### 2.1. Xem cài đặt Transcribe

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Truy cập vào trang "Cài đặt" | 1.1. Hệ thống hiển thị danh sách các loại cài đặt |
| 2. Chọn "Audio Transcribing" | 2.1. Hệ thống kiểm tra cài đặt của người dùng |
| | 2.2. Nếu chưa có cài đặt, hệ thống tạo cài đặt mặc định (IsEnabled=true, ModelType=Base, UseGpu=false) |
| | 2.3. Hệ thống hiển thị cài đặt transcribe (IsEnabled, ModelType, UseGpu, GpuDeviceId) |

#### 2.2. Cập nhật cài đặt Transcribe

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn "Audio Transcribing" | 1.1. Hệ thống hiển thị cài đặt hiện tại |
| 2. Điều chỉnh các tùy chọn (bật/tắt tính năng, chọn model, cấu hình GPU) | 2.1. Hệ thống kiểm tra request hợp lệ |
| 3. Bấm "Lưu" | 3.1. Hệ thống cập nhật hoặc tạo mới SubtitleTranscribeSetting |
| | 3.2. Hệ thống cập nhật IsEnabled, ModelType, UseGpu, GpuDeviceId |
| | 3.3. Hệ thống cập nhật ModifiedBy |
| | 3.4. Hệ thống lưu vào database |
| | 3.5. Hệ thống trả về cài đặt đã cập nhật |

### 3. Quản lý cài đặt Storage

#### 3.1. Xem cài đặt Storage

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Truy cập vào trang "Cài đặt" | 1.1. Hệ thống hiển thị danh sách các loại cài đặt |
| 2. Chọn "Cloudflare R2 Storage" | 2.1. Hệ thống kiểm tra cài đặt của người dùng |
| | 2.2. Nếu chưa có cài đặt, hệ thống tạo cài đặt mặc định (IsEnabled=true) |
| | 2.3. Hệ thống hiển thị cài đặt storage (IsEnabled) |

#### 3.2. Cập nhật cài đặt Storage

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn "Cloudflare R2 Storage" | 1.1. Hệ thống hiển thị cài đặt hiện tại |
| 2. Bật/tắt tính năng upload R2 | 2.1. Hệ thống kiểm tra request hợp lệ |
| 3. Bấm "Lưu" | 3.1. Hệ thống cập nhật hoặc tạo mới CloudflareStorageSetting |
| | 3.2. Hệ thống cập nhật IsEnabled |
| | 3.3. Hệ thống cập nhật ModifiedBy |
| | 3.4. Hệ thống lưu vào database |
| | 3.5. Hệ thống trả về cài đặt đã cập nhật |

## Luồng phụ

### Luồng phụ 1: Cài đặt mặc định
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Người dùng lần đầu truy cập cài đặt | 1.1. Hệ thống tự động tạo cài đặt mặc định cho người dùng |
| | 1.2. Hệ thống hiển thị cài đặt mặc định |

### Luồng phụ 2: Cập nhật Encoding - Không có resolution nào được bật
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Cập nhật cài đặt encoding với tất cả resolution bị tắt | 1.1. Hệ thống kiểm tra ít nhất một resolution phải được bật |
| | 1.2. Hệ thống trả về lỗi 400 Bad Request "At least one resolution must be enabled for encoding" |

## Ngoại lệ

### E1: Request không hợp lệ
- **Mô tả**: Request rỗng hoặc thiếu dữ liệu bắt buộc
- **Xử lý**: Hệ thống trả về lỗi 400 Bad Request

### E2: Encoding - Không có resolution nào được bật
- **Mô tả**: Tất cả resolution đều bị tắt khi cập nhật cài đặt encoding
- **Xử lý**: Hệ thống trả về lỗi 400 Bad Request "At least one resolution must be enabled for encoding"

### E3: User ID không tìm thấy
- **Mô tả**: UserId không tồn tại hoặc không hợp lệ
- **Xử lý**: Hệ thống trả về lỗi 401 Unauthorized "User ID not found"

### E4: Lỗi hệ thống
- **Mô tả**: Lỗi khi lưu database hoặc xử lý dữ liệu
- **Xử lý**: Hệ thống trả về lỗi 500 Internal Server Error

## Điều kiện sau (Post-conditions)

### Sau khi xem cài đặt
- Cài đặt được hiển thị cho người dùng
- Nếu chưa có cài đặt, cài đặt mặc định được tạo tự động

### Sau khi cập nhật cài đặt thành công
- Cài đặt được cập nhật hoặc tạo mới trong database
- Thông tin cài đặt được trả về cho người dùng
- Cài đặt mới sẽ được áp dụng cho các video xử lý sau này

## Yêu cầu đặc biệt

### SR1: Cài đặt Encoding
- Mỗi người dùng có thể cấu hình bitrate và bật/tắt cho từng resolution
- Bitrate mặc định cho các resolution:
  - 144p: 400 kbps
  - 240p: 600 kbps
  - 360p: 800 kbps
  - 480p: 1400 kbps
  - 720p: 2800 kbps
  - 960p: 4000 kbps
  - 1080p: 5000 kbps
  - 1440p: 8000 kbps
  - 1920p: 8000 kbps
  - 2160p: 15000 kbps
- Cài đặt mặc định khi tạo mới: 360p, 480p, 720p được bật
- Phải có ít nhất một resolution được bật

### SR2: Cài đặt Transcribe
- Cài đặt mặc định: IsEnabled=true, ModelType=Base, UseGpu=false
- ModelType có thể là: Tiny, Base, Small, Medium, Large
- UseGpu: Bật/tắt sử dụng GPU cho Whisper AI
- GpuDeviceId: ID của GPU device (nếu UseGpu=true)

### SR3: Cài đặt Storage
- Cài đặt mặc định: IsEnabled=true
- IsEnabled: Bật/tắt tính năng upload video lên Cloudflare R2 sau khi xử lý
- Khi IsEnabled=false, video chỉ được lưu local

### SR4: Tự động tạo cài đặt mặc định
- Khi người dùng lần đầu truy cập cài đặt, hệ thống tự động tạo cài đặt mặc định nếu chưa có
- Mỗi loại cài đặt được tạo riêng biệt

### SR5: Cài đặt theo người dùng
- Mỗi người dùng có cài đặt riêng, không ảnh hưởng đến người dùng khác
- Cài đặt được lưu với UserId tương ứng

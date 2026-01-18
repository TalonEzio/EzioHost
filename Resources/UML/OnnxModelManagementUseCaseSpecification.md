# Đặc tả Use Case: Quản lý mô hình AI

## Thông tin cơ bản
- **Tên Use case**: Quản lý mô hình AI
- **Tên Actor**: Administrator
- **Mức**: 1
- **Tiền điều kiện**: Đã đăng nhập vào hệ thống

## Mô tả
Use case này cho phép Administrator quản lý các mô hình AI (ONNX models) trong hệ thống, bao gồm xem danh sách, thêm mới, xóa, phân tích metadata và demo upscale để kiểm tra chất lượng mô hình trước khi sử dụng.

## Kích hoạt
Tác nhân yêu cầu chức năng quản lý mô hình AI.

## Luồng chính

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Truy cập vào trang quản lý mô hình AI | 1.1. Hệ thống hiển thị danh sách các mô hình ONNX |
| 2. Chọn chức năng cần thực hiện | 2.1. Hệ thống hiển thị form tương ứng |

### Luồng chính 1: Xem danh sách mô hình
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Truy cập trang quản lý mô hình | 1.1. Hệ thống lấy danh sách tất cả mô hình ONNX |
| 2. Chọn lọc theo có demo (tùy chọn) | 2.1. Hệ thống lọc chỉ hiển thị mô hình có DemoInput và DemoOutput |
| | 2.2. Hệ thống hiển thị danh sách mô hình với thông tin: tên, scale, element type, input size |

### Luồng chính 2: Thêm mô hình mới
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn "Thêm mô hình mới" | 1.1. Hệ thống hiển thị form upload |
| 2. Nhập tên mô hình, scale, element type, input size | 2.1. Hệ thống kiểm tra dữ liệu đầu vào |
| 3. Chọn file ONNX và upload | 3.1. Hệ thống kiểm tra file hợp lệ |
| | 3.2. Hệ thống lưu file ONNX vào thư mục mô hình |
| | 3.3. Hệ thống tạo OnnxModel entity với thông tin đã nhập |
| | 3.4. Hệ thống lưu vào database |
| | 3.5. Hệ thống hiển thị thông báo thành công |

### Luồng chính 3: Phân tích mô hình
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn "Phân tích mô hình" | 1.1. Hệ thống hiển thị form upload file ONNX |
| 2. Upload file ONNX cần phân tích | 2.1. Hệ thống lưu file tạm thời |
| | 2.2. Hệ thống đọc metadata từ file ONNX (ONNX Runtime) |
| | 2.3. Hệ thống phân tích input shape để xác định MustInputWidth và MustInputHeight |
| | 2.4. Hệ thống phân tích output shape để tính toán Scale |
| | 2.5. Hệ thống xác định ElementType từ input metadata |
| | 2.6. Hệ thống trả về metadata (Scale, MustInputWidth, MustInputHeight, ElementType) |
| | 2.7. Hệ thống xóa file tạm thời |

### Luồng chính 4: Demo upscale
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn mô hình cần test | 1.1. Hệ thống kiểm tra mô hình tồn tại |
| 2. Chọn "Demo" | 2.1. Hệ thống hiển thị form upload ảnh |
| 3. Upload ảnh để test | 3.1. Hệ thống lưu ảnh vào thư mục tạm |
| 4. Bấm "Demo" | 4.1. Hệ thống upscale ảnh bằng mô hình AI |
| | 4.2. Hệ thống lưu ảnh kết quả |
| | 4.3. Hệ thống cập nhật DemoInput và DemoOutput cho mô hình |
| | 4.4. Hệ thống tính toán thời gian xử lý |
| | 4.5. Hệ thống trả về kết quả với ảnh trước và sau upscale |
| | 4.6. Hệ thống hiển thị kết quả để so sánh |

### Luồng chính 5: Xóa mô hình
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn mô hình cần xóa | 1.1. Hệ thống kiểm tra mô hình tồn tại |
| 2. Bấm "Xóa" | 2.1. Hệ thống xóa OnnxModel entity khỏi database |
| | 2.2. Hệ thống xóa file ONNX trên đĩa |
| | 2.3. Hệ thống hiển thị thông báo xóa thành công |
| | 2.4. Hệ thống cập nhật danh sách mô hình |

## Luồng phụ

### Luồng phụ 1: Reset demo
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn mô hình đã có demo | 1.1. Hệ thống kiểm tra quyền sở hữu mô hình |
| 2. Chọn "Reset demo" | 2.1. Nếu không phải chủ sở hữu, hệ thống từ chối |
| | 2.2. Nếu là chủ sở hữu, hệ thống xóa DemoInput và DemoOutput |
| | 2.3. Hệ thống cập nhật mô hình |
| | 2.4. Hệ thống hiển thị thông báo thành công |

## Ngoại lệ

### Ngoại lệ 1: Mô hình không tồn tại
- **Điều kiện**: Tác nhân thao tác với modelId không tồn tại
- **Phản ứng**: Hệ thống hiển thị thông báo "404 Not Found"

### Ngoại lệ 2: File ONNX không hợp lệ
- **Điều kiện**: File upload không phải định dạng ONNX hoặc file bị hỏng
- **Phản ứng**: Hệ thống hiển thị thông báo "File Error" hoặc "Không thể đọc thông tin model"

### Ngoại lệ 3: Không có quyền reset demo
- **Điều kiện**: Tác nhân cố gắng reset demo của mô hình không phải do mình tạo
- **Phản ứng**: Hệ thống hiển thị thông báo "401 Unauthorized"

### Ngoại lệ 4: Lỗi khi upscale ảnh demo
- **Điều kiện**: Mô hình AI không thể upscale ảnh (lỗi model, lỗi input size, v.v.)
- **Phản ứng**: Hệ thống hiển thị thông báo lỗi và không cập nhật demo

### Ngoại lệ 5: File ảnh demo không hợp lệ
- **Điều kiện**: File ảnh upload không phải định dạng được hỗ trợ
- **Phản ứng**: Hệ thống hiển thị thông báo "Định dạng file không được hỗ trợ"

### Ngoại lệ 6: Lỗi khi phân tích mô hình
- **Điều kiện**: ONNX Runtime không thể đọc metadata từ file ONNX
- **Phản ứng**: Hệ thống trả về metadata với ErrorMessage mô tả lỗi

### Ngoại lệ 7: Lỗi hệ thống
- **Điều kiện**: Xảy ra lỗi không mong đợi (hết dung lượng đĩa, lỗi database, v.v.)
- **Phản ứng**: Hệ thống hiển thị thông báo "Đã xảy ra lỗi hệ thống" và ghi log lỗi

## Hậu điều kiện
- OnnxModel entity đã được tạo trong database (nếu thêm mô hình thành công)
- File ONNX đã được lưu vào thư mục mô hình (nếu thêm mô hình thành công)
- OnnxModel entity đã được xóa khỏi database và file đã được xóa (nếu xóa mô hình thành công)
- DemoInput và DemoOutput đã được cập nhật (nếu demo thành công)
- Metadata đã được trả về (nếu phân tích thành công)

## Yêu cầu đặc biệt
- Hệ thống phải hỗ trợ upload file ONNX với kích thước lớn
- Hệ thống phải sử dụng ONNX Runtime để đọc và phân tích metadata từ file ONNX
- Hệ thống phải tự động tính toán Scale từ input/output dimensions
- Hệ thống phải hỗ trợ nhiều ElementType khác nhau (Float, UInt8, Int8, v.v.)
- Hệ thống phải hỗ trợ demo upscale với ảnh để test mô hình trước khi sử dụng
- Hệ thống phải kiểm tra quyền sở hữu khi reset demo
- Hệ thống phải hỗ trợ lọc danh sách mô hình theo có demo hay không
- Hệ thống phải xóa file ONNX khi xóa mô hình để tránh lãng phí dung lượng
- Hệ thống phải xử lý lỗi khi phân tích mô hình và trả về thông báo lỗi rõ ràng

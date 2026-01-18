# Đặc tả Use Case: Upscale video

## Thông tin cơ bản
- **Tên Use case**: Upscale video
- **Tên Actor**: Administrator
- **Mức**: 1
- **Tiền điều kiện**: Đã đăng nhập vào hệ thống, video đã được upload và có Status = Ready

## Mô tả
Use case này cho phép Administrator nâng cấp chất lượng video bằng AI thông qua việc chọn video và mô hình AI (ONNX model). Hệ thống sẽ xử lý video trong nền bằng cách trích xuất từng frame, upscale từng frame bằng AI, sau đó ghép lại thành video với độ phân giải cao hơn.

## Kích hoạt
Tác nhân yêu cầu chức năng upscale video.

## Luồng chính

| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Truy cập vào trang upscale video | 1.1. Hệ thống hiển thị danh sách video sẵn sàng và danh sách mô hình AI |
| 2. Chọn video cần upscale | 2.1. Hệ thống kiểm tra video tồn tại và Status = Ready |
| 3. Chọn mô hình AI (ONNX model) | 3.1. Hệ thống kiểm tra mô hình tồn tại |
| 4. Bấm "Upscale" | 4.1. Hệ thống tạo VideoUpscale entity với Status = Queue |
| | 4.2. Hệ thống lưu thông tin: Video, Model, Scale, CreatedBy |
| | 4.3. Hệ thống trả về thông báo yêu cầu đã được tạo |
| | 4.4. Hệ thống bắt đầu xử lý nền (background job) |
| | 4.5. Hệ thống trích xuất frames và audio từ video gốc (FFmpeg) |
| | 4.6. Hệ thống upscale từng frame bằng AI model |
| | 4.7. Hệ thống ghép các frame đã upscale lại thành video |
| | 4.8. Hệ thống tạo HLS stream cho video đã upscale |
| | 4.9. Hệ thống cập nhật Status = Ready |
| | 4.10. Hệ thống gửi thông báo hoàn thành qua SignalR |

## Luồng phụ

### Luồng phụ 1: Demo upscale model
| Hành động tác nhân | Phản ứng hệ thống |
|-------------------|------------------|
| 1. Chọn mô hình AI cần test | 1.1. Hệ thống hiển thị form upload ảnh demo |
| 2. Upload ảnh để test | 2.1. Hệ thống lưu ảnh vào thư mục tạm |
| 3. Bấm "Demo" | 3.1. Hệ thống upscale ảnh bằng mô hình AI |
| | 3.2. Hệ thống lưu ảnh kết quả |
| | 3.3. Hệ thống cập nhật DemoInput và DemoOutput cho model |
| | 3.4. Hệ thống trả về kết quả và thời gian xử lý |
| | 3.5. Hệ thống hiển thị ảnh trước và sau upscale để so sánh |

## Ngoại lệ

### Ngoại lệ 1: Video không tồn tại
- **Điều kiện**: Tác nhân chọn video với videoId không tồn tại
- **Phản ứng**: Hệ thống hiển thị thông báo "404 Not Found"

### Ngoại lệ 2: Mô hình AI không tồn tại
- **Điều kiện**: Tác nhân chọn modelId không tồn tại
- **Phản ứng**: Hệ thống hiển thị thông báo "404 Not Found"

### Ngoại lệ 3: Video chưa sẵn sàng
- **Điều kiện**: Video có Status != Ready (đang xử lý hoặc lỗi)
- **Phản ứng**: Hệ thống hiển thị thông báo "400 Bad Request - Video chưa sẵn sàng"

### Ngoại lệ 4: Lỗi khi trích xuất frames
- **Điều kiện**: FFmpeg không thể trích xuất frames từ video
- **Phản ứng**: Hệ thống cập nhật Status = Failed, ghi log lỗi và thông báo lỗi

### Ngoại lệ 5: Lỗi khi upscale frame
- **Điều kiện**: AI model không thể upscale frame (lỗi model, lỗi memory, v.v.)
- **Phản ứng**: Hệ thống cập nhật Status = Failed, ghi log lỗi và thông báo lỗi

### Ngoại lệ 6: Lỗi khi ghép video
- **Điều kiện**: FFmpeg không thể ghép các frame đã upscale thành video
- **Phản ứng**: Hệ thống cập nhật Status = Failed, ghi log lỗi và thông báo lỗi

### Ngoại lệ 7: Lỗi hệ thống
- **Điều kiện**: Xảy ra lỗi không mong đợi trong quá trình xử lý (hết dung lượng đĩa, lỗi database, v.v.)
- **Phản ứng**: Hệ thống cập nhật Status = Failed, ghi log lỗi và thông báo "Đã xảy ra lỗi hệ thống"

### Ngoại lệ 8: File ảnh demo không hợp lệ
- **Điều kiện**: File ảnh upload cho demo không phải định dạng được hỗ trợ
- **Phản ứng**: Hệ thống hiển thị thông báo "Định dạng file không được hỗ trợ"

## Hậu điều kiện
- VideoUpscale entity đã được tạo với Status = Queue (nếu tạo yêu cầu thành công)
- Video đã được upscale và lưu vào thư mục video (nếu xử lý thành công)
- HLS stream đã được tạo cho video upscale (nếu xử lý thành công)
- Status đã được cập nhật = Ready (nếu thành công) hoặc Failed (nếu lỗi)
- Thông báo hoàn thành đã được gửi qua SignalR (nếu thành công)

## Yêu cầu đặc biệt
- Hệ thống phải xử lý upscale video trong nền (background job) để không block request
- Hệ thống phải hỗ trợ xử lý nhiều video upscale đồng thời
- Hệ thống phải sử dụng FFmpeg để trích xuất frames và ghép video
- Hệ thống phải sử dụng ONNX Runtime để chạy AI model
- Hệ thống phải tạo HLS stream cho video đã upscale để hỗ trợ streaming
- Hệ thống phải gửi thông báo real-time qua SignalR khi hoàn thành
- Hệ thống phải hỗ trợ demo upscale với ảnh để test model trước khi áp dụng cho video
- Hệ thống phải xử lý lỗi và cập nhật status phù hợp
- Hệ thống phải hỗ trợ nhiều scale khác nhau (2x, 4x, v.v.) tùy theo model

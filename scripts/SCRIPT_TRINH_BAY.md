# Script Trình Bày Bảo Vệ Đồ Án EzioHost (5-10 phút)

## HƯỚNG DẪN SỬ DỤNG
- Đọc kỹ script trước khi trình bày
- Tập nói trước để quen với timing
- Chuẩn bị demo sẵn, đảm bảo hệ thống chạy ổn
- Đánh dấu các phần quan trọng cần nhấn mạnh

---

## PHẦN 1: GIỚI THIỆU (1 phút)

**[Chào hội đồng]**

"Kính chào thầy cô và các bạn. Em tên là [TÊN], hôm nay em xin được trình bày đồ án tốt nghiệp của em."

**[Slide 1: Tên đề tài]**

"Đề tài của em là: **Hệ thống quản lý và phát video trực tuyến với mã hóa HLS, bảo vệ DRM và nâng cấp chất lượng bằng AI**."

**[Slide 2: Mục tiêu]**

"Mục tiêu của đồ án là xây dựng một nền tảng video hosting đầy đủ tính năng, có khả năng:
- Upload và lưu trữ video
- Tự động mã hóa video thành định dạng HLS để phát trực tuyến
- Bảo vệ video bằng công nghệ DRM
- Nâng cấp chất lượng video tự động bằng AI
- Tạo phụ đề tự động từ audio"

**[Slide 3: Công nghệ sử dụng]**

"Đồ án được xây dựng bằng các công nghệ:
- **Backend**: ASP.NET Core WebAPI với C#
- **Frontend**: Blazor WebAssembly
- **Xử lý video**: FFmpeg
- **AI**: ONNX Runtime cho upscaling, Whisper AI cho transcription
- **Database**: SQL Server với Entity Framework Core
- **Real-time**: SignalR cho thông báo real-time"

---

## PHẦN 2: KIẾN TRÚC HỆ THỐNG (1-2 phút)

**[Slide 4: Kiến trúc tổng quan]**

"Hệ thống được thiết kế theo kiến trúc 3 lớp:

**Lớp Presentation**: Gồm Blazor WebApp làm giao diện người dùng và WebAPI Controllers để xử lý các request.

**Lớp Business Logic**: Chứa các Services và Repositories, xử lý toàn bộ logic nghiệp vụ như encoding video, upscaling, transcription.

**Lớp Data Access**: Sử dụng Entity Framework Core để tương tác với SQL Server."

**[Slide 5: Luồng xử lý video]**

"Luồng xử lý video trong hệ thống như sau:

1. Người dùng upload video qua giao diện web, video được chia nhỏ thành các chunks để upload
2. Sau khi upload xong, hệ thống tự động tạo một background job để xử lý encoding
3. FFmpeg sẽ mã hóa video thành các HLS streams với nhiều độ phân giải khác nhau
4. Khi encoding hoàn thành, hệ thống gửi thông báo real-time qua SignalR đến client"

---

## PHẦN 3: TÍNH NĂNG CHÍNH VÀ DEMO (3-5 phút)

### 3.1. Upload & Encoding HLS (1 phút)

**[Slide 6: Upload Video]**

"Tính năng đầu tiên là Upload và Encoding HLS."

**[BẮT ĐẦU DEMO]**

"Em sẽ demo tính năng này. Đầu tiên, em sẽ upload một video mẫu."

*[Thực hiện upload video trên giao diện]*

"Video được upload theo cơ chế chunked upload, tức là chia nhỏ thành các phần để upload, giúp hỗ trợ file lớn và có thể resume nếu bị gián đoạn."

*[Chỉ vào progress bar]*

"Ở đây các thầy cô có thể thấy tiến trình upload real-time."

*[Chờ encoding bắt đầu]*

"Sau khi upload xong, hệ thống tự động bắt đầu encoding. Em có thể thấy thông báo real-time qua SignalR ở đây."

*[Chỉ vào notification hoặc progress]*

"FFmpeg đang tạo các HLS streams với nhiều độ phân giải: 480p, 720p, 1080p, và có thể lên đến 4K tùy vào video gốc."

*[Mở thư mục hoặc database để show các file .m3u8 và .ts]*

"Đây là các file HLS đã được tạo. Mỗi stream có một key và IV riêng để mã hóa AES-128, đảm bảo bảo mật."

**[KẾT THÚC DEMO]**

"Tính năng này giúp video có thể phát trực tuyến với adaptive bitrate, tự động chọn độ phân giải phù hợp với băng thông của người dùng."

---

### 3.2. AI Video Upscaling (1 phút)

**[Slide 7: AI Upscaling]**

"Tính năng thứ hai là nâng cấp chất lượng video bằng AI."

**[BẮT ĐẦU DEMO]**

"Em sẽ demo tính năng upscale video. Đầu tiên, em chọn một video đã được upload."

*[Chọn video trên giao diện]*

"Sau đó em chọn một ONNX model để upscale. Hệ thống hỗ trợ nhiều model từ OpenModelDB, mỗi model có hệ số scale khác nhau như 2x, 4x."

*[Chọn model và bắt đầu upscale]*

"Khi bắt đầu upscale, hệ thống sẽ:
1. Extract các frames từ video bằng FFmpeg
2. Upscale từng frame bằng ONNX Runtime - có thể sử dụng GPU nếu có CUDA
3. Ghép các frames đã upscale lại thành video
4. Encode video mới với HLS"

*[Chỉ vào progress hoặc log]*

"Ở đây có thể thấy quá trình upscale đang diễn ra. Hệ thống sử dụng ONNX Runtime với hỗ trợ GPU để tăng tốc xử lý."

*[Nếu có video so sánh, show]*

"Đây là kết quả so sánh trước và sau khi upscale. Các thầy cô có thể thấy chất lượng được cải thiện đáng kể, đặc biệt là với nội dung anime."

**[KẾT THÚC DEMO]**

"Tính năng này sử dụng các mô hình AI được huấn luyện sẵn, giúp nâng cấp chất lượng video một cách tự động mà không cần can thiệp thủ công."

---

### 3.3. Subtitle Transcription (30 giây)

**[Slide 8: Subtitle Transcription]**

"Tính năng thứ ba là tạo phụ đề tự động."

**[BẮT ĐẦU DEMO]**

"Em sẽ demo tính năng tạo phụ đề tự động cho video."

*[Chọn video và bắt đầu transcription]*

"Hệ thống sử dụng Whisper AI để transcribe audio. Quy trình như sau:
1. Extract audio từ video bằng FFmpeg, chuyển sang định dạng WAV 16kHz mono
2. Sử dụng Whisper model để nhận diện giọng nói và chuyển thành text
3. Tạo file phụ đề định dạng VTT"

*[Chờ transcription hoàn thành hoặc show kết quả]*

"Đây là kết quả phụ đề đã được tạo. Hệ thống hỗ trợ nhiều ngôn ngữ và có thể tự động nhận diện ngôn ngữ."

**[KẾT THÚC DEMO]**

"Tính năng này giúp tự động tạo phụ đề cho video, tiết kiệm thời gian và công sức so với việc làm thủ công."

---

### 3.4. Real-time Updates (30 giây)

**[Slide 9: Real-time Updates]**

"Tính năng cuối cùng là cập nhật real-time."

**[BẮT ĐẦU DEMO]**

"Hệ thống sử dụng SignalR để gửi thông báo real-time đến client."

*[Show notification hoặc console log]*

"Khi encoding hoàn thành, hệ thống tự động gửi thông báo. Tương tự với upscale và transcription."

*[Có thể show code hoặc architecture diagram]*

"Các background jobs sử dụng Quartz.NET để xử lý các tác vụ nặng như encoding, upscale. Khi hoàn thành, jobs gửi event qua SignalR Hub, và Hub sẽ broadcast đến client tương ứng."

**[KẾT THÚC DEMO]**

"Tính năng này giúp người dùng biết được trạng thái xử lý video ngay lập tức mà không cần refresh trang."

---

## PHẦN 4: KẾT QUẢ VÀ KẾT LUẬN (1 phút)

**[Slide 10: Kết quả đạt được]**

"Về kết quả đạt được, em đã hoàn thành các tính năng chính:

✅ Hệ thống upload video với chunked upload, hỗ trợ file lớn
✅ Mã hóa HLS đa độ phân giải với adaptive bitrate
✅ Bảo vệ video bằng DRM với AES-128 encryption
✅ Nâng cấp chất lượng video tự động bằng AI với ONNX Runtime
✅ Tạo phụ đề tự động bằng Whisper AI
✅ Cập nhật real-time qua SignalR
✅ Xử lý background jobs với Quartz.NET
✅ Tích hợp CloudFlare R2 để lưu trữ video"

**[Slide 11: Điểm mạnh]**

"Điểm mạnh của hệ thống:
- Kiến trúc phân lớp rõ ràng, dễ bảo trì và mở rộng
- Hỗ trợ GPU acceleration cho encoding và AI processing
- Real-time notifications giúp cải thiện trải nghiệm người dùng
- Có thể scale horizontal bằng cách thêm nhiều worker nodes"

**[Slide 12: Hướng phát triển]**

"Về hướng phát triển trong tương lai:
- Tăng code coverage lên trên 80%
- Thêm Redis caching để cải thiện performance
- Xây dựng hệ thống monitoring và logging tập trung
- Cân nhắc chuyển sang microservices architecture khi scale lớn"

**[Slide 13: Kết luận]**

"Tóm lại, đồ án đã xây dựng thành công một hệ thống video hosting đầy đủ tính năng, tích hợp các công nghệ hiện đại như AI và real-time communication. Hệ thống có thể được áp dụng trong thực tế cho các nền tảng streaming video."

"Em xin cảm ơn thầy cô và các bạn đã lắng nghe. Em sẵn sàng trả lời các câu hỏi."

---

## PHẦN 5: CHUẨN BỊ TRẢ LỜI CÂU HỎI

### Câu hỏi về kiến trúc

**Q: Tại sao chọn kiến trúc 3 lớp thay vì microservices?**

A: "Em chọn kiến trúc 3 lớp vì phù hợp với quy mô dự án hiện tại, dễ phát triển và bảo trì. Tuy nhiên, hệ thống đã được thiết kế để có thể chuyển sang microservices trong tương lai bằng cách tách các services thành các microservices độc lập."

**Q: Làm thế nào xử lý nhiều video đồng thời?**

A: "Hệ thống sử dụng Quartz.NET với queue system. Mỗi job chỉ xử lý một video tại một thời điểm để tránh quá tải. Khi cần scale, có thể thêm nhiều worker nodes, mỗi node chạy một instance của background job processor."

### Câu hỏi về DRM

**Q: DRM có an toàn không? Có thể bypass không?**

A: "Hệ thống sử dụng AES-128 encryption cho từng HLS segment. Key và IV được lưu trong database, chỉ truy cập qua API endpoint có authentication. Đây là mức bảo vệ cơ bản, phù hợp cho hầu hết các trường hợp sử dụng. Để tăng cường bảo mật hơn, có thể tích hợp với các DRM solution chuyên nghiệp như Widevine hoặc FairPlay."

### Câu hỏi về AI

**Q: Tại sao chọn ONNX Runtime?**

A: "ONNX Runtime là một framework tối ưu để chạy các AI models, hỗ trợ nhiều format model khác nhau, có thể tận dụng GPU với CUDA, và có performance tốt. Ngoài ra, có nhiều pre-trained models sẵn có trên OpenModelDB mà em có thể sử dụng trực tiếp."

**Q: Hiệu năng upscale như thế nào?**

A: "Hiệu năng phụ thuộc vào model và phần cứng. Với GPU NVIDIA và model 2x scale, upscale một frame mất khoảng 100-200ms. Với video 30fps, upscale toàn bộ video có thể mất vài phút đến vài giờ tùy độ dài. Hệ thống đã tối ưu bằng cách cache inference sessions và xử lý batch khi có thể."

### Câu hỏi về performance

**Q: Hệ thống có thể xử lý bao nhiêu video cùng lúc?**

A: "Hiện tại, mỗi worker node chỉ xử lý một video tại một thời điểm để đảm bảo chất lượng. Với nhiều worker nodes, có thể xử lý song song nhiều video. Upload và streaming được xử lý bất đồng bộ, không bị block bởi encoding jobs."

**Q: Có sử dụng caching không?**

A: "Hiện tại chưa có caching layer, nhưng đây là một trong những hướng phát triển. Có thể thêm Redis để cache các metadata, API responses, và inference sessions để cải thiện performance."

### Câu hỏi về scaling

**Q: Hệ thống có thể scale như thế nào?**

A: "Hệ thống có thể scale theo nhiều cách:
- Horizontal scaling: Thêm nhiều WebAPI instances và worker nodes
- Database scaling: Sử dụng read replicas cho SQL Server
- Storage scaling: CloudFlare R2 đã hỗ trợ CDN và auto-scaling
- Load balancing: Có thể thêm reverse proxy như Nginx hoặc Azure Load Balancer"

---

## CHECKLIST TRƯỚC KHI TRÌNH BÀY

- [ ] Đã đọc kỹ script và tập nói trước
- [ ] Đã chuẩn bị demo sẵn, đảm bảo hệ thống chạy ổn
- [ ] Đã có video mẫu để demo
- [ ] Đã test các tính năng: upload, encoding, upscale, transcription
- [ ] Đã chuẩn bị slides (7-8 slides)
- [ ] Đã kiểm tra kết nối internet (nếu cần)
- [ ] Đã chuẩn bị backup plan nếu demo bị lỗi
- [ ] Đã đọc lại code để trả lời câu hỏi về implementation

---

## LƯU Ý QUAN TRỌNG

1. **Timing**: Giữ đúng thời gian, không nói quá nhanh hoặc quá chậm
2. **Demo**: Nếu demo bị lỗi, bình tĩnh giải thích và tiếp tục với phần khác
3. **Tự tin**: Nói rõ ràng, tự tin, nhìn vào hội đồng
4. **Tập trung**: Nhấn mạnh các điểm nổi bật: AI, DRM, real-time
5. **Linh hoạt**: Sẵn sàng điều chỉnh nếu hội đồng muốn xem chi tiết hơn

---

**Chúc bạn trình bày thành công! 🎉**

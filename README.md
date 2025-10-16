# Video server EzioHost

EzioHost là một nền tảng video dựa trên Blazor, hỗ trợ tải lên video, mã hóa, bảo vệ (DRM) và nâng cấp chất lượng video bằng AI. Tài liệu này mô tả các luồng chính: mã hóa video, bảo vệ video bằng khóa, và nâng cấp video bằng AI.
## Demo output

Bạn có thể thử nghiệm các file mẫu/tài nguyên có sẵn trong thư mục `Resources`. Xem trực tiếp hoặc thao tác bằng cách nhấn vào từng file:

- **frame.jpg**: Ảnh khung hình gốc dùng thử xử lý/nâng cấp.<br>
  <img src="Resources/frame.jpg" alt="frame.jpg" width="320"/>

- **frame_upscaled.jpg**: Ảnh khung hình đã được nâng cấp bằng AI (tham khảo kết quả).<br>
  <img src="Resources/frame_upscaled.jpg" alt="frame_upscaled.jpg" width="320"/>

- **frame_compare.png**: Ảnh so sánh trước/sau nâng cấp để quan sát chất lượng.<br>
  <img src="Resources/frame_compare.png" alt="frame_compare.png" width="320"/>

- **demo_480p.mp4**: Video mẫu 480p để upload, mã hóa HLS và nâng cấp bằng AI.  
https://github.com/TalonEzio/EzioHost/blob/master/Resources/demo_480p.mp4

- **demo_480p_upscaled.mp4**: Video mẫu đã được nâng cấp từ 480p (tham khảo kết quả).  
https://github.com/TalonEzio/EzioHost/blob/master/Resources/demo_480p_upscaled.mp4

> **Lưu ý**: Các file này chỉ nên dùng cho mục đích kiểm thử. Có thể dùng chúng để upload thử nghiệm qua giao diện web hoặc các API liên quan.

## Tóm tắt nhanh

- **Mục tiêu**: Máy chủ video hỗ trợ tải lên, mã hóa HLS, DRM và nâng cấp AI.
- **Backend**: ASP.NET Core Web API + EF Core (SQL Server), SignalR.
- **Frontend**: Blazor (xem thư mục `Front-end`).
- **Media**: FFmpeg (mã hóa HLS), ONNX Runtime + OpenCV (nâng cấp khung hình).
- **CSDL**: SQL Server; script khởi tạo ở `Resources\db.sql`.

---

## Cài đặt & Chạy

### Hiện tại project đã được cấu hình sẵn kết nối tới database, OIDC, có thể bỏ qua các bước liên quan trong các file appsettings.json

### 1) Yêu cầu

- Windows 10/11, PowerShell
- .NET SDK 9.0+, 10.0+
- SQL Server (Developer/Express hoặc container)
- FFmpeg cài đặt và có trong PATH

Kiểm tra nhanh:

```powershell
dotnet --version
ffmpeg -version
```

### 2) Khởi tạo cơ sở dữ liệu (Resources\\db.sql)

#### Hiện tại database đã được cấu hình sẵn kết nối tới server, có thể bỏ qua bước này
1. Tạo database rỗng, ví dụ: `EzioHostDb`.
2. Chạy script `Resources\db.sql` bằng SSMS hoặc `sqlcmd`:

```powershell
sqlcmd -S . -d EzioHostDb -E -i .\Resources\db.sql
```

3. Cập nhật chuỗi kết nối trong `Back-end\EzioHost.WebAPI\appsettings.Development.json` (ví dụ):

```json
{
  "ConnectionStrings": {
    "EzioHostDb": "Server=localhost;Database=EzioHostDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Lưu ý: Có thể dùng EF Migrations thay cho script nếu đã cấu hình.

### 3) Khôi phục & Build solution

```powershell
cd D:\Sources\CSharp\Project\EzioHost
dotnet restore EzioHost.sln
dotnet build EzioHost.sln -c Debug
```

### 4) Chạy bằng Aspire (khuyến nghị)

- Mở solution trong Visual Studio.
- Chọn dự án khởi động là `Manager\EzioHost.Aspire\EzioHost.Aspire.AppHost`.
- Cấu hình lại cho đúng endpoint trong các file `appsettings.Development.json`.
- Nhấn Run/Debug để Aspire tự orchestrate các thành phần (WebAPI, ReverseProxy, v.v.).
- Đảm bảo biến môi trường/connection string đã đúng trước khi chạy.

Chạy bằng dòng lệnh (tuỳ chọn):

```powershell
cd .\Manager\EzioHost.Aspire\EzioHost.Aspire.AppHost
dotnet run
```

### 5) Chạy đồng thời ReverseProxy, WebAPI, WebApp (Visual Studio)

Nếu không dùng Aspire, bạn có thể chạy nhiều project cùng lúc theo thứ tự sau (rất quan trọng):

1. `Back-end\\EzioHost.WebAPI` (khởi động trước để cung cấp API)
2. `Front-end\\EzioHost.WebApp` (hoặc `EzioHost.WebApp.Client` tuỳ bạn dùng dự án nào)
3. `ReverseProxy\\EzioHost.ReverseProxy` (khởi động sau cùng để định tuyến)

Thiết lập trong Visual Studio:

- Nhấp phải vào Solution → Set Startup Projects…
- Chọn "Multiple startup projects"
- Đặt Action = Start cho 3 dự án ở trên và sắp xếp theo thứ tự: WebAPI → WebApp → ReverseProxy
- Apply → OK → F5

Lưu ý:

- Đảm bảo `appsettings.*.json` của ReverseProxy trỏ đúng tới địa chỉ WebAPI/Frontend.
- Nếu Frontend gọi API qua proxy, cập nhật base URL tương ứng.
- Kiểm tra `ffmpeg -version` hoạt động trước khi thực hiện encode HLS.

---

## Lưu ý chọn codec video (GPU/CPU)

- Nếu máy có GPU, ưu tiên dùng encoder phần cứng theo hãng để tăng tốc:
  - NVIDIA: `h264_nvenc`
  - Intel (Quick Sync): `h264_qsv`
  - AMD: `h264_amf`
- Nếu không có GPU/driver phù hợp, dùng CPU:
  - `libx264` (H.264)
- Cấu hình trong `Back-end\EzioHost.WebAPI\appsettings.json` mục `AppSettings:VideoEncode:VideoCodec`.

Kiểm tra encoder khả dụng của FFmpeg:

```powershell
ffmpeg -encoders | findstr /I "nvenc qsv amf libx264"
```

---

## Cấu hình sharedsettings.json

File cấu hình chia sẻ nằm tại: `Utility\EzioHost.Shared\sharedsettings.json`.

```json
{
  "ReverseProxyUrl": "https://localhost:7210",
  "WebApiUrl": "https://localhost:7289",
  "FrontendUrl": "https://localhost:7164",
  "WebApiPrefixStaticFile": "static"
}
```

Lưu ý:

- Đảm bảo các URL này khớp với cổng thực tế khi bạn chạy dự án:
  - `ReverseProxyUrl` → cổng của `EzioHost.ReverseProxy`
  - `WebApiUrl` → cổng của `EzioHost.WebAPI`
  - `FrontendUrl` → cổng của `EzioHost.WebApp` (hoặc `EzioHost.WebApp.Client`)
- Khi chạy bằng Aspire, các cổng có thể khác; cập nhật lại cho khớp.
- Nếu frontend gọi API qua ReverseProxy, đặt base URL tương ứng với `ReverseProxyUrl` và cấu hình route phù hợp với `WebApiPrefixStaticFile` (mặc định `static`).

---

## Tài khoản Keycloak (OIDC)

- Keycloak URL: `https://keycloak.talonezio.click`
- Realm: `EzioHost`
- Client (Audience): `Ezio-Host-Client`
- Tài khoản quản trị sẵn có:
  - user: `admin`
  - pass: `admin`

Vị trí cấu hình OIDC trong WebAPI: `Back-end\EzioHost.WebAPI\appsettings.json` → `AppSettings:JwtOidc`. Đảm bảo các giá trị `MetaDataAddress`, `Issuer`, `Audience` khớp với server Keycloak ở trên.

Lưu ý bảo mật: Đây là thông tin thử nghiệm. Hãy đổi mật khẩu và/hoặc tạo tài khoản riêng khi triển khai môi trường thực tế.

---

## Chạy Benchmark hiệu năng (tuỳ chọn)

Project benchmark nằm tại: `Test\EzioHost.Benchmark` (TargetFramework `net9.0`, dùng BenchmarkDotNet).

Chạy nhanh:

```powershell
cd .\Test\EzioHost.Benchmark
dotnet run -c Release

# hoặc từ root solution
dotnet run -c Release --project .\Test\EzioHost.Benchmark\EzioHost.Benchmark.csproj
```

Lưu ý:

- Chạy ở cấu hình Release để có kết quả chính xác.
- Lần chạy đầu có thể lâu vì BenchmarkDotNet thực hiện warmup và nhiều iteration.
- Nếu máy có GPU NVIDIA và đã cài CUDA phù hợp, OnnxRuntime GPU có thể được sử dụng để đo hiệu năng tăng tốc; nếu không, sẽ rơi về CPU.

---

## Tải và sử dụng model AI từ OpenModelDB

Trang mẫu model upscale: `https://openmodeldb.info/`

Hướng dẫn nhanh:

1. Truy cập trang trên và tìm model phù hợp (theo thể loại, tốc độ, chất lượng, v.v.).
2. Tải model (thường là định dạng ONNX) về máy.
3. Thêm model vào hệ thống:
   - Qua giao diện Upload Model trong ứng dụng: chọn file `.onnx`, nhập đúng thông tin.
   - Hoặc copy vào vị trí mà backend có thể truy cập rồi đăng ký trong phần quản trị (nếu có).
4. Chọn model khi thực hiện nâng cấp video/ảnh.

Lưu ý quan trọng về Scale:

- Mỗi model có hệ số phóng đại (Scale) cố định như 1x/2x/4x.
- Khi upload model mới vào hệ thống, hãy nhập đúng `Scale` theo mô tả trên trang model. Scale sai có thể gây méo hình, sai tỷ lệ hoặc giảm chất lượng.
- Nếu model là 1x (không phóng), hệ thống vẫn có thể dùng để khử nhiễu/phục hồi chi tiết mà không thay đổi độ phân giải.

Tham khảo thêm hướng dẫn từ trang FAQ của OpenModelDB:

- Định dạng model hỗ trợ: `.onnx` (ONNX), `.pth` (PyTorch), `.bin/.param` (NCNN).
- Khái niệm Upscaling (SISR/VSR) và dataset huấn luyện được giải thích rõ.
- Các chương trình được khuyến nghị để chạy model:
  - `chaiNNer` (open-source, hỗ trợ PyTorch/ONNX/NCNN),
  - `AnimeJaNaiConverterGui` (ONNX, hỗ trợ TensorRT/DirectML/NCNN),
  - `enhancr`, `VSGAN-TensorRT-docker`, `Upscayl`, v.v.

Tài liệu: [OpenModelDB FAQ](https://openmodeldb.info/docs/faq)

Ghi chú: Phần hướng dẫn này sẽ tiếp tục được cập nhật bổ sung.
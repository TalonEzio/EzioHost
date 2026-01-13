using Microsoft.AspNetCore.Components;

namespace EzioHost.WebApp.Components.Pages;

public partial class FaqPage : ComponentBase
{
    private List<FaqItem> _allFaqs = new();
    private string _searchTerm = "";
    private string _selectedCategory = "";

    private List<FaqItem> FilteredFaqs => _allFaqs
        .Where(f => (_selectedCategory == "" || f.Category == _selectedCategory) &&
                    (_searchTerm == "" ||
                     f.Question.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                     f.Answer.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)))
        .ToList();

    protected override void OnInitialized()
    {
        _allFaqs =
        [
            new FaqItem
            {
                Id = 1,
                Category = "general",
                Question = "EzioHost là gì?",
                Answer =
                    @"<p><strong>EzioHost</strong> là nền tảng video hosting cá nhân được phát triển bằng .NET 10 và Blazor United. Hệ thống cung cấp các tính năng mạnh mẽ bao gồm:</p>
                <ul class='list-disc pl-6 mt-3 space-y-2'>
                    <li>Upload và quản lý video với nhiều định dạng khác nhau</li>
                    <li>Upscale video hoặc ảnh bằng AI (tối ưu cho anime) sử dụng ONNX Runtime</li>
                    <li>Xác thực người dùng qua OpenID Connect sử dụng Keycloak</li>
                    <li>Video player hiện đại với nhiều tùy chọn chất lượng</li>
                    <li>Chia sẻ video với nhiều tùy chọn bảo mật</li>
                </ul>
                <p class='mt-3'>EzioHost được thiết kế để mang lại trải nghiệm video hosting nhanh chóng, an toàn và dễ sử dụng cho người dùng cá nhân.</p>"
            },
            new FaqItem
            {
                Id = 2,
                Category = "account",
                Question = "Làm sao để đăng ký và đăng nhập?",
                Answer =
                    @"<p>EzioHost sử dụng hệ thống xác thực qua <strong>Keycloak</strong> – nền tảng quản lý danh tính mã nguồn mở hàng đầu. Quy trình đăng ký và đăng nhập rất đơn giản:</p>
                <ol class='list-decimal pl-6 mt-3 space-y-2'>
                    <li>Nhấn vào nút <strong>""Đăng nhập""</strong> ở góc trên bên phải</li>
                    <li>Bạn sẽ được chuyển hướng tới giao diện Keycloak</li>
                    <li>Nếu chưa có tài khoản, nhấn <strong>""Đăng ký""</strong> và điền thông tin</li>
                    <li>Nếu đã có tài khoản, đăng nhập bằng email và mật khẩu</li>
                    <li>Sau khi xác thực thành công, bạn sẽ được chuyển về EzioHost</li>
                </ol>
                <p class='mt-3'><strong>Lưu ý:</strong> Keycloak hỗ trợ nhiều phương thức xác thực như email/password, social login (nếu được cấu hình), và các phương thức bảo mật cao như 2FA.</p>"
            },
            new FaqItem
            {
                Id = 3,
                Category = "upload",
                Question = "Những định dạng video nào được hỗ trợ?",
                Answer = @"<p>EzioHost hỗ trợ các định dạng video phổ biến nhất:</p>
                <div class='grid grid-cols-2 gap-2 mt-3'>
                    <div class='bg-gray-50 p-3 rounded-lg'><code class='text-primary-600'>.mp4</code> - MPEG-4 (H.264/H.265)</div>
                    <div class='bg-gray-50 p-3 rounded-lg'><code class='text-primary-600'>.webm</code> - WebM (VP8/VP9)</div>
                    <div class='bg-gray-50 p-3 rounded-lg'><code class='text-primary-600'>.mkv</code> - Matroska</div>
                    <div class='bg-gray-50 p-3 rounded-lg'><code class='text-primary-600'>.avi</code> - Audio Video Interleave</div>
                    <div class='bg-gray-50 p-3 rounded-lg'><code class='text-primary-600'>.mov</code> - QuickTime</div>
                    <div class='bg-gray-50 p-3 rounded-lg'><code class='text-primary-600'>.flv</code> - Flash Video</div>
                </div>
                <p class='mt-4'><strong>Lưu ý:</strong> Các định dạng không phổ biến khác có thể cần chuyển đổi trước khi upload. Hệ thống sẽ tự động xử lý và tối ưu hóa video sau khi upload để đảm bảo tương thích tốt nhất với trình phát video.</p>"
            },
            new FaqItem
            {
                Id = 4,
                Category = "upload",
                Question = "Kích thước file video tối đa là bao nhiêu?",
                Answer = @"<p>Hiện tại, EzioHost hỗ trợ upload video với các giới hạn sau:</p>
                <ul class='list-disc pl-6 mt-3 space-y-2'>
                    <li><strong>Kích thước tối đa mỗi file:</strong> 2GB</li>
                    <li><strong>Độ phân giải tối đa:</strong> 4K (3840x2160)</li>
                    <li><strong>Số lượng file upload cùng lúc:</strong> Không giới hạn (tùy thuộc vào băng thông)</li>
                </ul>
                <p class='mt-3'><strong>Mẹo:</strong> Nếu video của bạn lớn hơn 2GB, bạn có thể sử dụng các công cụ nén video như HandBrake hoặc FFmpeg để giảm kích thước file mà vẫn giữ được chất lượng tốt.</p>"
            },
            new FaqItem
            {
                Id = 5,
                Category = "upload",
                Question = "Video upload xong mất bao lâu để xử lý?",
                Answer = @"<p>Thời gian xử lý video phụ thuộc vào nhiều yếu tố:</p>
                <ul class='list-disc pl-6 mt-3 space-y-2'>
                    <li><strong>Kích thước file:</strong> File lớn hơn sẽ mất nhiều thời gian hơn</li>
                    <li><strong>Độ phân giải:</strong> Video 4K sẽ mất nhiều thời gian hơn 1080p</li>
                    <li><strong>Tải của server:</strong> Khi có nhiều người dùng cùng upload, thời gian có thể tăng</li>
                </ul>
                <p class='mt-3'>Thông thường:</p>
                <ul class='list-disc pl-6 mt-2 space-y-1'>
                    <li>Video 1080p (~500MB): 5-10 phút</li>
                    <li>Video 4K (~2GB): 20-40 phút</li>
                </ul>
                <p class='mt-3'>Bạn sẽ nhận được thông báo khi video đã sẵn sàng để phát. Bạn có thể theo dõi tiến trình trong trang quản lý video.</p>"
            },
            new FaqItem
            {
                Id = 6,
                Category = "ai",
                Question = "Upscale bằng AI hoạt động như thế nào?",
                Answer =
                    @"<p>EzioHost sử dụng <strong>ONNX Runtime</strong> để chạy các mô hình AI được huấn luyện sẵn, giúp nâng cao chất lượng ảnh và video một cách tự động.</p>
                <h4 class='font-semibold mt-4 mb-2'>Quy trình upscale:</h4>
                <ol class='list-decimal pl-6 space-y-2'>
                    <li><strong>Phân tích:</strong> Video/ảnh được phân tích và chia nhỏ thành các khung hình (frames)</li>
                    <li><strong>Xử lý:</strong> Mỗi khung được xử lý bằng mô hình AI <code class='bg-gray-100 px-2 py-1 rounded'>.onnx</code> để tăng độ phân giải</li>
                    <li><strong>Ghép nối:</strong> Các khung sau khi nâng cấp được ghép lại thành video/ảnh hoàn chỉnh</li>
                    <li><strong>Tối ưu:</strong> Video cuối cùng được tối ưu hóa để đảm bảo chất lượng và kích thước file hợp lý</li>
                </ol>
                <p class='mt-4'><strong>Ưu điểm:</strong></p>
                <ul class='list-disc pl-6 space-y-1'>
                    <li>Không cần cài đặt phần mềm bổ sung</li>
                    <li>Xử lý tự động trên server</li>
                    <li>Tối ưu đặc biệt cho nội dung anime</li>
                    <li>Hỗ trợ nhiều mô hình AI khác nhau</li>
                </ul>"
            },
            new FaqItem
            {
                Id = 7,
                Category = "ai",
                Question = "Mô hình AI nào được sử dụng để upscale?",
                Answer =
                    @"<p>EzioHost hỗ trợ nhiều mô hình AI khác nhau từ <strong>OpenModelDB</strong>. Bạn có thể quản lý và chọn mô hình trong trang <strong>AI Models</strong>.</p>
                <p class='mt-3'>Các mô hình phổ biến bao gồm:</p>
                <ul class='list-disc pl-6 mt-2 space-y-2'>
                    <li><strong>Real-ESRGAN:</strong> Tốt cho ảnh và video thực tế</li>
                    <li><strong>Real-ESRGAN Anime:</strong> Tối ưu đặc biệt cho nội dung anime</li>
                    <li><strong>ESPCN:</strong> Nhanh và hiệu quả cho video</li>
                    <li><strong>Waifu2x:</strong> Chuyên dụng cho anime và artwork</li>
                </ul>
                <p class='mt-3'>Bạn có thể upload mô hình ONNX của riêng mình hoặc tải từ OpenModelDB. Mỗi mô hình có thể có scale factor khác nhau (2x, 4x) và độ chính xác khác nhau (FP32, FP16, INT8).</p>"
            },
            new FaqItem
            {
                Id = 8,
                Category = "ai",
                Question = "Upscale video mất bao lâu?",
                Answer = @"<p>Thời gian upscale phụ thuộc vào:</p>
                <ul class='list-disc pl-6 mt-3 space-y-2'>
                    <li><strong>Độ phân giải gốc:</strong> Video 1080p sẽ nhanh hơn 4K</li>
                    <li><strong>Độ dài video:</strong> Video dài hơn sẽ mất nhiều thời gian hơn</li>
                    <li><strong>Mô hình AI:</strong> Một số mô hình nhanh hơn nhưng chất lượng thấp hơn</li>
                    <li><strong>Scale factor:</strong> Upscale 4x sẽ mất nhiều thời gian hơn 2x</li>
                    <li><strong>Tải của server:</strong> Khi có nhiều người dùng, thời gian có thể tăng</li>
                </ul>
                <p class='mt-3'><strong>Ước tính thời gian:</strong></p>
                <ul class='list-disc pl-6 mt-2 space-y-1'>
                    <li>Video 1080p, 1 phút, 2x upscale: ~10-15 phút</li>
                    <li>Video 1080p, 5 phút, 4x upscale: ~60-90 phút</li>
                    <li>Video 4K, 1 phút, 2x upscale: ~30-45 phút</li>
                </ul>
                <p class='mt-3'>Bạn sẽ nhận được thông báo khi quá trình upscale hoàn tất. Bạn có thể tiếp tục sử dụng hệ thống trong khi video đang được xử lý.</p>"
            },
            new FaqItem
            {
                Id = 9,
                Category = "technical",
                Question = "ONNX Runtime là gì?",
                Answer =
                    @"<p><strong>ONNX Runtime</strong> là engine tăng tốc mô hình học sâu (deep learning) do Microsoft phát triển, được thiết kế để chạy các mô hình AI hiệu quả trên nhiều nền tảng khác nhau.</p>
                <h4 class='font-semibold mt-4 mb-2'>Ưu điểm của ONNX Runtime:</h4>
                <ul class='list-disc pl-6 space-y-2'>
                    <li><strong>Đa nền tảng:</strong> Chạy trên Windows, Linux, macOS, và các thiết bị di động</li>
                    <li><strong>Hiệu năng cao:</strong> Tối ưu hóa bằng các thư viện như TensorRT, OpenVINO, DirectML</li>
                    <li><strong>Không cần Python:</strong> Chạy trực tiếp từ .NET mà không cần Python runtime</li>
                    <li><strong>Định dạng chuẩn:</strong> ONNX là định dạng mở, được hỗ trợ bởi nhiều framework AI</li>
                </ul>
                <p class='mt-3'>EzioHost sử dụng ONNX Runtime để thực thi các mô hình AI xử lý ảnh và video trực tiếp từ server .NET, mang lại hiệu suất cao và dễ dàng triển khai.</p>"
            },
            new FaqItem
            {
                Id = 10,
                Category = "technical",
                Question = "OpenModelDB là gì?",
                Answer =
                    @"<p><strong><a href='https://openmodeldb.info/' target='_blank' rel='noopener noreferrer' class='text-primary-600 hover:underline'>OpenModelDB</a></strong> là kho mô hình AI mở, chuyên về xử lý ảnh, video, audio và các tác vụ AI khác.</p>
                <h4 class='font-semibold mt-4 mb-2'>Tại sao sử dụng OpenModelDB?</h4>
                <ul class='list-disc pl-6 space-y-2'>
                    <li><strong>Nguồn mở:</strong> Tất cả mô hình đều miễn phí và mã nguồn mở</li>
                    <li><strong>Định dạng ONNX:</strong> Các mô hình ở định dạng ONNX, dễ tích hợp</li>
                    <li><strong>Đa dạng:</strong> Hàng trăm mô hình cho nhiều mục đích khác nhau</li>
                    <li><strong>Cộng đồng:</strong> Được cộng đồng phát triển và duy trì</li>
                </ul>
                <p class='mt-3'>Bạn có thể tìm và tải các mô hình từ OpenModelDB, sau đó upload vào EzioHost để sử dụng. Xem thêm chi tiết tại <a href='https://openmodeldb.info/docs/faq' target='_blank' rel='noopener noreferrer' class='text-primary-600 hover:underline'>trang FAQ chính thức của OpenModelDB</a>.</p>"
            },
            new FaqItem
            {
                Id = 11,
                Category = "account",
                Question = "Làm sao để thay đổi mật khẩu?",
                Answer = @"<p>Để thay đổi mật khẩu, bạn cần truy cập vào trang quản lý tài khoản Keycloak:</p>
                <ol class='list-decimal pl-6 mt-3 space-y-2'>
                    <li>Đăng nhập vào EzioHost</li>
                    <li>Nhấn vào tên tài khoản ở góc trên bên phải</li>
                    <li>Chọn <strong>""Cài đặt""</strong> hoặc <strong>""Hồ sơ""</strong></li>
                    <li>Bạn sẽ được chuyển đến trang quản lý Keycloak</li>
                    <li>Trong phần <strong>""Security""</strong>, chọn <strong>""Change Password""</strong></li>
                    <li>Nhập mật khẩu cũ và mật khẩu mới</li>
                    <li>Xác nhận thay đổi</li>
                </ol>
                <p class='mt-3'><strong>Lưu ý:</strong> Mật khẩu mới phải đáp ứng các yêu cầu bảo mật của Keycloak (thường là tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt).</p>"
            },
            new FaqItem
            {
                Id = 12,
                Category = "account",
                Question = "Tôi quên mật khẩu, làm sao để lấy lại?",
                Answer = @"<p>Nếu bạn quên mật khẩu, bạn có thể đặt lại mật khẩu qua trang đăng nhập Keycloak:</p>
                <ol class='list-decimal pl-6 mt-3 space-y-2'>
                    <li>Truy cập trang đăng nhập của EzioHost</li>
                    <li>Nhấn vào liên kết <strong>""Quên mật khẩu?""</strong> hoặc <strong>""Forgot Password?""</strong></li>
                    <li>Nhập email đã đăng ký tài khoản</li>
                    <li>Kiểm tra email và nhấn vào liên kết đặt lại mật khẩu</li>
                    <li>Nhập mật khẩu mới và xác nhận</li>
                </ol>
                <p class='mt-3'><strong>Lưu ý:</strong> Nếu không nhận được email, hãy kiểm tra thư mục spam. Nếu vẫn không thấy, hãy liên hệ với đội ngũ hỗ trợ.</p>"
            },
            new FaqItem
            {
                Id = 13,
                Category = "general",
                Question = "EzioHost có miễn phí không?",
                Answer =
                    @"<p>EzioHost hiện tại đang trong giai đoạn phát triển và có thể cung cấp dịch vụ miễn phí cho người dùng. Tuy nhiên, có một số giới hạn:</p>
                <ul class='list-disc pl-6 mt-3 space-y-2'>
                    <li><strong>Dung lượng lưu trữ:</strong> Có giới hạn tùy thuộc vào cấu hình server</li>
                    <li><strong>Băng thông:</strong> Có thể có giới hạn để đảm bảo chất lượng dịch vụ</li>
                    <li><strong>Tốc độ xử lý:</strong> Người dùng miễn phí có thể có độ ưu tiên thấp hơn</li>
                </ul>
                <p class='mt-3'>Trong tương lai, có thể sẽ có các gói trả phí với nhiều tính năng và tài nguyên hơn. Hãy theo dõi các thông báo từ chúng tôi.</p>"
            },
            new FaqItem
            {
                Id = 14,
                Category = "upload",
                Question = "Làm sao để chia sẻ video với người khác?",
                Answer = @"<p>EzioHost cung cấp nhiều tùy chọn chia sẻ video:</p>
                <h4 class='font-semibold mt-4 mb-2'>Các loại chia sẻ:</h4>
                <ul class='list-disc pl-6 space-y-2'>
                    <li><strong>Public:</strong> Bất kỳ ai có liên kết đều có thể xem</li>
                    <li><strong>Unlisted:</strong> Chỉ người có liên kết mới xem được (không hiển thị công khai)</li>
                    <li><strong>Private:</strong> Chỉ bạn mới xem được</li>
                </ul>
                <h4 class='font-semibold mt-4 mb-2'>Cách chia sẻ:</h4>
                <ol class='list-decimal pl-6 space-y-2'>
                    <li>Vào trang <strong>Video</strong> và chọn video bạn muốn chia sẻ</li>
                    <li>Nhấn vào nút <strong>""Share""</strong> (biểu tượng chia sẻ)</li>
                    <li>Chọn loại chia sẻ phù hợp</li>
                    <li>Sao chép liên kết và gửi cho người bạn muốn chia sẻ</li>
                </ol>
                <p class='mt-3'>Bạn cũng có thể embed video vào website của mình bằng cách sử dụng mã embed được cung cấp.</p>"
            },
            new FaqItem
            {
                Id = 15,
                Category = "technical",
                Question = "Công nghệ nào được sử dụng để xây dựng EzioHost?",
                Answer = @"<p>EzioHost được xây dựng bằng các công nghệ hiện đại và mạnh mẽ:</p>
                <div class='grid grid-cols-1 md:grid-cols-2 gap-4 mt-3'>
                    <div class='bg-gray-50 p-4 rounded-lg'>
                        <h5 class='font-semibold mb-2'>Backend:</h5>
                        <ul class='list-disc pl-5 space-y-1 text-sm'>
                            <li>.NET 10</li>
                            <li>ASP.NET Core</li>
                            <li>Entity Framework Core</li>
                            <li>SQL Server</li>
                            <li>ONNX Runtime</li>
                        </ul>
                    </div>
                    <div class='bg-gray-50 p-4 rounded-lg'>
                        <h5 class='font-semibold mb-2'>Frontend:</h5>
                        <ul class='list-disc pl-5 space-y-1 text-sm'>
                            <li>Blazor United</li>
                            <li>Tailwind CSS</li>
                            <li>SignalR</li>
                            <li>HLS.js</li>
                        </ul>
                    </div>
                    <div class='bg-gray-50 p-4 rounded-lg'>
                        <h5 class='font-semibold mb-2'>Infrastructure:</h5>
                        <ul class='list-disc pl-5 space-y-1 text-sm'>
                            <li>YARP (Reverse Proxy)</li>
                            <li>Keycloak (Authentication)</li>
                            <li>FFmpeg (Video Processing)</li>
                        </ul>
                    </div>
                    <div class='bg-gray-50 p-4 rounded-lg'>
                        <h5 class='font-semibold mb-2'>AI/ML:</h5>
                        <ul class='list-disc pl-5 space-y-1 text-sm'>
                            <li>ONNX Runtime</li>
                            <li>OpenModelDB Models</li>
                            <li>Real-ESRGAN</li>
                        </ul>
                    </div>
                </div>"
            },
            new FaqItem
            {
                Id = 16,
                Category = "general",
                Question = "Tôi cần hỗ trợ thì liên hệ như thế nào?",
                Answer = @"<p>Chúng tôi luôn sẵn sàng hỗ trợ bạn! Bạn có thể liên hệ với chúng tôi qua:</p>
                <ul class='list-disc pl-6 mt-3 space-y-2'>
                    <li><strong>Email:</strong> <a href='mailto:vuthemanh1707@gmail.com' class='text-primary-600 hover:underline'>vuthemanh1707@gmail.com</a></li>
                    <li><strong>Trang liên hệ:</strong> Truy cập <a href='/contact' class='text-primary-600 hover:underline'>/contact</a> để gửi phản hồi</li>
                    <li><strong>Trong ứng dụng:</strong> Sau khi đăng nhập, bạn có thể gửi phản hồi trong phần tài khoản cá nhân</li>
                </ul>
                <p class='mt-3'><strong>Thời gian phản hồi:</strong> Chúng tôi cố gắng phản hồi trong vòng 24-48 giờ làm việc. Đối với các vấn đề khẩn cấp, vui lòng ghi rõ trong tiêu đề email.</p>"
            }
        ];
    }

    private void ToggleFaq(int id)
    {
        var faq = _allFaqs.FirstOrDefault(f => f.Id == id);
        if (faq != null) faq.IsOpen = !faq.IsOpen;
    }

    private void OnSearchChanged(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? "";
    }

    private void SelectCategory(string category)
    {
        _selectedCategory = category;
    }

    private class FaqItem
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public bool IsOpen { get; set; }
    }
}
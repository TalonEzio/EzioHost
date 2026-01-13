namespace EzioHost.WebApp.Components.Pages;

public partial class PrivacyPage
{
    protected string LastUpdated => "15 tháng 12, 2024";

    protected string Content => @"
        <h2>1. Giới thiệu</h2>
        <p>EzioHost (""chúng tôi"", ""của chúng tôi"", hoặc ""dịch vụ"") cam kết bảo vệ quyền riêng tư của bạn. Chính sách bảo mật này giải thích cách chúng tôi thu thập, sử dụng, tiết lộ và bảo vệ thông tin cá nhân của bạn khi bạn sử dụng dịch vụ của chúng tôi.</p>

        <h2>2. Thông tin chúng tôi thu thập</h2>
        <h3>2.1. Thông tin bạn cung cấp</h3>
        <ul>
            <li><strong>Thông tin tài khoản:</strong> Khi bạn đăng ký, chúng tôi thu thập thông tin như tên, email, và mật khẩu (được mã hóa) thông qua hệ thống xác thực Keycloak.</li>
            <li><strong>Nội dung video:</strong> Các video bạn upload và metadata liên quan (tiêu đề, mô tả, cài đặt chia sẻ).</li>
            <li><strong>Thông tin liên hệ:</strong> Khi bạn liên hệ với chúng tôi, chúng tôi có thể lưu trữ thông tin liên hệ và nội dung tin nhắn của bạn.</li>
        </ul>

        <h3>2.2. Thông tin tự động thu thập</h3>
        <ul>
            <li><strong>Thông tin sử dụng:</strong> Chúng tôi thu thập thông tin về cách bạn sử dụng dịch vụ, bao gồm các trang bạn truy cập, thời gian truy cập, và các hành động bạn thực hiện.</li>
            <li><strong>Thông tin thiết bị:</strong> Địa chỉ IP, loại trình duyệt, hệ điều hành, và thông tin thiết bị khác.</li>
            <li><strong>Cookies và công nghệ tương tự:</strong> Chúng tôi sử dụng cookies để duy trì phiên đăng nhập và cải thiện trải nghiệm của bạn.</li>
        </ul>

        <h2>3. Cách chúng tôi sử dụng thông tin</h2>
        <p>Chúng tôi sử dụng thông tin thu thập được để:</p>
        <ul>
            <li>Cung cấp, duy trì và cải thiện dịch vụ của chúng tôi</li>
            <li>Xử lý và lưu trữ video của bạn</li>
            <li>Xác thực và quản lý tài khoản của bạn</li>
            <li>Gửi thông báo về dịch vụ và cập nhật</li>
            <li>Phát hiện và ngăn chặn gian lận, lạm dụng và các hoạt động bất hợp pháp</li>
            <li>Tuân thủ các nghĩa vụ pháp lý</li>
        </ul>

        <h2>4. Chia sẻ thông tin</h2>
        <p>Chúng tôi không bán thông tin cá nhân của bạn. Chúng tôi có thể chia sẻ thông tin trong các trường hợp sau:</p>
        <ul>
            <li><strong>Nhà cung cấp dịch vụ:</strong> Chúng tôi có thể chia sẻ thông tin với các nhà cung cấp dịch vụ bên thứ ba giúp chúng tôi vận hành dịch vụ (như Keycloak cho xác thực, nhà cung cấp hosting).</li>
            <li><strong>Yêu cầu pháp lý:</strong> Chúng tôi có thể tiết lộ thông tin nếu được yêu cầu bởi pháp luật hoặc để bảo vệ quyền và an toàn của chúng tôi và người dùng khác.</li>
            <li><strong>Với sự đồng ý của bạn:</strong> Chúng tôi có thể chia sẻ thông tin với sự đồng ý rõ ràng của bạn.</li>
        </ul>

        <h2>5. Bảo mật dữ liệu</h2>
        <p>Chúng tôi thực hiện các biện pháp bảo mật hợp lý để bảo vệ thông tin của bạn:</p>
        <ul>
            <li>Mã hóa dữ liệu trong quá trình truyền (HTTPS/TLS)</li>
            <li>Mã hóa dữ liệu nhạy cảm khi lưu trữ</li>
            <li>Kiểm soát truy cập nghiêm ngặt</li>
            <li>Giám sát và phát hiện các hoạt động đáng ngờ</li>
            <li>Sao lưu dữ liệu thường xuyên</li>
        </ul>
        <p>Tuy nhiên, không có phương thức truyền tải hoặc lưu trữ điện tử nào là 100% an toàn. Chúng tôi không thể đảm bảo bảo mật tuyệt đối.</p>

        <h2>6. Quyền của bạn</h2>
        <p>Bạn có các quyền sau đối với dữ liệu cá nhân của mình:</p>
        <ul>
            <li><strong>Quyền truy cập:</strong> Bạn có thể yêu cầu truy cập vào dữ liệu cá nhân của mình</li>
            <li><strong>Quyền chỉnh sửa:</strong> Bạn có thể cập nhật hoặc sửa đổi thông tin tài khoản của mình</li>
            <li><strong>Quyền xóa:</strong> Bạn có thể yêu cầu xóa tài khoản và dữ liệu của mình</li>
            <li><strong>Quyền từ chối:</strong> Bạn có thể từ chối việc xử lý dữ liệu của mình trong một số trường hợp nhất định</li>
            <li><strong>Quyền di chuyển dữ liệu:</strong> Bạn có thể yêu cầu xuất dữ liệu của mình</li>
        </ul>
        <p>Để thực hiện các quyền này, vui lòng liên hệ với chúng tôi qua email: <a href='mailto:vuthemanh1707@gmail.com' class='text-primary-600 hover:underline'>vuthemanh1707@gmail.com</a></p>

        <h2>7. Cookies</h2>
        <p>Chúng tôi sử dụng cookies và công nghệ tương tự để:</p>
        <ul>
            <li>Duy trì phiên đăng nhập của bạn</li>
            <li>Ghi nhớ tùy chọn và cài đặt của bạn</li>
            <li>Phân tích cách bạn sử dụng dịch vụ</li>
            <li>Cải thiện trải nghiệm người dùng</li>
        </ul>
        <p>Bạn có thể kiểm soát cookies thông qua cài đặt trình duyệt của mình. Tuy nhiên, việc vô hiệu hóa cookies có thể ảnh hưởng đến chức năng của dịch vụ.</p>

        <h2>8. Dữ liệu của trẻ em</h2>
        <p>Dịch vụ của chúng tôi không dành cho trẻ em dưới 13 tuổi. Chúng tôi không cố ý thu thập thông tin cá nhân từ trẻ em dưới 13 tuổi. Nếu chúng tôi phát hiện rằng chúng tôi đã thu thập thông tin từ trẻ em dưới 13 tuổi, chúng tôi sẽ xóa thông tin đó ngay lập tức.</p>

        <h2>9. Thay đổi chính sách</h2>
        <p>Chúng tôi có thể cập nhật chính sách bảo mật này theo thời gian. Chúng tôi sẽ thông báo cho bạn về bất kỳ thay đổi nào bằng cách đăng chính sách mới trên trang này và cập nhật ngày ""Cập nhật lần cuối"" ở đầu chính sách này.</p>
        <p>Chúng tôi khuyến khích bạn xem lại chính sách bảo mật này định kỳ để được thông báo về cách chúng tôi bảo vệ thông tin của bạn.</p>

        <h2>10. Liên hệ</h2>
        <p>Nếu bạn có bất kỳ câu hỏi nào về chính sách bảo mật này, vui lòng liên hệ với chúng tôi:</p>
        <ul>
            <li><strong>Email:</strong> <a href='mailto:vuthemanh1707@gmail.com' class='text-primary-600 hover:underline'>vuthemanh1707@gmail.com</a></li>
            <li><strong>Địa chỉ:</strong> [Địa chỉ của bạn]</li>
        </ul>

        <div class='bg-primary-50 border border-primary-200 rounded-lg p-6 mt-8'>
            <p class='font-semibold text-gray-900 mb-2'>Cam kết của chúng tôi</p>
            <p class='text-gray-700'>Chúng tôi cam kết bảo vệ quyền riêng tư của bạn và xử lý dữ liệu của bạn một cách minh bạch và có trách nhiệm. Nếu bạn có bất kỳ mối quan ngại nào, vui lòng liên hệ với chúng tôi ngay lập tức.</p>
        </div>
    ";
}
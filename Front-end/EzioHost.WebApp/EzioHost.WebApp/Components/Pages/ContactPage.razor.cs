using System.ComponentModel.DataAnnotations;
using EzioHost.WebApp.Client.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Components.Pages;

public partial class ContactPage : ComponentBase
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ContactFormModel _contactForm = new();
    private bool _isSubmitting = false;

    private async Task HandleSubmit()
    {
        _isSubmitting = true;
        
        try
        {
            // TODO: Implement actual email sending logic here
            // For now, we'll just simulate a delay
            await Task.Delay(1000);
            
            // In a real implementation, you would:
            // 1. Call an API endpoint to send the email
            // 2. Show success/error message to user
            // 3. Reset the form on success
            
            // Example: await ContactApi.SendContactEmail(_contactForm);
            
            // Reset form
            _contactForm = new ContactFormModel();
            
            // Show success message
            await JsRuntime.ShowSuccessToast("Tin nhắn đã được gửi thành công! Chúng tôi sẽ phản hồi trong vòng 24-48 giờ.");
        }
        catch (Exception ex)
        {
            // Show error message
            await JsRuntime.ShowErrorToast($"Lỗi khi gửi tin nhắn: {ex.Message}");
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    public class ContactFormModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email của bạn")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn chủ đề")]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
        [StringLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự")]
        [MinLength(10, ErrorMessage = "Nội dung phải có ít nhất 10 ký tự")]
        public string Message { get; set; } = "";
    }
}

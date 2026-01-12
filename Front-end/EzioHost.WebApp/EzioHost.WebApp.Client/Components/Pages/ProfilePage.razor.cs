using System.Security.Claims;
using EzioHost.WebApp.Client.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class ProfilePage : ComponentBase
{
    [CascadingParameter]
    public Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private AuthenticationState? _authState;
    private bool _showAllClaims = false;

    // User Information
    private string FullName => GetClaimValue(ClaimTypes.Name) ?? "N/A";
    private string GivenName => GetClaimValue(ClaimTypes.GivenName) ?? "N/A";
    private string Surname => GetClaimValue(ClaimTypes.Surname) ?? "N/A";
    private string Email => GetClaimValue(ClaimTypes.Email) ?? "N/A";
    private string Username => GetClaimValue("preferred_username") ?? "N/A";
    private string UserId => GetClaimValue(ClaimTypes.NameIdentifier) ?? "N/A";
    private bool IsEmailVerified => GetClaimValue("email_verified")?.ToLower() == "true";
    
    // Initials for avatar
    private string Initials
    {
        get
        {
            if (!string.IsNullOrEmpty(GivenName) && !string.IsNullOrEmpty(Surname) && GivenName != "N/A" && Surname != "N/A")
            {
                return $"{GivenName[0]}{Surname[0]}".ToUpper();
            }
            if (!string.IsNullOrEmpty(FullName) && FullName != "N/A")
            {
                var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                }
                return FullName[0].ToString().ToUpper();
            }
            if (!string.IsNullOrEmpty(Username) && Username != "N/A")
            {
                return Username.Substring(0, Math.Min(2, Username.Length)).ToUpper();
            }
            return "U";
        }
    }

    // Roles
    private List<string> Roles
    {
        get
        {
            if (_authState?.User?.Identity?.IsAuthenticated != true)
                return new List<string>();

            return _authState.User.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .Distinct()
                .ToList();
        }
    }

    // Resource Limits
    private long UploadMaxSpeedMb => _authState?.UploadMaxSpeedMb ?? 0;
    private long Storage => _authState?.Storage ?? 0;

    private string StorageDisplay
    {
        get
        {
            if (Storage >= 1024 * 1024) // >= 1TB
                return $"{Storage / (1024.0 * 1024.0):F2} TB";
            if (Storage >= 1024) // >= 1GB
                return $"{Storage / 1024.0:F2} GB";
            return $"{Storage} MB";
        }
    }

    // Session Information
    private string AuthTimeDisplay
    {
        get
        {
            var authTime = GetClaimValue("auth_time");
            if (string.IsNullOrEmpty(authTime) || !long.TryParse(authTime, out var timestamp))
                return "N/A";
            
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return dateTime.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }

    private string ExpirationDisplay
    {
        get
        {
            var exp = GetClaimValue("exp");
            if (string.IsNullOrEmpty(exp) || !long.TryParse(exp, out var timestamp))
                return "N/A";
            
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var timeRemaining = dateTime.LocalDateTime - DateTime.Now;
            
            if (timeRemaining.TotalDays >= 1)
                return $"{dateTime.LocalDateTime:dd/MM/yyyy HH:mm:ss} ({timeRemaining.Days} ngày còn lại)";
            if (timeRemaining.TotalHours >= 1)
                return $"{dateTime.LocalDateTime:dd/MM/yyyy HH:mm:ss} ({timeRemaining.Hours} giờ còn lại)";
            if (timeRemaining.TotalMinutes >= 1)
                return $"{dateTime.LocalDateTime:dd/MM/yyyy HH:mm:ss} ({timeRemaining.Minutes} phút còn lại)";
            
            return "Đã hết hạn";
        }
    }

    private string SessionId => GetClaimValue("sid") ?? "N/A";

    // All Claims for Debug
    private List<Claim> AllClaims
    {
        get
        {
            if (_authState?.User?.Identity?.IsAuthenticated != true)
                return new List<Claim>();

            return _authState.User.Claims.OrderBy(c => c.Type).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _authState = await AuthStateTask;
        await base.OnInitializedAsync();
    }

    private string? GetClaimValue(string claimType)
    {
        if (_authState?.User?.Identity?.IsAuthenticated != true)
            return null;

        return _authState.User.Claims
            .FirstOrDefault(c => c.Type == claimType)
            ?.Value;
    }
}

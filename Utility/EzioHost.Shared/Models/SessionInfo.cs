using System.Text.Json.Serialization;

namespace EzioHost.Shared.Models;

public class SessionInfo
{
    [JsonPropertyName("userId")] public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("userName")] public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")] public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")] public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("idToken")] public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")] public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")] public DateTimeOffset ExpiresAt { get; set; }

    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("lastAccessedAt")] public DateTimeOffset LastAccessedAt { get; set; }

    [JsonPropertyName("claims")] public Dictionary<string, string> Claims { get; set; } = new();

    [JsonPropertyName("roles")] public List<string> Roles { get; set; } = new();
}
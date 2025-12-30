using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface IAuthApi
{
    [Get("/access-token")]
    Task<string> GetAccessToken();

    [Get("/user")]
    Task<List<ClaimDto>> GetUser();
}
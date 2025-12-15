namespace EzioHost.Core.Services.Interface;

public interface IProtectService
{
    string GenerateRandomKey();
    string GenerateRandomIv();
}
using System.Security.Cryptography;
using EzioHost.Core.Services.Interface;

namespace EzioHost.Core.Services.Implement;

public class ProtectService : IProtectService
{
    public string GenerateRandomKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[16]; // AES-128 key (16 bytes)
        rng.GetBytes(keyBytes);
        return BitConverter.ToString(keyBytes).Replace("-", "").ToLower();
    }

    public string GenerateRandomIv()
    {
        using var rng = RandomNumberGenerator.Create();
        var ivBytes = new byte[16]; // AES IV (16 bytes)
        rng.GetBytes(ivBytes);
        return BitConverter.ToString(ivBytes).Replace("-", "").ToLower();
    }
}
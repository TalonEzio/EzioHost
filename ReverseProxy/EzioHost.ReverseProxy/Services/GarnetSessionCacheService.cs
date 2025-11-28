using EzioHost.Shared.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace EzioHost.ReverseProxy.Services
{

    public class GarnetSessionCacheService(
        IConnectionMultiplexer redis,
        IOptions<AppSettings> appSettings,
        ILogger<GarnetSessionCacheService> logger)
        : ISessionCacheService
    {
        private readonly IDatabase _database = redis.GetDatabase(appSettings.Value.GarnetCache.Database);
        private readonly GarnetCacheSetting _settings = appSettings.Value.GarnetCache;

        public async Task<SessionInfo?> GetSessionAsync(string sessionId)
        {
            try
            {
                var key = GetKey(sessionId);
                var cachedData = await _database.StringGetAsync(key);

                if (cachedData.HasValue)
                {
                    var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(cachedData.ToString());

                    // Update last accessed time
                    if (sessionInfo != null)
                    {
                        sessionInfo.LastAccessedAt = DateTimeOffset.UtcNow;
                        await SetSessionAsync(sessionId, sessionInfo);

                        logger.LogDebug("Session {SessionId} retrieved from cache", sessionId);
                    }

                    return sessionInfo;
                }

                logger.LogDebug("Session {SessionId} not found in cache", sessionId);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving session {SessionId} from cache", sessionId);
                return null;
            }
        }

        public async Task SetSessionAsync(string sessionId, SessionInfo sessionInfo, TimeSpan? expiry = null)
        {
            try
            {
                var key = GetKey(sessionId);
                var expiryTime = expiry ?? TimeSpan.FromDays(_settings.DefaultExpiryDays);

                // Update timestamps
                sessionInfo.LastAccessedAt = DateTimeOffset.UtcNow;
                if (sessionInfo.CreatedAt == default)
                {
                    sessionInfo.CreatedAt = DateTimeOffset.UtcNow;
                }

                var serializedData = JsonSerializer.Serialize(sessionInfo);
                await _database.StringSetAsync(key, serializedData, expiryTime);

                logger.LogDebug("Session {SessionId} saved to cache with expiry {Expiry}", sessionId, expiryTime);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving session {SessionId} to cache", sessionId);
                throw;
            }
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            try
            {
                var key = GetKey(sessionId);
                await _database.KeyDeleteAsync(key);

                logger.LogDebug("Session {SessionId} deleted from cache", sessionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting session {SessionId} from cache", sessionId);
                throw;
            }
        }

        public async Task RefreshSessionAsync(string sessionId, TimeSpan? expiry = null)
        {
            try
            {
                var key = GetKey(sessionId);
                var expiryTime = expiry ?? TimeSpan.FromDays(_settings.DefaultExpiryDays);

                // Check if session exists
                var exists = await _database.KeyExistsAsync(key);
                if (exists)
                {
                    await _database.KeyExpireAsync(key, expiryTime);

                    // Update last accessed time
                    var sessionInfo = await GetSessionAsync(sessionId);
                    if (sessionInfo != null)
                    {
                        sessionInfo.LastAccessedAt = DateTimeOffset.UtcNow;
                        await SetSessionAsync(sessionId, sessionInfo, expiryTime);
                    }

                    logger.LogDebug("Session {SessionId} refreshed with new expiry {Expiry}", sessionId, expiryTime);
                }
                else
                {
                    logger.LogWarning("Attempted to refresh non-existent session {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing session {SessionId} in cache", sessionId);
                throw;
            }
        }

        public string GenerateSessionId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private string GetKey(string sessionId)
        {
            return $"{_settings.KeyPrefix}{sessionId}";
        }
    }
}
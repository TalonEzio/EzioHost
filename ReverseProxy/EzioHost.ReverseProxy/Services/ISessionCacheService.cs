using EzioHost.Shared.Models;

namespace EzioHost.ReverseProxy.Services
{
    public interface ISessionCacheService
    {
        /// <summary>
        /// Lấy session information từ cache dựa trên session ID
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>Session information hoặc null nếu không tồn tại</returns>
        Task<SessionInfo?> GetSessionAsync(string sessionId);

        /// <summary>
        /// Lưu session information vào cache
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="sessionInfo">Session information</param>
        /// <param name="expiry">Thời gian expiry (mặc định 30 ngày)</param>
        Task SetSessionAsync(string sessionId, SessionInfo sessionInfo, TimeSpan? expiry = null);

        /// <summary>
        /// Xóa session khỏi cache
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        Task DeleteSessionAsync(string sessionId);

        /// <summary>
        /// Refresh session expiry time
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="expiry">Thời gian expiry mới (mặc định 30 ngày)</param>
        Task RefreshSessionAsync(string sessionId, TimeSpan? expiry = null);

        /// <summary>
        /// Tạo session ID mới
        /// </summary>
        /// <returns>Session ID mới</returns>
        string GenerateSessionId();
    }
}
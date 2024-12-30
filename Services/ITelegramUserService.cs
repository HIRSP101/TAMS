using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface ITelegramUserService
    {
        Task<List<UserSelectItem>> GetAvailableUsersAsync(bool forceRefresh = false);
        Task<CachedUserInfo> GetUserInfoAsync(long userId);
    }
}

using WebApplication1.Data;
using WebApplication1.Models;
namespace WebApplication1.Repository
{
    public interface ITelegramUserRepository
    {
        Task SaveUsersAsync(List<TelegramUserModel> users);
        Task<List<TelegramUserModel>> GetUsersAsync();
    }
}

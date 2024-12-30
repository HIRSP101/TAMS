using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Repository
{
    public class TelegramUserRepository : ITelegramUserRepository
    {
        private readonly ApplicationDbContext _context;

        public TelegramUserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveUsersAsync(List<TelegramUserModel> users)
        {
            foreach (var user in users)
            {
                var existingUser = await _context.TelegramUsers
                    .FirstOrDefaultAsync(u => u.ChatId == user.ChatId);

                if (existingUser == null)
                {
                    await _context.TelegramUsers.AddAsync(user);
                }
                else
                {
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.Username = user.Username;
                    existingUser.LastInteraction = user.LastInteraction;
                    existingUser.IsBlocked = user.IsBlocked;
                }
            }

            await _context.SaveChangesAsync();
        }
        public async Task<List<TelegramUserModel>> GetUsersAsync()
        {
            return await _context.TelegramUsers.ToListAsync();
        }
    }
}

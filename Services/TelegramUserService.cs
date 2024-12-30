namespace WebApplication1.Services
{
    using Microsoft.Extensions.Caching.Memory;
    using System.Text.Json;
    using Telegram.Bot;
    using WebApplication1.Models;
    using WebApplication1.Repository;

    public class TelegramUserService : ITelegramUserService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ITelegramUserRepository _userRepository;
        
        private const string USER_CACHE_KEY = "TelegramUsers";
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public TelegramUserService(
            ITelegramBotClient botClient,
            IMemoryCache memoryCache,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ITelegramUserRepository userRepository)
        {
            _botClient = botClient;
            _memoryCache = memoryCache;
            _configuration = configuration;
            _environment = environment;
            _userRepository = userRepository;
        }

        public async Task<List<UserSelectItem>> GetAvailableUsersAsync(bool forceRefresh = false)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Check cache first
                if (!forceRefresh && _memoryCache.TryGetValue(USER_CACHE_KEY, out UserCache cachedUsers))
                {
                    // Return cached users if less than 1 hour old
                    if (DateTime.UtcNow - cachedUsers.LastUpdated < TimeSpan.FromHours(1))
                    {
                        return MapToSelectItems(cachedUsers.Users);
                    }
                }

                // Fetch updates and create user cache
                var updates = await _botClient.GetUpdatesAsync();
                var userCache = new UserCache { LastUpdated = DateTime.UtcNow };

                foreach (var update in updates)
                {
                    if (update.Message?.From != null)
                    {
                        var user = update.Message.From;
                        var userId = user.Id;

                        // Add or update user in cache
                        userCache.Users[userId] = new CachedUserInfo
                        {
                            UserId = userId.ToString(),
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Username = user.Username,
                            LastInteraction = DateTime.UtcNow
                        };
                    }
                }

                // Persist cache to file for more permanent storage
                await PersistUserCacheAsync(userCache);

                // Store in memory cache
                _memoryCache.Set(USER_CACHE_KEY, userCache, TimeSpan.FromHours(2));

                return MapToSelectItems(userCache.Users);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching users: {ex.Message}");
                return new List<UserSelectItem>();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        private async Task SaveUsersToDatabaseAsync(List<CachedUserInfo> cachedUserInfos)
        {
            var users = cachedUserInfos.Select(u => new TelegramUserModel
            {
                ChatId = u.UserId, // Assuming UserId is stored as a string, so we parse it
                FirstName = u.FirstName,
                LastName = u.LastName,
                Username = u.Username,
                LastInteraction = u.LastInteraction
            }).ToList();

            await _userRepository.SaveUsersAsync(users);
        }

        private async Task PersistUserCacheAsync(UserCache userCache)
        {
            // Only persist in non-development environments
            if (!_environment.IsDevelopment())
            {
                var cacheFilePath = Path.Combine(_environment.ContentRootPath, "telegram_users_cache.json");
                var jsonContent = JsonSerializer.Serialize(userCache, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(cacheFilePath, jsonContent);
            }
        }

        private List<UserSelectItem> MapToSelectItems(Dictionary<long, CachedUserInfo> users)
        {
            return users.Values
                .OrderByDescending(u => u.LastInteraction)
                .Select(u => new UserSelectItem
                {
                    UserId = u.UserId,
                    UserDisplay = $"{u.FirstName} {u.LastName} - Last Seen: {u.LastInteraction:g}"
                })
                .ToList();
        }

        public async Task<CachedUserInfo> GetUserInfoAsync(long userId)
        {
            var users = await GetAvailableUsersAsync();
            return users.Any()
                ? new CachedUserInfo
                {
                    UserId = userId.ToString(),
                    // Add additional logic to get more details
                }
                : null;
        }
    }
}

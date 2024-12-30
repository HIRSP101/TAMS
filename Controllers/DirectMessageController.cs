using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Collections.Generic;
using WebApplication1.Models;
using WebApplication1.Repository;

public class DirectMessageController : Controller
{
    private readonly ITelegramBotClient _botClient;
    private readonly IConfiguration _configuration;
    private readonly ITelegramUserRepository _userRepository;

    public DirectMessageController(ITelegramBotClient botClient, IConfiguration configuration, ITelegramUserRepository userRepository)
    {
        _botClient = botClient;
        _configuration = configuration;
        _userRepository = userRepository;
    }

    public async Task<IActionResult> Index()
    {

     //   var usersFromDb = await GetAvailableUsersFromDB();
        var usersFromTelegram = await GetAvailableUsersFromTelegram();
        
        var model = new MessageViewModel
        {
            AvailableUsers = usersFromTelegram
        };

       
        await SaveUsersToDatabaseAsync(usersFromTelegram);

        return View(model);
    }

    private async Task<List<UserSelectItem>> GetAvailableUsersFromTelegram()
    {
        var users = new List<UserSelectItem>();
        try
        {
            var updates = await _botClient.GetUpdatesAsync();

            var uniqueUsers = new HashSet<long>();
            foreach (var update in updates)
            {
                if (update.Message?.From != null && uniqueUsers.Add(update.Message.From.Id))
                {
                    users.Add(new UserSelectItem
                    {
                        UserId = update.Message.From.Id.ToString(),
                        UserDisplay = $"{update.Message.From.FirstName} {update.Message.From.LastName} (@{update.Message.From.Username})"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users from Telegram: {ex.Message}");
        }

        return users;
    }

    private async Task<List<UserSelectItem>> GetAvailableUsersFromDB()
    {
        var dbUsers = await _userRepository.GetUsersAsync(); 


        var users = dbUsers.Select(user => new UserSelectItem
        {
            UserId = user.ChatId.ToString(),
            UserDisplay = $"{user.FirstName} {user.LastName} (@{user.Username})"
        }).ToList();

        return users;
    }

    private async Task SaveUsersToDatabaseAsync(List<UserSelectItem> users)
    {
        var telegramUsers = users.Select(u => new TelegramUserModel
        {
            ChatId = u.UserId,
            FirstName = u.UserDisplay.Split(" ")[0],
            LastName = u.UserDisplay.Split(" ").Length > 1 ? u.UserDisplay.Split(" ")[1] : "",
            Username = u.UserDisplay.Split("@")[1].Substring(0,u.UserDisplay.Split("@")[1].Length -1), // Could be added if available
            LastInteraction = DateTime.UtcNow,
            IsBlocked = false
        }).ToList();

        await _userRepository.SaveUsersAsync(telegramUsers);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(MessageViewModel model)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: long.Parse(model.UserId),
                text: model.Message
            );
            TempData["Success"] = "Message sent successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error sending message: {ex.Message}";
        }
        return RedirectToAction("Index");
    }
}

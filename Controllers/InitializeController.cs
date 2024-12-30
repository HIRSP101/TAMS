using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

public class InitializeController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ITelegramBotVerificationService _botVerificationService;
    private readonly IWebHostEnvironment _environment;

    public InitializeController(
        IConfiguration configuration, 
        ITelegramBotVerificationService botVerificationService,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _botVerificationService = botVerificationService;
        _environment = environment;
    }

    public IActionResult Index()
    {
        return PartialView();
    }

    [HttpPost]
    public async Task<IActionResult> SaveBotToken(string botToken)
    {

        var botInfo = await _botVerificationService.VerifyBotApiKeyAsync(botToken);

        if (botInfo == null)
        {
            ModelState.AddModelError("botToken", "Invalid Telegram Bot Token");
            return View("Index");
        }

        try
        {
            var configPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            
            var configJson = await System.IO.File.ReadAllTextAsync(configPath);
            var configDocument = JsonDocument.Parse(configJson);
            var rootElement = configDocument.RootElement;

            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) 
                ?? new Dictionary<string, object>();

            if (!configDict.ContainsKey("TelegramBot"))
            {
                configDict["TelegramBot"] = new Dictionary<string, string>();
            }

            var telegramBotSection = configDict["TelegramBot"] as Dictionary<string, object> 
                ?? new Dictionary<string, object>();
            telegramBotSection["BotToken"] = botToken;
            configDict["TelegramBot"] = telegramBotSection;

            var updatedConfigJson = JsonSerializer.Serialize(
                configDict, 
                new JsonSerializerOptions { WriteIndented = true }
            );
            await System.IO.File.WriteAllTextAsync(configPath, updatedConfigJson);

            // Save to a separate configuration file
            var botConfigPath = Path.Combine(_environment.ContentRootPath, "BotConfig.json");
            var botConfig = new Dictionary<string, string>
            {
                { "BotToken", botToken }
            };
            await System.IO.File.WriteAllTextAsync(
                botConfigPath, 
                JsonSerializer.Serialize(botConfig, new JsonSerializerOptions { WriteIndented = true })
            );

            // Return bot information to potentially display in a success view
            TempData["BotInfo"] = JsonSerializer.Serialize(botInfo);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while saving the bot token.");
            return View("Index");
        }
    }
}

public static class TelegramBotConfigurationExtensions
{
    public static bool IsTelegramBotConfigured(this IConfiguration configuration)
    {
        var telegramBotSection = configuration.GetSection("TelegramBot");
        return telegramBotSection.Exists() && 
               !string.IsNullOrWhiteSpace(telegramBotSection["BotToken"]);
    }
}
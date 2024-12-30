using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

public class TelegramBotSetupController : Controller
{
    private readonly ITelegramBotVerificationService _botVerificationService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public TelegramBotSetupController(
        ITelegramBotVerificationService botVerificationService,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _botVerificationService = botVerificationService;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> VerifyBotApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest(new { message = "API key cannot be empty" });
        }

        var botInfo = await _botVerificationService.VerifyBotApiKeyAsync(apiKey);

        if (botInfo == null)
        {
            return BadRequest(new { message = "Invalid bot API key" });
        }

        TempData["BotApiKey"] = apiKey;

        return Ok(botInfo);
    }

    [HttpPost]
    public async Task<IActionResult> SaveBotApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest(new { message = "API key cannot be empty" });
        }

        var botInfo = await _botVerificationService.VerifyBotApiKeyAsync(apiKey);

        if (botInfo == null)
        {
            return BadRequest(new { message = "Invalid bot API key" });
        }

        // Save API key to appsettings.json
        var configPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        var configJson = await System.IO.File.ReadAllTextAsync(configPath);
        var configObject = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

        if (!configObject.ContainsKey("TelegramBot"))
        {
            configObject["TelegramBot"] = new Dictionary<string, string>();
        }

        ((Dictionary<string, object>)configObject["TelegramBot"])["ApiKey"] = apiKey;

        var updatedConfigJson = JsonSerializer.Serialize(configObject, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(configPath, updatedConfigJson);

        // Save API key to a separate JSON file in the project directory
        var botConfigPath = Path.Combine(_environment.ContentRootPath, "BotConfig.json");
        var botConfig = new Dictionary<string, string>
        {
            { "ApiKey", apiKey }
        };

        await System.IO.File.WriteAllTextAsync(botConfigPath, 
            JsonSerializer.Serialize(botConfig, new JsonSerializerOptions { WriteIndented = true }));

        return Ok(botInfo);
    }
}
using WebApplication1.Models;

public interface ITelegramBotVerificationService
{
    Task<TelegramBotInfoDto> VerifyBotApiKeyAsync(string apiKey);
}
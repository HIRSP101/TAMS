using System.Text.Json;
using WebApplication1.Models;

public class TelegramBotVerificationService : ITelegramBotVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramBotVerificationService> _logger;

    public TelegramBotVerificationService(
        HttpClient httpClient,
        ILogger<TelegramBotVerificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TelegramBotInfoDto> VerifyBotApiKeyAsync(string apiKey)
    {
        try
        {
            // Increase timeout to handle potential slow responses
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            // First, verify the bot's basic information
            var getMeUrl = $"https://api.telegram.org/bot{apiKey}/getMe";
            _logger.LogInformation($"Calling getMe endpoint: {getMeUrl}");

            var getMeResponse = await _httpClient.GetAsync(getMeUrl);

            // Log the raw response for debugging
            var getMeResponseContent = await getMeResponse.Content.ReadAsStringAsync();
            _logger.LogInformation($"getMe Response: {getMeResponseContent}");

            // Ensure successful response
            getMeResponse.EnsureSuccessStatusCode();

            // Parse the response manually to handle potential JSON variations
            using var getMeDoc = JsonDocument.Parse(getMeResponseContent);
            var root = getMeDoc.RootElement;

            // Check for successful response in Telegram's API format
            if (!root.TryGetProperty("ok", out var okProperty) ||
                !okProperty.GetBoolean() ||
                !root.TryGetProperty("result", out var resultProperty))
            {
                _logger.LogWarning("Invalid bot API response");
                return null;
            }

            // Extract bot information
            var botInfo = new TelegramBotInfoDto
            {
                Username = resultProperty.TryGetProperty("username", out var usernameProperty)
                    ? usernameProperty.GetString()
                    : null,
                Name = resultProperty.TryGetProperty("first_name", out var firstNameProperty)
                    ? firstNameProperty.GetString()
                    : null
            };

            // Try to get profile photos (this step is optional)
            try
            {
                var userId = resultProperty.TryGetProperty("id", out var idProperty)
                    ? idProperty.GetInt64()
                    : 0;

                if (userId > 0)
                {
                    var photosUrl = $"https://api.telegram.org/bot{apiKey}/getUserProfilePhotos?user_id={userId}&limit=1";
                    _logger.LogInformation($"Calling getUserProfilePhotos endpoint: {photosUrl}");

                    var photosResponse = await _httpClient.GetAsync(photosUrl);
                    var photosResponseContent = await photosResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation($"getUserProfilePhotos Response: {photosResponseContent}");

                    using var photosDoc = JsonDocument.Parse(photosResponseContent);
                    var photosRoot = photosDoc.RootElement;

                    // Check if photos exist and retrieve file information
                    if (photosRoot.TryGetProperty("ok", out var photosOkProperty) &&
                        photosOkProperty.GetBoolean() &&
                        photosRoot.TryGetProperty("result", out var photosResultProperty) &&
                        photosResultProperty.TryGetProperty("photos", out var photosArrayProperty) &&
                        photosArrayProperty.GetArrayLength() > 0)
                    {
                        // Get the first photo's file ID
                        var firstPhotoArray = photosArrayProperty[0];
                        if (firstPhotoArray.GetArrayLength() > 0)
                        {
                            var firstPhotoFileId = firstPhotoArray[0].TryGetProperty("file_id", out var fileIdProperty)
                                ? fileIdProperty.GetString()
                                : null;

                            if (!string.IsNullOrWhiteSpace(firstPhotoFileId))
                            {
                                // Get file path
                                var fileUrl = $"https://api.telegram.org/bot{apiKey}/getFile?file_id={firstPhotoFileId}";
                                _logger.LogInformation($"Calling getFile endpoint: {fileUrl}");

                                var fileResponse = await _httpClient.GetAsync(fileUrl);
                                var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                                _logger.LogInformation($"getFile Response: {fileResponseContent}");

                                using var fileDoc = JsonDocument.Parse(fileResponseContent);
                                var fileRoot = fileDoc.RootElement;

                                if (fileRoot.TryGetProperty("ok", out var fileOkProperty) &&
                                    fileOkProperty.GetBoolean() &&
                                    fileRoot.TryGetProperty("result", out var fileResultProperty) &&
                                    fileResultProperty.TryGetProperty("file_path", out var filePathProperty))
                                {
                                    botInfo.ProfilePictureUrl = $"https://api.telegram.org/file/bot{apiKey}/{filePathProperty.GetString()}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception photoEx)
            {
                // Log photo retrieval errors, but don't fail the entire verification
                _logger.LogWarning(photoEx, "Error retrieving bot profile photo");
            }

            return botInfo;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP request failed when verifying bot API key: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"JSON parsing error when verifying bot API key: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error when verifying bot API key: {ex.Message}");
            return null;
        }
    }
}
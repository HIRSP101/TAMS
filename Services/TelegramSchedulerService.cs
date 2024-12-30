namespace WebApplication1.Services
{
    using Hangfire;
    using Hangfire.Storage;
    using Microsoft.EntityFrameworkCore;
    using System.Text.Json;
    using Telegram.Bot;
    using WebApplication1.Data;
    using WebApplication1.Models;

    public class TelegramSchedulerService : ITelegramSchedulerService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;

        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TelegramSchedulerService> _logger;
        private const string SCHEDULED_MESSAGES_FILE = "scheduled_messages.json";

        public TelegramSchedulerService(
            ITelegramBotClient botClient,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            IServiceProvider serviceProvider,
             ILogger<TelegramSchedulerService> logger)
        {
            _botClient = botClient;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }



        public async Task<Guid> ScheduleMessageAsync(ScheduledMessageModel messageModel)
        {
            try
            {
                // Extensive logging
                _logger.LogInformation($"Scheduling message: UserId={messageModel.UserId}, " +
                    $"Message={messageModel.Message}, " +
                    $"ScheduledTime={messageModel.ScheduledTime}, " +
                    $"JobId={messageModel.JobId}");

                // Validate input
                if (string.IsNullOrWhiteSpace(messageModel.UserId))
                {
                    throw new ArgumentException("User ID is required.");
                }

                if (string.IsNullOrWhiteSpace(messageModel.Message))
                {
                    throw new ArgumentException("Message is required.");
                }

                // Ensure a valid JobId
                if (messageModel.JobId == Guid.Empty)
                {
                    messageModel.JobId = Guid.NewGuid();
                }

                string scheduledJobId;
                if (messageModel.IsRecurring &&
                    !string.IsNullOrWhiteSpace(messageModel.RecurrencePattern))
                {
                    // Recurring job
                    RecurringJob.AddOrUpdate(
                        messageModel.JobId.ToString(),
                        () => SendScheduledMessageAsync(messageModel),
                        messageModel.RecurrencePattern
                    );
                    scheduledJobId = messageModel.JobId.ToString();
                }
                else
                {
                    // One-time scheduled job
                    scheduledJobId = BackgroundJob.Schedule(
                        () => SendScheduledMessageAsync(messageModel),
                        messageModel.ScheduledTime
                    );
                }

                _logger.LogInformation($"Job scheduled with ID: {scheduledJobId}");

                // Persist scheduled message details
              //  await PersistScheduledMessageAsync(messageModel);

                return messageModel.JobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ScheduleMessageAsync: {ex.Message}");
                throw; // Re-throw to preserve stack trace
            }
        }
        public async Task<bool> CancelScheduledMessageAsync(Guid jobId)
        {
            try
            {
                // Remove recurring job
                RecurringJob.RemoveIfExists(jobId.ToString());

                // Remove one-time scheduled job
                BackgroundJob.Delete(jobId.ToString());

                await MarkMessageAsDone(jobId);

                // Remove from persistent storage
                var messages = await GetScheduledMessagesAsync();
                var message = messages.FirstOrDefault(m => m.JobId == jobId);
                if (message != null)
                {
                   // message.IsDone = true; // Mark as canceled
                   // await PersistScheduledMessageAsync(message);
                   
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling scheduled message: {ex.Message}");
                return false;
            }
        }


        public async Task SendScheduledMessageAsync(ScheduledMessageModel messageModel)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: long.Parse(messageModel.UserId),
                    text: messageModel.Message
                );

                _logger.LogInformation($"Scheduled message sent to {messageModel.UserId} at {DateTime.UtcNow}");

                if (!messageModel.IsRecurring)
                {
                    await MarkMessageAsDone(messageModel.JobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending scheduled message: {ex.Message}");
            }
        }


        private async Task PersistScheduledMessageAsync(ScheduledMessageModel messageModel)
        {
            var messages = await GetScheduledMessagesAsync();

            // Remove existing job if it exists
            messages.RemoveAll(m => m.JobId == messageModel.JobId);

            // Add new job
            messages.Add(messageModel);

            // Save to file
            var filePath = Path.Combine(_environment.ContentRootPath, SCHEDULED_MESSAGES_FILE);
            var jsonContent = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonContent);
        }

        private async Task RemoveScheduledMessageAsync(Guid jobId)
        {
            var messages = await GetScheduledMessagesAsync();
            messages.RemoveAll(m => m.JobId == jobId);

            var filePath = Path.Combine(_environment.ContentRootPath, SCHEDULED_MESSAGES_FILE);
            var jsonContent = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonContent);
        }

        public async Task<List<ScheduledMessageModel>> GetScheduledMessagesAsync()
        {
            var filePath = Path.Combine(_environment.ContentRootPath, SCHEDULED_MESSAGES_FILE);

            if (!File.Exists(filePath))
                return new List<ScheduledMessageModel>();

            var jsonContent = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<ScheduledMessageModel>>(jsonContent)
                ?? new List<ScheduledMessageModel>();
        }
        public async Task<List<ScheduledMessageModel>> GetMessagesByDateAsync(DateTime date)
        {
            var messages = await GetScheduledMessagesAsync();

            return messages
                .Where(m => m.ScheduledTime.Date == date.Date)
                .ToList();
        }

        public async Task MarkMessageAsDone(Guid jobId)
        {
            try
            {
                // Retrieve the scheduled message from the database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var scheduledMessage = await dbContext.ScheduledMessages
                    .FirstOrDefaultAsync(m => m.JobId == jobId);

                if (scheduledMessage != null)
                {
                    scheduledMessage.IsDone = true;
                    dbContext.ScheduledMessages.Update(scheduledMessage);
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Marked message with JobId {jobId} as done in the database.");
                }
                else
                {
                    _logger.LogWarning($"Message with JobId {jobId} not found in the database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking message as done: {ex.Message}");
                throw;
            }
        }


    }
}

using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Services;

namespace WebApplication1.Repository
{
    public class ScheduledMessageRepository : IScheduledMessageRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ITelegramSchedulerService _schedulerService;
        private readonly Func<ITelegramSchedulerService> _schedulerServiceFactory;
        private readonly ILogger<ScheduledMessageRepository> _logger;

        public ScheduledMessageRepository(
            ApplicationDbContext context,
            ITelegramSchedulerService schedulerService,
            
            ILogger<ScheduledMessageRepository> logger)
        {
            _context = context;
            _schedulerService = schedulerService;
            _logger = logger;
        }

        public async Task<Guid> ScheduleMessageAsync(ScheduledMessageModel messageModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(messageModel.UserId))
                {
                    throw new ArgumentException("User ID is required.");
                }

                if (string.IsNullOrWhiteSpace(messageModel.Message))
                {
                    throw new ArgumentException("Message is required.");
                }

                if (messageModel.JobId == Guid.Empty)
                {
                    messageModel.JobId = Guid.NewGuid();
                }

                await _context.ScheduledMessages.AddAsync(messageModel);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message scheduled with JobId: {messageModel.JobId}");

                return messageModel.JobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scheduling message: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Guid>> ScheduleMessagesForUsersAsync(
            string[] userIds,
            string message,
            DateTime scheduledTime,
            bool isRecurring = false,
            string recurrencePattern = null)
        {
            var scheduledJobIds = new List<Guid>();

            try
            {
                foreach (var userId in userIds)
                {
                    var scheduledMessage = new ScheduledMessageModel
                    {
                        JobId = Guid.NewGuid(),
                        UserId = userId,
                        Message = message,
                        ScheduledTime = scheduledTime,
                        IsRecurring = isRecurring,
                        RecurrencePattern = recurrencePattern,
                        IsDone = false
                    };

                    await _context.ScheduledMessages.AddAsync(scheduledMessage);

                    var jobId = await _schedulerService.ScheduleMessageAsync(scheduledMessage);
                    scheduledJobIds.Add(jobId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Scheduled messages for {userIds.Length} users");

                return scheduledJobIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scheduling messages for multiple users: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateScheduledMessageAsync(ScheduledMessageModel scheduledMessage)
        {
            try
            {
                _context.ScheduledMessages.Update(scheduledMessage);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating scheduled message: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CancelScheduledMessageAsync(Guid jobId)
        {
            try
            {
                var message = await _context.ScheduledMessages
                    .FirstOrDefaultAsync(m => m.JobId == jobId);

                if (message == null)
                {
                    _logger.LogWarning($"No message found with JobId: {jobId}");
                    return false;
                }

                message.IsDone = true;
                _context.ScheduledMessages.Update(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message with JobId {jobId} has been cancelled.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling message: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<ScheduledMessageModel>> GetScheduledMessagesAsync()
        {
            try
            {
                return await _context.ScheduledMessages
                        
                            .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving scheduled messages: {ex.Message}");
                return Enumerable.Empty<ScheduledMessageModel>();
            }
        }

        public async Task<IEnumerable<ScheduledMessageModel>> GetMessagesByDateAsync(DateTime date)
        {
            try
            {
                return await _context.ScheduledMessages
                    .Where(m =>
                        m.ScheduledTime.Date == date.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving messages for {date}: {ex.Message}");
                return Enumerable.Empty<ScheduledMessageModel>();
            }
        }
        public async Task<bool> DeleteScheduledMessageAsync(Guid jobId)
        {
            var scheduledMessage = await _context.ScheduledMessages
                .FirstOrDefaultAsync(m => m.JobId == jobId);

            if (scheduledMessage == null)
            {
                return false;
            }

            _context.ScheduledMessages.Remove(scheduledMessage);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> MarkMessageAsDoneAsync(Guid jobId)
        {
            try
            {
                var message = await _context.ScheduledMessages
                    .FirstOrDefaultAsync(m => m.JobId == jobId);

                if (message == null)
                {
                    _logger.LogWarning($"No message found with JobId: {jobId}");
                    return false;
                }

                message.IsDone = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message with JobId {jobId} marked as done.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking message as done: {ex.Message}");
                return false;
            }
        }

        public async Task<ScheduledMessageModel> GetScheduledMessageAsync(Guid jobId)
        {
            return await _context.ScheduledMessages.FindAsync(jobId);
        }

        public async Task AddScheduledMessageAsync(ScheduledMessageModel scheduledMessage)
        {
            await _context.ScheduledMessages.AddAsync(scheduledMessage);
            await _context.SaveChangesAsync();
        }
    }
}
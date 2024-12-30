using WebApplication1.Repository;

namespace WebApplication1.Services
{
    public class MessageCompletionService : IMessageCompletionService
    {
        private readonly IScheduledMessageRepository _scheduledMessageRepository;
        private readonly ILogger<MessageCompletionService> _logger;

        public MessageCompletionService(
            IScheduledMessageRepository scheduledMessageRepository,
            ILogger<MessageCompletionService> logger)
        {
            _scheduledMessageRepository = scheduledMessageRepository;
            _logger = logger;
        }

        public async Task<bool> MarkMessageAsDoneAsync(Guid jobId)
        {
            try
            {
                _logger.LogInformation($"Marking message {jobId} as done");
                return await _scheduledMessageRepository.MarkMessageAsDoneAsync(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message {jobId} as done");
                return false;
            }
        }
    }
}
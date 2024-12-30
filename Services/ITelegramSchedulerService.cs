using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface ITelegramSchedulerService
    {
        Task<Guid> ScheduleMessageAsync(ScheduledMessageModel messageModel);
        Task<bool> CancelScheduledMessageAsync(Guid jobId);
       // Task<bool> MarkMessageAsDoneAsync(Guid jobId);
        Task<List<ScheduledMessageModel>> GetScheduledMessagesAsync();
    }
}

using WebApplication1.Data;
using WebApplication1.Models;
namespace WebApplication1.Repository
{
    public interface IScheduledMessageRepository
    {
        Task<Guid> ScheduleMessageAsync(ScheduledMessageModel scheduledMessage);
        Task<IEnumerable<ScheduledMessageModel>> GetMessagesByDateAsync(DateTime date);
        Task<bool> DeleteScheduledMessageAsync(Guid jobId);
        Task<ScheduledMessageModel> GetScheduledMessageAsync(Guid jobId);
        Task<IEnumerable<ScheduledMessageModel>> GetScheduledMessagesAsync();
        Task AddScheduledMessageAsync(ScheduledMessageModel scheduledMessage);
        Task<bool> CancelScheduledMessageAsync(Guid jobId);
        Task<bool> UpdateScheduledMessageAsync(ScheduledMessageModel scheduledMessage);
        Task<List<Guid>> ScheduleMessagesForUsersAsync(
            string[] userIds, 
            string message, 
            DateTime scheduledTime, 
            bool isRecurring = false, 
            string recurrencePattern = null);
         Task<bool> MarkMessageAsDoneAsync(Guid jobId);
    }
}

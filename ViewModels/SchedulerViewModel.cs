using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class SchedulerViewModel
    {
        public ScheduledMessageModel NewMessage { get; set; } = new ScheduledMessageModel();
        public List<UserSelectItem> AvailableUsers { get; set; } = new List<UserSelectItem>();
        public List<ScheduledMessageModel> ScheduledMessages { get; set; } = new List<ScheduledMessageModel>();
    }
}

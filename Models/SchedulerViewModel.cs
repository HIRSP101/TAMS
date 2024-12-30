namespace WebApplication1.Models
{
    public class SchedulerViewModel
    {
        public ScheduledMessageModel NewMessage { get; set; } = new ScheduledMessageModel();
        public List<UserSelectItem> AvailableUsers { get; set; } = new List<UserSelectItem>();
        public List<ScheduledMessageModel> ScheduledMessages { get; set; } = new List<ScheduledMessageModel>();
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}

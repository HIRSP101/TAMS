namespace WebApplication1.Models
{
    public class MessageViewModel
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public List<UserSelectItem> AvailableUsers { get; set; } = new List<UserSelectItem>();
    }
}

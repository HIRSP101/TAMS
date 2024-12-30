using System;

namespace WebApplication1.Models
{
    public class TelegramUserModel
    {
        public int Id { get; set; }
        public string ChatId { get; set; } // Chat ID is a long to support Telegram IDs
        public string? Username { get; set; } // Nullable since not all users have a username
        public string? FirstName { get; set; } // Nullable since it may not always be provided
        public string? LastName { get; set; } // Nullable for the same reason as above
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow; // Default to current UTC time
        public bool IsBlocked { get; set; } = false; // Default value indicating whether the user is blocked

       // public virtual ICollection<ScheduledMessageModel> ScheduledMessages {get; set;}
    }
}

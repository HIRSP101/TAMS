using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
   public class ScheduledMessageModel
    {
        public Guid JobId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool IsRecurring { get; set; }
        public bool IsDone { get; set; }
        public bool IsCancel { get; set; } 
        public string? RecurrencePattern { get; set; }
        
        //public virtual TelegramUserModel TelegramUsers {get; set;}
    }
}

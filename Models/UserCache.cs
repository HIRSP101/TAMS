namespace WebApplication1.Models
{
    public class UserCache
    {
        public Dictionary<long, CachedUserInfo> Users { get; set; } = new Dictionary<long, CachedUserInfo>();
        public DateTime LastUpdated { get; set; }
    }

    public class CachedUserInfo
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public DateTime LastInteraction { get; set; }
    }
}

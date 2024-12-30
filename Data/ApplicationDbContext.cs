using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet properties for each model
        public DbSet<ScheduledMessageModel> ScheduledMessages { get; set; }
        public DbSet<CachedUserInfo> Users { get; set; }
        public DbSet<TelegramUserModel> TelegramUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ScheduledMessageModel
            modelBuilder.Entity<ScheduledMessageModel>(entity =>
            {
                entity.HasKey(e => e.JobId);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.ScheduledTime).IsRequired();
                entity.Property(e => e.IsDone).IsRequired();
                entity.Property(e => e.IsCancel).IsRequired();
                entity.Property(e => e.IsRecurring).HasMaxLength(100);
                entity.Property(e => e.RecurrencePattern).HasMaxLength(100);
                /*
                entity.HasOne(sm => sm.TelegramUsers)         // ScheduledMessageModel has one TelegramUserModel
                      .WithMany(tu => tu.ScheduledMessages)  // TelegramUserModel has many ScheduledMessageModel
                      .HasForeignKey(sm => sm.UserId)
                      .HasPrincipalKey(tu => tu.ChatId)        // ForeignKey is UserId in ScheduledMessageModel
                      .OnDelete(DeleteBehavior.Cascade);     // Optional: Cascade delete (delete ScheduleMessages when TelegramUser is deleted)
                */
           
            });

            modelBuilder.Entity<CachedUserInfo>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastInteraction).IsRequired();
            });
            modelBuilder.Entity<TelegramUserModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ChatId).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.LastInteraction).IsRequired();
                entity.Property(e => e.IsBlocked).HasDefaultValue(false);
            });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Models;
using WebApplication1.Repository;
using System.Linq;


namespace WebApplication1.Controllers
{
    public class SchedulerController : Controller
    {
        private readonly ITelegramSchedulerService _schedulerService;
        private readonly ITelegramUserService _userService;
        private readonly ITelegramUserRepository _userRepository;
        private readonly ILogger<SchedulerController> _logger;
        private readonly IScheduledMessageRepository _scheduledMessageRepository;

        public SchedulerController(
            ITelegramSchedulerService schedulerService,
            ITelegramUserService userService,
            ITelegramUserRepository userRepository,
            IScheduledMessageRepository scheduledMessageRepository,
            ILogger<SchedulerController> logger)
        {
            _schedulerService = schedulerService;
            _userService = userService;
            _userRepository = userRepository;
            _scheduledMessageRepository = scheduledMessageRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? filterDate)
        {
            var users = await GetAvailableUsersFromDB();
            List<ScheduledMessageModel> scheduledMessages = (await _scheduledMessageRepository.GetScheduledMessagesAsync()).ToList();

            // await SaveUsersToDatabaseAsync(users);
            // Filter by date if filterDate is provided
            if (filterDate.HasValue)
            {
                scheduledMessages = scheduledMessages
                    .Where(m => m.ScheduledTime.Date == filterDate.Value.Date)
                    .ToList();
            }

            var model = new SchedulerViewModel
            {
                AvailableUsers = users,
                ScheduledMessages = scheduledMessages,
                NewMessage = new ScheduledMessageModel()
            };


            return View(model);
        }
        // not used, moved to HomeController
        private async Task SaveUsersToDatabaseAsync(List<UserSelectItem> users)
        {
            var telegramUsers = users.Select(u => new TelegramUserModel
            {
                ChatId = u.UserId,
                FirstName = u.UserDisplay.Split(" ")[0],
                LastName = u.UserDisplay.Split(" ").Length > 1 ? u.UserDisplay.Split(" ")[1] : "",
                Username = "",
                LastInteraction = DateTime.UtcNow,
                IsBlocked = false
            }).ToList();

            await _userRepository.SaveUsersAsync(telegramUsers);
        }
        private async Task<List<UserSelectItem>> GetAvailableUsersFromDB()
        {
            var dbUsers = await _userRepository.GetUsersAsync();

            var users = dbUsers.Select(user => new UserSelectItem
            {
                UserId = user.ChatId.ToString(),
                UserDisplay = $"{user.FirstName} {user.LastName} (@{user.Username})"
            }).ToList();

            return users;
        }

        [HttpPost]
        public async Task<IActionResult> ScheduleMessage(SchedulerViewModel model)
        {
            try
            {

                if (!model.SelectedUserIds.Any())
                {
                    _logger.LogWarning("No users selected");
                    ModelState.AddModelError("SelectedUserIds", "Please select at least one user.");
                    model.AvailableUsers = await GetAvailableUsersFromDB();
                    return View("Index", model);
                }

                if (string.IsNullOrWhiteSpace(model.NewMessage.Message))
                {
                    _logger.LogWarning("Message is empty");
                    ModelState.AddModelError("NewMessage.Message", "Message cannot be empty.");
                    model.AvailableUsers = await GetAvailableUsersFromDB();
                    return View("Index", model);
                }

                if (model.NewMessage.ScheduledTime == default)
                {
                    model.NewMessage.ScheduledTime = DateTime.UtcNow.AddMinutes(5); // Default time
                }

                var scheduledJobIds = new List<Guid>();
                foreach (var userId in model.SelectedUserIds)
                {
                    var scheduledMessage = new ScheduledMessageModel
                    {
                        JobId = Guid.NewGuid(),
                        UserId = userId,
                        Message = model.NewMessage.Message,
                        ScheduledTime = model.NewMessage.ScheduledTime,
                        IsRecurring = model.NewMessage.IsRecurring,
                        RecurrencePattern = model.NewMessage.RecurrencePattern,
                        IsDone = false
                    };

                    await _scheduledMessageRepository.AddScheduledMessageAsync(scheduledMessage);

                    var jobId = await _schedulerService.ScheduleMessageAsync(scheduledMessage);
                    scheduledJobIds.Add(jobId);

                    _logger.LogInformation($"Message scheduled for UserId: {userId}. Job ID: {jobId}");
                }

                TempData["Success"] = $"{scheduledJobIds.Count} message(s) scheduled successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scheduling messages: {ex.Message}");
                TempData["Error"] = $"Error scheduling messages: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteScheduledMessage(Guid jobId)
        {
            try
            {
                var scheduledMessage = await _scheduledMessageRepository.GetScheduledMessageAsync(jobId);

                if (scheduledMessage == null)
                {
                    TempData["Error"] = "Scheduled message not found.";
                    return RedirectToAction("Index");
                }
                /*
                if (scheduledMessage.IsDone)
                {
                    TempData["Error"] = "Cannot delete sent message.";
                    return RedirectToAction("Index");
                }
                */

                // Attempt to cancel the job in Hangfire and remove from the database
                var result = await _schedulerService.CancelScheduledMessageAsync(jobId);
                if (result)
                {
                    await _scheduledMessageRepository.DeleteScheduledMessageAsync(jobId);
                    TempData["Success"] = "Scheduled message deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to delete scheduled message.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting scheduled message with JobId: {jobId}. Exception: {ex.Message}");
                TempData["Error"] = "Error deleting scheduled message.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkMessageAsDoneAsync(Guid jobId)
        {
            try
            {
                _logger.LogInformation($"Canceling scheduled message with Job ID: {jobId}");

                var result = await _scheduledMessageRepository.MarkMessageAsDoneAsync(jobId);

                if (result) {
                    TempData["Success"] = "Scheduled message has been Marked successfully.";
                } else {
                    TempData["Error"] = "Failed to Marked Scheduled message.";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking message as done: {ex.Message}");
                return RedirectToAction("Index");
            }
        }


        [HttpPost]
        public async Task<IActionResult> CancelScheduledMessage(Guid jobId)
        {
            try
            {
                _logger.LogInformation($"Canceling scheduled message with Job ID: {jobId}");

                var result = await _schedulerService.CancelScheduledMessageAsync(jobId);

                if (result)
                {
                    TempData["Success"] = "Scheduled message canceled successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to cancel the scheduled message.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error canceling scheduled message: {ex.Message}");
                TempData["Error"] = $"Error canceling scheduled message: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}

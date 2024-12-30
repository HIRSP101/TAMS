using Hangfire;
using Hangfire.Dashboard;
using Telegram.Bot;
using WebApplication1.Services;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Repository;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
void ConfigureServices(IServiceCollection services)
{
    // Database Context
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // MVC and Caching
    services.AddControllersWithViews();
    services.AddMemoryCache();

    services.AddControllersWithViews(options =>
   {
       options.Filters.Add<TelegramBotConfigurationFilter>();
   });

    // Telegram Bot Configuration
    services.AddScoped<ITelegramUserService, TelegramUserService>();
    services.AddScoped<ITelegramSchedulerService, TelegramSchedulerService>();
    services.AddScoped<ITelegramUserRepository, TelegramUserRepository>();
    services.AddScoped<IScheduledMessageRepository, ScheduledMessageRepository>();
    services.AddHttpClient("telegram")
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            return new TelegramBotClient(
                builder.Configuration["TelegramBot:BotToken"] ??
                throw new InvalidOperationException("Telegram Bot Token is not configured"),
                httpClient
            );
        });
    services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

    services.AddScoped<IMessageCompletionService, MessageCompletionService>();
    services.AddHttpClient<ITelegramBotVerificationService, TelegramBotVerificationService>();

    services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));

    services.AddHangfireServer();

}

ConfigureServices(builder.Services);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
           // constraints: new { isTelegramBotConfigured = true }
        );

// Additional route for initialization if not configured
if (builder.Configuration.IsTelegramBotConfigured())
{
    app.MapControllerRoute(
        name: "initialize",
        pattern: "{controller=Initialize}/{action=Index}/{id?}"
    );
}

app.Run();

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Implement your authorization logic here i.e:
        // 1. Check if user is authenticated
        // 2. Verify user roles
        // 3. Use dependency injection to check permissions
        return false;
    }
}


public class TelegramBotConfigurationFilter : IActionFilter
{
    private readonly IConfiguration _configuration;

    public TelegramBotConfigurationFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Skip check for Initialize controller
        if (context.Controller is InitializeController)
            return;

        // Check if Telegram Bot is configured
        if (!_configuration.IsTelegramBotConfigured())
        {
            // Redirect to Initialize controller if not configured
            context.Result = new RedirectToActionResult("Index", "Initialize", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
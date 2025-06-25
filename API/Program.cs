using API.Data;
using API.Entities;
using API.Extensions;
using API.HangFire;
using API.Middleware;
using API.SignalR;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Remove logging to Event Log and use Console instead
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(); // Logs to console instead of Windows Event Log

            // Add services to the container.

            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddIdentityServices(builder.Configuration);

            //builder.Services.AddScoped<HttpClient>();

            var app = builder.Build();

            //Configure the HTTP request pipleline
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
               .WithOrigins("http://localhost:4200", "https://localhost:4200"));

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:4200");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                }
            });

            app.MapControllers();
            app.MapFallbackToController("Index", "Fallback");
            app.MapHub<PresenceHub>("hubs/presence");

            using var scope = app.Services.CreateScope();

            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate<ReminderJobService>("CheckReminderEvery1Minute", x => x.CheckAndPushReminders(), Cron.Minutely());

            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<DataContext>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
                await context.Database.MigrateAsync();
                await Seed.SeedUsers(userManager, roleManager);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during migration");
            }

            app.Run();
        }
    }
}

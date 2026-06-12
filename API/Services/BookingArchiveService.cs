using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

// Runs once every 24 hours and logs a summary of upcoming bookings.
// Demonstrates the IServiceScopeFactory pattern for background services
// that need scoped dependencies like DbContext.
public class BookingArchiveService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingArchiveService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking archive service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);

            // Task.Delay respects the cancellation token — the service shuts
            // down cleanly when the application stops mid-delay.
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // Background services are singletons. DbContext is scoped.
        // Create a scope for this unit of work and dispose it when done.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var count = await db.Bookings
            .AsNoTracking()
            .CountAsync(b => b.StartTime.Date == tomorrow, cancellationToken);

        logger.LogInformation(
            "Daily summary: {Count} bookings scheduled for tomorrow ({Date:yyyy-MM-dd}).",
            count,
            tomorrow);
    }
}

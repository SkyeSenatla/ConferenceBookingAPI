using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

// Week 2 — SeedData provides a realistic starting dataset for development and demos.
//
// WHEN is this called?
//   At application startup, from Program.cs, inside a DI scope.
//   It runs after MigrateAsync() ensures the schema exists.
//
// HOW does it stay safe to call repeatedly?
//   The first line checks whether the bookings table already has rows.
//   If it does, the method returns immediately — nothing is inserted twice.
//   This pattern is called an idempotent seed: safe to run on every startup.
//
// WHAT happens in production?
//   Seed data is a development convenience. In production you would either:
//   a) Remove this call entirely, or
//   b) Use a separate data migration or admin tool to populate reference data.
public static class SeedData
{
    public static async Task SeedAsync(BookingDbContext db)
    {
        // Guard: if any bookings already exist, skip seeding entirely.
        // This ensures data created through the API is never wiped on restart.
        if (await db.Bookings.AnyAsync())
            return;

        // Six realistic conference bookings spread across two days and three rooms.
        // Times are UTC — consistent with DateTime.UtcNow used throughout the API.
        // The unique constraint on (Room, StartTime) means no two bookings can
        // share the same room and slot — these are intentionally staggered.
        var bookings = new List<Booking>
        {
            new Booking
            {
                Id        = Guid.NewGuid(),
                Title     = ".NET 10 Performance Deep Dive",
                Speaker   = "Jane Doe",
                Room      = "Room A",
                StartTime = DateTime.UtcNow.Date.AddDays(7).AddHours(9)   // Day 1, 09:00
            },
            new Booking
            {
                Id        = Guid.NewGuid(),
                Title     = "Building Microservices with ASP.NET Core",
                Speaker   = "John Smith",
                Room      = "Room B",
                StartTime = DateTime.UtcNow.Date.AddDays(7).AddHours(9)   // Day 1, 09:00 — different room
            },
            new Booking
            {
                Id        = Guid.NewGuid(),
                Title     = "PostgreSQL Performance Tuning",
                Speaker   = "Sarah Johnson",
                Room      = "Room C",
                StartTime = DateTime.UtcNow.Date.AddDays(7).AddHours(9)   // Day 1, 09:00 — different room
            },
            new Booking
            {
                Id        = Guid.NewGuid(),
                Title     = "React 19 and Server Components",
                Speaker   = "Mike Chen",
                Room      = "Room A",
                StartTime = DateTime.UtcNow.Date.AddDays(7).AddHours(11)  // Day 1, 11:00
            },
            new Booking
            {
                Id        = Guid.NewGuid(),
                Title     = "Docker and Kubernetes for .NET Developers",
                Speaker   = "Emily Davis",
                Room      = "Room B",
                StartTime = DateTime.UtcNow.Date.AddDays(8).AddHours(9)   // Day 2, 09:00
            },
            new Booking
            {
                Id        = Guid.NewGuid(),
                Title     = "Clean Architecture in Practice",
                Speaker   = "Chris Wilson",
                Room      = "Room A",
                StartTime = DateTime.UtcNow.Date.AddDays(8).AddHours(9)   // Day 2, 09:00 — different room
            }
        };

        // AddRange stages all six entities in the Change Tracker as Added.
        // A single SaveChangesAsync then executes six INSERT statements
        // wrapped in one transaction — if any fail, none are committed.
        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync();
    }
}

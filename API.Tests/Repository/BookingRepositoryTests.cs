using API.Data;
using API.Models;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.Repository;

// IClassFixture<T> shares the container across all tests in this class.
// The container starts once, all tests run, container is destroyed.
public class BookingRepositoryTests(PostgreSqlContainerFixture fixture) : IClassFixture<PostgreSqlContainerFixture>
{
    private BookingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new BookingDbContext(options);

        // Apply all migrations — this creates the schema exactly as production does.
        // The check constraints, indexes, and computed columns are all created here.
        context.Database.Migrate();

        return context;
    }

    [Fact]
    public async Task GetAllAsync_Page1_ReturnsFirstPageOfResults()
    {
        // Arrange
        await using var context = CreateContext();
        await SeedData.SeedAsync(context);
        var repository = new BookingRepository(context);

        // Act
        var result = await repository.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Data.Count());
        Assert.True(result.TotalCount >= 2);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetAllAsync_Page2_ReturnsDifferentResults()
    {
        // Arrange
        await using var context = CreateContext();
        await SeedData.SeedAsync(context);
        var repository = new BookingRepository(context);

        // Act
        var page1 = await repository.GetAllAsync(page: 1, pageSize: 2);
        var page2 = await repository.GetAllAsync(page: 2, pageSize: 2);

        // Assert — pages must not overlap
        var page1Ids = page1.Data.Select(b => b.Id).ToHashSet();
        var page2Ids = page2.Data.Select(b => b.Id).ToHashSet();

        Assert.Empty(page1Ids.Intersect(page2Ids));
        Assert.True(page2.HasPreviousPage);
    }

    [Fact]
    public async Task GetAllAsync_ResultsAreOrderedByStartTimeAscending()
    {
        // Arrange
        
        await using var context = CreateContext();
        await SeedData.SeedAsync(context);
        var repository = new BookingRepository(context);

        // Act
        var result = await repository.GetAllAsync(page: 1, pageSize: 10);

        // Assert — each booking starts after or at the same time as the previous
        var startTimes = result.Data.Select(b => b.StartTime).ToList();
        for (var i = 1; i < startTimes.Count; i++)
        {
            Assert.True(startTimes[i] >= startTimes[i - 1],
                "Results must be sorted by StartTime ascending for pagination to be deterministic");
        }
    }

    [Fact]
    public async Task CheckConstraint_RejectsBookingWithEndTimeBeforeStartTime()
    {
        // Arrange — this bypasses the service layer entirely.
        // We are proving the database enforces the constraint independently.
        await using var context = CreateContext();
        var room = await SeedRoom(context);

        var invalidBooking = new Booking
        {
            Id = Guid.NewGuid(),
            Title = "Bad Booking",
            RoomId = room.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(1), // end before start
            Type = BookingType.Meeting,
            OrganizerEmail = "test@test.com"
        };

        context.Bookings.Add(invalidBooking);

        // Act & Assert — SaveChangesAsync must throw because the DB constraint fires
        await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());
    }

    private async Task<Room> SeedRoom(BookingDbContext context)
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = $"Room-{Guid.NewGuid():N}",
            Floor = "Level 1",
            Capacity = 10,
            IsAvailable = true
        };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();
        return room;
    }
}

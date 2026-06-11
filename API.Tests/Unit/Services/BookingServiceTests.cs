using API.DTOs;
using API.Models;
using API.Repositories;
using API.Services;
using API.Exceptions;
using NSubstitute;

namespace API.Tests.Unit.Services;

public class BookingServiceTests
{
    // Each test gets a fresh instance of these — no shared state.
    private readonly IBookingRepository _repository;
    private readonly IRoomRepository _roomRepository;
    private readonly BookingService _sut; // System Under Test

    public BookingServiceTests()
    {
        _repository = Substitute.For<IBookingRepository>();
        _roomRepository = Substitute.For<IRoomRepository>();
        _sut = new BookingService(_repository, _roomRepository);
    }

    [Fact]
    public async Task CreateAsync_WhenConflictExists_ThrowsDuplicateBookingException()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Title: "Sprint Review",
            RoomId: Guid.NewGuid(),
            StartTime: DateTime.UtcNow.AddHours(1),
            EndTime: DateTime.UtcNow.AddHours(2),
            Type: BookingType.Meeting,
            OrganizerEmail: "organiser@example.com",
            Description: null);

        _roomRepository.GetByIdAsync(request.RoomId)
            .Returns(new Room { Id = request.RoomId, Name = "Boardroom", Floor = "Level 1", Capacity = 10, IsAvailable = true });

        _repository.HasConflictAsync(request.RoomId, request.StartTime!.Value, request.EndTime!.Value)
            .Returns(true);

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<DuplicateBookingException>(act);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task CreateAsync_WhenEndTimeBeforeStartTime_ThrowsInvalidBookingException()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Title: "Bad Booking",
            RoomId: Guid.NewGuid(),
            StartTime: DateTime.UtcNow.AddHours(2),
            EndTime: DateTime.UtcNow.AddHours(1), // end is before start
            Type: BookingType.Meeting,
            OrganizerEmail: "organiser@example.com",
            Description: null);

        _roomRepository.GetByIdAsync(request.RoomId)
            .Returns(new Room { Id = request.RoomId, Name = "Boardroom", Floor = "Level 1", Capacity = 10, IsAvailable = true });

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<InvalidBookingException>(act);
        await _repository.DidNotReceive().HasConflictAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task PatchAsync_WhenOnlyTitleChanged_DoesNotCallHasConflictAsync()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var existingBooking = new Booking
        {
            Id = bookingId,
            Title = "Original Title",
            RoomId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Type = BookingType.Meeting,
            OrganizerEmail = "organiser@example.com"
        };

        _repository.GetEntityByIdAsync(bookingId).Returns(existingBooking);

        _repository.GetByIdAsync(bookingId).Returns(new BookingDetailResponse(
            bookingId, "Updated Title", null, "Meeting", "Boardroom", "Level 1", 10,
            existingBooking.StartTime, existingBooking.EndTime,
            "organiser@example.com", [], []));

        var request = new UpdateBookingRequest(
            Title: "Updated Title", // only this field is non-null
            Description: null,
            RoomId: null,
            StartTime: null,
            EndTime: null,
            Type: null);

        // Act
        await _sut.PatchAsync(bookingId, request);

        // Assert — times were not touched, so conflict check must not run
        await _repository.DidNotReceive().HasConflictAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task PatchAsync_WhenStartTimeChanged_CallsHasConflictAsync()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var bookingId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var existingBooking = new Booking
        {
            Id = bookingId,
            Title = "Original Title",
            RoomId = roomId,
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2),
            Type = BookingType.Meeting,
            OrganizerEmail = "organiser@example.com"
        };

        _repository.GetEntityByIdAsync(bookingId).Returns(existingBooking);

        _repository.HasConflictAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>())
            .Returns(false);

        _repository.GetByIdAsync(bookingId).Returns(new BookingDetailResponse(
            bookingId, "Original Title", null, "Meeting", "Boardroom", "Level 1", 10,
            now.AddMinutes(90), now.AddHours(2),
            "organiser@example.com", [], []));

        var request = new UpdateBookingRequest(
            Title: null,
            Description: null,
            RoomId: null,
            StartTime: now.AddMinutes(90), // only this is non-null
            EndTime: null,
            Type: null);

        // Act
        await _sut.PatchAsync(bookingId, request);

        // Assert — start time was changed, so conflict check must have run
        await _repository.Received(1).HasConflictAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>());
    }
}

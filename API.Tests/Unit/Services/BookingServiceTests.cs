using API.DTOs;
using API.Models;
using API.Repositories;
using API.Services;
using API.Exceptions;
using NSubstitute;
using System.Runtime.CompilerServices;

namespace API.Tests.Unit.Services;

public class BookingServiceTests
{
    private readonly IBookingRepository _repository;
    private readonly IRoomRepository _roomservice;
    private readonly BookingService _sut; // System Under Test

    public BookingServiceTests()
    {
        _repository = Substitute.For<IBookingRepository>();
        _roomservice = Substitute.For<IRoomRepository>();
        _sut = new BookingService(_repository, _roomservice);

    }

    [Fact]
    public async Task CreateAsync_WhenConflictExists_ThrowsDuplicateBookingException()
    {
        // Arrange 
        var request = new CreateBookingRequest(
          Title: "Sptint review",
          RoomId: Guid.NewGuid(),
          StartTime: DateTime.UtcNow.AddHours(1),
          EndTime: DateTime.UtcNow.AddHours(2),
          Type: BookingType.Meeting,
          OrganizerEmail: "organiser@example.com",
          Description: null);

        var mockRoom = new Room
        {
            Id = request.RoomId,
            Name = "Boredroom",
            Floor = "2nd",
            Capacity = 10,
            IsAvailable = true
        };
        _roomservice.GetByIdAsync(request.RoomId).Returns(mockRoom);
        _repository.HasConflictAsync(request.RoomId, request.StartTime.Value, request.EndTime.Value).Returns(true);

        //Act 
        var act = () => _sut.CreateAsync(request);

        //Assert

        await Assert.ThrowsAnyAsync<DuplicateBookingException>(act);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Booking>());




    }
    [Fact]
    public async Task CreateAsync_WhenEndTimeBeforeStart_ThrowsInvalidBookingException()
    {
        // Arrange 
        var request = new CreateBookingRequest(
          Title: "Sptint review",
          RoomId: Guid.NewGuid(),
          StartTime: DateTime.UtcNow.AddHours(2),
          EndTime: DateTime.UtcNow.AddHours(1),
          Type: BookingType.Meeting,
          OrganizerEmail: "organiser@example.com",
          Description: null);

        var mockRoom = new Room
        {
            Id = request.RoomId,
            Name = "Boredroom",
            Floor = "2nd",
            Capacity = 10,
            IsAvailable = true
        };
        _roomservice.GetByIdAsync(request.RoomId).Returns(mockRoom);
        _repository.HasConflictAsync(request.RoomId, request.StartTime.Value, request.EndTime.Value).Returns(true);

        //Act 
        var act = () => _sut.CreateAsync(request);

        //Assert

        await Assert.ThrowsAnyAsync<InvalidBookingException>(act);
        await _repository.DidNotReceive().HasConflictAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<Booking>());

    }

    
     /*[Fact]

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

        // GetByIdAsync is called at the end of PatchAsync to return the updated response 

      Fix this error. 
       _repository.GetByIdAsync(bookingId).Returns(new BookingDetailResponse(
            bookingId, "Updated Title", null, "Meeting", "Boardroom", 1, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2),"organiser@example.com", 10, [], [])); 

       ?/ var request = new UpdateBookingRequest(
            Title: "Updated Title",        // only this field is non-null 
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

    [Fact] *
    /*
    Home work for trainees.
    public async Task PatchAsync_WhenStartTimeChanged_CallsHasConflicAsync()
    {
        
    } */
}
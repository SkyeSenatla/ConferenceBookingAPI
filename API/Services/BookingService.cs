using API.Repositories;
using API.DTOs;
using API.Exceptions;
using API.Models;

namespace API.Services;

public class BookingService(IBookingRepository bookingRepository, IRoomRepository roomRepository) : IBookingService
{
    /* Before Pagination
    public Task<IEnumerable<BookingResponse>> GetAllAsync() =>
       bookingRepository.GetAllAsync();
*/
    //After Pagination

    public Task<PagedResponse<BookingResponse>> GetAllAsync(int page = 1, int
        pageSize = 20) =>
        bookingRepository.GetAllAsync(page, pageSize);
    public Task<IEnumerable<BookingResponse>> SearchAsync(BookingSearchQuery query) =>
        bookingRepository.SearchAsync(query);

    public async Task<BookingDetailResponse> GetByIdAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdAsync(id);
        if (booking is null)
            throw new BookingNotFoundException(id);

        return booking;
    }

    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request)
    {
        var room = await roomRepository.GetByIdAsync(request.RoomId)
            ?? throw new RoomNotFoundException(request.RoomId);

        // End time must come after start time — checked before hitting the database.
        if (request.EndTime!.Value <= request.StartTime!.Value)
            throw new InvalidBookingException("End time must be after start time.");

        if (await bookingRepository.HasConflictAsync(
                request.RoomId, request.StartTime.Value, request.EndTime.Value))
            throw new DuplicateBookingException(
                room.Name, request.StartTime.Value, request.EndTime.Value);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            StartTime = request.StartTime.Value,
            EndTime = request.EndTime.Value,
            Type = request.Type,
            OrganizerEmail = request.OrganizerEmail,
            RoomId = request.RoomId
        };

        await bookingRepository.AddAsync(booking);

        // Re-fetch as a detail response then map down to the summary shape.
        // ToSummary() is defined in DTOs/MappingExtensions.cs.
        return (await bookingRepository.GetByIdAsync(booking.Id))!.ToSummary();
    }

    public async Task<BookingResponse> UpdateAsync(Guid id, CreateBookingRequest request)
    {
        // GetEntityByIdAsync returns a tracked Booking so the Change Tracker
        // can detect property mutations and generate the correct UPDATE statement.
        var booking = await bookingRepository.GetEntityByIdAsync(id)
            ?? throw new BookingNotFoundException(id);

        var room = await roomRepository.GetByIdAsync(request.RoomId)
            ?? throw new RoomNotFoundException(request.RoomId);

        if (request.EndTime!.Value <= request.StartTime!.Value)
            throw new InvalidBookingException("End time must be after start time.");

        // Exclude the booking being updated from the conflict check — a booking
        // never conflicts with its own existing time slot.
        if (await bookingRepository.HasConflictAsync(
                request.RoomId, request.StartTime.Value, request.EndTime.Value,
                excludeBookingId: id))
            throw new DuplicateBookingException(
                room.Name, request.StartTime.Value, request.EndTime.Value);

        booking.Title = request.Title;
        booking.Description = request.Description;
        booking.StartTime = request.StartTime.Value;
        booking.EndTime = request.EndTime.Value;
        booking.Type = request.Type;
        booking.OrganizerEmail = request.OrganizerEmail;
        booking.RoomId = request.RoomId;

        await bookingRepository.UpdateAsync(booking);

        return (await bookingRepository.GetByIdAsync(id))!.ToSummary();
    }

    public async Task<BookingResponse> PatchAsync(Guid id, UpdateBookingRequest request)
    {
        var booking = await bookingRepository.GetEntityByIdAsync(id)
            ?? throw new BookingNotFoundException(id);

        // Only apply fields the client explicitly sent.
        // Null means "do not touch this field".
        if (request.Title       is not null) booking.Title       = request.Title;
        if (request.Description is not null) booking.Description = request.Description;
        if (request.RoomId      is not null) booking.RoomId      = request.RoomId.Value;
        if (request.StartTime   is not null) booking.StartTime   = request.StartTime.Value;
        if (request.EndTime     is not null) booking.EndTime     = request.EndTime.Value;
        if (request.Type        is not null) booking.Type        = request.Type.Value;

        // Re-validate only if the times were touched.
        if (request.StartTime is not null || request.EndTime is not null)
        {
            if (booking.EndTime <= booking.StartTime)
                throw new InvalidBookingException("End time must be after start time.");

            var hasConflict = await bookingRepository.HasConflictAsync(
                booking.RoomId, booking.StartTime, booking.EndTime, excludeBookingId: id);
            if (hasConflict)
                throw new DuplicateBookingException(booking.RoomId.ToString(), booking.StartTime, booking.EndTime);
        }

        await bookingRepository.UpdateAsync(booking);

        return (await bookingRepository.GetByIdAsync(id))!.ToSummary();
    }

    public async Task DeleteAsync(Guid id)
    {
        var booking = await bookingRepository.GetEntityByIdAsync(id)
            ?? throw new BookingNotFoundException(id);

        await bookingRepository.DeleteAsync(booking);
    }
}

using API.DTOs;
using API.Models;

namespace API.Repositories;

public interface IBookingRepository
{
    //Before Pagination
    //Task<IEnumerable<BookingResponse>> GetAllAsync();\
    //With Pagination 
    Task<PagedResponse<BookingResponse>> GetAllAsync( int page, int pageSize); 
    Task<BookingDetailResponse?> GetByIdAsync(Guid id);
    // Returns the tracked domain entity — used by UpdateAsync and DeleteAsync in the service
    // so the Change Tracker can detect mutations and generate the correct SQL.
    Task<Booking?> GetEntityByIdAsync(Guid id);
    Task<IEnumerable<BookingResponse>> SearchAsync(BookingSearchQuery bookingSearchQuery);
    Task<bool> HasConflictAsync(
        Guid roomId,
        DateTime start,
        DateTime end,
        Guid? excludeBookingId = null);
    Task<IEnumerable<BookingResponse>> FullTextSearchAsync(string searchTerm);
    Task<IEnumerable<RoomUtilisationResponse>> GetRoomUtilisationAsync(DateTime from, DateTime to);
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task DeleteAsync(Booking booking);
}
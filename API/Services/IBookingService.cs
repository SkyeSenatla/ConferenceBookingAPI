using API.DTOs;

namespace API.Services;

public interface IBookingService
{
    //Before Pagination
    //Task<IEnumerable<BookingResponse>> GetAllAsync();
    Task<PagedResponse<BookingResponse>> GetAllAsync (int page =1, int pageSize=20);
    Task<BookingDetailResponse>
    GetByIdAsync(Guid id);
    Task<IEnumerable<BookingResponse>> SearchAsync(BookingSearchQuery query);
   
    Task<BookingResponse> CreateAsync( CreateBookingRequest request);
    Task<BookingResponse> UpdateAsync(Guid id,  CreateBookingRequest request);
    Task<BookingResponse> PatchAsync(Guid id, UpdateBookingRequest request);
    Task DeleteAsync(Guid id);
}
using API.Models; 
namespace API.DTOs; 

public record UpdateBookingRequest( 
string? Title, 
string?Description,
Guid? RoomId, 
DateTime?    StartTime, 
DateTime?    EndTime, 
BookingType? Type);
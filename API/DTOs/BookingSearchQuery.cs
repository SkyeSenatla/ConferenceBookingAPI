using API.Models;

namespace API.DTOs;

 /* Before sorting
 public record BookingSearchQuery(
    string?      RoomName,
    BookingType? Type,
    DateTime?    From,
    DateTime?    To,
    string?      Q
); */ 

public record BookingSearchQuery( 
    string? RoomName, 
    BookingType? Type, 
    DateTime? From, 
    DateTime? To, 
    string? Q, 
    string? Sort = "startTime", 
    string? Dir  = "asc"
    
    );
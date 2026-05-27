namespace API.Models;

// A record is ideal for API response shapes:
// - Immutable by default (data travels, it doesn't mutate)
// - Value equality built-in (two Bookings with same data are equal)
// - Concise positional syntax
public record Booking(
    Guid Id,
    string Title,
    string Speaker,
    string Room,
    DateTime StartTime
);

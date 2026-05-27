using API.Models;

namespace API.Data;

// Static in-memory store — stands in for a database today only.
// The GUIDs are generated once when the class first loads.
// Week 2: this entire file disappears and is replaced by EF Core + PostgreSQL.
public static class BookingStore
{
    public static readonly List<Booking> Bookings =
    [
        new Booking(
            Guid.NewGuid(),
            ".NET 10 Performance Deep Dive",
            "Jane Doe",
            "Room A",
            DateTime.UtcNow.AddDays(5)),
        new Booking(
            Guid.NewGuid(),
            "Async/Await: Handling code in parallel",
            "John Smith",
            "Room B",
            DateTime.UtcNow.AddDays(5).AddHours(2))
    ];
}

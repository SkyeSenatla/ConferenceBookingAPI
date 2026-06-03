using API.Models;

namespace API.Data;

// Week 2 — THIS FILE IS NOW OBSOLETE.
// It has been replaced by BookingDbContext + PostgreSQL.
// Kept here only for reference during the Week 1 → Week 2 transition demo.
// It will be deleted at the end of Week 2, Day 1.
//
// The GetBookings_Untyped endpoint in BookingsController still references this
// temporarily as a pattern-comparison teaching tool (IActionResult vs ActionResult<T>).
// That endpoint will also be removed once the pattern comparison is no longer needed.
public static class BookingStore
{
    public static readonly List<Booking> Bookings =
    [
        new Booking
        {
            Id        = Guid.NewGuid(),
            Title     = ".NET 10 Performance Deep Dive",
            Speaker   = "Jane Doe",
            Room      = "Room A",
            StartTime = DateTime.UtcNow.AddDays(5)
        },
        new Booking
        {
            Id        = Guid.NewGuid(),
            Title     = "Async/Await: Handling code in parallel",
            Speaker   = "John Smith",
            Room      = "Room B",
            StartTime = DateTime.UtcNow.AddDays(5).AddHours(2)
        }
    ];
}

namespace API.Models;

// Week 2 — Converted from a positional record to a mutable class.
// EF Core's Change Tracker needs to set individual properties when it hydrates
// entities from the database and detects mutations between snapshots.
public class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Optional context about the meeting — agenda notes, maintenance description, etc.
    public string? Description { get; set; }

    public DateTime StartTime { get; set; }

    // A booking must have a defined end so room availability can be calculated.
    // Without EndTime the system cannot detect overlapping bookings (e.g. 09:00–11:00 vs 10:00–12:00).
    public DateTime EndTime { get; set; }

    // What the room is being used for.
    // Facilities Managers primarily create Maintenance bookings.
    // Receptionists create Meeting and ClientPresentation bookings for visitors.
    public BookingType Type { get; set; }

    // The email of the person who created this booking, stamped from the JWT sub claim
    // at creation time. We do not have a Users table yet — Week 3 introduces ASP.NET Core Identity.
    public string OrganizerEmail { get; set; } = string.Empty;

    // RoomId is the foreign key column EF Core stores in the bookings table.
    // Room is the navigation property — EF Core populates it only when Include() is used.
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // Many-to-many via explicit join entity.
    // REMOVED: Speaker — not applicable to a corporate meeting room booking system.
    public ICollection<BookingAttendee> Attendees { get; set; } = [];
}

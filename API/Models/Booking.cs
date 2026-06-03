namespace API.Models;

// Week 2 — Converted from a positional record to a mutable class.
// EF Core's Change Tracker needs to be able to set individual properties
// when it hydrates entities from the database and when it detects mutations.
// A record's init-only properties prevent this.
// The parameterized constructor has been removed — object initializer syntax
// (new Booking { Id = ..., Title = ... }) is used throughout instead.
public class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}

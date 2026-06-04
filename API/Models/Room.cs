namespace API.Models;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;     // e.g. "Board Room", "Meeting Room 3"
    public int Capacity { get; set; }                    // Maximum number of people
    public string Floor { get; set; } = string.Empty;   // e.g. "Ground Floor", "Level 2"

    // When false the room is closed — under renovation or long-term maintenance.
    // The Facilities Manager sets this flag when scheduling extended works.
    // Day-to-day maintenance is represented by a Booking with Type = Maintenance,
    // which appears in the schedule so everyone can see the room is occupied.
    // IsAvailable = false completely removes the room from search results.
    public bool IsAvailable { get; set; } = true;

    // Navigation properties — EF Core populates these only when asked via Include().
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<RoomEquipment> Equipment { get; set; } = [];
}

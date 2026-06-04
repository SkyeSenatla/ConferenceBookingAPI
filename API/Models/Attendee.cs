namespace API.Models;

public class Attendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // IsExternal = true means this person is a client or visitor, not an employee. 
    // The receptionist uses this flag to prepare sign-in forms and visitor badges. 
    public bool IsExternal { get; set; }
    public ICollection<BookingAttendee> Bookings { get; set; } = [];
}
// API/Models/BookingAttendee.cs 
namespace API.Models;
// Join entity for the Booking ↔ Attendee many-to-many relationship. 
// Modelled explicitly because it carries InvitedAt — the receptionist 
// needs to know when each person was added so they can manage the guest list 
// in arrival order. A hidden join table cannot hold this column. 
public class BookingAttendee
{ 

   public Guid BookingId { get; set; }
    public Guid AttendeeId { get; set; }
    // When this person was added to the meeting invite. 
    public DateTime InvitedAt { get; set; }
    public Booking Booking { get; set; } = null!;
    public Attendee Attendee { get; set; } = null!; 
}
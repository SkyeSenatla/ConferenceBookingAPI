namespace API.Exceptions;

// Thrown by the controller when a new booking's time slot overlaps with an existing booking
// in the same room. The GlobalExceptionHandler maps this to HTTP 409 Conflict.
public class DuplicateBookingException : Exception
{
    public DuplicateBookingException(string roomName, DateTime startTime, DateTime endTime)
        : base($"Room '{roomName}' is already booked between {startTime:HH:mm} and {endTime:HH:mm} on {startTime:yyyy-MM-dd}")
    {
    }
}

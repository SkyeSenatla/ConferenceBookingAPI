namespace API.Models;

// Defines what a room is being reserved for.
// Stored as a string in the database (configured in DbContext) so the data is
// human-readable in PostgreSQL without needing a lookup table.
// "Meeting" in the database is immediately understandable; integer 0 requires
// a lookup table to interpret and is a support nightmare in raw SQL logs.
public enum BookingType
{
    Meeting,
    ClientPresentation,
    Training,
    Maintenance,
    TeamEvent
}

namespace API.DTOs;

// Response DTO — what the API returns to clients.
// Records are immutable and serialise cleanly to JSON.
// Using a DTO rather than returning the Booking entity directly means:
//   1. The API shape is decoupled from the database schema.
//   2. Sensitive or irrelevant columns (FK Guids, navigation objects) are hidden.
//   3. We can reshape data (e.g. enum to string, count subquery) without changing the model.
public record BookingResponse(
    Guid     Id,
    string   Title,
    // Enum stored as an integer in memory but returned as a readable string in JSON.
    string   Type,
    // Room name and floor from the joined rooms table — no FK Guid exposed to clients.
    string   RoomName,
    string   Floor,
    DateTime StartTime,
    DateTime EndTime,
    string   OrganizerEmail,
    // Derived from a COUNT(*) subquery — no attendee rows are loaded into memory for list endpoints.
    int      AttendeeCount,
    // Names of external visitors only — the receptionist uses this to prepare badges.
    // Filtered in the database via a WHERE clause on Attendee.IsExternal.
    List<string> ExternalAttendees
);

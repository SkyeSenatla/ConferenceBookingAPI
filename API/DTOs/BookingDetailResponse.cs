namespace API.DTOs;

// Detail DTO for GET /api/bookings/{id}.
// Returns the full object graph: room equipment and all attendees.
// Used when a single booking's complete context is needed (e.g. receptionist's meeting view).
public record BookingDetailResponse(
    Guid                       Id,
    string                     Title,
    string?                    Description,
    string                     Type,
    string                     RoomName,
    string                     Floor,
    int                        Capacity,
    DateTime                   StartTime,
    DateTime                   EndTime,
    string                     OrganizerEmail,
    // Equipment installed in the room — loaded via ThenInclude(r => r.Equipment)
    List<RoomEquipmentResponse> Equipment,
    // All attendees on the invite, including external visitors
    List<AttendeeResponse>      Attendees
);

// Represents one equipment item installed in a room, with its quantity.
public record RoomEquipmentResponse(
    string Name,
    string Description,
    int    Quantity
);

// Represents one attendee on a booking invite.
// IsExternal = true signals the receptionist to prepare a visitor badge.
public record AttendeeResponse(
    string   Name,
    string   Email,
    bool     IsExternal,
    DateTime InvitedAt
);

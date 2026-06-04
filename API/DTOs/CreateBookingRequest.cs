using System.ComponentModel.DataAnnotations;
using API.Models;

namespace API.DTOs;

// Request DTO — what the client sends to the API to create or update a booking.
// Validation attributes run before the controller action body executes.
// ModelState is checked automatically by [ApiController] — invalid requests return 400.
public record CreateBookingRequest(
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    string Title,

    // The room is now a typed FK reference, not a free-text string.
    // The client must supply a valid room GUID obtained from GET /api/rooms.
    [Required(ErrorMessage = "RoomId is required")]
    Guid RoomId,

    [Required(ErrorMessage = "Start time is required")]
    DateTime? StartTime,

    [Required(ErrorMessage = "End time is required")]
    DateTime? EndTime,

    // What the room is being used for — validated against the BookingType enum by the model binder.
    BookingType Type,

    // The organiser's email is stamped at creation time.
    // In Week 3 this will be read from the JWT sub claim automatically.
    [Required(ErrorMessage = "Organizer email is required")]
    [EmailAddress(ErrorMessage = "Organizer email must be a valid email address")]
    string OrganizerEmail,

    // Optional agenda notes, maintenance description, etc.
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    string? Description
);

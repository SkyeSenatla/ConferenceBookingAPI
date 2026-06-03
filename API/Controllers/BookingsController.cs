using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.DTOs;
using API.Exceptions;
using API.Models;

namespace API.Controllers;

// Role definitions for this system:
//   Employee          — can view and create bookings
//   Receptionist      — can view, create, and update bookings
//   FacilitiesManager — can view and update bookings
//   Admin             — full access, including delete

[ApiController]
[Route("api/[controller]")]
// Week 2 — BookingDbContext is injected via primary constructor.
// The DI container provides one DbContext per request (Scoped lifetime).
// The static BookingStore has been removed — all reads and writes go to PostgreSQL.
public class BookingsController(BookingDbContext db) : ControllerBase
{
    // ── PATTERN A vs PATTERN B comparison ────────────────────────────────
    // Kept here as a teaching reference — shows why ActionResult<T> is preferred.
    // This endpoint still references the now-obsolete BookingStore purely for
    // the demo illustration. It will be removed once the pattern point has been made.

    // PATTERN A: IActionResult — flexible but OpenAPI cannot infer the response shape.
    [HttpGet("v-iactionresult")]
    public async Task<IActionResult> GetBookings_Untyped()
    {
        // Uses the database — BookingStore reference removed.
        var bookings = await db.Bookings.ToListAsync();
        return Ok(bookings); // ← OpenAPI still shows response body as "any" with IActionResult
    }

    // PATTERN B: ActionResult<T> — OpenAPI knows the exact response shape.
    // This is the correct pattern for all endpoints in this course.

    // GET: /api/bookings
    // Anonymous — the conference schedule is public. No token required.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Booking>>> GetBookingsAsync()
    {
        // Week 2 — ToListAsync() executes: SELECT * FROM bookings
        // The Change Tracker loads all rows into memory for this request.
        var bookings = await db.Bookings.ToListAsync();
        return Ok(bookings);
    }

    // GET: /api/bookings/{id}
    // The ":guid" constraint means ASP.NET Core only matches this route if
    // {id} is a valid GUID. /api/bookings/abc → 400 before this code runs.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Booking>> GetBookingByIdAsync(Guid id)
    {
        // Week 2 — FindAsync checks the Change Tracker first (already-loaded entities),
        // then hits the database. Generates: SELECT * FROM bookings WHERE id = @id LIMIT 1
        var booking = await db.Bookings.FindAsync(id);

        if (booking is null)
            throw new BookingNotFoundException(id); // 404 via GlobalExceptionHandler

        return Ok(booking);
    }

    // POST: /api/bookings
    // Employees, Receptionists, and Admins can create bookings.
    // Facilities Managers manage existing room assignments — they do not create new ones.
    [Authorize(Roles = "Employee,Receptionist,Admin")]
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBookingAsync(
        [FromBody] CreateBookingRequest request)
    {
        // Week 2 — AnyAsync generates EXISTS(SELECT ...) — stops at the first match.
        // More efficient than loading the entity just to check if it exists.
        bool isDuplicate = await db.Bookings.AnyAsync(b =>
            b.Room == request.Room && b.StartTime == request.StartTime);

        if (isDuplicate)
            throw new DuplicateBookingException(request.Room, request.StartTime!.Value); // 409

        // Object initializer syntax — no positional constructor needed.
        // The application generates the ID; the database never controls this.
        var newBooking = new Booking
        {
            Id        = Guid.NewGuid(),
            Title     = request.Title,
            Speaker   = request.Speaker,
            Room      = request.Room,
            StartTime = request.StartTime!.Value
        };

        // Add() registers the entity with the Change Tracker as Added (synchronous).
        // SaveChangesAsync() then executes: INSERT INTO bookings (...)
        db.Bookings.Add(newBooking);
        await db.SaveChangesAsync();

        var response = new BookingResponse(
            newBooking.Id,
            newBooking.Title,
            newBooking.Speaker,
            newBooking.Room,
            newBooking.StartTime
        );

        // 201 Created + Location header pointing to GET /api/bookings/{id}
        return CreatedAtAction(nameof(GetBookingByIdAsync), new { id = response.id }, response);
    }

    // PUT: /api/bookings/{id}
    // Receptionists update on behalf of attendees.
    // Facilities Managers can reassign or adjust room details.
    // Admins have full access. Employees must contact the front desk to modify.
    [Authorize(Roles = "Receptionist,FacilitiesManager,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> UpdateBookingAsync(
        Guid id,
        [FromBody] CreateBookingRequest request)
    {
        // Week 2 — Load the entity so the Change Tracker can snapshot it.
        var booking = await db.Bookings.FindAsync(id);

        if (booking is null)
            return NotFound();

        // Mutate properties directly — the Change Tracker detects these changes
        // by comparing the current state against the snapshot it took on FindAsync.
        booking.Title     = request.Title;
        booking.Speaker   = request.Speaker;
        booking.Room      = request.Room;
        booking.StartTime = request.StartTime!.Value;

        // SaveChangesAsync compares against the snapshot and generates:
        // UPDATE bookings SET title=@t, speaker=@s, room=@r, start_time=@dt WHERE id=@id
        await db.SaveChangesAsync();

        var response = new BookingResponse(
            booking.Id,
            booking.Title,
            booking.Speaker,
            booking.Room,
            booking.StartTime
        );

        return Ok(response);
    }

    // DELETE: /api/bookings/{id}
    // Admin only — deletion is irreversible; no other role can perform it.
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteBookingAsync(Guid id)
    {
        var booking = await db.Bookings.FindAsync(id);

        if (booking is null)
            throw new BookingNotFoundException(id); // 404 via GlobalExceptionHandler

        // Remove() marks the entity as Deleted in the Change Tracker (synchronous).
        // SaveChangesAsync() then executes: DELETE FROM bookings WHERE id = @id
        db.Bookings.Remove(booking);
        await db.SaveChangesAsync();

        return NoContent(); // 204 — operation succeeded, nothing to return
    }
}

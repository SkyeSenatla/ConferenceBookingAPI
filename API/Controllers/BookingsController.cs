using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;
using API.DTOs;
using API.Models;
using API.Services;

namespace API.Controllers;

// Role definitions for this system:
//   Employee          — can view and create bookings
//   Receptionist      — can view, create, and update bookings (manages visitor registrations)
//   FacilitiesManager — can view, create, and update bookings (manages maintenance windows)
//   Admin             — full access, including delete

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")] // backward-compatible — GET /api/bookings treated as v1 by AssumeDefaultVersionWhenUnspecified
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    // ── IActionResult vs ActionResult<T> comparison (teaching reference) ──────
    // PATTERN A: IActionResult — flexible but OpenAPI cannot infer the response shape.
    [HttpGet("v-iactionresult")]
    public async Task<IActionResult> GetBookings_Untyped()
    {
        var bookings = await bookingService.GetAllAsync();
        return Ok(bookings); // OpenAPI shows response body as "any" — not helpful for client generation
    }

    // PATTERN B: ActionResult<T> — OpenAPI knows the exact response shape. Use this everywhere.

    // ── GET /api/v1/bookings ──────────────────────────────────────────────────
    // Anonymous — the conference schedule is public. No token required.
    // Returns a pagination envelope; defaults to page 1, 20 items per page.
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingResponse>>> GetBookingsAsync(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await bookingService.GetAllAsync(page, pageSize);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result);
    }

    // ── GET /api/v1/bookings/search ───────────────────────────────────────────
    // Optional filters for the receptionist's schedule view.
    // Tighter rate limit (20/min sliding window) because full-text queries are expensive.
    [HttpGet("search")]
    [EnableRateLimiting("search")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> SearchBookingsAsync(
        [FromQuery] string?      roomName,
        [FromQuery] BookingType? type,
        [FromQuery] DateTime?    from,
        [FromQuery] DateTime?    to,
        [FromQuery] string?      q,
        [FromQuery] string?      sort,
        [FromQuery] string?      dir) =>
        Ok(await bookingService.SearchAsync(new BookingSearchQuery(roomName, type, from, to, q, sort, dir)));

    // ── GET /api/v1/bookings/{id} ─────────────────────────────────────────────
    // Returns full booking detail including room equipment and all attendees.
    // Includes an ETag fingerprint so clients can skip re-fetching unchanged data.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBookingByIdAsync(Guid id)
    {
        var booking = await bookingService.GetByIdAsync(id);

        // Fingerprint from the fields most likely to change.
        // If times or title change, the ETag changes and the client re-fetches.
        var etag = $"\"{booking.Id}-{booking.StartTime.Ticks}-{booking.EndTime.Ticks}\"";

        if (Request.Headers.IfNoneMatch == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        return Ok(booking);
    }

    // ── POST /api/v1/bookings ─────────────────────────────────────────────────
    // Employees, Receptionists, FacilitiesManagers, and Admins can create bookings.
    [Authorize(Roles = "Employee,Receptionist,FacilitiesManager,Admin")]
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBookingAsync(
        [FromBody] CreateBookingRequest request)
    {
        var response = await bookingService.CreateAsync(request);
        return CreatedAtAction(nameof(GetBookingByIdAsync), new { id = response.Id }, response);
    }

    // ── PUT /api/v1/bookings/{id} ─────────────────────────────────────────────
    // Full replacement — all fields required.
    // Receptionists update on behalf of attendees; FacilitiesManagers reschedule maintenance.
    [Authorize(Roles = "Receptionist,FacilitiesManager,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> UpdateBookingAsync(
        Guid id,
        [FromBody] CreateBookingRequest request) =>
        Ok(await bookingService.UpdateAsync(id, request));

    // ── PATCH /api/v1/bookings/{id} ───────────────────────────────────────────
    // Partial update — only fields present in the request body are changed.
    // Null fields are left untouched in the database.
    [Authorize(Roles = "Receptionist,FacilitiesManager,Admin")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> PatchBookingAsync(
        Guid id,
        [FromBody] UpdateBookingRequest request) =>
        Ok(await bookingService.PatchAsync(id, request));

    // ── DELETE /api/v1/bookings/{id} ──────────────────────────────────────────
    // Admin only — deletion is irreversible.
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteBookingAsync(Guid id)
    {
        await bookingService.DeleteAsync(id);
        return NoContent(); // 204 — operation succeeded, no body returned
    }
}

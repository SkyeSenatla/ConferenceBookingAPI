using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    // ── PATTERN A: IActionResult ──────────────────────────────────────
    // Flexible — can return any response type.
    // PROBLEM: OpenAPI cannot infer what data shape is returned.
    // The generated documentation will show an unknown response body.
    [HttpGet("v-iactionresult")]
    public async Task<IActionResult> GetBookings_Untyped()
    {
        await Task.Delay(100);
        return Ok(BookingStore.Bookings); // ← OpenAPI: response body is "any"
    }

    // ── PATTERN B: ActionResult<T> ────────────────────────────────────
    // Typed — wraps both the T and allows returning IActionResult results.
    // OpenAPI knows the exact shape of the success response.
    // This is the correct pattern for all endpoints in this course.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Booking>>> GetBookingsAsync()
    {
        // await does NOT block the thread.
        // It says: "pause this method, return the thread to the pool,
        // and resume here when the I/O finishes."
        await Task.Delay(200); // stands in for: await _db.Bookings.ToListAsync()
        return Ok(BookingStore.Bookings); // HTTP 200 OK — Body: JSON array of Booking objects
    }

    // GET: /api/bookings/{id}
    // The ":guid" constraint means ASP.NET Core will ONLY match this route
    // if the {id} segment is a valid GUID format.
    // /api/bookings/abc         → 400 Bad Request (rejected by framework, never hits our code)
    // /api/bookings/00000000-...→ Hits our code, we handle 404 if not found
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Booking>> GetBookingByIdAsync(Guid id)
    {
        await Task.Delay(50);

        var booking = BookingStore.Bookings.FirstOrDefault(b => b.Id == id);

        // Guard clause: if the resource does not exist, say so explicitly.
        // Never return null. Never return a 200 with an empty body.
        // A 404 tells the client "this thing does not exist" — that is useful information.
        if (booking is null)
        {
            return NotFound(); // HTTP 404 Not Found
        }

        return Ok(booking); // HTTP 200 OK — Body: single Booking object as JSON
    }
}

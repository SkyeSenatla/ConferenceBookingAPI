using API.Data;
using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Models;
namespace API.Repositories;

public class BookingRepository(BookingDbContext db) : IBookingRepository
{
    /*
     Without Pagination
    public async Task<IEnumerable<BookingResponse>> GetAllAsync() =>

   await db.Bookings
        .AsNoTracking()
        .Select(b => new BookingResponse(
            b.Id,
            b.Title,
            b.Type.ToString(),
            b.Room.Name,
            b.Room.Floor,
            b.StartTime,
            b.EndTime,
            b.OrganizerEmail,
            b.Attendees.Count,
            b.Attendees
                .Where(ba => ba.Attendee.IsExternal)
                .Select(ba => ba.Attendee.Name)
                .ToList()))
        .ToListAsync();
*/
    // GetAllAsync With Pagination
    public async Task<PagedResponse<BookingResponse>> GetAllAsync(int page, int pageSize)
    {
        var query = db.Bookings.AsNoTracking()
        .OrderBy(b => b.StartTime);

        var totalCount = await query.CountAsync();

        var data = await query.Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(b => new BookingResponse(
          b.Id, b.Title, b.Type.ToString(),
            b.Room.Name, b.Room.Floor,
            b.StartTime, b.EndTime, b.OrganizerEmail,
            b.Attendees.Count,
            b.Attendees
                .Where(ba => ba.Attendee.IsExternal)
                .Select(ba => ba.Attendee.Name)
                .ToList()))
        .ToListAsync();

      return new PagedResponse<BookingResponse>(
       Data: data,
       Page: page,
       PageSize: pageSize,
       TotalCount: totalCount,
       TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize),
       HasNextPage: page * pageSize < totalCount,
       HasPreviousPage: page > 1);

    }
    public async Task<BookingDetailResponse?> GetByIdAsync(Guid id)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r.Equipment)
                    .ThenInclude(re => re.Equipment)
            .Include(b => b.Attendees)
                .ThenInclude(ba => ba.Attendee)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return null;

        return new BookingDetailResponse(
            booking.Id,
            booking.Title,
            booking.Description,
            booking.Type.ToString(),
            booking.Room.Name,
            booking.Room.Floor,
            booking.Room.Capacity,
            booking.StartTime,
            booking.EndTime,
            booking.OrganizerEmail,
            booking.Room.Equipment.Select(re => new RoomEquipmentResponse(
                re.Equipment.Name,
                re.Equipment.Description,
                re.Quantity)).ToList(),
            booking.Attendees.Select(ba => new AttendeeResponse(
                ba.Attendee.Name,
                ba.Attendee.Email,
                ba.Attendee.IsExternal,
                ba.InvitedAt)).ToList()
        );
    }

    // FindAsync checks the Change Tracker before hitting the database.
    // No AsNoTracking — the caller (service) needs a tracked entity to mutate and save.
    public async Task<Booking?> GetEntityByIdAsync(Guid id) =>
        await db.Bookings.FindAsync(id);

    // Compiled queries are translated once at startup and reused on every call.
    // The expression tree is never re-parsed. Use on genuinely hot paths only —
    // adding complexity for queries that run rarely has no measurable benefit.
    private static readonly Func<BookingDbContext, Guid, DateTime, DateTime, Guid?, Task<bool>>
        _conflictQuery = EF.CompileAsyncQuery(
            (BookingDbContext db, Guid roomId, DateTime start, DateTime end, Guid? excludeId) =>
                db.Bookings.Any(b =>
                    b.RoomId == roomId &&
                    b.Id != excludeId &&
                    b.StartTime < end &&
                    b.EndTime > start));

    public Task<bool> HasConflictAsync(
        Guid roomId, DateTime start, DateTime end, Guid? excludeBookingId = null) =>
        _conflictQuery(db, roomId, start, end, excludeBookingId);

    public async Task<IEnumerable<BookingResponse>> SearchAsync(BookingSearchQuery query)
    {
        // When a text search term is provided, route to PostgreSQL full-text search.
        // EF.Functions.ToTsVector generates the @@ operator — not LIKE — so the GIN index can be used.
        if (!string.IsNullOrWhiteSpace(query.Q))
            return await FullTextSearchAsync(query.Q);

        IQueryable<Booking> q = db.Bookings
            .AsNoTracking()
            .Where(b => b.Room.IsAvailable);

        if (!string.IsNullOrWhiteSpace(query.RoomName))
            q = q.Where(b => b.Room.Name.Contains(query.RoomName));

        if (query.Type.HasValue)
            q = q.Where(b => b.Type == query.Type.Value);

        if (query.From.HasValue)
            q = q.Where(b => b.StartTime >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(b => b.EndTime <= query.To.Value);

       /*  Before sort parameters on Search
       return await q
            .OrderBy(b => b.StartTime)
            .Select(b => new BookingResponse(
                b.Id, b.Title, b.Type.ToString(),
                b.Room.Name, b.Room.Floor,
                b.StartTime, b.EndTime, b.OrganizerEmail,
                b.Attendees.Count,
                b.Attendees
                    .Where(ba => ba.Attendee.IsExternal)
                    .Select(ba => ba.Attendee.Name)
                    .ToList()))
            .ToListAsync(); */
            
            // After: 
        var descending = query.Dir?.ToLower() == "desc"; 
        
        q = query.Sort?.ToLower() switch 
        { 
            "title"    => descending ? q.OrderByDescending(b => b.Title)     : q.OrderBy(b => b.Title), 
            "roomname" => descending ? q.OrderByDescending(b => b.Room.Name) : 
        q.OrderBy(b => b.Room.Name), 
            "endtime"  => descending ? q.OrderByDescending(b => b.EndTime)   : q.OrderBy(b => 
        b.EndTime), 
            _          => descending ? q.OrderByDescending(b => b.StartTime) : q.OrderBy(b => 
        b.StartTime), 
        }; 
 
        return await q 
            .Select(b => new BookingResponse( 
                b.Id, b.Title, b.Type.ToString(), 
                b.Room.Name, b.Room.Floor, 
                b.StartTime, b.EndTime, b.OrganizerEmail, 
                b.Attendees.Count, 
                b.Attendees 
                    .Where(ba => ba.Attendee.IsExternal) 
                    .Select(ba => ba.Attendee.Name) 
                    .ToList())) 
            .ToListAsync(); 

    }

    // Full-text search across title and description using PostgreSQL's native engine.
    // EF.Functions.ToTsQuery translates the search term into a tsquery expression.
    // The GIN index on the computed tsvector column makes this fast at any scale.
    // Compare: Contains("board") → LIKE '%board%' → seq scan every time.
    //          FullTextSearch("board") → @@ operator → GIN index scan.
    public async Task<IEnumerable<BookingResponse>> FullTextSearchAsync(string searchTerm)
    {
        return await db.Bookings
            .AsNoTracking()
            .Where(b => EF.Functions.ToTsVector("english", b.Title + " " + b.Description)
                .Matches(EF.Functions.ToTsQuery("english", searchTerm)))
            .Select(b => new BookingResponse(
                b.Id,
                b.Title,
                b.Type.ToString(),
                b.Room.Name,
                b.Room.Floor,
                b.StartTime,
                b.EndTime,
                b.OrganizerEmail,
                b.Attendees.Count,
                b.Attendees
                    .Where(ba => ba.Attendee.IsExternal)
                    .Select(ba => ba.Attendee.Name)
                    .ToList()))
            .ToListAsync();
    }

    // Room utilisation ranking — shows which rooms are busiest this month.
    // Uses PostgreSQL RANK() window function which EF Core cannot generate from LINQ.
    // FromSql maps the raw SQL result back to RoomUtilisationResponse.
    public async Task<IEnumerable<RoomUtilisationResponse>> GetRoomUtilisationAsync(
        DateTime from, DateTime to)
    {
        return await db.Database
            .SqlQuery<RoomUtilisationResponse>(
                $"""
                SELECT r."Name" AS {nameof(RoomUtilisationResponse.RoomName)},
                    COUNT(b."Id") AS {nameof(RoomUtilisationResponse.BookingCount)},
                    COALESCE(SUM(EXTRACT(EPOCH FROM (b."EndTime" - b."StartTime")) / 3600), 0)
                        AS {nameof(RoomUtilisationResponse.TotalHours)},
                    RANK() OVER (ORDER BY COUNT(b."Id") DESC)
                        AS {nameof(RoomUtilisationResponse.UsageRank)}
                FROM rooms r
                LEFT JOIN bookings b
                    ON b."RoomId" = r."Id"
                    AND b."StartTime" >= {from}
                    AND b."EndTime" <= {to}
                GROUP BY r."Id", r."Name"
                ORDER BY {nameof(RoomUtilisationResponse.UsageRank)}
                """)
            .ToListAsync();
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        db.Bookings.Update(booking);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Booking booking)
    {
        db.Bookings.Remove(booking);
        await db.SaveChangesAsync();
    }
}

# Instructor Demo Guide
## Conference Booking System
### Week 2, Day 2 — Relationships, Loading Strategies & Query Optimisation

**Estimated Duration:** ~90 minutes of content + Q&A  
**Objective:** Evolve the Conference Booking schema from isolated entities to a properly related model. Trainees will configure one-to-many and many-to-many relationships using the Fluent API, understand EF Core's three loading strategies, diagnose and fix the N+1 query problem, and learn to write efficient queries using `IQueryable`, `AsNoTracking`, and projections.

---

## Phase 0 – Day 1 Recap & Day 2 Framing
*(5 min — talking points only)*

**Talking Points:**

> "Yesterday we replaced the in-memory store with PostgreSQL. Our `Booking` entity maps to a real table, migrations keep the schema in sync with our code, and data now survives a restart."

> "But look at our current `Booking` class. `Room` and `Speaker` are plain strings. If a room is renamed, we have to update every booking row manually. If we want all upcoming sessions for a speaker, we query by a string that anyone could have misspelled. There is no referential integrity — nothing prevents you from creating a booking for a room that does not exist."

> "Today we fix that. We introduce proper `Room` and `Speaker` entities connected to `Booking` by foreign keys. We also add `Attendee` registrations as a many-to-many relationship. And then we talk about the most dangerous trap in EF Core — the N+1 problem — and how to avoid it."

**Write on the board:**

```
Yesterday                           Today
─────────────────────────           ──────────────────────────────────────
Booking.Room = "Room A" (string)    Booking.RoomId → Room.Id (FK)
Booking.Speaker = "Jane" (string)   Booking.SpeakerId → Speaker.Id (FK)
No attendee tracking                BookingAttendee join table (many-to-many)
```

---

## Phase 1 – Relationship Modelling: The Mental Model
*(10 min — no code)*

> "Before we write any configuration, let us draw the data model we are building."

**Draw on the board:**

```
Speaker  ──────<  Booking  >──────  Room
                    │
                    │  (join table)
                    │
              BookingAttendee
                    │
                 Attendee
```

**Talking Points:**

> "A `Speaker` gives many sessions at a conference. A `Room` hosts many sessions. A `Booking` belongs to exactly one `Speaker` and exactly one `Room`. This is two one-to-many relationships."

> "An `Attendee` can register for many sessions. A session can have many attendees. This is a many-to-many relationship. The database cannot represent this directly — it needs a **join table** called `BookingAttendee` that holds the foreign keys for both sides."

> "Why use an explicit join entity instead of letting EF Core generate a hidden one? Because real join tables almost always carry extra data. In our case, `BookingAttendee` records *when* an attendee registered. You cannot store that on a hidden join table. Always model join entities explicitly when the join itself has meaning."

**Three relationship types — write on the board:**

```
One-to-One   → A User has one Profile
One-to-Many  → A Room has many Bookings
Many-to-Many → A Booking has many Attendees, an Attendee has many Bookings
```

---

## Phase 2 – One-to-Many: Room and Speaker Entities
*(15 min)*

**Action:** Create `API/Models/Room.cs`.

```csharp
// API/Models/Room.cs
namespace API.Models;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Location { get; set; } = string.Empty;

    // Navigation property — EF Core populates this when asked (via Include).
    // Initialised to an empty collection so it is never null — prevents
    // NullReferenceException if accessed before the database is queried.
    public ICollection<Booking> Bookings { get; set; } = [];
}
```

**Action:** Create `API/Models/Speaker.cs`.

```csharp
// API/Models/Speaker.cs
namespace API.Models;

public class Speaker
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Navigation property — one speaker delivers many sessions.
    public ICollection<Booking> Bookings { get; set; } = [];
}
```

**Action:** Update `API/Models/Booking.cs` — replace the string properties with foreign keys and navigation properties.

```csharp
// API/Models/Booking.cs
namespace API.Models;

public class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }

    // CHANGED: Room and Speaker are no longer plain strings.
    // RoomId and SpeakerId are the foreign key columns in the bookings table.
    // Room and Speaker are navigation properties — EF Core populates them
    // when you use Include(), not automatically on every query.
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = null!;

    // Many-to-many via explicit join entity — added in Phase 3.
    public ICollection<BookingAttendee> Attendees { get; set; } = [];
}
```

**Talking Points:**

> "The `= null!` on navigation properties is a nullable reference type suppressor. It tells the compiler: 'I know this looks nullable, but EF Core will always populate it before anyone accesses it — trust me.' It is a deliberate signal, not a lazy shortcut."

> "The `ICollection<Booking> Bookings = []` on `Room` and `Speaker` is the reverse side of the relationship — the *collection navigation property*. It lets you write `room.Bookings` to get all sessions in that room without writing a LINQ query. EF Core calls this **navigation property traversal**."

**Action:** Configure the relationships in `BookingDbContext.OnModelCreating`.

```csharp
// In BookingDbContext.OnModelCreating — add Room and Speaker DbSets and configuration

public DbSet<Room> Rooms => Set<Room>();
public DbSet<Speaker> Speakers => Set<Speaker>();

// Room configuration
modelBuilder.Entity<Room>(entity =>
{
    entity.ToTable("rooms");
    entity.HasKey(r => r.Id);
    entity.Property(r => r.Id).ValueGeneratedNever();
    entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
    entity.Property(r => r.Location).HasMaxLength(200);
});

// Speaker configuration
modelBuilder.Entity<Speaker>(entity =>
{
    entity.ToTable("speakers");
    entity.HasKey(s => s.Id);
    entity.Property(s => s.Id).ValueGeneratedNever();
    entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
    entity.Property(s => s.Bio).HasMaxLength(1000);
    entity.Property(s => s.Email).IsRequired().HasMaxLength(200);
});

// Booking — configure the two one-to-many relationships using Fluent API
modelBuilder.Entity<Booking>(entity =>
{
    entity.ToTable("bookings");
    entity.HasKey(b => b.Id);
    entity.Property(b => b.Id).ValueGeneratedNever();
    entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
    entity.Property(b => b.StartTime).IsRequired();

    // One Room → many Bookings
    // HasOne/WithMany always reads: "A Booking HAS ONE Room; a Room has WITH MANY Bookings"
    // OnDelete Restrict: you cannot delete a Room that still has Bookings.
    // This enforces data integrity at the database level.
    entity.HasOne(b => b.Room)
          .WithMany(r => r.Bookings)
          .HasForeignKey(b => b.RoomId)
          .OnDelete(DeleteBehavior.Restrict);

    // One Speaker → many Bookings
    entity.HasOne(b => b.Speaker)
          .WithMany(s => s.Bookings)
          .HasForeignKey(b => b.SpeakerId)
          .OnDelete(DeleteBehavior.Restrict);

    // Room + StartTime unique constraint — kept from Day 1
    entity.HasIndex(b => new { b.RoomId, b.StartTime })
          .IsUnique()
          .HasDatabaseName("ix_bookings_room_starttime");
});
```

**Talking Points:**

> "The Fluent API reads like a sentence: `HasOne(b => b.Room).WithMany(r => r.Bookings).HasForeignKey(b => b.RoomId)`. EF Core could infer most of this by convention, but explicit configuration is self-documenting and catches mistakes at compile time rather than runtime."

> "`DeleteBehavior.Restrict` means PostgreSQL will reject a `DELETE` on a `rooms` row if any `bookings` rows still reference it. The other option is `Cascade` — deleting the room deletes all its bookings too. For this domain, Restrict is correct: you should not be able to silently wipe all sessions in a room."

**Action:** Generate and apply the migration.

```bash
dotnet ef migrations add AddRoomAndSpeakerRelationships
dotnet ef database update
```

> "Open the generated migration file. You will see `CreateTable` for `rooms` and `speakers`, an `AddColumn` for `room_id` and `speaker_id` on `bookings`, `DropColumn` for the old `room` and `speaker` string columns, and `AddForeignKey` calls. EF Core calculated all of this from the difference between the previous snapshot and the current model configuration. Review it before applying — especially the `DropColumn` lines. Dropped data cannot be recovered."

---

## Phase 3 – Many-to-Many with an Explicit Join Entity
*(15 min)*

**Action:** Create `API/Models/Attendee.cs`.

```csharp
// API/Models/Attendee.cs
namespace API.Models;

public class Attendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Collection navigation — an Attendee can be registered for many Bookings.
    // The ICollection is of the join entity, not Booking directly,
    // because the join entity carries extra data (RegisteredAt).
    public ICollection<BookingAttendee> Bookings { get; set; } = [];
}
```

**Action:** Create `API/Models/BookingAttendee.cs` — the explicit join entity.

```csharp
// API/Models/BookingAttendee.cs
namespace API.Models;

// This is the join entity for the Booking ↔ Attendee many-to-many relationship.
// It is modelled explicitly (rather than letting EF Core generate a hidden join table)
// because it carries additional data: when the attendee registered.
// Any time a join table has its own meaning or data, model it explicitly.
public class BookingAttendee
{
    // Composite primary key — configured in OnModelCreating.
    // A booking + attendee combination can only appear once.
    public Guid BookingId { get; set; }
    public Guid AttendeeId { get; set; }

    // The timestamp when the attendee registered for this session.
    // This is what makes the explicit join entity necessary — a hidden table cannot hold it.
    public DateTime RegisteredAt { get; set; }

    // Navigation properties back to both sides of the relationship.
    public Booking Booking { get; set; } = null!;
    public Attendee Attendee { get; set; } = null!;
}
```

**Action:** Add the `Attendee` DbSet and configure both sides in `BookingDbContext`.

```csharp
public DbSet<Attendee> Attendees => Set<Attendee>();
public DbSet<BookingAttendee> BookingAttendees => Set<BookingAttendee>();

// Attendee configuration
modelBuilder.Entity<Attendee>(entity =>
{
    entity.ToTable("attendees");
    entity.HasKey(a => a.Id);
    entity.Property(a => a.Id).ValueGeneratedNever();
    entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
    entity.Property(a => a.Email).IsRequired().HasMaxLength(200);
    entity.HasIndex(a => a.Email).IsUnique(); // No duplicate registrants
});

// BookingAttendee — explicit join entity configuration
modelBuilder.Entity<BookingAttendee>(entity =>
{
    entity.ToTable("booking_attendees");

    // Composite primary key: the pair (BookingId, AttendeeId) must be unique.
    // An attendee cannot register for the same session twice.
    entity.HasKey(ba => new { ba.BookingId, ba.AttendeeId });

    entity.Property(ba => ba.RegisteredAt).IsRequired();

    // Configure both sides of the many-to-many
    entity.HasOne(ba => ba.Booking)
          .WithMany(b => b.Attendees)
          .HasForeignKey(ba => ba.BookingId);

    entity.HasOne(ba => ba.Attendee)
          .WithMany(a => a.Bookings)
          .HasForeignKey(ba => ba.AttendeeId);
});
```

**Talking Points:**

> "The composite primary key `new { ba.BookingId, ba.AttendeeId }` means the database enforces uniqueness on the combination — not on either column independently. One attendee can appear in many rows (different bookings) and one booking can appear in many rows (different attendees), but the same pair cannot repeat. This is the relational model doing exactly what it is designed for."

> "Contrast this with the implicit many-to-many that EF Core supports where you skip the join entity entirely. That works fine for simple tag systems where the join has no meaning. As soon as the join carries data — a timestamp, a seat number, a payment status — you need the explicit model. Default to explicit."

**Action:** Generate the next migration.

```bash
dotnet ef migrations add AddAttendeesManyToMany
dotnet ef database update
```

---

## Phase 4 – Loading Strategies and the N+1 Problem
*(15 min)*

**Write on the board:**

```
Three loading strategies:
1. Eager Loading    — load related data in the same query (Include)
2. Explicit Loading — load related data on demand (LoadAsync)  
3. Lazy Loading     — load related data automatically on first access (avoid in APIs)
```

### Eager Loading — `Include` and `ThenInclude`

```csharp
// Load bookings with their Room and Speaker in a single SQL query.
// Generates a query with two JOINs:
// SELECT b.*, r.*, s.*
// FROM bookings b
// JOIN rooms r ON b.room_id = r.id
// JOIN speakers s ON b.speaker_id = s.id
var bookings = await db.Bookings
    .Include(b => b.Room)
    .Include(b => b.Speaker)
    .ToListAsync();
```

```csharp
// ThenInclude — navigating further down the graph
// Loads a Booking's Attendees AND each Attendee's full details
var booking = await db.Bookings
    .Include(b => b.Room)
    .Include(b => b.Speaker)
    .Include(b => b.Attendees)        // Load the join entities
        .ThenInclude(ba => ba.Attendee) // Then load the Attendee on each join entity
    .FirstOrDefaultAsync(b => b.Id == id);
```

**Talking Points:**

> "`Include` and `ThenInclude` build the JOIN clauses in the SQL query. Without them, navigation properties are `null` — EF Core does not load related data unless you ask. This is the default behaviour and it is correct: you should only pay for the data you actually need."

> "`ThenInclude` chains off a navigation that was already included. Think of it as following a path through the object graph: `Booking → BookingAttendee → Attendee`. You need both `Include` and `ThenInclude` to walk two levels deep."

### The N+1 Problem — Live Demonstration

> "This is the most common performance bug in EF Core applications. Watch carefully."

```csharp
// ── THE PROBLEM ──────────────────────────────────────────────────────────
// Naive code that looks harmless but generates N+1 database round-trips.

var bookings = await db.Bookings.ToListAsync();
// ↑ Query 1: SELECT * FROM bookings  (returns 100 rows)

foreach (var booking in bookings)
{
    // Each access to booking.Room triggers a separate database query
    // because Room was not included — EF Core has to go back to the database
    // for each booking individually.
    Console.WriteLine(booking.Room.Name); // ↑ Query 2, 3, 4 ... 101
}

// 100 bookings = 101 total queries.
// 1000 bookings = 1001 total queries.
// This is the N+1 problem: 1 query to get the list + N queries for the related data.
```

> "Enable EF Core query logging to see this in the terminal:"

```csharp
// In BookingDbContext — add to constructor or configure in Program.cs
// This is for demonstration only — remove before committing.
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
}
```

> "Now run the naive loop and count the SQL statements in the terminal. One statement per booking. Then fix it:"

```csharp
// ── THE FIX ──────────────────────────────────────────────────────────────
// One query with JOINs instead of N+1 round-trips.

var bookings = await db.Bookings
    .Include(b => b.Room)    // ← this is all it takes
    .Include(b => b.Speaker)
    .ToListAsync();
// ↑ Query 1: SELECT b.*, r.*, s.* FROM bookings JOIN rooms ... JOIN speakers ...

foreach (var booking in bookings)
{
    Console.WriteLine(booking.Room.Name); // Already in memory — no database call
}
// 100 bookings = 1 total query. Always.
```

**Talking Points:**

> "The N+1 problem is particularly dangerous in APIs because it is invisible in development with small datasets. Five seed bookings = six queries — acceptable. Five thousand real bookings = five thousand and one queries — your API times out under load. Always think about the SQL that will be generated, not just the C# that is written."

> "The rule of thumb: if you are iterating over a collection and accessing a navigation property inside the loop, you have — or will eventually have — an N+1 problem. The fix is always `Include` before the loop."

### Lazy Loading — What It Is and Why to Avoid It

```csharp
// Lazy loading requires the Proxies package and virtual navigation properties.
// EF Core generates a proxy class that intercepts property access.
// DO NOT enable this in APIs.

// optionsBuilder.UseLazyLoadingProxies(); // ← the line that enables it

public class Booking
{
    // Navigation properties must be 'virtual' for lazy loading proxies to intercept them
    public virtual Room Room { get; set; } = null!; // ← required for lazy loading
}
```

**Talking Points:**

> "Lazy loading sounds convenient: access `booking.Room` and EF Core fetches it automatically. The problem is it makes every navigation property access a hidden database query. In an async API, this is even worse — lazy loading uses synchronous database calls, blocking the thread pool."

> "Lazy loading is designed for desktop applications that display one record at a time. In an API processing concurrent requests and returning lists of data, it produces the worst possible version of the N+1 problem — one you cannot easily see in the code. Keep navigation properties non-virtual and keep lazy loading disabled."

### `AsNoTracking` for Read-Only Queries

```csharp
// Default behaviour — Change Tracker snapshots every loaded entity.
// Has CPU and memory cost proportional to the number of rows loaded.
var bookings = await db.Bookings
    .Include(b => b.Room)
    .ToListAsync();
// EF Core is now watching all of these objects for mutations.

// AsNoTracking — skip the snapshot entirely.
// Use this for every GET endpoint that does not need to save changes.
var bookings = await db.Bookings
    .AsNoTracking()         // ← no snapshot, no change tracking
    .Include(b => b.Room)
    .Include(b => b.Speaker)
    .ToListAsync();
// Faster: less CPU, less memory. The entities are read-only for this request.
```

**Talking Points:**

> "`AsNoTracking` is not a micro-optimisation — on a list endpoint returning 500 bookings with two included entities each, you are skipping 1500 object snapshots. The savings compound under load. The rule is simple: if you are not calling `SaveChangesAsync` after the query, use `AsNoTracking`."

---

## Phase 5 – `IQueryable<T>` vs `IEnumerable<T>`
*(10 min)*

> "This is one of the most important concepts in EF Core and one of the most commonly misunderstood."

**Draw on the board:**

```
IQueryable<T>   — an expression tree that has NOT been executed yet
                  adding .Where() / .Select() / .OrderBy() modifies the SQL
                  execution happens at ToListAsync() / FirstOrDefaultAsync()

IEnumerable<T>  — data already in memory
                  adding .Where() / .Select() filters in C# after the DB has responded
```

```csharp
// ── QUERYABLE — SQL built and sent once ─────────────────────────────────

IQueryable<Booking> query = db.Bookings; // No SQL yet — just an expression tree

// Adding conditions builds the WHERE clause, not a C# filter
query = query.Where(b => b.StartTime > DateTime.UtcNow);     // WHERE start_time > now
query = query.Where(b => b.Room.Location == "Main Building"); // AND room.location = '...'
query = query.OrderBy(b => b.StartTime);                      // ORDER BY start_time

// SQL is only sent here — all conditions combined into a single efficient query
var results = await query.ToListAsync();
// SELECT b.* FROM bookings b
// JOIN rooms r ON b.room_id = r.id
// WHERE b.start_time > @now AND r.location = 'Main Building'
// ORDER BY b.start_time
```

```csharp
// ── ENUMERABLE — data already loaded, filters run in C# ─────────────────

// ToList() here materialises ALL bookings from the database into memory
IEnumerable<Booking> bookings = await db.Bookings.ToListAsync();

// .Where() here is LINQ-to-Objects — runs on the already-loaded list in memory
var upcoming = bookings.Where(b => b.StartTime > DateTime.UtcNow);

// Result: the database sent 10,000 rows; C# filtered them to 50.
// 9,950 rows were transferred and discarded — wasted bandwidth and memory.
```

**Talking Points:**

> "The key question is: at what point does the data leave the database? With `IQueryable`, the answer is 'as late as possible, with the most specific query'. With `IEnumerable`, the answer is 'immediately, with everything'."

> "A method that accepts `IEnumerable<T>` and calls `.Where()` on it is doing C# filtering. A method that accepts `IQueryable<T>` and calls `.Where()` on it is building SQL. Knowing which one you have tells you where the filtering happens."

> "The practical rule: chain your `.Where()`, `.Select()`, `.OrderBy()` calls on the `DbSet` or `IQueryable` before calling `ToListAsync()`. Never call `ToList()` early and then filter."

```csharp
// ── BUILDING DYNAMIC QUERIES SAFELY ─────────────────────────────────────
// IQueryable lets you conditionally add filters without multiple code paths.

public async Task<List<Booking>> SearchAsync(string? room, DateTime? from, DateTime? to)
{
    // Start with the full queryable — no SQL yet
    IQueryable<Booking> query = db.Bookings
        .AsNoTracking()
        .Include(b => b.Room)
        .Include(b => b.Speaker);

    // Conditionally add WHERE clauses — each adds to the SQL, not a C# filter
    if (!string.IsNullOrEmpty(room))
        query = query.Where(b => b.Room.Name == room);

    if (from.HasValue)
        query = query.Where(b => b.StartTime >= from.Value);

    if (to.HasValue)
        query = query.Where(b => b.StartTime <= to.Value);

    // One SQL statement, however many conditions were applied
    return await query.OrderBy(b => b.StartTime).ToListAsync();
}
```

---

## Phase 6 – Projections with `Select`
*(10 min)*

> "Even with `AsNoTracking`, loading a full entity and all its navigations brings back columns you may not need. Projections let you SELECT only the data a DTO requires."

```csharp
// Loading full entities — transfers every column of every joined table
var bookings = await db.Bookings
    .AsNoTracking()
    .Include(b => b.Room)    // all Room columns
    .Include(b => b.Speaker) // all Speaker columns
    .ToListAsync();

// Then mapping in C# — the Speaker.Bio and Room.Capacity columns were loaded
// but your BookingResponse DTO only needs the names
var responses = bookings.Select(b => new BookingResponse(...)).ToList();
```

```csharp
// Projection with Select — only the columns the DTO needs travel from the database
var responses = await db.Bookings
    .AsNoTracking()
    .Select(b => new BookingResponse(
        b.Id,
        b.Title,
        b.Speaker.Name,          // EF Core joins to speakers to get just the name
        b.Room.Name,             // EF Core joins to rooms to get just the name
        b.StartTime,
        b.Attendees.Count        // Generates COUNT(*) subquery — no Attendee data loaded
    ))
    .ToListAsync();

// Generated SQL (approximately):
// SELECT b.id, b.title, s.name, r.name, b.start_time,
//        (SELECT COUNT(*) FROM booking_attendees ba WHERE ba.booking_id = b.id)
// FROM bookings b
// JOIN speakers s ON b.speaker_id = s.id
// JOIN rooms r ON b.room_id = r.id
```

**Talking Points:**

> "Projections are the most efficient query pattern EF Core supports. You never materialise an entity — no Change Tracker work, no extra columns. The `Select` lambda is translated directly into the SQL column list."

> "Notice `b.Attendees.Count` — EF Core translates this into a SQL `COUNT(*)` subquery. You do not load a list of attendees into memory just to call `.Count` on it. EF Core is smart enough to push the aggregation to the database where it belongs."

> "The general rule: use `Include` when you need to mutate the related data or when you genuinely need the full entity. Use `Select` projections for all read endpoints (GET). This alone will noticeably improve the performance of a real API under load."

---

## Phase 7 – EF Core 10 Highlights
*(5 min)*

### `ComplexType` — Value Objects Without Extra Tables

```csharp
// A value object that maps to columns in the parent table — not a separate table.
// Perfect for concepts that are not independent entities (they have no ID).
// EF Core 10 improves complex type support: better queries, nullable complex types.

// Declare the value object
[ComplexType]
public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Computed property — never stored, always derived
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
}

// Use it in the entity
public class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Maps to two columns in the bookings table: Schedule_StartTime and Schedule_EndTime
    // No join, no separate table, no foreign key — just two columns
    public TimeSlot Schedule { get; set; } = new();

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
}
```

**Talking Points:**

> "`[ComplexType]` is for concepts that are part of the entity but complex enough to deserve their own class — a time slot, a money amount, an address. They have no identity of their own and cannot exist without the owning entity. EF Core maps them to columns on the parent table, not a separate table. No join, no FK, no performance cost — just better organisation in your C# code."

### Improved `GroupBy` Translation

```csharp
// EF Core 10 translates GroupBy more reliably into SQL GROUP BY.
// Useful for summary/analytics endpoints.

// Count bookings per room
var roomSummary = await db.Bookings
    .AsNoTracking()
    .GroupBy(b => b.Room.Name)
    .Select(g => new
    {
        RoomName = g.Key,
        BookingCount = g.Count(),
        NextSession = g.Min(b => b.StartTime)
    })
    .ToListAsync();

// Generated SQL (approximately):
// SELECT r.name, COUNT(*) AS booking_count, MIN(b.start_time) AS next_session
// FROM bookings b
// JOIN rooms r ON b.room_id = r.id
// GROUP BY r.name
```

**Talking Points:**

> "In earlier EF Core versions, complex `GroupBy` queries would often throw a runtime exception or silently fall back to loading all data into memory before grouping in C#. EF Core 10 pushes significantly more of these aggregations to SQL where they run efficiently."

---

## Phase 8 – Testing Queries in Scalar
*(5 min)*

**Action:** Run the application with EF Core query logging enabled. Open the Scalar UI.

1. **Include verification:** Call `GET /api/bookings`. Watch the terminal — confirm a single SQL statement with JOIN clauses rather than N+1 round-trips.
2. **AsNoTracking verification:** Add a breakpoint in the GET endpoint. Inspect `db.ChangeTracker.Entries()` — it should be empty after an `AsNoTracking` query.
3. **Projection verification:** Call `GET /api/bookings` and observe the SQL logged. Confirm only the columns named in the `Select` projection appear in the SQL, not `SELECT *`.
4. **N+1 demonstration (live):** Temporarily remove `.Include()` from `GetBookingsAsync`, restart, and call the endpoint again. Count the SQL statements in the terminal. Restore the `Include`.

---

## Wrap-Up & What's Next
*(5 min)*

**Write on the board:**

```
Query Performance Hierarchy (fastest → slowest for read endpoints)

1. Projection with Select + AsNoTracking  ← use for all GET list endpoints
2. Include + AsNoTracking                 ← use when you need full entities
3. Include without AsNoTracking           ← use only when SaveChangesAsync follows
4. No Include (N+1)                       ← never
5. Lazy Loading                           ← never in APIs
```

**Closing Talking Points:**

> "The patterns from today are not theoretical — they are the exact queries you write in every real .NET API. A GET endpoint that returns a list should almost always use `AsNoTracking` and `Select`. A GET-by-ID that precedes a PUT should use `FindAsync` without `AsNoTracking` so the Change Tracker can detect the mutation."

> "Tomorrow we add filtering, sorting, and pagination. An endpoint that returns the full bookings table will not survive contact with real-world data volumes — you will learn how to apply those at the database level using the `IQueryable` pattern we covered today."

---

## Assignment 2.2 — CareerHub: Relationships, Eager Loading & Efficient Queries

**Objective:** Evolve the CareerHub schema to model real relationships, configure them with the Fluent API, and refactor your query logic to avoid the N+1 problem.

**Part 1 – Relationship Design**

Before writing code, draw the entity relationship diagram for the following model:

- A `Company` can post many `JobListings` (one-to-many)
- An `Applicant` can apply for many `JobListings` and a `JobListing` can receive many `Applicants` (many-to-many via an `Application` join entity)
- The `Application` join entity must record when the application was submitted and its current status (e.g. Pending, Reviewed, Rejected)

Identify which relationships are one-to-many and which require an explicit join entity, and explain why the `Application` entity must be explicit.

**Part 2 – Entity Classes**

Create `Company`, `Applicant`, and `Application` entity classes in your `Models` folder. Follow the same conventions established in Day 1: mutable class, `= string.Empty` initialisers on strings, `= []` on collection navigations, `= null!` on required navigations.

Update `JobListing` to replace the `Company` string property with a `CompanyId` foreign key and `Company` navigation property. Add an `Applications` collection navigation.

**Part 3 – Fluent API Configuration**

Configure all entities in `OnModelCreating`:
- `Company` → `job_listings`: `HasOne`/`WithMany`/`HasForeignKey`, `DeleteBehavior.Restrict`
- `Application` join entity: composite primary key `(JobListingId, ApplicantId)`, both `HasOne`/`WithMany` relationships configured
- Unique index on `Applicant.Email`

Generate and apply the migration. Open the generated file and verify that the foreign keys, indexes, and composite primary key are all present before running `dotnet ef database update`.

**Part 4 – Eager Loading**

Update `GetAllJobListingsAsync` and `GetJobListingByIdAsync` to use `Include` and `ThenInclude` appropriately:
- The list endpoint should include the `Company` name
- The by-ID endpoint should include `Company`, `Applications`, and each `Application`'s `Applicant`

**Part 5 – Eliminating the N+1 Problem**

Enable EF Core query logging in development by adding `optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)` to your DbContext. Run your application and call the list endpoint. Inspect the terminal output and confirm there is exactly one SQL statement. If there are multiple, diagnose which navigation property is causing the extra queries and fix it with `Include`.

**Part 6 – Efficient Queries**

Refactor your GET endpoints to use `AsNoTracking` and `Select` projections. Your `JobListingResponse` DTO should include the company name and application count without loading full `Company` or `Application` entities. Confirm the projected SQL in the terminal matches only the columns the DTO requires.

**Proving It Works**

1. **Schema proof:** Open your PostgreSQL client and show the `companies`, `applicants`, and `applications` tables with the correct foreign key constraints.
2. **Include proof:** Call `GET /api/jobs` and confirm one SQL statement with JOINs in the terminal log.
3. **N+1 proof:** Temporarily remove `Include` from a list query, call the endpoint with 5 seed records, and show the 6 SQL statements. Restore `Include` and show it returns to 1.
4. **Projection proof:** Call `GET /api/jobs` and show the generated SQL contains only the projected columns, not `SELECT *`.
5. **AsNoTracking proof:** Add a debug log of `db.ChangeTracker.Entries().Count()` after a GET query. Confirm it is 0 with `AsNoTracking` and greater than 0 without.

**Version Control** — Suggested commits:
- Add Company, Applicant, and Application entity classes
- Configure relationships in CareerHubDbContext with Fluent API
- Add AddCompanyAndApplicantRelationships migration
- Refactor JobsController to use Include, AsNoTracking, and Select projections

**README Updates**

1. **N+1 Problem:** Describe what the N+1 problem is, how you detected it using query logging, and how `Include` fixes it.
2. **IQueryable vs IEnumerable:** Explain the difference in your own words, with a specific example of why calling `ToList()` too early is harmful.
3. **AsNoTracking:** Explain when to use `AsNoTracking` and when NOT to use it.

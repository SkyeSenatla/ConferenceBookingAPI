using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data;

public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    // One DbSet per entity — the entry point for all LINQ queries against that table.
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<RoomEquipment> RoomEquipment => Set<RoomEquipment>();
    public DbSet<BookingAttendee> BookingAttendees => Set<BookingAttendee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Booking ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(b => b.Id);
            // ValueGeneratedNever: the application assigns the GUID; the database never auto-generates it.
            entity.Property(b => b.Id).ValueGeneratedNever();

            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Description).HasMaxLength(1000);
            entity.Property(b => b.StartTime).IsRequired();
            entity.Property(b => b.EndTime).IsRequired();
            entity.Property(b => b.OrganizerEmail).IsRequired().HasMaxLength(200);

            // HasConversion<string>() stores the enum as its name ("Meeting") rather than its
            // integer ordinal (0). Human-readable in PostgreSQL without needing a lookup table.
            entity.Property(b => b.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // One Room → many Bookings.
            // DeleteBehavior.Restrict: deleting a room that still has bookings is rejected by
            // the database. The admin must cancel or reassign bookings before removing the room.
            entity.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevents two bookings with the exact same start time in the same room.
            // Full overlap detection (e.g. 09:00–11:00 vs 10:00–12:00) requires an
            // application-layer AnyAsync check — it cannot be expressed as a simple unique index.
            entity.HasIndex(b => new { b.RoomId, b.StartTime })
                .IsUnique()
                .HasDatabaseName("ix_bookings_room_starttime");
        });

        // ── Room ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Floor).HasMaxLength(50);
            entity.Property(r => r.IsAvailable).IsRequired();
        });

        // ── Equipment ────────────────────────────────────────────────────────
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("equipment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            // No duplicate equipment categories — "Projector" can only exist once.
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ── RoomEquipment (explicit join entity: Room ↔ Equipment) ───────────
        modelBuilder.Entity<RoomEquipment>(entity =>
        {
            entity.ToTable("room_equipment");

            // Composite primary key: a room can have a given equipment type only once.
            // Quantity tracks how many units are in the room, not multiple rows per unit.
            entity.HasKey(re => new { re.RoomId, re.EquipmentId });
            entity.Property(re => re.Quantity).IsRequired();

            entity.HasOne(re => re.Room)
                .WithMany(r => r.Equipment)
                .HasForeignKey(re => re.RoomId);

            entity.HasOne(re => re.Equipment)
                .WithMany(e => e.Rooms)
                .HasForeignKey(re => re.EquipmentId);
        });

        // ── Attendee ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Attendee>(entity =>
        {
            entity.ToTable("attendees");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Email).IsRequired().HasMaxLength(200);
            // Email is the natural unique identifier for an attendee across all meetings.
            entity.HasIndex(a => a.Email).IsUnique();
        });

        // ── BookingAttendee (explicit join entity: Booking ↔ Attendee) ───────
        modelBuilder.Entity<BookingAttendee>(entity =>
        {
            entity.ToTable("booking_attendees");

            // Composite primary key: a person cannot be on the same booking invite twice.
            entity.HasKey(ba => new { ba.BookingId, ba.AttendeeId });
            entity.Property(ba => ba.InvitedAt).IsRequired();

            entity.HasOne(ba => ba.Booking)
                .WithMany(b => b.Attendees)
                .HasForeignKey(ba => ba.BookingId);

            entity.HasOne(ba => ba.Attendee)
                .WithMany(a => a.Bookings)
                .HasForeignKey(ba => ba.AttendeeId);
        });
    }
}

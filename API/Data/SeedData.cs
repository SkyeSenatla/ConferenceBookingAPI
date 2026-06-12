using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

// Week 2 — SeedData provides a realistic starting dataset for development and demos.
//
// WHEN is this called?
//   At application startup, from Program.cs, inside a DI scope.
//   It runs after MigrateAsync() ensures the schema exists.
//
// HOW does it stay safe to call repeatedly?
//   We check whether rooms already exist before inserting anything.
//   If rooms are present the method returns immediately — idempotent seed.
//
// WHY seed Rooms first?
//   Bookings carry a RoomId foreign key. The rooms must exist before the bookings
//   can be inserted, or the database rejects the FK constraint.
public static class SeedData
{
    public static async Task SeedAsync(BookingDbContext db)
    {
        // Guard: skip seeding if bookings already exist.
        // Checking bookings (not rooms) ensures a room created by a test fixture
        // does not falsely indicate the full seed dataset is present.
        if (await db.Bookings.AnyAsync())
            return;

        // ── Rooms ────────────────────────────────────────────────────────────
        // Three rooms across two floors that the receptionist and facilities
        // manager can assign to bookings.
        var boardRoom = new Room
        {
            Id          = Guid.NewGuid(),
            Name        = "Board Room",
            Capacity    = 12,
            Floor       = "Level 1",
            IsAvailable = true
        };

        var meetingRoomA = new Room
        {
            Id          = Guid.NewGuid(),
            Name        = "Meeting Room A",
            Capacity    = 6,
            Floor       = "Ground Floor",
            IsAvailable = true
        };

        var meetingRoomB = new Room
        {
            Id          = Guid.NewGuid(),
            Name        = "Meeting Room B",
            Capacity    = 8,
            Floor       = "Ground Floor",
            IsAvailable = true
        };

        var trainingRoom = new Room
        {
            Id          = Guid.NewGuid(),
            Name        = "Training Room",
            Capacity    = 20,
            Floor       = "Level 2",
            IsAvailable = true
        };

        db.Rooms.AddRange(boardRoom, meetingRoomA, meetingRoomB, trainingRoom);

        // ── Equipment ────────────────────────────────────────────────────────
        // Equipment items that can appear in multiple rooms. The join table
        // (RoomEquipment) tracks which rooms have which items and how many.
        var projector = new Equipment
        {
            Id          = Guid.NewGuid(),
            Name        = "4K Projector",
            Description = "Optoma UHD50X — 3400 lumens, supports HDMI and wireless casting"
        };

        var whiteboard = new Equipment
        {
            Id          = Guid.NewGuid(),
            Name        = "Whiteboard",
            Description = "Magnetic dry-erase whiteboard, 180 × 90 cm"
        };

        var videoConference = new Equipment
        {
            Id          = Guid.NewGuid(),
            Name        = "Video Conferencing System",
            Description = "Logitech Rally Bar — 4K camera, integrated speakers and microphone array"
        };

        var hdmiCable = new Equipment
        {
            Id          = Guid.NewGuid(),
            Name        = "HDMI Cable",
            Description = "2 m HDMI 2.1 cable — supports 4K @ 120 Hz"
        };

        db.Equipment.AddRange(projector, whiteboard, videoConference, hdmiCable);

        // ── RoomEquipment ────────────────────────────────────────────────────
        // Explicit join rows that assign equipment to rooms with quantities.
        // Quantity > 1 means the room has multiple units of the same item type.
        // The composite PK (RoomId, EquipmentId) prevents duplicate assignments.
        db.RoomEquipment.AddRange(
            // Board Room — fully equipped for client presentations
            new RoomEquipment { RoomId = boardRoom.Id,     EquipmentId = projector.Id,       Quantity = 1 },
            new RoomEquipment { RoomId = boardRoom.Id,     EquipmentId = whiteboard.Id,      Quantity = 2 },
            new RoomEquipment { RoomId = boardRoom.Id,     EquipmentId = videoConference.Id, Quantity = 1 },

            // Meeting Room A — lightweight, whiteboard and cables only
            new RoomEquipment { RoomId = meetingRoomA.Id, EquipmentId = whiteboard.Id,      Quantity = 1 },
            new RoomEquipment { RoomId = meetingRoomA.Id, EquipmentId = hdmiCable.Id,        Quantity = 2 },

            // Meeting Room B — projector and video conferencing
            new RoomEquipment { RoomId = meetingRoomB.Id, EquipmentId = projector.Id,       Quantity = 1 },
            new RoomEquipment { RoomId = meetingRoomB.Id, EquipmentId = whiteboard.Id,      Quantity = 1 },
            new RoomEquipment { RoomId = meetingRoomB.Id, EquipmentId = videoConference.Id, Quantity = 1 },

            // Training Room — multiple projectors and whiteboards for large sessions
            new RoomEquipment { RoomId = trainingRoom.Id, EquipmentId = projector.Id,       Quantity = 2 },
            new RoomEquipment { RoomId = trainingRoom.Id, EquipmentId = whiteboard.Id,      Quantity = 3 }
        );

        // ── Attendees ────────────────────────────────────────────────────────
        // A mix of internal employees and external clients.
        // IsExternal = true flags visitors the receptionist must sign in at the front desk.
        var alice = new Attendee
        {
            Id         = Guid.NewGuid(),
            Name       = "Alice Johnson",
            Email      = "alice.johnson@company.com",
            IsExternal = false   // internal employee
        };

        var bob = new Attendee
        {
            Id         = Guid.NewGuid(),
            Name       = "Bob Smith",
            Email      = "bob.smith@company.com",
            IsExternal = false   // internal employee
        };

        var claire = new Attendee
        {
            Id         = Guid.NewGuid(),
            Name       = "Claire Marchetti",
            Email      = "claire.marchetti@clientcorp.com",
            IsExternal = true    // external client — needs visitor badge
        };

        var david = new Attendee
        {
            Id         = Guid.NewGuid(),
            Name       = "David Lee",
            Email      = "david.lee@company.com",
            IsExternal = false   // internal employee
        };

        db.Attendees.AddRange(alice, bob, claire, david);

        // ── Bookings ─────────────────────────────────────────────────────────
        // Realistic bookings spread across two days.
        // Times are UTC — consistent with DateTime.UtcNow used throughout the API.
        // The unique index on (RoomId, StartTime) means no two bookings can share
        // the exact same room and start time — these slots are intentionally staggered.
        var now         = DateTime.UtcNow.Date;
        var dayOne      = now.AddDays(7);
        var dayTwo      = now.AddDays(8);

        var clientPresentation = new Booking
        {
            Id             = Guid.NewGuid(),
            Title          = "Q3 Product Roadmap Review",
            Description    = "Quarterly roadmap presentation for ClientCorp stakeholders.",
            StartTime      = dayOne.AddHours(9),
            EndTime        = dayOne.AddHours(11),
            Type           = BookingType.ClientPresentation,
            OrganizerEmail = "alice.johnson@company.com",
            RoomId         = boardRoom.Id
        };

        var teamStandup = new Booking
        {
            Id             = Guid.NewGuid(),
            Title          = "Engineering Team Standup",
            Description    = "Daily sync — blockers, progress, and priorities.",
            StartTime      = dayOne.AddHours(9),
            EndTime        = dayOne.AddHours(9).AddMinutes(30),
            Type           = BookingType.Meeting,
            OrganizerEmail = "bob.smith@company.com",
            RoomId         = meetingRoomA.Id
        };

        var trainingSession = new Booking
        {
            Id             = Guid.NewGuid(),
            Title          = "ASP.NET Core 10 Workshop",
            Description    = "Full-day hands-on training for the development team.",
            StartTime      = dayOne.AddHours(9),
            EndTime        = dayOne.AddHours(17),
            Type           = BookingType.Training,
            OrganizerEmail = "david.lee@company.com",
            RoomId         = trainingRoom.Id
        };

        var maintenanceWindow = new Booking
        {
            Id             = Guid.NewGuid(),
            Title          = "Projector Servicing — Board Room",
            Description    = "Annual projector calibration and lamp replacement.",
            StartTime      = dayOne.AddHours(11),
            EndTime        = dayOne.AddHours(12),
            Type           = BookingType.Maintenance,
            OrganizerEmail = "facilities@company.com",
            RoomId         = boardRoom.Id
        };

        var afternoonMeeting = new Booking
        {
            Id             = Guid.NewGuid(),
            Title          = "Sprint Retrospective",
            Description    = "End-of-sprint retrospective — what went well and what to improve.",
            StartTime      = dayOne.AddHours(14),
            EndTime        = dayOne.AddHours(15),
            Type           = BookingType.Meeting,
            OrganizerEmail = "alice.johnson@company.com",
            RoomId         = meetingRoomB.Id
        };

        var dayTwoMeeting = new Booking
        {
            Id             = Guid.NewGuid(),
            Title          = "Architecture Review",
            Description    = "Review proposed microservices migration design.",
            StartTime      = dayTwo.AddHours(10),
            EndTime        = dayTwo.AddHours(12),
            Type           = BookingType.Meeting,
            OrganizerEmail = "david.lee@company.com",
            RoomId         = boardRoom.Id
        };

        db.Bookings.AddRange(
            clientPresentation, teamStandup, trainingSession,
            maintenanceWindow, afternoonMeeting, dayTwoMeeting);

        // ── BookingAttendees ──────────────────────────────────────────────────
        // Join rows linking bookings to their attendees.
        // InvitedAt records when the invitation was sent — the receptionist uses this
        // to manage the guest list in arrival order and prepare visitor badges.
        db.BookingAttendees.AddRange(
            // Q3 Roadmap — Alice (organiser), Bob, and Claire (external client)
            new BookingAttendee { BookingId = clientPresentation.Id, AttendeeId = alice.Id,  InvitedAt = DateTime.UtcNow.AddDays(-3) },
            new BookingAttendee { BookingId = clientPresentation.Id, AttendeeId = bob.Id,    InvitedAt = DateTime.UtcNow.AddDays(-3) },
            new BookingAttendee { BookingId = clientPresentation.Id, AttendeeId = claire.Id, InvitedAt = DateTime.UtcNow.AddDays(-2) },

            // Engineering Standup — Bob and David
            new BookingAttendee { BookingId = teamStandup.Id,        AttendeeId = bob.Id,    InvitedAt = DateTime.UtcNow.AddDays(-1) },
            new BookingAttendee { BookingId = teamStandup.Id,        AttendeeId = david.Id,  InvitedAt = DateTime.UtcNow.AddDays(-1) },

            // ASP.NET Core Workshop — all internal employees
            new BookingAttendee { BookingId = trainingSession.Id,    AttendeeId = alice.Id,  InvitedAt = DateTime.UtcNow.AddDays(-5) },
            new BookingAttendee { BookingId = trainingSession.Id,    AttendeeId = bob.Id,    InvitedAt = DateTime.UtcNow.AddDays(-5) },
            new BookingAttendee { BookingId = trainingSession.Id,    AttendeeId = david.Id,  InvitedAt = DateTime.UtcNow.AddDays(-5) },

            // Sprint Retrospective — Alice and Bob
            new BookingAttendee { BookingId = afternoonMeeting.Id,   AttendeeId = alice.Id,  InvitedAt = DateTime.UtcNow.AddDays(-1) },
            new BookingAttendee { BookingId = afternoonMeeting.Id,   AttendeeId = bob.Id,    InvitedAt = DateTime.UtcNow.AddDays(-1) },

            // Architecture Review — all internal employees
            new BookingAttendee { BookingId = dayTwoMeeting.Id,      AttendeeId = alice.Id,  InvitedAt = DateTime.UtcNow.AddDays(-2) },
            new BookingAttendee { BookingId = dayTwoMeeting.Id,      AttendeeId = bob.Id,    InvitedAt = DateTime.UtcNow.AddDays(-2) },
            new BookingAttendee { BookingId = dayTwoMeeting.Id,      AttendeeId = david.Id,  InvitedAt = DateTime.UtcNow.AddDays(-2) }
        );

        // A single SaveChangesAsync wraps all inserts in one transaction.
        // If any insert fails, none are committed — the database stays consistent.
        await db.SaveChangesAsync();
    }
}

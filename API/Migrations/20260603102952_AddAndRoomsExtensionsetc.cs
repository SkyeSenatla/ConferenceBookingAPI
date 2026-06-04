using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddAndRoomsExtensionsetc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_room_starttime",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "Room",
                table: "bookings",
                newName: "BookingType");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "bookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "OrganizerEmail",
                table: "bookings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "attendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Floor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "booking_attendees",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_attendees", x => new { x.BookingId, x.AttendeeId });
                    table.ForeignKey(
                        name: "FK_booking_attendees_attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalTable: "attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_attendees_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_equipment",
                columns: table => new
                {
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_equipment", x => new { x.RoomId, x.EquipmentId });
                    table.ForeignKey(
                        name: "FK_room_equipment_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_equipment_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_room_starttime",
                table: "bookings",
                columns: new[] { "RoomId", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendees_Email",
                table: "attendees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_attendees_AttendeeId",
                table: "booking_attendees",
                column: "AttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_Name",
                table: "equipment",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_equipment_EquipmentId",
                table: "room_equipment",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_rooms_RoomId",
                table: "bookings",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_rooms_RoomId",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "booking_attendees");

            migrationBuilder.DropTable(
                name: "room_equipment");

            migrationBuilder.DropTable(
                name: "attendees");

            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropIndex(
                name: "ix_bookings_room_starttime",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "OrganizerEmail",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "BookingType",
                table: "bookings",
                newName: "Room");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_room_starttime",
                table: "bookings",
                columns: new[] { "Room", "StartTime" },
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using API.Models; 

namespace API.Data;

public class BookingDbContext(DbContextOptions<BookingDbContext> options): DbContext(options)
{
    //List you db sets
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       modelBuilder.Entity<Booking>(entity =>
       {
           entity.ToTable("bookings"); 
           entity.HasKey(b => b.Id);
           entity.Property(b=> b.Id).ValueGeneratedNever();

           entity.Property(b=> b.Title).IsRequired().HasMaxLength(200); 
           entity.Property(b => b.Speaker).IsRequired().HasMaxLength(100); 
           entity.Property(b => b.Room).IsRequired().HasMaxLength(50); 
           entity.Property(b => b.StartTime).IsRequired();
          
          // set up constraint to en sure no duplicate booking records
           entity.HasIndex(b => new {b.Room, b.StartTime}).IsUnique().HasDatabaseName("ix_bookings_room_starttime"); 



       });
    }
}
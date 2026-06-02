using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR;

namespace API.Models;

// A record is ideal for API response shapes:
// - Immutable by default (data travels, it doesn't mutate)
// - Value equality built-in (two Bookings with same data are equal)
// - Concise positional syntax
public class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
   public string Room { get; set; } = string.Empty;
   public DateTime StartTime { get; set; } 

   public Booking( Guid id, string title, string speaker, string room, DateTime startTime)
    {
        Id = id; 
        Title = title;
        Speaker = speaker;
        Room = room;
        StartTime = startTime;  
    }
}

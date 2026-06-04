namespace API.Models;

public class Equipment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;// e.g. "4K Projector", "Whiteboard" 
    public string Description { get; set; } = string.Empty; // Model number, specs, etc. 
                                                            
    // Navigation property — the rooms this equipment type is assigned to. 
    public ICollection<RoomEquipment> Rooms { get; set; } = [];
}
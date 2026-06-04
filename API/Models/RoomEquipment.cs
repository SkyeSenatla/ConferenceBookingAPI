namespace API.Models;
// Join entity for the Room ↔ Equipment many-to-many relationship. 
// Modelled explicitly because it carries Quantity — 
// a room can have two projectors, four chairs, three whiteboards. 
// A hidden join table cannot hold this column. 
public class RoomEquipment
{ 

public Guid RoomId { get; set; }
    public Guid EquipmentId { get; set; }
    // How many of this equipment item are in this room. 
    public int Quantity { get; set; } = 1;
    public Room Room { get; set; } = null!;
    public Equipment Equipment { get; set; } = null!;
}
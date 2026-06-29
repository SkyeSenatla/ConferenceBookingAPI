using API.Services;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;
[ApiController]
[Route("api/rooms")]
public class RoomsController(IRoomService roomService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rooms = await roomService.GetAllAsync();
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var room = await roomService.GetByIdAsync(id);
        return Ok(room);
    }
}

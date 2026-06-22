using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IAuthService authService) : ControllerBase
{
    // GET /api/users — Admin only.
    // Returns all registered users with their roles.
    // Any authenticated non-Admin caller receives 403 Forbidden.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAll() => Ok(authService.GetAllUsers());
}

using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Interfaces;
using CollegeSupport.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeSupport.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly AuthService _auth;

    public UsersController(IUserRepository users, AuthService auth)
    {
        _users = users;
        _auth = auth;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetAll() =>
        (await _users.GetAllAsync()).Select(u => u.ToDto()).ToList();

    /// <summary>Список техников для назначения на обращение.</summary>
    [HttpGet("technicians")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<List<UserDto>>> GetTechnicians() =>
        (await _users.GetTechniciansAsync()).Select(u => u.ToDto()).ToList();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        var user = await _auth.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetAll), user.ToDto());
    }
}

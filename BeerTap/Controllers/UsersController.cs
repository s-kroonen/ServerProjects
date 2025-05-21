using Microsoft.AspNetCore.Mvc;
using BeerTap.Repositories;
using BeerTap.Models;

namespace BeerTap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public UsersController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // GET: api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> GetUserById(Guid id)
    {
        var user = await _userRepository.GetUserAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // GET: api/users/by-userid/{userId}
    [HttpGet("by-userid/{userId}")]
    public async Task<ActionResult<User>> GetUserByUserId(string userId)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        var newUser = await _userRepository.CreateUserAsync(request.UserId, request.Pin);
        if (newUser == null)
            return Conflict("User already exists");

        return CreatedAtAction(nameof(GetUserById), new { id = newUser.ID }, newUser);
    }
    // POST: api/users/validate
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateUser([FromBody] ValidateUserDto dto)
    {
        var user = await _userRepository.GetUserAsync(dto.UserId);
        if (user == null)
            return Unauthorized("User not found");

        var isValid = string.IsNullOrEmpty(user.PinHash) && string.IsNullOrEmpty(dto.Pin)
            || (!string.IsNullOrEmpty(user.PinHash) && _userRepository.ValidateUserAsync(dto.UserId, dto.Pin, user.PinHash).Result);

        return isValid ? Ok("Valid") : Unauthorized("Invalid PIN");
    }

    public record ValidateUserDto(string UserId, string? Pin);


    // PUT: api/users/{userId}/score
    [HttpPut("{id:guid}/score")]
    public async Task<IActionResult> UpdateScore(Guid id, [FromBody] int newScore)
    {
        await _userRepository.UpdateUserScoreAsync(id, newScore);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/users — Returns a paginated list of users with optional search and role filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Page must be >= 1 and pageSize must be between 1 and 100.");

        var result = await _userService.GetAllUsersAsync(page, pageSize, search, role);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/users/{id} — Returns a single user by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user is null)
            return NotFound(ApiResponse<UserResponse>.Fail($"User with ID {id} not found."));

        return Ok(ApiResponse<UserResponse>.Ok(user));
    }

    /// <summary>
    /// POST /api/users — Creates a new user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// PUT /api/users/{id} — Updates an existing user's profile fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.UpdateUserAsync(id, request);
        if (!result.Success)
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/users/{id} — Permanently deletes a user.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/users/{id}/change-password — Changes a user's password.
    /// </summary>
    [HttpPatch("{id:int}/change-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.ChangePasswordAsync(id, request);
        if (!result.Success)
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/users/{id}/toggle-status — Activates or deactivates a user.
    /// </summary>
    [HttpPatch("{id:int}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var result = await _userService.ToggleUserStatusAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}

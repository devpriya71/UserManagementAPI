using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Get All (with pagination + search + role filter) ──────────────────────
    public async Task<PagedResponse<UserResponse>> GetAllUsersAsync(
        int page, int pageSize, string? search, string? role)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => MapToResponse(u))
            .ToListAsync();

        return new PagedResponse<UserResponse>
        {
            Data = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // ── Get By Id ─────────────────────────────────────────────────────────────
    public async Task<UserResponse?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user is null ? null : MapToResponse(user);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return ApiResponse<UserResponse>.Fail("A user with this email already exists.");

        if (request.Role != "Admin" && request.Role != "User")
            return ApiResponse<UserResponse>.Fail("Role must be 'Admin' or 'User'.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user {Email} with ID {Id}", user.Email, user.Id);
        return ApiResponse<UserResponse>.Ok(MapToResponse(user), "User created successfully.");
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public async Task<ApiResponse<UserResponse>> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return ApiResponse<UserResponse>.Fail("User not found.");

        // Check email uniqueness if changing
        if (!string.IsNullOrWhiteSpace(request.Email) &&
            request.Email.ToLower() != user.Email &&
            await _context.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return ApiResponse<UserResponse>.Fail("Email is already used by another user.");

        if (request.Role is not null && request.Role != "Admin" && request.Role != "User")
            return ApiResponse<UserResponse>.Fail("Role must be 'Admin' or 'User'.");

        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.LastName is not null)  user.LastName  = request.LastName.Trim();
        if (request.Email is not null)     user.Email     = request.Email.Trim().ToLower();
        if (request.Role is not null)      user.Role      = request.Role;
        if (request.IsActive.HasValue)     user.IsActive  = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user ID {Id}", id);
        return ApiResponse<UserResponse>.Ok(MapToResponse(user), "User updated successfully.");
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return ApiResponse<bool>.Fail("User not found.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted user ID {Id}", id);
        return ApiResponse<bool>.Ok(true, "User deleted successfully.");
    }

    // ── Change Password ───────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> ChangePasswordAsync(int id, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return ApiResponse<bool>.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<bool>.Fail("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user ID {Id}", id);
        return ApiResponse<bool>.Ok(true, "Password changed successfully.");
    }

    // ── Toggle Active Status ──────────────────────────────────────────────────
    public async Task<ApiResponse<UserResponse>> ToggleUserStatusAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return ApiResponse<UserResponse>.Fail("User not found.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var status = user.IsActive ? "activated" : "deactivated";
        return ApiResponse<UserResponse>.Ok(MapToResponse(user), $"User {status} successfully.");
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static UserResponse MapToResponse(User u) => new()
    {
        Id        = u.Id,
        FirstName = u.FirstName,
        LastName  = u.LastName,
        Email     = u.Email,
        Role      = u.Role,
        IsActive  = u.IsActive,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt
    };
}

using UserManagementAPI.DTOs;

namespace UserManagementAPI.Services;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetAllUsersAsync(int page, int pageSize, string? search, string? role);
    Task<UserResponse?> GetUserByIdAsync(int id);
    Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserResponse>> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(int id);
    Task<ApiResponse<bool>> ChangePasswordAsync(int id, ChangePasswordRequest request);
    Task<ApiResponse<UserResponse>> ToggleUserStatusAsync(int id);
}

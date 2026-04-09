using System.Security.Claims;
using FilmAPI.Model;

namespace FilmAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in claims");
    }

    public static string GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new UnauthorizedAccessException("Email not found in claims");
    }

    public static string GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? "";
    }

    public static UserRole GetUserRole(this ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleClaim))
        {
            return UserRole.User; // Default role
        }

        if (Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return role;
        }

        return UserRole.User;
    }

    public static bool IsInRole(this ClaimsPrincipal user, UserRole requiredRole)
    {
        var userRole = user.GetUserRole();
        return userRole == requiredRole || userRole == UserRole.Admin;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.GetUserRole() == UserRole.Admin;
    }

    public static bool IsPowerUserOrAbove(this ClaimsPrincipal user)
    {
        var role = user.GetUserRole();
        return role == UserRole.Admin || role == UserRole.PowerUser;
    }
}

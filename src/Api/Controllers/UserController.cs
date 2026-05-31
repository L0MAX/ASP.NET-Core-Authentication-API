using Application.Common.Constants;
using Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Authenticated user endpoints. Requires User or Admin role.
/// </summary>
[ApiController]
[Route("api/user")]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
public class UserController : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public IActionResult GetDashboard()
    {
        var email = User.FindFirstValue(JwtClaimTypes.Email) ?? "unknown";
        var roles = User.FindAll(JwtClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            message = $"Hello, {email}",
            roles
        }));
    }

    [HttpGet("activity")]
    [Authorize(Roles = $"{AuthRoles.User},{AuthRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetActivity()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            recentLogins = Array.Empty<string>(),
            lastActive = DateTime.UtcNow
        }));
    }
}

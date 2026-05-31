using Application.Common.Constants;
using Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Admin-only endpoints. Requires the Admin role.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = AuthRoles.Admin)]
public class AdminController : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public IActionResult GetDashboard()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            message = "Welcome to the admin dashboard.",
            totalUsers = 0,
            pendingVerifications = 0
        }));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public IActionResult GetSettings()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            maintenanceMode = false,
            allowRegistration = true
        }));
    }
}

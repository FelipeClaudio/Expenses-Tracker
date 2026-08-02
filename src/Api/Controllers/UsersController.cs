using System.Security.Claims;
using Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var displayName = User.FindFirstValue(ClaimTypes.Name)!;
        var avatarUrl = User.FindFirstValue("avatar_url");

        return Ok(new AuthenticatedUserResponse(id, email, displayName, avatarUrl));
    }
}

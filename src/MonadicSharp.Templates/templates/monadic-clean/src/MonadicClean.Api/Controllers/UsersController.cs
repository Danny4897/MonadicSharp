using MonadicSharp;
using MonadicClean.Application.Users.Queries;
using MonadicClean.Application.Users.Commands;
using MonadicClean.Application.DTOs;
using MediatR;

namespace MonadicClean.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _mediator.Send(new GetAllUsersQuery());

        return result.Match(
            success: users => Ok(new { success = true, data = users }),
            failure: error => BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
        );
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));

        return result.Match(
            success: user => Ok(new { success = true, data = user }),
            failure: error => error.Message.Contains("not found")
                ? NotFound(new { success = false, error = new { code = error.Code, message = error.Message } })
                : BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
        );
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user</returns>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        var result = await _mediator.Send(new CreateUserCommand(request.Name, request.Email));

        return result.Match(
            success: user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { success = true, data = user }),
            failure: error => error.Message.Contains("already exists")
                ? Conflict(new { success = false, error = new { code = error.Code, message = error.Message } })
                : BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
        );
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto request)
    {
        var result = await _mediator.Send(new UpdateUserCommand(id, request.Name, request.Email, request.IsActive));

        return result.Match(
            success: user => Ok(new { success = true, data = user }),
            failure: error => error.Message.Contains("not found")
                ? NotFound(new { success = false, error = new { code = error.Code, message = error.Message } })
                : error.Message.Contains("already in use")
                    ? Conflict(new { success = false, error = new { code = error.Code, message = error.Message } })
                    : BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
        );
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _mediator.Send(new DeleteUserCommand(id));

        return result.Match(
            success: _ => Ok(new { success = true, message = "User deleted successfully" }),
            failure: error => error.Message.Contains("not found")
                ? NotFound(new { success = false, error = new { code = error.Code, message = error.Message } })
                : BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
        );
    }
}

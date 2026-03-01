namespace MonadicClean.Application.DTOs;

public record UserDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateUserDto
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record UpdateUserDto
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public bool? IsActive { get; init; }
}

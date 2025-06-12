using MonadicSharp;
using MonadicClean.Domain.Common;
using MonadicClean.Domain.ValueObjects;

namespace MonadicClean.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }

    private User() { } // For EF

    private User(string name, Email email)
    {
        Name = name;
        Email = email;
        IsActive = true;
    }

    public static Result<User> Create(string name, string email)
    {
        return ValidateName(name)
            .Bind(_ => Email.Create(email))
            .Map(validEmail => new User(name, validEmail));
    }

    public Result<Unit> UpdateName(string newName)
    {
        return ValidateName(newName)
            .Map(validName =>
            {
                Name = validName;
                UpdateTimestamp();
                return Unit.Value;
            });
    }

    public Result<Unit> UpdateEmail(string newEmail)
    {
        return Email.Create(newEmail)
            .Map(validEmail =>
            {
                Email = validEmail;
                UpdateTimestamp();
                return Unit.Value;
            });
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    private static Result<string> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Failure(Error.Create("Name cannot be empty"));

        if (name.Length > 100)
            return Result<string>.Failure(Error.Create("Name cannot exceed 100 characters"));

        return Result<string>.Success(name.Trim());
    }
}

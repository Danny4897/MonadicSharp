using MonadicSharp;

namespace MonadicClean.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure(Error.Create("Email cannot be empty"));

        if (!IsValidFormat(email))
            return Result<Email>.Failure(Error.Create("Invalid email format"));

        if (email.Length > 255)
            return Result<Email>.Failure(Error.Create("Email cannot exceed 255 characters"));

        return Result<Email>.Success(new Email(email.ToLowerInvariant()));
    }

    private static bool IsValidFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}

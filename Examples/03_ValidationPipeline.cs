// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 03 — VALIDATION PIPELINE
//      Multi-rule, multi-field validation with accumulated errors
// ============================================================
//
//  Two strategies:
//
//  FAIL-FAST  (Bind chain): stop at first error
//    → Good for dependent validations (validate name before using it)
//
//  ACCUMULATE (Sequence / Combine): collect all errors at once
//    → Good for forms where you want all field errors in one pass
// ============================================================

static class ValidationExamples
{
    record RegistrationRequest(string Name, string Email, string Password, int Age);
    record ValidatedRegistration(string Name, string Email, string HashedPassword, int Age);

    // ── 3a. Individual field validators ───────────────────────────────────

    static Result<string> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Name is required", field: "name");

        var trimmed = name.Trim();
        if (trimmed.Length < 2)
            return Error.Validation("Name must be at least 2 characters", field: "name");

        if (trimmed.Length > 50)
            return Error.Validation("Name cannot exceed 50 characters", field: "name");

        return trimmed;
    }

    static Result<string> ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("Email is required", field: "email");

        var lower = email.Trim().ToLower();
        if (!lower.Contains('@') || !lower.Contains('.'))
            return Error.Validation("Invalid email format", field: "email");

        return lower;
    }

    static Result<string> ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Error.Validation("Password is required", field: "password");

        if (password.Length < 8)
            return Error.Validation("Password must be at least 8 characters", field: "password");

        if (!password.Any(char.IsUpper))
            return Error.Validation("Password must contain at least one uppercase letter", field: "password");

        if (!password.Any(char.IsDigit))
            return Error.Validation("Password must contain at least one digit", field: "password");

        return HashPassword(password);
    }

    static Result<int> ValidateAge(int age) =>
        age is >= 18 and <= 120
            ? Result<int>.Success(age)
            : Error.Validation($"Age must be between 18 and 120 (got {age})", field: "age");

    // ── 3b. FAIL-FAST: Bind chain (stops at first error) ──────────────────

    static Result<ValidatedRegistration> ValidateFailFast(RegistrationRequest req) =>
        ValidateName(req.Name)
            .Bind(name   => ValidateEmail(req.Email)
            .Bind(email  => ValidatePassword(req.Password)
            .Bind(hash   => ValidateAge(req.Age)
            .Map(age     => new ValidatedRegistration(name, email, hash, age)))));

    // ── 3c. ACCUMULATE: Sequence — all errors at once ─────────────────────

    static Result<ValidatedRegistration> ValidateAccumulated(RegistrationRequest req)
    {
        // Collect all field results
        var nameResult     = ValidateName(req.Name);
        var emailResult    = ValidateEmail(req.Email);
        var passwordResult = ValidatePassword(req.Password);
        var ageResult      = ValidateAge(req.Age);

        // Combine failures if any exist
        var failures = new[] { nameResult.IsFailure, emailResult.IsFailure,
                               passwordResult.IsFailure, ageResult.IsFailure };

        if (failures.Any(f => f))
        {
            var errors = new List<Error>();
            if (nameResult.IsFailure)     errors.Add(nameResult.Error);
            if (emailResult.IsFailure)    errors.Add(emailResult.Error);
            if (passwordResult.IsFailure) errors.Add(passwordResult.Error);
            if (ageResult.IsFailure)      errors.Add(ageResult.Error);

            return Result<ValidatedRegistration>.Failure(
                errors.Count == 1 ? errors[0] : Error.Combine([.. errors]));
        }

        return Result<ValidatedRegistration>.Success(
            new(nameResult.Value, emailResult.Value, passwordResult.Value, ageResult.Value));
    }

    // ── 3d. Using Ensure for post-validation business rules ───────────────

    static Result<RegistrationRequest> CheckEmailNotTaken(RegistrationRequest req, IEnumerable<string> existingEmails) =>
        Result<RegistrationRequest>.Success(req)
            .Ensure(r => !existingEmails.Contains(r.Email.ToLower()),
                    Error.Conflict("Email already registered", resource: "User"))
            .Ensure(r => !r.Name.Contains("admin", StringComparison.OrdinalIgnoreCase),
                    Error.Validation("Name cannot contain 'admin'", field: "name"));

    // ── 3e. Full service combining validation + business rules ─────────────

    class RegistrationService
    {
        private readonly HashSet<string> _emails = ["existing@example.com"];

        public Result<ValidatedRegistration> Register(RegistrationRequest req) =>
            CheckEmailNotTaken(req, _emails)
                .Bind(r => ValidateAccumulated(r));

        public string Process(RegistrationRequest req) =>
            Register(req).Match(
                onSuccess: v  => $"Registered: {v.Name} <{v.Email}>",
                onFailure: e  => FormatErrors(e)
            );

        static string FormatErrors(Error error)
        {
            var all = error.GetAllErrors().ToList();
            if (all.Count == 1)
                return $"Validation failed: {all[0].Message}";

            var messages = string.Join("\n  ", all.Select(e => $"• [{e.Metadata.GetValueOrDefault("Field", "?")}] {e.Message}"));
            return $"Validation failed ({all.Count} errors):\n  {messages}";
        }
    }

    // ── 3f. Traverse — validate a batch of items ──────────────────────────

    static Result<IEnumerable<string>> ValidateEmails(IEnumerable<string> emails) =>
        emails.Traverse(ValidateEmail);  // first failure aborts; or use Sequence for accumulate

    static string HashPassword(string plain) => $"hashed:{plain}";
}

namespace MonadicSharp;

/// <summary>
/// Exception that carries a structured <see cref="Error"/>.
/// Used when interop with exception-based APIs is required (e.g. Application Insights).
/// </summary>
public sealed class MonadicException : Exception
{
    public Error Error { get; }

    public MonadicException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    public MonadicException(Error error, Exception innerException)
        : base(error.Message, innerException)
    {
        Error = error;
    }
}

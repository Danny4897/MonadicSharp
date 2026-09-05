#nullable enable
using Microsoft.AspNetCore.Http;

namespace MonadicSharp.Interop.AspNetCore;

/// <summary>
/// Bridges MonadicSharp <see cref="Result{T}"/> with ASP.NET Core
/// minimal API <see cref="IResult"/> responses.
/// </summary>
public static class ResultMinimalApiExtensions
{
    /// <summary>
    /// Converts a <c>Result&lt;T&gt;</c> to an <see cref="IResult"/> using the provided
    /// <paramref name="onSuccess"/> mapping and a default error-to-status mapping.
    /// </summary>
    /// <example>
    /// <code>
    /// app.MapGet("/users/{id}", async (int id, UserService svc) =>
    ///     (await svc.GetByIdAsync(id)).ToMinimalApiResult(user => Results.Ok(user)));
    /// </code>
    /// </example>
    public static IResult ToMinimalApiResult<T>(this Result<T> result,
        Func<T, IResult> onSuccess) =>
        result.Match(
            onSuccess: onSuccess,
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound   => Results.NotFound(new { error.Message, error.Code }),
                ErrorType.Validation => Results.BadRequest(new { error.Message, error.Code }),
                ErrorType.Forbidden  => Results.Forbid(),
                ErrorType.Conflict   => Results.Conflict(new { error.Message, error.Code }),
                _                    => Results.Problem(error.Message, statusCode: 500)
            });

    /// <summary>
    /// Converts a <c>Result&lt;T&gt;</c> to an <see cref="IResult"/>, using <see cref="Results.Ok{T}"/>
    /// on success and the default error-to-status mapping on failure.
    /// </summary>
    public static IResult ToOkResult<T>(this Result<T> result) =>
        result.ToMinimalApiResult(value => Results.Ok(value));

    /// <summary>
    /// Converts a <c>Result&lt;T&gt;</c> to an <see cref="IResult"/>, using <see cref="Results.Created"/>
    /// on success and the default error-to-status mapping on failure.
    /// </summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, string location) =>
        result.ToMinimalApiResult(value => Results.Created(location, value));

    /// <summary>
    /// Asynchronous variant: awaits the task and converts the <c>Result&lt;T&gt;</c> to
    /// an <see cref="IResult"/>.
    /// </summary>
    public static async Task<IResult> ToMinimalApiResult<T>(this Task<Result<T>> resultTask,
        Func<T, IResult> onSuccess)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(onSuccess);
    }

    /// <summary>
    /// Asynchronous variant of <see cref="ToOkResult{T}"/>.
    /// </summary>
    public static async Task<IResult> ToOkResult<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;
        return result.ToOkResult();
    }
}

using System.Net;
using FluentAssertions;
using MonadicSharp.Interop;
using MonadicSharp.Interop.Http;

namespace MonadicSharp.Interop.Tests;

public class HttpErrorMapperTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest,          ErrorType.Validation)]
    [InlineData(HttpStatusCode.Unauthorized,        ErrorType.Forbidden)]
    [InlineData(HttpStatusCode.Forbidden,           ErrorType.Forbidden)]
    [InlineData(HttpStatusCode.NotFound,            ErrorType.NotFound)]
    [InlineData(HttpStatusCode.Conflict,            ErrorType.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.Failure)]
    public void Map_MapsStatusCodeToCorrectErrorType(HttpStatusCode code, ErrorType expected)
    {
        var error = HttpErrorMapper.Map(code);
        error.Type.Should().Be(expected);
    }

    [Fact]
    public void Map_IncludesStatusCodeInCode()
    {
        var error = HttpErrorMapper.Map(HttpStatusCode.NotFound);
        error.Code.Should().Be("HTTP_404");
    }
}

public class HttpResponseMessageExtensionsTests
{
    [Fact]
    public void ToResult_OnSuccessResponse_ReturnsSuccessUnit()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        var result = response.ToResult();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToResult_OnNotFoundResponse_ReturnsNotFoundFailure()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var result = response.ToResult();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void ToResult_OnServerError_ReturnsFailure()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var result = response.ToResult();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("HTTP_500");
    }
}

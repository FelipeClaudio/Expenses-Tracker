using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.IntegrationTests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_AnyException_Writes500ProblemDetailsWithoutLeakingExceptionDetails()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("some sensitive internal detail");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(httpContext.Response.Body);

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem!.Status);
        Assert.DoesNotContain("sensitive internal detail", problem.Title ?? string.Empty);
        Assert.DoesNotContain("sensitive internal detail", problem.Detail ?? string.Empty);
    }
}

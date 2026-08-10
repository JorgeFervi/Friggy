using System.Text.Json;
using Friggy.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.IntegrationTests.Api;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DbUpdateException_ReturnsSafeConflictWithoutTechnicalDetail()
    {
        const string technicalDetail = "Sensitive PostgreSQL constraint detail.";
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        await using var responseBody = new MemoryStream();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Response.Body = responseBody;
        var handler = new ApiExceptionHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new DbUpdateException(technicalDetail),
            TestContext.Current.CancellationToken);
        responseBody.Position = 0;
        using var problem = await JsonDocument.ParseAsync(
            responseBody,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal(
            "persistence.conflict",
            problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            "La operación entra en conflicto con el estado actual de los datos.",
            problem.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain(technicalDetail, problem.RootElement.GetRawText(), StringComparison.Ordinal);
    }
}

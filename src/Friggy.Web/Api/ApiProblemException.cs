using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.Web.Api;

public sealed class ApiProblemException : Exception
{
    public ApiProblemException(string message)
        : this(
            HttpStatusCode.InternalServerError,
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = message,
            })
    {
    }

    private ApiProblemException(HttpStatusCode statusCode, ProblemDetails problemDetails)
        : base(problemDetails.Detail ?? problemDetails.Title ?? "No se pudo completar la operación.")
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }

    public HttpStatusCode StatusCode { get; }

    public ProblemDetails ProblemDetails { get; }

    public static async Task<ApiProblemException> FromResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        ProblemDetails? problemDetails = null;
        try
        {
            problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // The fallback below preserves the HTTP status when the response is not ProblemDetails.
        }

        problemDetails ??= new ProblemDetails
        {
            Title = "No se pudo completar la operación.",
        };
        problemDetails.Status ??= (int)response.StatusCode;

        return new ApiProblemException(response.StatusCode, problemDetails);
    }
}

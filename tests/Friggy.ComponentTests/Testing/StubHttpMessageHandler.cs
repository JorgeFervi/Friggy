using System.Net;
using System.Text;

namespace Friggy.ComponentTests.Testing;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> responses = new();

    public HttpRequestMessage? LastRequest { get; private set; }

    public List<(HttpMethod Method, Uri? Uri)> Requests { get; } = [];

    public void RespondWith(string mediaType, string content)
    {
        RespondWith(HttpStatusCode.OK, mediaType, content);
    }

    public void RespondWith(HttpStatusCode statusCode, string mediaType, string content)
    {
        responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType),
        });
    }

    public void RespondWith(HttpStatusCode statusCode)
    {
        responses.Enqueue(new HttpResponseMessage(statusCode));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;
        Requests.Add((request.Method, request.RequestUri));

        if (!responses.TryDequeue(out var response))
        {
            throw new InvalidOperationException(
                "El fake HTTP necesita una respuesta configurada mediante RespondWith.");
        }

        return Task.FromResult(response);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}

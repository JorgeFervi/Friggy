using System.Net;
using System.Text;

namespace Friggy.ComponentTests.Testing;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> responses = new();

    public HttpRequestMessage? LastRequest { get; private set; }

    public void RespondWith(string mediaType, string content)
    {
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType),
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;

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

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Testing;

public abstract class ComponentTest : IDisposable
{
    private readonly BunitContext context = new();

    protected ComponentTest()
    {
        Api = new StubHttpMessageHandler();
        ApiClient = new HttpClient(Api)
        {
            BaseAddress = new Uri("http://localhost"),
        };
        context.Services.AddSingleton(ApiClient);
    }

    protected StubHttpMessageHandler Api { get; }

    protected HttpClient ApiClient { get; }

    protected IServiceCollection Services => context.Services;

    protected IRenderedComponent<TComponent> Render<TComponent>()
        where TComponent : IComponent => context.Render<TComponent>();

    public void Dispose()
    {
        context.Dispose();
        ApiClient.Dispose();
        GC.SuppressFinalize(this);
    }
}

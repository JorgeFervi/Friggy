using System.Reflection;
using System.Runtime.Versioning;

namespace Friggy.IntegrationTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void InfrastructureAssembly_IsReferencedAndTargetsNet10()
    {
        var assembly = typeof(global::Friggy.Infrastructure.AssemblyMarker).Assembly;
        var targetFramework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.Equal("Friggy.Infrastructure", assembly.GetName().Name);
        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework?.FrameworkName);
    }
}

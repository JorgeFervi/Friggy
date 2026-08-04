using System.Reflection;
using System.Runtime.Versioning;

namespace Friggy.Domain.Tests;

public sealed class ProjectReferenceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DomainAssembly_IsReferencedAndTargetsNet10()
    {
        var assembly = typeof(global::Friggy.Domain.AssemblyMarker).Assembly;
        var targetFramework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.Equal("Friggy.Domain", assembly.GetName().Name);
        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework?.FrameworkName);
    }
}

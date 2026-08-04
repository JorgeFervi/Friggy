using System.Reflection;
using System.Runtime.Versioning;

namespace Friggy.Application.Tests;

public sealed class ProjectReferenceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void ApplicationAssembly_IsReferencedAndTargetsNet10()
    {
        var assembly = typeof(global::Friggy.Application.AssemblyMarker).Assembly;
        var targetFramework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.Equal("Friggy.Application", assembly.GetName().Name);
        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework?.FrameworkName);
    }
}

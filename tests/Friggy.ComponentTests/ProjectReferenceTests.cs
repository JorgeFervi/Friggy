using System.Reflection;
using System.Runtime.Versioning;

namespace Friggy.ComponentTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    [Trait("Category", "Component")]
    public void WebAssembly_IsReferencedAndTargetsNet10()
    {
        var assembly = typeof(global::Friggy.Web.AssemblyMarker).Assembly;
        var targetFramework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.Equal("Friggy.Web", assembly.GetName().Name);
        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework?.FrameworkName);
    }
}

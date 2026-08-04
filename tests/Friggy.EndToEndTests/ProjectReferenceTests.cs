using System.Reflection;
using System.Runtime.Versioning;

namespace Friggy.EndToEndTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    [Trait("Category", "E2E")]
    public void HostAssemblies_AreReferencedAndTargetNet10()
    {
        var assemblies = new[]
        {
            typeof(global::Friggy.Api.AssemblyMarker).Assembly,
            typeof(global::Friggy.Web.AssemblyMarker).Assembly
        };

        Assert.Equal(
            ["Friggy.Api", "Friggy.Web"],
            assemblies.Select(assembly => assembly.GetName().Name));
        Assert.All(assemblies, assembly =>
            Assert.Equal(
                ".NETCoreApp,Version=v10.0",
                assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName));
    }
}

using System.Reflection;

namespace Friggy.IntegrationTests.Architecture;

public sealed class CompositionRootContractTests
{
    [Theory]
    [Trait("Category", "Architecture")]
    [InlineData(typeof(global::Friggy.Application.AssemblyMarker), "Friggy.Application.DependencyInjection", "AddApplication")]
    [InlineData(typeof(global::Friggy.Infrastructure.AssemblyMarker), "Friggy.Infrastructure.DependencyInjection", "AddInfrastructure")]
    public void Layer_ExposesOnePublicRegistrationMethod(
        Type assemblyMarker,
        string registrationTypeName,
        string registrationMethodName)
    {
        var registrationType = assemblyMarker.Assembly.GetType(registrationTypeName);

        Assert.NotNull(registrationType);

        var registrationMethods = registrationType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == registrationMethodName)
            .ToArray();

        Assert.Single(registrationMethods);
    }
}

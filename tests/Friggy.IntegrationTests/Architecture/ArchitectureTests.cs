using System.Reflection;
using System.Xml.Linq;

namespace Friggy.IntegrationTests.Architecture;

public sealed class ArchitectureTests
{
    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_Dependencies_PointInward()
    {
        AssertProjectReferences("src/Friggy.Domain/Friggy.Domain.csproj", []);
        AssertPackageReferences("src/Friggy.Domain/Friggy.Domain.csproj", []);
        AssertAssemblyDoesNotReference(
            typeof(global::Friggy.Domain.AssemblyMarker).Assembly,
            "Friggy.Application",
            "Friggy.Infrastructure",
            "Friggy.Api",
            "Friggy.Web",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Application_Dependencies_PointInward()
    {
        AssertProjectReferences(
            "src/Friggy.Application/Friggy.Application.csproj",
            ["Friggy.Domain"]);
        AssertPackageReferences("src/Friggy.Application/Friggy.Application.csproj", []);
        AssertAssemblyDoesNotReference(
            typeof(global::Friggy.Application.AssemblyMarker).Assembly,
            "Friggy.Infrastructure",
            "Friggy.Api",
            "Friggy.Web",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Infrastructure_Dependencies_PointInward()
    {
        AssertProjectReferences(
            "src/Friggy.Infrastructure/Friggy.Infrastructure.csproj",
            ["Friggy.Application", "Friggy.Domain"]);
        AssertPackageReferencesDoNotStartWith(
            "src/Friggy.Infrastructure/Friggy.Infrastructure.csproj",
            "Microsoft.AspNetCore");
        AssertAssemblyDoesNotReference(
            typeof(global::Friggy.Infrastructure.AssemblyMarker).Assembly,
            "Friggy.Api",
            "Friggy.Web",
            "Microsoft.AspNetCore");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Api_Dependencies_PointInward()
    {
        AssertProjectReferences(
            "src/Friggy.Api/Friggy.Api.csproj",
            ["Friggy.Application", "Friggy.Infrastructure"]);
        AssertPackageReferencesDoNotStartWith(
            "src/Friggy.Api/Friggy.Api.csproj",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
        AssertAssemblyDoesNotReference(
            typeof(global::Friggy.Api.AssemblyMarker).Assembly,
            "Friggy.Domain",
            "Friggy.Web");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Web_Dependencies_UseOnlyTransportContracts()
    {
        AssertProjectReferences(
            "src/Friggy.Web/Friggy.Web.csproj",
            ["Friggy.Application"]);
        AssertPackageReferencesDoNotStartWith(
            "src/Friggy.Web/Friggy.Web.csproj",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
        AssertAssemblyDoesNotReference(
            typeof(global::Friggy.Web.AssemblyMarker).Assembly,
            "Friggy.Domain",
            "Friggy.Infrastructure",
            "Friggy.Api",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    private static void AssertProjectReferences(
        string projectPath,
        IReadOnlyCollection<string> expected)
    {
        var actual = ArchitectureRules.GetProjectReferences(projectPath);

        Assert.Equal(expected.Order(), actual.Order());
    }

    private static void AssertPackageReferences(
        string projectPath,
        IReadOnlyCollection<string> expected)
    {
        var actual = ArchitectureRules.GetPackageReferences(projectPath);

        Assert.Equal(expected.Order(), actual.Order());
    }

    private static void AssertAssemblyDoesNotReference(
        Assembly assembly,
        params string[] forbiddenPrefixes)
    {
        var references = ArchitectureRules.GetAssemblyReferences(assembly);

        Assert.DoesNotContain(
            references,
            reference => forbiddenPrefixes.Any(prefix =>
                reference.StartsWith(prefix, StringComparison.Ordinal)));
    }

    private static void AssertPackageReferencesDoNotStartWith(
        string projectPath,
        params string[] forbiddenPrefixes)
    {
        var references = ArchitectureRules.GetPackageReferences(projectPath);

        Assert.DoesNotContain(
            references,
            reference => forbiddenPrefixes.Any(prefix =>
                reference.StartsWith(prefix, StringComparison.Ordinal)));
    }
}

internal static class ArchitectureRules
{
    private static readonly Lazy<string> SolutionRoot = new(FindSolutionRoot);

    public static IReadOnlyCollection<string> GetProjectReferences(string projectPath)
    {
        var projectFile = ResolveProjectFile(projectPath);
        var projectDirectory = projectFile.DirectoryName
            ?? throw new InvalidOperationException(
                $"El proyecto '{projectFile.FullName}' no tiene directorio.");

        return LoadProject(projectFile)
            .Descendants("ProjectReference")
            .Select(reference => GetRequiredInclude(reference, projectFile))
            .Select(include => include.Replace('\\', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyCollection<string> GetPackageReferences(string projectPath)
    {
        var projectFile = ResolveProjectFile(projectPath);

        return LoadProject(projectFile)
            .Descendants("PackageReference")
            .Select(reference => GetRequiredInclude(reference, projectFile))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyCollection<string> GetAssemblyReferences(Assembly assembly) =>
        assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static XDocument LoadProject(FileInfo projectFile) =>
        XDocument.Load(projectFile.FullName, LoadOptions.None);

    private static FileInfo ResolveProjectFile(string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath, SolutionRoot.Value);
        var projectFile = new FileInfo(fullPath);

        return projectFile.Exists
            ? projectFile
            : throw new FileNotFoundException(
                $"No se encontró el proyecto '{projectPath}'.",
                fullPath);
    }

    private static string GetRequiredInclude(XElement reference, FileInfo projectFile) =>
        reference.Attribute("Include")?.Value
        ?? throw new InvalidDataException(
            $"Una referencia de '{projectFile.FullName}' no define Include.");

    private static string FindSolutionRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Friggy.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"No se encontró Friggy.sln desde '{AppContext.BaseDirectory}'.");
    }
}

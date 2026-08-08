using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Tests.Catalogs;

public sealed class RecipeTagTests
{
    [Fact]
    public void Create_ValidName_CreatesNormalizedTag()
    {
        var tag = RecipeTag.Create(" Sin gluten ");

        Assert.Equal("Sin gluten", tag.Name.Value);
        Assert.Equal("SIN GLUTEN", tag.Name.Normalized);
    }
}

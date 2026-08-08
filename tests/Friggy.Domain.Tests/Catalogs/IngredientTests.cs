using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Tests.Catalogs;

public sealed class IngredientTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_NameIsBlank_ThrowsDomainValidationException(string name)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Ingredient.Create(name));

        Assert.Equal("ingredient.name.required", exception.Code);
    }

    [Fact]
    public void Create_NameHasWhitespace_TrimsAndNormalizesName()
    {
        var ingredient = Ingredient.Create("  Tomáte  ");

        Assert.Equal("Tomáte", ingredient.Name.Value);
        Assert.Equal("TOMÁTE", ingredient.Name.Normalized);
        Assert.NotEqual(Guid.Empty, ingredient.Id);
    }

    [Fact]
    public void Rename_ValidName_ChangesNameAndPreservesIdentity()
    {
        var ingredient = Ingredient.Create("Tomate");
        var id = ingredient.Id;

        ingredient.Rename(" Cebolla ");

        Assert.Equal(id, ingredient.Id);
        Assert.Equal("Cebolla", ingredient.Name.Value);
    }
}

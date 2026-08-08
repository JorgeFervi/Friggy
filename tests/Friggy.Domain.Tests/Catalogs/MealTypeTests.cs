using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Tests.Catalogs;

public sealed class MealTypeTests
{
    [Fact]
    public void Create_NegativeOrder_ThrowsDomainValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(() => MealType.Create("Cena", -1));

        Assert.Equal("meal-type.order.non-negative", exception.Code);
    }

    [Fact]
    public void Update_ValidValues_ChangesNameAndOrder()
    {
        var mealType = MealType.Create("Cena", 2);

        mealType.Update("Merienda", 3);

        Assert.Equal("Merienda", mealType.Name.Value);
        Assert.Equal(3, mealType.Order);
    }
}

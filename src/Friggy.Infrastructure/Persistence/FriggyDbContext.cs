using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence;

public sealed class FriggyDbContext(DbContextOptions<FriggyDbContext> options)
    : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();

    public DbSet<MealType> MealTypes => Set<MealType>();

    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FriggyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

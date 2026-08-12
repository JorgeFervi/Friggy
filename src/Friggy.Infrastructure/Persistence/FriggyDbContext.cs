using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;
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

    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();

    public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();

    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FriggyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

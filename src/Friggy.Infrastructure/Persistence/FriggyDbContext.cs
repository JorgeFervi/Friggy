using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;
using Friggy.Domain.DailyPlanTemplates;
using Friggy.Domain.Inventory;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence;

/// <summary>
/// Contexto de Entity Framework Core que representa la persistencia de Friggy.
/// </summary>
public sealed class FriggyDbContext(DbContextOptions<FriggyDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// Conjunto persistido de ingredientes.
    /// </summary>
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    /// <summary>
    /// Conjunto persistido de unidades de medida.
    /// </summary>
    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    /// <summary>
    /// Conjunto persistido de etiquetas de receta.
    /// </summary>
    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();

    /// <summary>
    /// Conjunto persistido de tipos de comida.
    /// </summary>
    public DbSet<MealType> MealTypes => Set<MealType>();

    /// <summary>
    /// Conjunto persistido de recetas.
    /// </summary>
    public DbSet<Recipe> Recipes => Set<Recipe>();

    /// <summary>
    /// Conjunto persistido de planes diarios.
    /// </summary>
    public DbSet<DailyPlan> DailyPlans => Set<DailyPlan>();

    public DbSet<DailyPlanTemplate> DailyPlanTemplates => Set<DailyPlanTemplate>();

    public DbSet<DailyPlanTemplateMeal> DailyPlanTemplateMeals => Set<DailyPlanTemplateMeal>();

    /// <summary>
    /// Conjunto persistido de asignaciones de comidas.
    /// </summary>
    public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();

    /// <summary>
    /// Conjunto persistido de huecos de comida.
    /// </summary>
    public DbSet<MealPlanSlot> MealPlanSlots => Set<MealPlanSlot>();

    /// <summary>
    /// Conjunto persistido de lotes de inventario.
    /// </summary>
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();

    /// <summary>
    /// Conjunto persistido de movimientos de inventario.
    /// </summary>
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    /// <summary>
    /// Método que aplica las configuraciones de las entidades del dominio.
    /// </summary>
    /// <param name="modelBuilder">
    /// Constructor del modelo de Entity Framework Core.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FriggyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

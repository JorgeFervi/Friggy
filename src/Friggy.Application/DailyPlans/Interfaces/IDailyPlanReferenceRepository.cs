using Friggy.Domain.Catalogs;

namespace Friggy.Application.DailyPlans.Interfaces;

/// <summary>Puerto de lectura de referencias de planificación.</summary>
public interface IDailyPlanReferenceRepository
{
    /// <summary>Comprueba si existe una receta.</summary>
    Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Obtiene en una sola lectura los tiempos de las recetas indicadas.</summary>
    Task<IReadOnlyDictionary<Guid, TimeSpan?>> GetRecipeEstimatedTimesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>Comprueba si existe un tipo de comida.</summary>
    Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lista los tipos de comida.</summary>
    Task<IReadOnlyList<MealType>> ListMealTypesAsync(CancellationToken cancellationToken);
}

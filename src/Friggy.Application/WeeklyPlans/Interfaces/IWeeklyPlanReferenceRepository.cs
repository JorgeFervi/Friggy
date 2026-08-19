using Friggy.Domain.Catalogs;

namespace Friggy.Application.WeeklyPlans.Interfaces;

/// <summary>
/// Contrato para consultar las referencias necesarias para un plan semanal.
/// </summary>
public interface IWeeklyPlanReferenceRepository
{
    /// <summary>
    /// Comprueba que exista una receta.
    /// </summary>
    Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene el tiempo estimado de una receta.
    /// </summary>
    Task<TimeSpan?> GetRecipeEstimatedTimeAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Comprueba que exista un tipo de comida.
    /// </summary>
    Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene los tipos de comida disponibles.
    /// </summary>
    Task<IReadOnlyList<MealType>> ListMealTypesAsync(CancellationToken cancellationToken);
}

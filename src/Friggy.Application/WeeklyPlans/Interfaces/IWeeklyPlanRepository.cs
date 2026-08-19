using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.WeeklyPlans.Interfaces;

/// <summary>
/// Contrato de persistencia para los planes semanales.
/// </summary>
public interface IWeeklyPlanRepository
{
    /// <summary>
    /// Obtiene todos los planes semanales completos.
    /// </summary>
    Task<IReadOnlyList<WeeklyPlan>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene los datos resumidos de los planes para listados.
    /// </summary>
    Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListSummariesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Busca un plan semanal por su identificador.
    /// </summary>
    Task<WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Comprueba si ya existe un plan con el nombre normalizado indicado.
    /// </summary>
    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Añade un plan semanal para su persistencia.
    /// </summary>
    Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken);

    /// <summary>
    /// Marca un plan semanal para eliminarlo.
    /// </summary>
    void Remove(WeeklyPlan plan);

    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

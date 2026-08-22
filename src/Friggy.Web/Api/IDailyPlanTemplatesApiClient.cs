using Friggy.Application.DailyPlanTemplates.Dtos;

namespace Friggy.Web.Api;

public interface IDailyPlanTemplatesApiClient
{
    Task<IReadOnlyList<DailyPlanTemplateResponse>> ListAsync(CancellationToken cancellationToken);
    Task<DailyPlanTemplateResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DailyPlanTemplateResponse> CreateAsync(CreateDailyPlanTemplateRequest request, CancellationToken cancellationToken);
    Task<DailyPlanTemplateResponse> UpdateAsync(Guid id, UpdateDailyPlanTemplateRequest request, CancellationToken cancellationToken);
    Task<ApplyDailyPlanTemplateResponse> ApplyAsync(Guid id, ApplyDailyPlanTemplateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

using Friggy.Application.DailyPlanTemplates.Dtos;

namespace Friggy.Web.Api;

public interface IDailyPlanTemplatesApiClient
{
    Task<IReadOnlyList<DailyPlanTemplateResponse>> ListAsync(CancellationToken cancellationToken);
    Task<DailyPlanTemplateResponse> CreateAsync(CreateDailyPlanTemplateRequest request, CancellationToken cancellationToken);
    Task<ApplyDailyPlanTemplateResponse> ApplyAsync(Guid id, ApplyDailyPlanTemplateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

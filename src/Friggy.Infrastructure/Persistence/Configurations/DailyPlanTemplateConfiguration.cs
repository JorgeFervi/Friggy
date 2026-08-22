using Friggy.Domain.DailyPlanTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class DailyPlanTemplateConfiguration : IEntityTypeConfiguration<DailyPlanTemplate>
{
    public void Configure(EntityTypeBuilder<DailyPlanTemplate> builder)
    {
        builder.ToTable("daily_plan_templates");
        builder.HasKey(planTemplate => planTemplate.Id);
        builder.Property(planTemplate => planTemplate.Id).ValueGeneratedNever();
        builder.OwnsOne(planTemplate => planTemplate.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(planTemplate => planTemplate.Meals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

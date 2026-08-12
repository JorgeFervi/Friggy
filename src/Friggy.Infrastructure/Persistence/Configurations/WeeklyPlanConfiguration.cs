using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyPlan> builder)
    {
        builder.ToTable("weekly_plans");
        builder.HasKey(plan => plan.Id);
        builder.OwnsOne(plan => plan.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(plan => plan.Name).IsRequired();
        builder.Property(plan => plan.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired();
        builder.Property(plan => plan.Description)
            .HasColumnName("description");
        builder.Ignore(plan => plan.EndDate);
        builder.Ignore(plan => plan.Dates);
        builder.Ignore(plan => plan.Slots);
        builder.Navigation(plan => plan.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

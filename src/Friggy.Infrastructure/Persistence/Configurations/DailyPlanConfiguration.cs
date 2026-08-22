using Friggy.Domain.DailyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>Configura la persistencia de los planes diarios.</summary>
internal sealed class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    /// <summary>Configura la fecha única y las colecciones del agregado.</summary>
    public void Configure(EntityTypeBuilder<DailyPlan> builder)
    {
        builder.ToTable("daily_plans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();
        builder.Property(plan => plan.Date)
            .HasColumnName("date")
            .HasColumnType("date")
            .IsRequired();
        builder.HasIndex(plan => plan.Date).IsUnique();
        builder.Navigation(plan => plan.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(plan => plan.Slots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

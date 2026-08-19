using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de los planes semanales y sus colecciones.
/// </summary>
internal sealed class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    /// <summary>
    /// Configura el nombre, las fechas, la descripción y las colecciones del plan.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
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
        builder.Navigation(plan => plan.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(plan => plan.Slots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la entidad MealType.
/// </summary>
internal sealed class MealTypeConfiguration : IEntityTypeConfiguration<MealType>
{
    /// <summary>
    /// Configura la tabla, el orden y el nombre del tipo de comida.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<MealType> builder)
    {
        builder.ToTable("meal_types", table =>
            table.HasCheckConstraint("ck_meal_types_order_non_negative", "\"order\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Order).HasColumnName("order");
        builder.OwnsOne(item => item.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(item => item.Name).IsRequired();
    }
}

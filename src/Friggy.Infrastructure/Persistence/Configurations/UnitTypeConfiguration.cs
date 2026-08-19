using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la entidad UnitType.
/// </summary>
internal sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    /// <summary>
    /// Configura la tabla, el símbolo y el nombre de las unidades de medida.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.ToTable("unit_types");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(UnitType.MaximumSymbolLength)
            .IsRequired();
        builder.HasIndex(item => item.Symbol).IsUnique();
        builder.OwnsOne(item => item.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(item => item.Name).IsRequired();
    }
}

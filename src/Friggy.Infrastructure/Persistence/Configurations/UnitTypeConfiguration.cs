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
        builder.Property(item => item.MeasurementDimension)
            .HasColumnName("measurement_dimension")
            .HasConversion<short>();
        builder.Property(item => item.BaseUnitFactor)
            .HasColumnName("base_unit_factor")
            .HasPrecision(18, 9);
        builder.Property(item => item.CanUseForCooking)
            .HasColumnName("can_use_for_cooking");
        builder.Property(item => item.CanUseForShopping)
            .HasColumnName("can_use_for_shopping");
        builder.HasIndex(item => item.Symbol).IsUnique();
        builder.HasIndex(item => new
        {
            item.MeasurementDimension,
            item.CanUseForShopping,
            item.BaseUnitFactor,
        });
        builder.ToTable(
            "unit_types",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_unit_types_measurement_dimension",
                    "measurement_dimension BETWEEN 0 AND 3");
                table.HasCheckConstraint(
                    "ck_unit_types_base_unit_factor_positive",
                    "base_unit_factor > 0");
                table.HasCheckConstraint(
                    "ck_unit_types_unconverted_factor",
                    "measurement_dimension <> 0 OR base_unit_factor = 1");
            });
        builder.OwnsOne(item => item.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(item => item.Name).IsRequired();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Friggy.Infrastructure.Persistence;

/// <summary>
/// Factoría que crea el contexto de datos para las herramientas de Entity
/// Framework Core en tiempo de diseño.
/// </summary>
public sealed class FriggyDbContextFactory : IDesignTimeDbContextFactory<FriggyDbContext>
{
    private const string LocalConnectionString =
        "Host=localhost;Port=5432;Database=friggy;Username=friggy;Password=friggy_local";

    /// <summary>
    /// Crea un contexto configurado con la cadena de conexión del entorno o
    /// con la cadena local predeterminada.
    /// </summary>
    /// <param name="args">
    /// Argumentos recibidos por las herramientas de diseño.
    /// </param>
    /// <returns>
    /// Contexto de datos configurado para PostgreSQL.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando los argumentos son nulos.
    /// </exception>
    public FriggyDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var connectionString = Environment.GetEnvironmentVariable("FRIGGY_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalConnectionString;
        }

        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName))
            .Options;

        return new FriggyDbContext(options);
    }
}

using Friggy.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.IntegrationTests.Testing;

public sealed class FriggyApiFactory(string connectionString)
    : WebApplicationFactory<global::Friggy.Api.AssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var contextRegistrations = services
                .Where(IsFriggyDbContextRegistration)
                .ToArray();

            foreach (var registration in contextRegistrations)
            {
                services.Remove(registration);
            }

            services.AddDbContext<FriggyDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName)));
        });
    }

    private static bool IsFriggyDbContextRegistration(ServiceDescriptor descriptor)
    {
        if (descriptor.ServiceType == typeof(FriggyDbContext) ||
            descriptor.ServiceType == typeof(DbContextOptions<FriggyDbContext>))
        {
            return true;
        }

        return descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition().FullName ==
            "Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1" &&
            descriptor.ServiceType.GenericTypeArguments[0] == typeof(FriggyDbContext);
    }
}

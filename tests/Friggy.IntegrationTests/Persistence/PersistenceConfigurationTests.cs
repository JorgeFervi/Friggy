using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Infrastructure;
using Friggy.Infrastructure.Persistence;
using Friggy.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.IntegrationTests.Persistence;

public sealed class PersistenceConfigurationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=friggy_tests;Username=friggy;Password=friggy_local";

    [Fact]
    [Trait("Category", "Integration")]
    public void AddInfrastructure_MissingConnectionString_FailsImmediately()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(configuration));

        Assert.Contains("Friggy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void AddInfrastructure_ValidConfiguration_RegistersScopedNpgsqlContext()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddInfrastructure(configuration);

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(FriggyDbContext));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.True(typeof(FriggyDbContext).IsSealed);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IRecipeRepository) &&
                service.ImplementationType == typeof(RecipeRepository) &&
                service.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IRecipeCatalogRepository) &&
                service.ImplementationType == typeof(RecipeCatalogRepository) &&
                service.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IDailyPlanRepository) &&
                service.ImplementationType == typeof(DailyPlanRepository) &&
                service.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IDailyPlanReferenceRepository) &&
                service.ImplementationType == typeof(DailyPlanReferenceRepository) &&
                service.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInventoryLotRepository) &&
                service.ImplementationType == typeof(InventoryLotRepository) &&
                service.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInventoryUnitOfWork) &&
                service.ImplementationType == typeof(InventoryUnitOfWork) &&
                service.Lifetime == ServiceLifetime.Scoped);

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<FriggyDbContext>();
        var sameScopeContext = firstScope.ServiceProvider.GetRequiredService<FriggyDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<FriggyDbContext>();

        Assert.Same(firstContext, sameScopeContext);
        Assert.NotSame(firstContext, secondContext);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", firstContext.Database.ProviderName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void DesignTimeFactory_CreatesNpgsqlContextWithInitialMigration()
    {
        using var context = new FriggyDbContextFactory().CreateDbContext([]);

        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Collection(
            migrations,
            migration => Assert.EndsWith("_InitialInfrastructure", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddCatalogs", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddRecipes", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddWeeklyPlans", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddInventoryAndMealCompletion",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddDailyMealPlanSlots",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddMealPlanSlotSchedule",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddMealPlanEntrySkippedState",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddRecipeStepIngredients",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_DeferRecipeIngredientOrderUniqueness",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_ReplaceWeeklyPlansWithDailyPlans",
                migration,
                StringComparison.Ordinal));
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Friggy"] = ConnectionString,
            })
            .Build();
}

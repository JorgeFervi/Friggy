using Friggy.Api.Endpoints;
using Friggy.Api.Errors;
using Friggy.Application;
using Friggy.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapIngredientEndpoints();
app.MapUnitTypeEndpoints();
app.MapRecipeTagEndpoints();
app.MapMealTypeEndpoints();
app.MapRecipeEndpoints();
app.MapDailyPlanEndpoints();
app.MapDailyPlanTemplateEndpoints();
app.MapInventoryEndpoints();

app.Run();

/// <summary>
/// Punto de entrada parcial de la aplicación ASP.NET Core.
/// </summary>
public partial class Program;

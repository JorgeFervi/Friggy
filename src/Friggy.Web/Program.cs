using Friggy.Web.Api;
using Friggy.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5292";
builder.Services.AddHttpClient<CatalogApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<IIngredientsApiClient>(services => services.GetRequiredService<CatalogApiClient>());
builder.Services.AddScoped<IUnitTypesApiClient>(services => services.GetRequiredService<CatalogApiClient>());
builder.Services.AddScoped<IRecipeTagsApiClient>(services => services.GetRequiredService<CatalogApiClient>());
builder.Services.AddScoped<IMealTypesApiClient>(services => services.GetRequiredService<CatalogApiClient>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

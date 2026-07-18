using dashboard.Components;
using dashboard.Data;
using dashboard.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:7404");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<MongoContext>();

var app = builder.Build();

// Local single-user tool, HTTP only — no cert/HSTS ceremony needed.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapTaskEndpoints();
app.MapPrEndpoints();
app.MapEventEndpoints();
app.MapStandupEndpoints();
app.MapNotifyEndpoints();

app.Run();

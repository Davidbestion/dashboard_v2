using Dashboard_v2.Infrastructure.Data;
using Dashboard_v2.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // En desarrollo el proxy del SPA ya gestiona HTTPS; en producción lo gestiona el host externo.
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// En Testing el esquema ya fue creado por EnsureCreatedAsync en el test setup; saltar migraciones.
if (!app.Environment.IsEnvironment("Testing"))
    await app.InitialiseDatabaseAsync();

await app.InitialiseMinioAsync();

app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.MapRazorPages();

app.MapFallbackToFile("index.html");

app.UseExceptionHandler(options => { });


app.MapEndpoints();

app.Run();

public partial class Program { }


using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Services;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllersWithViews();

// Configurar conexión con MySQL usando ADO.NET
builder.Services.AddScoped(sp => new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "es-AR", "es" };
    options.SetDefaultCulture("es-AR")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// Configurar el pipeline HTTP
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
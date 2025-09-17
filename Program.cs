using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Services;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Data.Repositorios;


var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllersWithViews();

// Configurar conexión con MySQL usando ADO.NET
builder.Services.AddScoped(sp => new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IRepositorioPropietario, RepositorioPropietario>();
builder.Services.AddScoped<IRepositorioInquilino, RepositorioInquilino>();
builder.Services.AddScoped<IRepositorioInmueble, RepositorioInmueble>();
builder.Services.AddScoped<IRepositorioContrato, RepositorioContrato>();
builder.Services.AddScoped<IRepositorioImagen, RepositorioImagen>();
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
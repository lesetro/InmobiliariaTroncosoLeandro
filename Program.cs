using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Services;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Data.Repositorios;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Inmobiliaria_troncoso_leandro.Data;



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
builder.Services.AddScoped<IRepositorioAlquiler, RepositorioAlquiler>();
builder.Services.AddScoped<IRepositorioVenta, RepositorioVenta>();
builder.Services.AddScoped<IRepositorioReporte, RepositorioReporte>();
builder.Services.AddScoped<IRepositorioTipoInmueble, RepositorioTipoInmueble>();
builder.Services.AddScoped<IRepositorioInteresInmueble, RepositorioInteresInmueble>();
builder.Services.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
builder.Services.AddScoped<IDatabaseSeederService, DatabaseSeederService>();
builder.Services.AddScoped<ISystemSetupService, SystemSetupService>();
builder.Services.AddScoped<IRepositorioAdmin, RepositorioAdmin>();
builder.Services.AddScoped<IRepositorioEmpleado, RepositorioEmpleado>();
builder.Services.AddScoped<IRepositorioContacto, RepositorioContacto>();
builder.Services.AddScoped<IRepositorioTipoInmueble, RepositorioTipoInmueble>();
builder.Services.AddScoped<IRepositorioContratoVenta, RepositorioContratoVenta>();
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);




// 2. CONFIGURAR AUTENTICACIÓN CON COOKIES
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";        // Ruta para login
        options.LogoutPath = "/Account/Logout";      // Ruta para logout
        options.AccessDeniedPath = "/Account/AccessDenied"; // Ruta para acceso denegado
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Duración de la sesión
        options.SlidingExpiration = true; // Renovar automáticamente
    });

// 3. CONFIGURAR POLÍTICAS DE AUTORIZACIÓN
builder.Services.AddAuthorization(options =>
{
    // Políticas existentes
    options.AddPolicy("Administrador", policy =>
        policy.RequireClaim(ClaimTypes.Role, "administrador"));

    options.AddPolicy("AdminOEmpleado", policy =>
        policy.RequireClaim(ClaimTypes.Role, "administrador", "empleado"));
    options.AddPolicy("Empleado", policy =>
        policy.RequireClaim(ClaimTypes.Role, "empleado"));

    options.AddPolicy("Propietario", policy =>
        policy.RequireClaim(ClaimTypes.Role, "propietario"));

    options.AddPolicy("Inquilino", policy =>
        policy.RequireClaim(ClaimTypes.Role, "inquilino"));

   
    options.AddPolicy("EmpleadoOSuperior", policy =>
        policy.RequireClaim(ClaimTypes.Role, "empleado", "administrador")); 
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "es-AR", "es" };
    options.SetDefaultCulture("es-AR")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// =============== INICIALIZAR DATOS AL ARRANCAR ===============

using (var scope = app.Services.CreateScope())
{
    try 
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeederService>();
        await seeder.SeedDatabaseAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar base de datos");
        throw; // Re-lanzar la excepción para evitar que la app inicie en un estado inconsistente
    }
}


// Configurar el pipeline HTTP
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");

// Solo redirige la página principal si necesita configuración
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    
    // Solo verificar en la página de inicio
    if (path == "/" || path == "/Home" || path == "/Home/Index")
    {
        try
        {
            var setupService = context.RequestServices.GetRequiredService<ISystemSetupService>();
            if (await setupService.NeedsInitialSetupAsync())
            {
                context.Response.Redirect("/Account/Setup");
                return;
            }
        }
        catch
        {
            // Si hay error en el setup, continuar normal (no bloquear la app)
        }
    }
    
    await next();
});

app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
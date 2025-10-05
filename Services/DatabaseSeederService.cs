using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Services
{
    public interface IDatabaseSeederService
    {
        Task SeedDatabaseAsync();
        Task CreateTestUsersAsync();
    }

    public class DatabaseSeederService : IDatabaseSeederService
    {
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly ILogger<DatabaseSeederService> _logger;
        private readonly IConfiguration _configuration;

        public DatabaseSeederService(IRepositorioUsuario repositorioUsuario, 
                                   ILogger<DatabaseSeederService> logger,
                                   IConfiguration configuration)
        {
            _repositorioUsuario = repositorioUsuario;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Iniciando configuración de base de datos...");
                
                // Verificar conexión antes de continuar
                if (!await TestDatabaseConnectionAsync())
                {
                    _logger.LogError("❌ No se pudo establecer conexión con la base de datos");
                    throw new InvalidOperationException("Error de conexión a la base de datos");
                }

                await CreateTestUsersAsync();
                
                _logger.LogInformation("✅ Base de datos configurada exitosamente");
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "❌ Error de MySQL al configurar base de datos");
                _logger.LogError("💡 Verifica que MySQL esté corriendo y la cadena de conexión sea correcta");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error general al configurar base de datos");
                throw;
            }
        }

        private async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("❌ Cadena de conexión 'DefaultConnection' no encontrada en appsettings.json");
                    return false;
                }

                _logger.LogInformation($"🔗 Probando conexión a base de datos...");
                
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                
                _logger.LogInformation("✅ Conexión a base de datos exitosa");
                return true;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión MySQL: {Message}", ex.Message);
                _logger.LogError("💡 Verifica que MySQL esté corriendo y los datos de conexión sean correctos");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado al probar conexión");
                return false;
            }
        }

        public async Task CreateTestUsersAsync()
        {
            try
            {
                // Lista de usuarios de prueba 
                var usuariosPrueba = new[]
                {
                    new { 
                        Email = "admin@inmobiliaria.com", 
                        Nombre = "Super", 
                        Apellido = "Administrador", 
                        Dni = "11111111", 
                        Rol = "administrador",
                        Telefono = "2664111111",
                        Direccion = "Dirección Admin"
                    },
                    new { 
                        Email = "empleado@inmobiliaria.com", 
                        Nombre = "Juan", 
                        Apellido = "Empleado", 
                        Dni = "22222222", 
                        Rol = "empleado",
                        Telefono = "2664222222",
                        Direccion = "Dirección Empleado"
                    },
                    new { 
                        Email = "propietario@inmobiliaria.com", 
                        Nombre = "María", 
                        Apellido = "Propietaria", 
                        Dni = "33333333", 
                        Rol = "propietario",
                        Telefono = "2664333333",
                        Direccion = "Dirección Propietaria"
                    },
                    new { 
                        Email = "inquilino@inmobiliaria.com", 
                        Nombre = "Pedro", 
                        Apellido = "Inquilino", 
                        Dni = "44444444", 
                        Rol = "inquilino",
                        Telefono = "2664444444",
                        Direccion = "Dirección Inquilino"
                    }
                };

                _logger.LogInformation("📝 Creando/verificando usuarios de prueba...");

                foreach (var userData in usuariosPrueba)
                {
                    await CreateUserIfNotExists(userData.Email, userData.Nombre, userData.Apellido, 
                        userData.Dni, userData.Rol, userData.Telefono, userData.Direccion);
                }

                _logger.LogInformation("✅ Usuarios de prueba verificados/creados");
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "❌ Error de MySQL al crear usuarios de prueba");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al crear usuarios de prueba");
                throw;
            }
        }

        private async Task CreateUserIfNotExists(string email, string nombre, string apellido, 
            string dni, string rol, string telefono, string direccion)
        {
            try
            {
                _logger.LogInformation($"🔍 Verificando usuario: {email}");
                
                // Verificar si el usuario ya existe
                var existingUser = await _repositorioUsuario.GetByEmailAsync(email);
                if (existingUser != null)
                {
                    _logger.LogInformation($"ℹ️  Usuario {email} ya existe - omitiendo creación");
                    return;
                }

                // Crear usuario según el rol usando los métodos estáticos
                Usuario nuevoUsuario = rol.ToLower() switch
                {
                    "administrador" => Usuario.CrearAdministrador(nombre, apellido, dni, email, telefono, direccion),
                    "empleado" => Usuario.CrearEmpleado(nombre, apellido, dni, email, telefono, direccion),
                    "propietario" => Usuario.CrearPropietario(nombre, apellido, dni, email, telefono, direccion),
                    "inquilino" => Usuario.CrearInquilino(nombre, apellido, dni, email, telefono, direccion),
                    _ => throw new InvalidOperationException($"Rol '{rol}' no válido")
                };

                // Hash de la contraseña
                nuevoUsuario.Password = BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123");

                // Crear usuario
                await _repositorioUsuario.CreateAsync(nuevoUsuario);
                
                _logger.LogInformation($"✅ Usuario {email} creado exitosamente con rol '{rol}'");
                _logger.LogInformation($"   📱 Teléfono: {telefono}");
                _logger.LogInformation($"   🔑 Contraseña temporal: PasswordTemporal123");
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, $"❌ Error de MySQL al crear usuario {email}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al crear usuario {email}");
                throw;
            }
        }
    }
}
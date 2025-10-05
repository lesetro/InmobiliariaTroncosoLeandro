using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Services
{
    public class SystemSetupService : ISystemSetupService
    {
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly ILogger<SystemSetupService> _logger;

        public SystemSetupService(IRepositorioUsuario repositorioUsuario, ILogger<SystemSetupService> logger)
        {
            _repositorioUsuario = repositorioUsuario;
            _logger = logger;
        }

        public async Task<bool> NeedsInitialSetupAsync()
        {
            var admins = await _repositorioUsuario.GetAdministradoresAsync();
            return !admins.Any();
        }

        public async Task<SetupStatus> GetSystemStatusAsync()
        {
            try
            {
                var admins = await _repositorioUsuario.GetAdministradoresAsync();
                var empleados = await _repositorioUsuario.GetEmpleadosAsync();
                var propietarios = await _repositorioUsuario.GetPropietariosAsync();
                var inquilinos = await _repositorioUsuario.GetInquilinosAsync();
                var totalUsers = await _repositorioUsuario.GetTotalUsuariosAsync();

                var status = new SetupStatus
                {
                    HasAdministrators = admins.Any(),
                    TotalUsers = totalUsers,
                    AdminCount = admins.Count(),
                    EmployeeCount = empleados.Count(),
                    PropietarioCount = propietarios.Count(),
                    InquilinoCount = inquilinos.Count(),
                    ExistingAdminEmails = admins.Select(a => a.Email).ToList()
                };

                // Determinar qué acción recomendar
                if (!status.HasAdministrators)
                {
                    if (status.TotalUsers > 0)
                    {
                        status.NeedsSetup = true;
                        status.RecommendedAction = "UPGRADE_EXISTING_USER";
                    }
                    else
                    {
                        status.NeedsSetup = true;
                        status.RecommendedAction = "CREATE_INITIAL_ADMIN";
                    }
                }
                else
                {
                    status.NeedsSetup = false;
                    status.RecommendedAction = "SYSTEM_READY";
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado del sistema");
                throw;
            }
        }

        public async Task CreateInitialAdminAsync(string nombre, string apellido, string dni, 
                                                string email, string telefono, string direccion, string password)
        {
            try
            {
                // Verificar que no haya administradores
                var existingAdmins = await _repositorioUsuario.GetAdministradoresAsync();
                if (existingAdmins.Any())
                {
                    throw new InvalidOperationException("Ya existen administradores en el sistema");
                }

                // Verificar que el email no exista
                var existingUser = await _repositorioUsuario.GetByEmailAsync(email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("Ya existe un usuario con ese email");
                }

                // Crear nuevo administrador
                var admin = new Usuario
                {
                    Nombre = nombre,
                    Apellido = apellido,
                    Dni = dni,
                    Email = email,
                    Telefono = telefono,
                    Direccion = direccion,
                    Rol = "administrador",
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Estado = "activo",
                    Avatar = $"{nombre[0]}{apellido[0]}".ToUpper()
                };

                await _repositorioUsuario.CreateAsync(admin);
                
                _logger.LogInformation($"Administrador inicial creado: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear administrador inicial");
                throw;
            }
        }

        public async Task UpgradeExistingUserToAdminAsync(string email, string newPassword = null)
        {
            try
            {
                // Verificar que no haya administradores
                var existingAdmins = await _repositorioUsuario.GetAdministradoresAsync();
                if (existingAdmins.Any())
                {
                    throw new InvalidOperationException("Ya existen administradores en el sistema");
                }

                // Buscar usuario existente
                var usuario = await _repositorioUsuario.GetByEmailAsync(email);
                if (usuario == null)
                {
                    throw new InvalidOperationException("Usuario no encontrado");
                }

                // Actualizar a administrador
                usuario.Rol = "administrador";
                
                // Cambiar contraseña si se proporciona
                if (!string.IsNullOrEmpty(newPassword))
                {
                    usuario.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                await _repositorioUsuario.UpdateAsync(usuario);
                
                _logger.LogInformation($"Usuario {email} promovido a administrador");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al promover usuario a administrador");
                throw;
            }
        }
    }
}
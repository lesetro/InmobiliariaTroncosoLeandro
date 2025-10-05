using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Services;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [AllowAnonymous]
    public class DebugController : Controller
    {
        private readonly IRepositorioUsuario _repositorioUsuario;

        public DebugController(IRepositorioUsuario repositorioUsuario)
        {
            _repositorioUsuario = repositorioUsuario;
        }

        // GET: /Debug/TestUsers
        public async Task<IActionResult> TestUsers()
        {
            try
            {
                var usuarios = await _repositorioUsuario.GetAllAsync();
                
                var emailsPrueba = new[] { "admin@inmobiliaria.com", "empleado@inmobiliaria.com", 
                                         "propietario@inmobiliaria.com", "inquilino@inmobiliaria.com" };
                
                var usuariosPrueba = usuarios.Where(u => emailsPrueba.Contains(u.Email)).ToList();
                
                var resultado = new
                {
                    totalUsuarios = usuarios.Count(),
                    usuariosPrueba = usuariosPrueba.Select(u => new
                    {
                        u.Email,
                        u.Rol,
                        u.NombreCompleto,
                        u.Estado,
                        tienePassword = !string.IsNullOrEmpty(u.Password),
                        passwordLength = u.Password?.Length ?? 0
                    }),
                    faltanUsuarios = emailsPrueba.Except(usuariosPrueba.Select(u => u.Email)).ToList()
                };
                
                return Json(resultado);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: /Debug/CheckPassword
        public async Task<IActionResult> CheckPassword(string email = "admin@inmobiliaria.com", 
                                                     string password = "PasswordTemporal123")
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByEmailAsync(email);
                
                if (usuario == null)
                {
                    return Json(new 
                    { 
                        success = false, 
                        message = $"Usuario {email} no encontrado",
                        suggestion = "Ejecuta /Debug/CreateUser para crearlo"
                    });
                }

                var passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.Password);
                
                return Json(new
                {
                    email = email,
                    passwordIntentado = password,
                    usuarioEncontrado = true,
                    passwordValido = passwordValido,
                    rolUsuario = usuario.Rol,
                    estadoUsuario = usuario.Estado,
                    hashAlmacenado = usuario.Password?.Substring(0, 20) + "..." // Solo los primeros caracteres por seguridad
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: /Debug/CreateUser - Crear usuario específico
        [HttpPost]
        public async Task<IActionResult> CreateUser(string email, string nombre, string apellido, 
                                                   string rol, string password = "PasswordTemporal123")
        {
            try
            {
                // Verificar si ya existe
                var existeUsuario = await _repositorioUsuario.GetByEmailAsync(email);
                if (existeUsuario != null)
                {
                    return Json(new { success = false, message = "El usuario ya existe" });
                }

                // Crear usuario básico
                var usuario = new Models.Usuario
                {
                    Email = email,
                    Nombre = nombre,
                    Apellido = apellido,
                    Dni = "00000000",
                    Telefono = "2664000000",
                    Direccion = "Dirección de prueba",
                    Rol = rol,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Estado = "activo",
                    Avatar = $"{nombre[0]}{apellido[0]}".ToUpper()
                };

                await _repositorioUsuario.CreateAsync(usuario);

                return Json(new 
                { 
                    success = true, 
                    message = $"Usuario {email} creado exitosamente",
                    usuario = new { usuario.Email, usuario.Rol, usuario.NombreCompleto }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // GET: /Debug/CreateAllTestUsers - Crear todos los usuarios de prueba
        public async Task<IActionResult> CreateAllTestUsers()
        {
            try
            {
                var usuariosCrear = new[]
                {
                    new { Email = "admin@inmobiliaria.com", Nombre = "Super", Apellido = "Admin", Rol = "administrador" },
                    new { Email = "empleado@inmobiliaria.com", Nombre = "Juan", Apellido = "Empleado", Rol = "empleado" },
                    new { Email = "propietario@inmobiliaria.com", Nombre = "María", Apellido = "Propietaria", Rol = "propietario" },
                    new { Email = "inquilino@inmobiliaria.com", Nombre = "Pedro", Apellido = "Inquilino", Rol = "inquilino" }
                };

                var resultados = new List<object>();

                foreach (var userData in usuariosCrear)
                {
                    try
                    {
                        var existeUsuario = await _repositorioUsuario.GetByEmailAsync(userData.Email);
                        if (existeUsuario != null)
                        {
                            resultados.Add(new { userData.Email, status = "Ya existe", success = true });
                            continue;
                        }

                        var usuario = new Models.Usuario
                        {
                            Email = userData.Email,
                            Nombre = userData.Nombre,
                            Apellido = userData.Apellido,
                            Dni = $"{userData.Rol[0]}{userData.Rol[0]}{userData.Rol[0]}{userData.Rol[0]}1111",
                            Telefono = "2664000000",
                            Direccion = $"Dirección {userData.Rol}",
                            Rol = userData.Rol,
                            Password = BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123"),
                            Estado = "activo",
                            Avatar = $"{userData.Nombre[0]}{userData.Apellido[0]}".ToUpper()
                        };

                        await _repositorioUsuario.CreateAsync(usuario);
                        resultados.Add(new { userData.Email, status = "Creado", success = true });
                    }
                    catch (Exception ex)
                    {
                        resultados.Add(new { userData.Email, status = $"Error: {ex.Message}", success = false });
                    }
                }

                return Json(new { success = true, resultados = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // GET: /Debug - Página principal de debug
        public IActionResult Index()
        {
            return View();
        }
    }
}
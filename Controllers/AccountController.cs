using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using System.Security.Claims;
using Inmobiliaria_troncoso_leandro.Services;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly ISystemSetupService _setupService;

        public AccountController(IRepositorioUsuario repositorioUsuario, ISystemSetupService setupService)
        {
            _repositorioUsuario = repositorioUsuario;
            _setupService = setupService;
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = "")
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Email y contraseña son requeridos");
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Validar usuario
                var usuario = await _repositorioUsuario.ValidateUserAsync(email, password);
                if (usuario == null)
                {
                    ModelState.AddModelError("", "Email o contraseña incorrectos");
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Crear claims para la cookie de autenticación
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Email),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim("FullName", usuario.NombreCompleto),
                    new Claim(ClaimTypes.Role, usuario.Rol),
                    new Claim("Avatar", usuario.Avatar ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Mantener sesión
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // 8 horas de sesión
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirección según rol después del login exitoso
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToDashboardByRole(usuario.Rol);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al iniciar sesión: {ex.Message}";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
        }

        // GET: Account/Register - Solo para admin
        [Authorize(Policy = "Administrador")]
        public IActionResult Register()
        {
            ViewBag.Roles = new List<string> { "empleado", "propietario", "inquilino" };
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Register([Bind("Nombre,Apellido,Dni,Email,Telefono,Direccion,Rol")] Usuario usuario)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validar que el rol sea válido (admin no puede crear otros admin via register)
                    var rolesValidos = new[] { "empleado", "propietario", "inquilino" };
                    if (!rolesValidos.Contains(usuario.Rol?.ToLower()))
                    {
                        ModelState.AddModelError("Rol", "Rol no válido para registro");
                        ViewBag.Roles = rolesValidos.ToList();
                        return View(usuario);
                    }

                    // Crear usuario según el rol
                    Usuario nuevoUsuario;
                    switch ((usuario.Rol ?? string.Empty).ToLower())
                    {
                        case "empleado":
                            nuevoUsuario = Usuario.CrearEmpleado(usuario.Nombre, usuario.Apellido,
                                usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                            break;
                        case "propietario":
                            nuevoUsuario = Usuario.CrearPropietario(usuario.Nombre, usuario.Apellido,
                                usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                            break;
                        case "inquilino":
                            nuevoUsuario = Usuario.CrearInquilino(usuario.Nombre, usuario.Apellido,
                                usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                            break;
                        default:
                            throw new InvalidOperationException("Rol no válido");
                    }

                    await _repositorioUsuario.CreateAsync(nuevoUsuario);
                    TempData["Success"] = $"Usuario {nuevoUsuario.NombreCompleto} registrado exitosamente. Contraseña temporal: PasswordTemporal123";
                    return RedirectToAction("Index", "Usuario");
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al registrar usuario: {ex.Message}";
            }

            ViewBag.Roles = new List<string> { "empleado", "propietario", "inquilino" };
            return View(usuario);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Sesión cerrada exitosamente";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/ChangePassword
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var usuario = await _repositorioUsuario.GetByIdAsync(userId);
                    if (usuario != null)
                    {
                        // Pasar datos básicos via ViewBag para usar en JavaScript
                        ViewBag.UserName = usuario.NombreCompleto;
                        ViewBag.UserEmail = usuario.Email;
                        ViewBag.UserRole = usuario.Rol;
                        ViewBag.UserInitials = GetInitials(usuario.NombreCompleto);
                        ViewBag.RoleBadgeClass = GetRoleBadgeClass(usuario.Rol);
                    }
                }
            }
            catch
            {
                // Si hay error, usa valores por defecto
            }

            return View();
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "US";
            var parts = fullName.Split(' ');
            return parts.Length >= 2 ?
                parts[0].Substring(0, 1) + parts[1].Substring(0, 1) :
                fullName.Substring(0, Math.Min(2, fullName.Length));
        }

        private string GetRoleBadgeClass(string role)
        {
            return role?.ToLower() switch
            {
                "administrador" => "badge-danger",
                "empleado" => "badge-warning",
                "propietario" => "badge-primary",
                "inquilino" => "badge-success",
                _ => "badge-secondary"
            };
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    ModelState.AddModelError("", "Todos los campos son requeridos");
                    return View();
                }

                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("", "La nueva contraseña y la confirmación no coinciden");
                    return View();
                }

                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("", "La nueva contraseña debe tener al menos 6 caracteres");
                    return View();
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    ModelState.AddModelError("", "Error al obtener información del usuario");
                    return View();
                }

                // Validar contraseña actual
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuario == null)
                {
                    ModelState.AddModelError("", "Usuario no encontrado");
                    return View();
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, usuario.Password))
                {
                    ModelState.AddModelError("", "Contraseña actual incorrecta");
                    return View();
                }

                // Cambiar contraseña
                var resultado = await _repositorioUsuario.CambiarPasswordAsync(userId, newPassword);
                if (resultado)
                {
                    TempData["Success"] = "Contraseña cambiada exitosamente";
                    return RedirectToAction("Perfil", "Usuario");
                }
                else
                {
                    ModelState.AddModelError("", "Error al cambiar contraseña");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar contraseña: {ex.Message}";
            }

            return View();
        }

        // Método privado para redirección según rol
        private IActionResult RedirectToDashboardByRole(string rol)
        {
            return rol.ToLower() switch
            {
                "administrador" => RedirectToAction("Index", "Admin"),
                "empleado" => RedirectToAction("Index", "Empleado"),
                "propietario" => RedirectToAction("Index", "Propietario"),
                "inquilino" => RedirectToAction("Index", "Inquilino"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // GET: Account/Profile - Alias para el perfil del usuario
        [Authorize]
        public IActionResult Profile()
        {
            return RedirectToAction("Perfil", "Usuario");
        }

        // GET: Account/ForgotPassword - Para futuras implementaciones
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            ViewBag.Message = "Funcionalidad de recuperación de contraseña próximamente";
            return View();
        }

        // Método auxiliar para validar formato de email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // GET: Account/UpdateProfile - Actualizar perfil personal
        [Authorize]
        public async Task<IActionResult> UpdateProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["Error"] = "Error al obtener información del usuario";
                    return RedirectToAction("Index", "Home");
                }

                var usuario = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Home");
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([Bind("IdUsuario,Nombre,Apellido,Telefono,Direccion,Avatar")] Usuario usuario)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["Error"] = "Error al obtener información del usuario";
                    return RedirectToAction("Index", "Home");
                }

                if (userId != usuario.IdUsuario)
                {
                    TempData["Error"] = "No tiene permisos para editar este perfil";
                    return RedirectToAction("Index", "Home");
                }

                var usuarioActual = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuarioActual == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Home");
                }

                // Solo permitir actualizar ciertos campos desde el perfil personal
                usuarioActual.Nombre = usuario.Nombre;
                usuarioActual.Apellido = usuario.Apellido;
                usuarioActual.Telefono = usuario.Telefono;
                usuarioActual.Direccion = usuario.Direccion;

                // Solo actualizar avatar si se proporciona uno nuevo
                if (!string.IsNullOrWhiteSpace(usuario.Avatar))
                {
                    usuarioActual.Avatar = usuario.Avatar;
                }

                await _repositorioUsuario.UpdateAsync(usuarioActual);
                TempData["Success"] = "Perfil actualizado exitosamente";

                return RedirectToAction("Perfil", "Usuario");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar perfil: {ex.Message}";
                return View(usuario);
            }
        }

        // POST: Account/RefreshAvatar - Regenerar avatar automático
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RefreshAvatar()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["Error"] = "Error al obtener información del usuario";
                    return RedirectToAction("Index", "Home");
                }

                var usuario = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Home");
                }

                // Regenerar avatar automático
                usuario.ActualizarAvatar("");
                await _repositorioUsuario.UpdateAsync(usuario);

                TempData["Success"] = "Avatar actualizado exitosamente";
                return RedirectToAction("UpdateProfile");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar avatar: {ex.Message}";
                return RedirectToAction("UpdateProfile");
            }
        }

        // GET: Account/Dashboard - Redirección inteligente según rol
        [Authorize]
        public IActionResult Dashboard()
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            return RedirectToDashboardByRole(rol ?? "");
        }

        // Método para verificar si el usuario está autenticado (para AJAX)
        [HttpGet]
        public IActionResult CheckAuth()
        {
            return Json(new
            {
                isAuthenticated = User.Identity != null && User.Identity.IsAuthenticated,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                userName = User.FindFirst("FullName")?.Value
            });
        }

      
        //Configuracion inicial del sistema


        // GET: Account/Setup - Página inteligente de configuración
        [AllowAnonymous]
        public async Task<IActionResult> Setup()
        {
            try
            {
                var status = await _setupService.GetSystemStatusAsync();

                if (!status.NeedsSetup)
                {
                    TempData["Info"] = "El sistema ya está configurado. Inicia sesión normalmente.";
                    return RedirectToAction("Login");
                }

                ViewBag.SystemStatus = status;
                return View("Setup", status);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al verificar configuración: {ex.Message}";
                return RedirectToAction("Login");
            }
        }

        // POST: Account/CreateInitialAdmin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInitialAdmin([Bind("Nombre,Apellido,Dni,Email,Telefono,Direccion")] Usuario usuario,
                                                           string password, string confirmPassword)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(password) || password != confirmPassword)
                {
                    ModelState.AddModelError("", "Las contraseñas no coinciden");
                    var status = await _setupService.GetSystemStatusAsync();
                    ViewBag.SystemStatus = status;
                    return View("Setup", status);
                }

                if (password.Length < 8)
                {
                    ModelState.AddModelError("", "La contraseña debe tener al menos 8 caracteres");
                    var status = await _setupService.GetSystemStatusAsync();
                    ViewBag.SystemStatus = status;
                    return View("Setup", status);
                }

                if (ModelState.IsValid)
                {
                    await _setupService.CreateInitialAdminAsync(
                        usuario.Nombre, usuario.Apellido, usuario.Dni,
                        usuario.Email, usuario.Telefono, usuario.Direccion, password);

                    TempData["Success"] = $"Administrador {usuario.NombreCompleto} creado exitosamente. Ya puedes iniciar sesión.";
                    return RedirectToAction("Login");
                }

                var statusError = await _setupService.GetSystemStatusAsync();
                ViewBag.SystemStatus = statusError;
                return View("Setup", statusError);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                var status = await _setupService.GetSystemStatusAsync();
                ViewBag.SystemStatus = status;
                return View("Setup", status);
            }
        }

        // POST: Account/UpgradeToAdmin - Promover usuario existente
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeToAdmin(string email, string newPassword, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                {
                    TempData["Error"] = "Las contraseñas no coinciden";
                    return RedirectToAction("Setup");
                }

                if (newPassword.Length < 8)
                {
                    TempData["Error"] = "La contraseña debe tener al menos 8 caracteres";
                    return RedirectToAction("Setup");
                }

                await _setupService.UpgradeExistingUserToAdminAsync(email, newPassword);

                TempData["Success"] = $"Usuario {email} promovido a administrador exitosamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Setup");
            }
        }

        // GET: Account/SystemStatus - Para debugging
        [AllowAnonymous]
        public async Task<IActionResult> SystemStatus()
        {
            try
            {
                var status = await _setupService.GetSystemStatusAsync();
                return Json(status);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        


    }
}

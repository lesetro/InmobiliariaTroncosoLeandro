using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using System.Security.Claims;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "Empleado")] // Empleados y superiores pueden acceder
    public class EmpleadoController : Controller
    {
        private readonly IRepositorioEmpleado _repositorioEmpleado;
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public EmpleadoController(
            IRepositorioEmpleado repositorioEmpleado,
            IRepositorioUsuario repositorioUsuario,
            IWebHostEnvironment webHostEnvironment)
        {
            _repositorioEmpleado = repositorioEmpleado;
            _repositorioUsuario = repositorioUsuario;
            _webHostEnvironment = webHostEnvironment;
        }
        // GET: Empleado/Index - Dashboard del empleado
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Admin");
        }


        // GET: Empleado/EditarMiPerfil
        public async Task<IActionResult> EditarMiPerfil()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Dashboard));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction("Index", "Admin");
            }
        }



        // POST: Empleado/EditarMiPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMiPerfil(Usuario usuario, IFormFile? archivoAvatar)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (usuario.IdUsuario != userId)
                {
                    TempData["Error"] = "No tiene permisos para editar este perfil";
                    return RedirectToAction("Index", "Admin");
                }

                var usuarioActual = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuarioActual == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Admin");
                }

                // Remover validaciones de campos que no se pueden editar
                ModelState.Remove("Email");
                ModelState.Remove("Dni");
                ModelState.Remove("Rol");
                ModelState.Remove("Estado");
                ModelState.Remove("Password");

                if (ModelState.IsValid)
                {
                    // Preservar campos que NO se pueden modificar
                    usuario.Email = usuarioActual.Email;
                    usuario.Dni = usuarioActual.Dni;
                    usuario.Rol = usuarioActual.Rol;
                    usuario.Estado = usuarioActual.Estado;
                    usuario.Password = usuarioActual.Password;

                    // Aquí podrías agregar lógica para subir el archivo de avatar si lo necesitas
                    // if (archivoAvatar != null && archivoAvatar.Length > 0) { ... }

                    await _repositorioUsuario.UpdateAsync(usuario);
                    TempData["Success"] = "Perfil actualizado exitosamente";
                    return RedirectToAction("Index", "Admin");
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar perfil: {ex.Message}";
                return View(usuario);
            }
        }

        // GET: Empleado/Dashboard - Vista principal del empleado
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Login", "Account");
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar dashboard: {ex.Message}";
                return RedirectToAction("Login", "Account");
            }
        }

        // GET: Empleado/MiPerfil - Solo ve su propio perfil
        public async Task<IActionResult> MiPerfil()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Dashboard));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction(nameof(Dashboard));
            }
        }



    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using System.Security.Claims;


namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Roles = "inquilino")]
    public class InquilinoController : Controller
    {
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly IRepositorioAlquiler _repositorioAlquiler;
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioPropietario _repositorioPropietario;

        private readonly IRepositorioInquilino _repositorioInquilino;

        public InquilinoController(
            IRepositorioUsuario repositorioUsuario,
            IRepositorioAlquiler repositorioAlquiler,
            IRepositorioPropietario repositorioPropietario,
            IRepositorioInquilino repositorioInquilino,
            IRepositorioInmueble repositorioInmueble)
        {
            _repositorioUsuario = repositorioUsuario;
            _repositorioAlquiler = repositorioAlquiler;
            _repositorioInmueble = repositorioInmueble;
            _repositorioPropietario = repositorioPropietario;
            _repositorioInquilino = repositorioInquilino;
        }

       // GET: Inquilino/Index
public async Task<IActionResult> Index()
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

        // OBTENER EL ID_INQUILINO
        var idInquilino = await _repositorioAlquiler.ObtenerIdInquilinoPorUsuarioAsync(userId);

        Console.WriteLine($"=== DEBUG: UserId: {userId}, IdInquilino: {idInquilino} ===");

        if (idInquilino == 0)
        {
            TempData["Error"] = "No está registrado como inquilino";
            return RedirectToAction("Login", "Account");
        }

        // OBTENER LOS DATOS QUE USA TU VISTA
        var contrato = await _repositorioAlquiler.ObtenerContratoVigentePorInquilinoAsync(idInquilino);
        var proximoPago = await _repositorioAlquiler.ObtenerProximoPagoAsync(idInquilino);
        var pagosPendientes = await _repositorioAlquiler.ObtenerPagosPendientesPorInquilinoAsync(idInquilino);

        // DEBUG SIMPLIFICADO
        Console.WriteLine($"=== DATOS OBTENIDOS ===");
        Console.WriteLine($"Contrato: {(contrato != null ? "SÍ" : "NO")}");
        Console.WriteLine($"Próximo Pago: {(proximoPago != null ? "SÍ" : "NO")}");
        Console.WriteLine($"Pagos Pendientes: {pagosPendientes?.Count ?? 0}");

        // Cargar datos relacionados del contrato si existe
        if (contrato != null)
        {
            contrato.Inmueble = await _repositorioInmueble.ObtenerInmuebleConGaleriaAsync(contrato.IdInmueble);
            contrato.Propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(contrato.IdPropietario);
            ViewBag.DiasParaVencimiento = (contrato.FechaFin - DateTime.Now).Days;
            
            Console.WriteLine($"Inmueble cargado: {(contrato.Inmueble != null ? "SÍ" : "NO")}");
            Console.WriteLine($"Propietario cargado: {(contrato.Propietario != null ? "SÍ" : "NO")}");
        }
        else
        {
            ViewBag.DiasParaVencimiento = 0;
        }

        // PASAR LOS DATOS EXACTOS QUE USA TU VISTA
        ViewBag.Contrato = contrato;
        ViewBag.ProximoPago = proximoPago;
        ViewBag.PagosPendientes = pagosPendientes;
        ViewBag.IdInquilino = idInquilino;

        return View(usuario);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ERROR en Index: {ex.Message} ===");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        TempData["Error"] = $"Error al cargar dashboard: {ex.Message}";
        return RedirectToAction("Login", "Account");
    }
}


        // GET: Inquilino/MiPerfil
        public async Task<IActionResult> MiPerfil()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Cargar contrato actual para la vista
                var contrato = await _repositorioAlquiler.ObtenerContratoVigentePorInquilinoAsync(userId);
                if (contrato != null)
                {
                    contrato.Inmueble = await _repositorioInmueble.GetByIdAsync(contrato.IdInmueble);
                    contrato.Propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(contrato.IdPropietario);
                    ViewBag.Contrato = contrato;
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Inquilino/EditarPerfil
        public async Task<IActionResult> EditarPerfil()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inquilino/EditarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(Usuario usuario, IFormFile? archivoAvatar)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (usuario.IdUsuario != userId)
                {
                    TempData["Error"] = "No tiene permisos para editar este perfil";
                    return RedirectToAction(nameof(Index));
                }

                var usuarioActual = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuarioActual == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
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

                    await _repositorioUsuario.UpdateAsync(usuario);
                    TempData["Success"] = "Perfil actualizado exitosamente";
                    return RedirectToAction(nameof(MiPerfil));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar perfil: {ex.Message}";
                return View(usuario);
            }
        }

        // GET: Inquilino/MiContrato
        public async Task<IActionResult> MiContrato()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Primero obtener el ID del inquilino (no del usuario)
                var idInquilino = await _repositorioInquilino.ObtenerIdInquilinoPorUsuarioAsync(userId);

                if (idInquilino == 0)
                {
                    TempData["Error"] = "No está registrado como inquilino";
                    return RedirectToAction(nameof(Index));
                }

                var contrato = await _repositorioAlquiler.ObtenerContratoVigentePorInquilinoAsync(idInquilino);

                if (contrato == null)
                {
                    TempData["Error"] = "No tiene un contrato activo";
                    return RedirectToAction(nameof(Index));
                }

                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: Inquilino/MiInmueble
        public async Task<IActionResult> MiInmueble()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var contrato = await _repositorioAlquiler.ObtenerContratoVigentePorInquilinoAsync(userId);

                if (contrato == null)
                {
                    TempData["Error"] = "No tiene un contrato activo";
                    return RedirectToAction(nameof(Index));
                }

                var inmueble = await _repositorioInmueble.ObtenerInmuebleConGaleriaAsync(contrato.IdInmueble);
                if (inmueble == null)
                {
                    TempData["Error"] = "Inmueble no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Cargar propietario
                inmueble.Propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(contrato.IdPropietario);

                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar vivienda: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Inquilino/MisPagos
        public async Task<IActionResult> MisPagos()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var pagos = await _repositorioAlquiler.ObtenerPagosPorInquilinoAsync(userId);

                // Cargar datos relacionados
                foreach (var pago in pagos)
                {
                    if (pago.Contrato != null)
                    {
                        pago.Contrato.Inmueble = await _repositorioInmueble.GetByIdAsync(pago.Contrato.IdInmueble);
                    }
                }

                return View(pagos);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar pagos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Inquilino/ReportarProblema
        public IActionResult ReportarProblema()
        {
            return View();
        }

        // POST: Inquilino/ReportarProblema
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportarProblema(string tipoProblema, string urgencia, string descripcion, IFormFileCollection evidencia)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);
                var contrato = await _repositorioAlquiler.ObtenerContratoVigentePorInquilinoAsync(userId);

                if (contrato == null)
                {
                    TempData["Error"] = "No tiene un contrato activo para reportar problemas";
                    return RedirectToAction(nameof(Index));
                }

                // Aquí iría la lógica para guardar el reporte en la base de datos
                // Por ahora simulamos el envío

                TempData["Success"] = "Problema reportado exitosamente. Nos contactaremos con usted pronto.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al reportar problema: {ex.Message}";
                return View();
            }
        }
    }
}
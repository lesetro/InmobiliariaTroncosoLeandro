using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;


namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class ContactoController : Controller
    {
        private readonly IRepositorioContacto _repositorioContacto;
        private readonly ILogger<ContactoController> _logger;

        public ContactoController(IRepositorioContacto repositorioContacto, ILogger<ContactoController> logger)
        {
            _repositorioContacto = repositorioContacto;
            _logger = logger;
        }

        // GET: Contacto 
        [AllowAnonymous]
        public IActionResult Index(int? idInmueble = null)
        {
            var contacto = new Contacto();
            if (idInmueble.HasValue)
            {
                contacto.IdInmueble = idInmueble.Value;
                contacto.Asunto = $"Consulta sobre inmueble ID: {idInmueble}";
            }

            return View(contacto);
        }

        // POST: Contacto - Enviar formulario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Index(Contacto contacto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Capturar información adicional
                    contacto.IpOrigen = GetClientIpAddress();
                    contacto.UserAgent = Request.Headers["User-Agent"].ToString();
                    contacto.FechaContacto = DateTime.Now;

                    bool resultado = await _repositorioContacto.CrearContactoAsync(contacto);

                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "¡Mensaje enviado correctamente! Nos pondremos en contacto contigo pronto.";
                        _logger.LogInformation($"Nuevo contacto recibido: {contacto.NombreCompleto} - {contacto.Email}");

                        // Aquí puedes agregar envío de email de notificación
                        // await EnviarNotificacionEmailAsync(contacto);
                        //No esta implementado el servicio de email
                        //Desarrollar segun el servicio que uses
                        // Ejemplo: SendGrid, SMTP, etc.
                        // Por ahora, solo se registra en el log
                        // Se hizo a modo de aprendizaje sobre el tema de notificaciones email
                        
                        return RedirectToAction(nameof(Gracias));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Error al enviar el mensaje. Inténtalo nuevamente.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar contacto");
                TempData["ErrorMessage"] = "Ocurrió un error inesperado. Inténtalo nuevamente.";
            }

            return View(contacto);
        }

      
        // GET: Contacto/Administrar - Panel de administración
        
        public async Task<IActionResult> Administrar(int pagina = 1, string buscar = "", string estado = "")
        {
            const int itemsPorPagina = 15;

            try
            {
                var (contactos, totalRegistros) = await _repositorioContacto.ObtenerContactosConPaginacionAsync(
                    pagina, buscar, estado, itemsPorPagina);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;

                // Estadísticas para el dashboard
                var estadisticas = await _repositorioContacto.ObtenerEstadisticasContactosAsync();
                ViewBag.Estadisticas = estadisticas;

                return View(contactos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar contactos");
                TempData["ErrorMessage"] = "Error al cargar los contactos";
                return View(new List<Contacto>());
            }
        }

        // GET: Contacto/Details/5
        
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var contacto = await _repositorioContacto.ObtenerContactoPorIdAsync(id);
                
                if (contacto == null)
                {
                    TempData["ErrorMessage"] = "Contacto no encontrado";
                    return RedirectToAction(nameof(Administrar));
                }

                return View(contacto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar contacto {Id}", id);
                TempData["ErrorMessage"] = "Error al cargar el contacto";
                return RedirectToAction(nameof(Administrar));
            }
        }
        // GET: Contacto/Gracias - Página de agradecimiento
        [AllowAnonymous]
        public IActionResult Gracias()
        {
            return View();
        }

        // POST: Contacto/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            try
            {
                var estadosPermitidos = new[] { "pendiente", "respondido", "cerrado" };
                
                if (!estadosPermitidos.Contains(nuevoEstado))
                {
                    TempData["ErrorMessage"] = "Estado no válido";
                    return RedirectToAction(nameof(Details), new { id });
                }

                bool resultado = await _repositorioContacto.ActualizarEstadoContactoAsync(id, nuevoEstado);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = $"Estado cambiado a '{nuevoEstado}' exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al cambiar el estado";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del contacto {Id}", id);
                TempData["ErrorMessage"] = "Error al cambiar el estado";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // DELETE: Contacto/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool resultado = await _repositorioContacto.EliminarContactoAsync(id);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Contacto eliminado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al eliminar el contacto";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar contacto {Id}", id);
                TempData["ErrorMessage"] = "Error al eliminar el contacto";
            }

            return RedirectToAction(nameof(Administrar));
        }

        // Método auxiliar para obtener IP del cliente
        private string GetClientIpAddress()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                // Verificar si viene de un proxy
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
                }
                else if (Request.Headers.ContainsKey("X-Real-IP"))
                {
                    ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
                }

                return ipAddress ?? "Desconocida";
            }
            catch
            {
                return "Desconocida";
            }
        }

        // Método para enviar notificación por email (implementar según tu servicio de email)
        private async Task EnviarNotificacionEmailAsync(Contacto contacto)
        {
            try
            {
                // Aquí implementarías el envío de email
                // Ejemplo usando un servicio de email:
                /*
                var mensaje = $@"
                    Nuevo contacto recibido:
                    
                    Nombre: {contacto.NombreCompleto}
                    Email: {contacto.Email}
                    Teléfono: {contacto.Telefono}
                    Asunto: {contacto.Asunto}
                    Mensaje: {contacto.Mensaje}
                    Fecha: {contacto.FechaContacto:dd/MM/yyyy HH:mm}
                ";
                
                await _emailService.EnviarEmailAsync(
                    "admin@inmobiliaria.com", 
                    "Nuevo contacto - Inmobiliaria", 
                    mensaje
                );
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación por email");
                // No fallar por esto
            }
        }
    }
}
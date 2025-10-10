using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class InteresInmuebleController : Controller
    {
        private readonly IRepositorioInteresInmueble _repositorioInteresInmueble;

        public InteresInmuebleController(IRepositorioInteresInmueble repositorioInteresInmueble)
        {
            _repositorioInteresInmueble = repositorioInteresInmueble;
        }

        // GET: InteresInmueble
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "", 
            int? idInmueble = null, string fechaDesde = "", string fechaHasta = "", int itemsPorPagina = 10)
        {
            try
            {
                // Convertir fechas si están presentes
                DateTime? fechaDesdeDate = null;
                DateTime? fechaHastaDate = null;

                if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out var fDesde))
                    fechaDesdeDate = fDesde;

                if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out var fHasta))
                    fechaHastaDate = fHasta;

                var (intereses, totalRegistros) = await _repositorioInteresInmueble
                    .ObtenerConPaginacionYBusquedaAsync(pagina, buscar, estado, idInmueble, fechaDesdeDate, fechaHastaDate, itemsPorPagina);

                // Calcular información de paginación
                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);
                
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;
                ViewBag.IdInmueble = idInmueble;
                ViewBag.FechaDesde = fechaDesde;
                ViewBag.FechaHasta = fechaHasta;
                ViewBag.ITEMS_POR_PAGINA = itemsPorPagina;

                // Cargar lista de inmuebles para el filtro
                await CargarInmueblesParaFiltroAsync();

                // Cargar estadísticas para mostrar en la vista
                await CargarEstadisticasAsync();

                return View(intereses);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los intereses: {ex.Message}";
                return View(new List<InteresInmueble>());
            }
        }

        // GET: InteresInmueble/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var interes = await _repositorioInteresInmueble.ObtenerInteresConDetallesAsync(id);
                
                if (interes == null)
                {
                    return NotFound();
                }

                // Obtener otros intereses del mismo inmueble
                var (otrosIntereses, _) = await _repositorioInteresInmueble
                    .ObtenerConPaginacionYBusquedaAsync(1, "", "", interes.IdInmueble, null, null, 10);

                ViewBag.OtrosIntereses = otrosIntereses.Where(i => i.IdInteres != id).ToList();

                return View(interes);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el detalle del interés: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Marcar como contactado via AJAX
        [HttpPost]
        public async Task<JsonResult> MarcarContactado(int id, string? observaciones = null)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "ID de interés inválido" });
                }

                var resultado = await _repositorioInteresInmueble.MarcarComoContactadoAsync(id, observaciones);
                
                if (resultado)
                {
                    return Json(new { 
                        success = true, 
                        message = "Interés marcado como contactado exitosamente",
                        fechaContacto = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo marcar como contactado" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Desmarcar contactado via AJAX
        [HttpPost]
        public async Task<JsonResult> DesmarcarContactado(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "ID de interés inválido" });
                }

                var resultado = await _repositorioInteresInmueble.DesmarcarContactadoAsync(id);
                
                if (resultado)
                {
                    return Json(new { 
                        success = true, 
                        message = "Interés marcado como pendiente exitosamente"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo desmarcar como contactado" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Actualizar observaciones via AJAX
        [HttpPost]
        public async Task<JsonResult> ActualizarObservaciones(int id, string observaciones)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "ID de interés inválido" });
                }

                var resultado = await _repositorioInteresInmueble.ActualizarObservacionesAsync(id, observaciones ?? "");
                
                if (resultado)
                {
                    return Json(new { 
                        success = true, 
                        message = "Observaciones actualizadas exitosamente"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudieron actualizar las observaciones" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Dashboard con estadísticas
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var estadisticas = await _repositorioInteresInmueble.ObtenerEstadisticasInteresesAsync();
                var interesesRecientes = await _repositorioInteresInmueble.ObtenerInteresesRecientesAsync(10);
                var interesesUrgentes = await _repositorioInteresInmueble.ObtenerInteresesUrgentesAsync();

                ViewBag.Estadisticas = estadisticas;
                ViewBag.InteresesRecientes = interesesRecientes;
                ViewBag.InteresesUrgentes = interesesUrgentes;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el dashboard: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Obtener intereses urgentes via AJAX
        [HttpGet]
        public async Task<JsonResult> ObtenerInteresesUrgentes()
        {
            try
            {
                var interesesUrgentes = await _repositorioInteresInmueble.ObtenerInteresesUrgentesAsync();
                
                var resultado = interesesUrgentes.Select(i => new
                {
                    id = i.IdInteres,
                    nombre = i.Nombre,
                    email = i.Email,
                    telefono = i.Telefono,
                    direccion = i.Inmueble?.Direccion,
                    diasDesdeInteres = i.DiasDesdeInteres,
                    fecha = i.Fecha.ToString("dd/MM/yyyy")
                });

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Obtener estadísticas via AJAX
        [HttpGet]
        public async Task<JsonResult> ObtenerEstadisticas()
        {
            try
            {
                var estadisticas = await _repositorioInteresInmueble.ObtenerEstadisticasInteresesAsync();
                return Json(new { success = true, data = estadisticas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Acción para exportar a Excel (opcional)
        public async Task<IActionResult> ExportarExcel(string buscar = "", string estado = "", 
            int? idInmueble = null, string fechaDesde = "", string fechaHasta = "")
        {
            try
            {
                // Convertir fechas si están presentes
                DateTime? fechaDesdeDate = null;
                DateTime? fechaHastaDate = null;

                if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out var fDesde))
                    fechaDesdeDate = fDesde;

                if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out var fHasta))
                    fechaHastaDate = fHasta;

                // Obtener todos los registros sin paginación
                var (intereses, _) = await _repositorioInteresInmueble
                    .ObtenerConPaginacionYBusquedaAsync(1, buscar, estado, idInmueble, fechaDesdeDate, fechaHastaDate, int.MaxValue);

                // Aquí implementarías la lógica de exportación a Excel
                // Por ahora retornamos los datos como CSV simple
                var csv = GenerarCSV(intereses);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                
                return File(bytes, "text/csv", $"intereses_inmuebles_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Métodos auxiliares privados
        private async Task CargarInmueblesParaFiltroAsync()
        {
            var inmuebles = await _repositorioInteresInmueble.ObtenerInmueblesConInteresesAsync();
            ViewBag.InmueblesConIntereses = new SelectList(inmuebles, "IdInmueble", "Direccion");
        }

        private async Task CargarEstadisticasAsync()
        {
            var estadisticas = await _repositorioInteresInmueble.ObtenerEstadisticasInteresesAsync();
            ViewBag.EstadisticasResumen = estadisticas;
        }

        private string GenerarCSV(IList<InteresInmueble> intereses)
        {
            var csv = new System.Text.StringBuilder();
            
            // Headers
            csv.AppendLine("Fecha,Nombre,Email,Telefono,Inmueble,Precio,Estado,Fecha Contacto,Observaciones");
            
            // Data
            foreach (var interes in intereses)
            {
                csv.AppendLine($"{interes.Fecha:dd/MM/yyyy}," +
                              $"\"{interes.Nombre}\"," +
                              $"\"{interes.Email}\"," +
                              $"\"{interes.Telefono ?? ""}\"," +
                              $"\"{interes.Inmueble?.Direccion ?? ""}\"," +
                              $"{interes.Inmueble?.Precio:C}," +
                              $"{interes.EstadoTexto}," +
                              $"{interes.FechaContacto?.ToString("dd/MM/yyyy") ?? ""}," +
                              $"\"{interes.Observaciones?.Replace("\"", "\"\"") ?? ""}\"");
            }
            
            return csv.ToString();
        }

        // Acción para procesar múltiples intereses (marcar varios como contactados)
        [HttpPost]
        public async Task<JsonResult> MarcarMultiplesContactados([FromBody] int[] ids, string? observaciones = null)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "No se seleccionaron intereses" });
                }

                int exitosos = 0;
                int errores = 0;

                foreach (var id in ids)
                {
                    var resultado = await _repositorioInteresInmueble.MarcarComoContactadoAsync(id, observaciones);
                    if (resultado)
                        exitosos++;
                    else
                        errores++;
                }

                return Json(new { 
                    success = true, 
                    message = $"Se marcaron {exitosos} intereses como contactados. {(errores > 0 ? $"Errores: {errores}" : "")}",
                    exitosos = exitosos,
                    errores = errores
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Acción para obtener información rápida de un interés
        [HttpGet]
        public async Task<JsonResult> ObtenerInfoRapida(int id)
        {
            try
            {
                var interes = await _repositorioInteresInmueble.ObtenerInteresConDetallesAsync(id);
                
                if (interes == null)
                {
                    return Json(new { success = false, message = "Interés no encontrado" });
                }

                var info = new
                {
                    id = interes.IdInteres,
                    nombre = interes.Nombre,
                    email = interes.Email,
                    telefono = interes.Telefono,
                    fecha = interes.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    contactado = interes.Contactado,
                    fechaContacto = interes.FechaContacto?.ToString("dd/MM/yyyy HH:mm"),
                    observaciones = interes.Observaciones,
                    inmueble = new
                    {
                        direccion = interes.Inmueble?.Direccion,
                        precio = interes.Inmueble?.Precio.ToString("C"),
                        tipo = interes.Inmueble?.TipoInmueble?.Nombre
                    },
                    diasDesdeInteres = interes.DiasDesdeInteres
                };

                return Json(new { success = true, data = info });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
    }
}
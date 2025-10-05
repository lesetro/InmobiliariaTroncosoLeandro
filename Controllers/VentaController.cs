using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Services;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class VentasController : Controller
    {
        private readonly IRepositorioVenta _repositorioVenta;
        private readonly ISearchService _searchService;
        private const int ITEMS_POR_PAGINA = 10;

        public VentasController(IRepositorioVenta repositorioVenta, ISearchService searchService)
        {
            _repositorioVenta = repositorioVenta;
            _searchService = searchService;
        }

        // GET: Ventas - Index con paginación
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                var (pagos, totalRegistros) = await _repositorioVenta
                    .ObtenerPagosVentaConPaginacionAsync(pagina, buscar, estado, ITEMS_POR_PAGINA);

                // Preparar datos de paginación
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / ITEMS_POR_PAGINA);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;
                ViewBag.ITEMS_POR_PAGINA = ITEMS_POR_PAGINA;

                return View(pagos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar las ventas: {ex.Message}";
                return View(new List<Pago>());
            }
        }

        // GET: Ventas/Create
        public IActionResult Create()
        {
            var pago = new Pago
            {
                TipoPago = "venta",
                FechaPago = DateTime.Now.Date,
                NumeroPago = 1,
                Estado = "pagado"
            };
            
            return View(pago);
        }

        // POST: Ventas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pago pago, IFormFile? ComprobanteArchivo)
        {
            // Forzar valores específicos para ventas
            pago.TipoPago = "venta";
            pago.IdContrato = null;
            pago.FechaVencimiento = null;
            pago.DiasMora = null;
            pago.MontoDiarioMora = null;
            pago.RecargoMora = 0;
            pago.MontoTotal = pago.MontoBase;

            // Validaciones específicas de ventas
            if (pago.IdInmueble <= 0)
            {
                ModelState.AddModelError("IdInmueble", "Debe seleccionar un inmueble válido");
            }
            
            if (pago.MontoBase <= 0)
            {
                ModelState.AddModelError("MontoBase", "El monto debe ser mayor a 0");
            }

            // Validar que el inmueble esté disponible
            if (pago.IdInmueble > 0 && !await _repositorioVenta.InmuebleDisponibleParaVentaAsync(pago.IdInmueble))
            {
                ModelState.AddModelError("IdInmueble", "El inmueble seleccionado no está disponible para venta");
            }

            // Validar número de pago único para el inmueble
            if (pago.IdInmueble > 0 && await _repositorioVenta.ExisteNumeroPagoVentaAsync(pago.IdInmueble, pago.NumeroPago))
            {
                ModelState.AddModelError("NumeroPago", "Ya existe un pago con este número para el inmueble");
            }

            // Procesar archivo de comprobante si se subió
            if (ComprobanteArchivo != null && ComprobanteArchivo.Length > 0)
            {
                var resultadoArchivo = await ProcesarComprobanteAsync(ComprobanteArchivo);
                if (resultadoArchivo.exito)
                {
                    pago.ComprobanteRuta = resultadoArchivo.ruta;
                    pago.ComprobanteNombre = resultadoArchivo.nombre;
                }
                else
                {
                    ModelState.AddModelError("ComprobanteArchivo", resultadoArchivo.error);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(pago);
            }

            try
            {
                var resultado = await _repositorioVenta.CrearPagoVentaAsync(pago);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Venta registrada exitosamente. El inmueble ha sido marcado como vendido.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Error al registrar la venta. Verifique que el inmueble esté disponible.");
                    return View(pago);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al registrar venta: {ex.Message}");
                return View(pago);
            }
        }

        // GET: Ventas/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var pago = await _repositorioVenta.ObtenerPagoVentaConDetallesAsync(id);
                
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(pago);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar venta: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Ventas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pago pago, IFormFile? ComprobanteArchivo)
        {
            if (id != pago.IdPago)
            {
                return NotFound();
            }

            // Forzar valores específicos para ventas
            pago.TipoPago = "venta";
            pago.MontoTotal = pago.MontoBase;

            // Validaciones específicas
            if (pago.MontoBase <= 0)
            {
                ModelState.AddModelError("MontoBase", "El monto debe ser mayor a 0");
            }

            // Validar número de pago único (excluyendo el actual)
            if (await _repositorioVenta.ExisteNumeroPagoVentaAsync(pago.IdInmueble, pago.NumeroPago, pago.IdPago))
            {
                ModelState.AddModelError("NumeroPago", "Ya existe otro pago con este número para el inmueble");
            }

            // Procesar nuevo comprobante si se subió
            if (ComprobanteArchivo != null && ComprobanteArchivo.Length > 0)
            {
                // Eliminar comprobante anterior si existe
                if (!string.IsNullOrEmpty(pago.ComprobanteRuta))
                {
                    await EliminarComprobanteAnteriorAsync(pago.ComprobanteRuta);
                }

                var resultadoArchivo = await ProcesarComprobanteAsync(ComprobanteArchivo);
                if (resultadoArchivo.exito)
                {
                    pago.ComprobanteRuta = resultadoArchivo.ruta;
                    pago.ComprobanteNombre = resultadoArchivo.nombre;
                }
                else
                {
                    ModelState.AddModelError("ComprobanteArchivo", resultadoArchivo.error);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(pago);
            }

            try
            {
                var resultado = await _repositorioVenta.ActualizarPagoVentaAsync(pago);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Venta actualizada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar la venta");
                    return View(pago);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar venta: {ex.Message}");
                return View(pago);
            }
        }

        // GET: Ventas/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var pago = await _repositorioVenta.ObtenerPagoVentaConDetallesAsync(id);
                
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(pago);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar venta: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Ventas/Delete/5
        // TODO: Reemplazar con usuario autenticado cuando se implemente auth
        private const int USUARIO_SISTEMA_TEMPORAL = 1;

        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int idUsuarioAnulador = USUARIO_SISTEMA_TEMPORAL)
        {
            try
            {
                var resultado = await _repositorioVenta.AnularPagoVentaAsync(id, idUsuarioAnulador);

                if (resultado)
                {
                    TempData["SuccessMessage"] = "Venta anulada exitosamente. El inmueble vuelve a estar disponible.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al anular la venta";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al anular venta: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========================
        // ENDPOINTS AJAX PARA AUTOCOMPLETADO
        // ========================

        [HttpGet]
        public async Task<IActionResult> BuscarInmueblesVenta(string termino, int limite = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                {
                    return Json(new List<object>());
                }

                // Buscar usando SearchService (filtrado por disponibles)
                var resultados = await _searchService.BuscarInmueblesAsync(termino, limite);
                
                // Filtrar solo los disponibles para venta
                var inmueblesDisponibles = new List<object>();
                
                foreach (var resultado in resultados)
                {
                    int idInmueble = int.Parse(resultado.Id);
                    if (await _repositorioVenta.InmuebleDisponibleParaVentaAsync(idInmueble))
                    {
                        inmueblesDisponibles.Add(new
                        {
                            id = resultado.Id,
                            texto = resultado.Texto,
                            info = "Disponible para venta"
                        });
                    }
                }

                return Json(inmueblesDisponibles);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarUsuarios(string termino, int limite = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                {
                    return Json(new List<object>());
                }

                var usuarios = await _repositorioVenta.ObtenerUsuariosActivosAsync(limite);
                
                var usuariosFiltrados = usuarios
                    .Where(u => u.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                               u.Apellido.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                               u.Dni.Contains(termino, StringComparison.OrdinalIgnoreCase))
                    .Select(u => new
                    {
                        id = u.IdUsuario,
                        texto = $"{u.Apellido}, {u.Nombre}",
                        info = $"DNI: {u.Dni}"
                    });

                return Json(usuariosFiltrados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ========================
        // MÉTODOS PRIVADOS DE APOYO
        // ========================

        private async Task<(bool exito, string ruta, string nombre, string error)> ProcesarComprobanteAsync(IFormFile archivo)
        {
            try
            {
                // Validar formato
                var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                
                if (!extensionesPermitidas.Contains(extension))
                {
                    return (false, "", "", "Solo se permiten archivos PDF, JPG, JPEG o PNG");
                }

                // Validar tamaño (5MB máximo)
                if (archivo.Length > 5 * 1024 * 1024)
                {
                    return (false, "", "", "El archivo no puede superar 5MB");
                }

                // Crear directorio si no existe
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "comprobantes");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generar nombre único
                var nombreArchivo = $"venta_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}{extension}";
                var rutaCompleta = Path.Combine(uploadsDir, nombreArchivo);
                var rutaRelativa = $"/uploads/comprobantes/{nombreArchivo}";

                // Guardar archivo
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                return (true, rutaRelativa, archivo.FileName, "");
            }
            catch (Exception ex)
            {
                return (false, "", "", $"Error al procesar archivo: {ex.Message}");
            }
        }

        private async Task EliminarComprobanteAnteriorAsync(string rutaComprobante)
        {
            try
            {
                if (string.IsNullOrEmpty(rutaComprobante)) return;

                var rutaCompleta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rutaComprobante.TrimStart('/'));
                
                if (System.IO.File.Exists(rutaCompleta))
                {
                    await Task.Run(() => System.IO.File.Delete(rutaCompleta));
                }
            }
            catch (Exception ex)
            {
                // Log pero no fallar por esto
                Console.WriteLine($"Error al eliminar comprobante anterior: {ex.Message}");
            }
        }

        // ========================
        // MÉTODO PARA DESCARGAR COMPROBANTE
        // ========================

        public async Task<IActionResult> DescargarComprobante(int id)
        {
            try
            {
                var pago = await _repositorioVenta.ObtenerPagoVentaPorIdAsync(id);
                
                if (pago == null || string.IsNullOrEmpty(pago.ComprobanteRuta))
                {
                    return NotFound();
                }

                var rutaCompleta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pago.ComprobanteRuta.TrimStart('/'));
                
                if (!System.IO.File.Exists(rutaCompleta))
                {
                    return NotFound();
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(rutaCompleta);
                var contentType = GetContentType(pago.ComprobanteRuta);
                var nombreDescarga = pago.ComprobanteNombre ?? $"comprobante_venta_{id}{Path.GetExtension(pago.ComprobanteRuta)}";

                return File(bytes, contentType, nombreDescarga);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al descargar comprobante: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private static string GetContentType(string ruta)
        {
            var extension = Path.GetExtension(ruta).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
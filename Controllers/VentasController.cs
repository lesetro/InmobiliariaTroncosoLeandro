using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Services;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data;
using System.Security.Claims;


namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class VentasController : Controller
    {
        private readonly IRepositorioVenta _repositorioVenta;
        private readonly IRepositorioContratoVenta _repositorioContratoVenta;

        private const int ITEMS_POR_PAGINA = 10;

        public VentasController(
            IRepositorioVenta repositorioVenta,
            IRepositorioContratoVenta repositorioContratoVenta)
        {
            _repositorioVenta = repositorioVenta;
            _repositorioContratoVenta = repositorioContratoVenta;

        }

        // GET: Ventas - Index con paginación de PAGOS de ventas
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                Console.WriteLine($"=== DEBUG VENTAS INDEX ===");
                Console.WriteLine($"Parámetros - Página: {pagina}, Buscar: '{buscar}', Estado: '{estado}'");

                var (pagos, totalRegistros) = await _repositorioVenta
                    .ObtenerPagosVentaConPaginacionAsync(pagina, buscar, estado, ITEMS_POR_PAGINA);

                Console.WriteLine($"Resultados - Total registros: {totalRegistros}, Pagos devueltos: {pagos?.Count ?? 0}");

                // Verificar datos de los primeros 3 pagos
                if (pagos != null && pagos.Any())
                {
                    for (int i = 0; i < Math.Min(3, pagos.Count); i++)
                    {
                        var pago = pagos[i];
                        Console.WriteLine($"Pago {i + 1}: ID={pago.IdPago}, InmuebleID={pago.IdInmueble}, " +
                                        $"Dirección='{pago.Inmueble?.Direccion ?? "NULL"}', " +
                                        $"Concepto='{pago.Concepto}', Monto={pago.MontoTotal}");
                    }
                }
                else
                {
                    Console.WriteLine(" NO HAY PAGOS DEVUELTOS");
                }

                // Preparar datos de paginación
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / ITEMS_POR_PAGINA);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;


                ViewBag.ITEMS_POR_PAGINA = ITEMS_POR_PAGINA;

                Console.WriteLine($"ViewBag - PaginaActual: {ViewBag.PaginaActual}, TotalPaginas: {ViewBag.TotalPaginas}");
                Console.WriteLine($"=== FIN DEBUG ===");

                return View(pagos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en Index: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Error al cargar los pagos de venta: {ex.Message}";
                return View(new List<Pago>());
            }
        }
        // GET: Ventas/Create - Para crear nuevo PAGO de venta
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pago pago, IFormFile? ComprobanteArchivo)
        {
            Console.WriteLine($"🔍 INICIANDO CREATE - IdInmueble: {pago.IdInmueble}, MontoBase: {pago.MontoBase}");

            // 🔥 NUEVO: Obtener el ID del usuario logueado
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"🔍 Usuario logueado - Claim NameIdentifier: {usuarioIdClaim}");

            // Si no funciona con NameIdentifier, intenta con otros claims
            if (string.IsNullOrEmpty(usuarioIdClaim))
            {
                usuarioIdClaim = User.FindFirstValue("id_usuario") ??
                                User.FindFirstValue(ClaimTypes.Sid) ??
                                User.FindFirstValue("sub");
                Console.WriteLine($"🔍 Usuario logueado - Claim alternativo: {usuarioIdClaim}");
            }

            // Convertir a int y asignar al pago
            if (int.TryParse(usuarioIdClaim, out int usuarioId) && usuarioId > 0)
            {
                pago.IdUsuarioCreador = usuarioId;
                Console.WriteLine($"✅ IdUsuarioCreador asignado: {pago.IdUsuarioCreador}");
            }
            else
            {
                // Fallback: usar el usuario JeanCarlos (ID 1) que existe en tu BD
                pago.IdUsuarioCreador = 1;
                Console.WriteLine($"⚠️ Usando usuario por defecto (ID 1) - No se pudo obtener usuario logueado");
            }

            // Validar que exista un contrato de venta para este inmueble
            if (pago.IdInmueble > 0)
            {
                Console.WriteLine($"🔍 Buscando contrato para inmueble: {pago.IdInmueble}");

                var contratoExistente = await _repositorioContratoVenta.ObtenerContratoPorInmuebleAsync(pago.IdInmueble);

                if (contratoExistente == null)
                {
                    Console.WriteLine($"❌ No se encontró contrato para inmueble: {pago.IdInmueble}");
                    ModelState.AddModelError("IdInmueble", "No existe un contrato de venta para este inmueble. Primero debe crear el contrato.");
                }
                else
                {
                    Console.WriteLine($"✅ Contrato encontrado: ID {contratoExistente.IdContratoVenta}, Estado: {contratoExistente.Estado}, PrecioTotal: {contratoExistente.PrecioTotal}");

                    // Asignar el ID del contrato de venta al pago
                    pago.IdContratoVenta = contratoExistente.IdContratoVenta;
                    Console.WriteLine($"📝 IdContratoVenta asignado: {pago.IdContratoVenta}");

                    // Calcular número de pago automáticamente
                    var ultimoPago = await _repositorioVenta.ObtenerUltimoPagoVentaAsync(pago.IdInmueble);
                    pago.NumeroPago = (ultimoPago?.NumeroPago ?? 0) + 1;
                    Console.WriteLine($"📝 NumeroPago calculado: {pago.NumeroPago} (Último pago: {ultimoPago?.NumeroPago})");

                    // Validar que no exceda el precio total del contrato
                    var totalPagado = await _repositorioVenta.ObtenerTotalPagadoVentaAsync(pago.IdInmueble);
                    Console.WriteLine($"💰 Total pagado actual: {totalPagado}, Monto nuevo: {pago.MontoBase}, Precio total: {contratoExistente.PrecioTotal}");

                    if (totalPagado + pago.MontoBase > contratoExistente.PrecioTotal)
                    {
                        var mensajeError = $"El monto excede el precio de venta. Total pagado: ${totalPagado}, Precio venta: ${contratoExistente.PrecioTotal}, Máximo permitido: ${contratoExistente.PrecioTotal - totalPagado}";
                        Console.WriteLine($"❌ {mensajeError}");
                        ModelState.AddModelError("MontoBase", mensajeError);
                    }
                }
            }

            // Forzar valores específicos para ventas
            pago.TipoPago = "venta";
            pago.FechaVencimiento = null;
            pago.DiasMora = null;
            pago.MontoDiarioMora = null;
            pago.RecargoMora = 0;
            pago.MontoTotal = pago.MontoBase;

            Console.WriteLine($"📝 Valores forzados - TipoPago: {pago.TipoPago}, MontoTotal: {pago.MontoTotal}");

            // Validaciones específicas de ventas
            if (pago.IdInmueble <= 0)
            {
                Console.WriteLine($"❌ IdInmueble no válido: {pago.IdInmueble}");
                ModelState.AddModelError("IdInmueble", "Debe seleccionar un inmueble válido");
            }

            if (pago.MontoBase <= 0)
            {
                Console.WriteLine($"❌ MontoBase no válido: {pago.MontoBase}");
                ModelState.AddModelError("MontoBase", "El monto debe ser mayor a 0");
            }

            // Procesar archivo de comprobante si se subió
            if (ComprobanteArchivo != null && ComprobanteArchivo.Length > 0)
            {
                Console.WriteLine($"📎 Procesando archivo: {ComprobanteArchivo.FileName}, Tamaño: {ComprobanteArchivo.Length}");

                var resultadoArchivo = await ProcesarComprobanteAsync(ComprobanteArchivo);
                if (resultadoArchivo.exito)
                {
                    pago.ComprobanteRuta = resultadoArchivo.ruta;
                    pago.ComprobanteNombre = resultadoArchivo.nombre;
                    Console.WriteLine($"✅ Archivo procesado: {pago.ComprobanteNombre}");
                }
                else
                {
                    Console.WriteLine($"❌ Error procesando archivo: {resultadoArchivo.error}");
                    ModelState.AddModelError("ComprobanteArchivo", resultadoArchivo.error);
                }
            }
            else
            {
                Console.WriteLine($"📎 No se subió archivo de comprobante");
            }

            // Verificar estado del ModelState
            Console.WriteLine($"🔍 Estado del ModelState: Válido = {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"❌ Error en {error.Key}: {err.ErrorMessage}");
                    }
                }
                return View(pago);
            }

            try
            {
                Console.WriteLine($"🚀 Intentando crear pago en la base de datos...");

                var resultado = await _repositorioVenta.CrearPagoVentaAsync(pago);

                if (resultado)
                {
                    Console.WriteLine($"✅ Pago creado exitosamente");
                    TempData["SuccessMessage"] = "Pago de venta registrado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    Console.WriteLine($"❌ CrearPagoVentaAsync devolvió false");
                    ModelState.AddModelError("", "Error al registrar el pago - el repositorio devolvió false");
                    return View(pago);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 EXCEPCIÓN en Create: {ex.Message}");
                Console.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error al registrar pago: {ex.Message}");
                return View(pago);
            }
        }


        // ========================
        // ENDPOINTS AJAX MODIFICADOS
        // ========================

        [HttpGet]
        public async Task<JsonResult> BuscarInmueblesVenta(string termino, int limite = 20)
        {
            try
            {
                var resultados = await _repositorioVenta.BuscarInmueblesParaVentaAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {

                return Json(new List<SearchResultVenta>());
                return Json(new { success = false, error = $"Error al buscar inmuebles: {ex.Message}" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerInfoContratoVenta(int idInmueble)
        {
            try
            {
                var contrato = await _repositorioContratoVenta.ObtenerContratoPorInmuebleAsync(idInmueble);
                if (contrato == null)
                {
                    return Json(new { error = "No se encontró contrato de venta para este inmueble" });
                }

                var totalPagado = await _repositorioVenta.ObtenerTotalPagadoVentaAsync(idInmueble);
                var saldoPendiente = contrato.PrecioTotal - totalPagado;
                var ultimoPago = await _repositorioVenta.ObtenerUltimoPagoVentaAsync(idInmueble);
                var proximoNumeroPago = (ultimoPago?.NumeroPago ?? 0) + 1;

                return Json(new
                {
                    contratoId = contrato.IdContratoVenta,
                    compradorNombre = $"{contrato.Comprador?.Nombre} {contrato.Comprador?.Apellido}",
                    precioVenta = contrato.PrecioTotal,
                    totalPagado = totalPagado,
                    saldoPendiente = saldoPendiente,
                    proximoNumeroPago = proximoNumeroPago
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Los métodos restantes se mantienen IGUALES
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
        // GET: Ventas/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    TempData["ErrorMessage"] = "ID de pago no especificado";
                    return RedirectToAction(nameof(Index));
                }

                var pago = await _repositorioVenta.ObtenerPagoVentaConDetallesAsync(id.Value);
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Cargar datos adicionales para la vista
                await CargarDatosAdicionales(pago);

                return View(pago);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en Details: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "Error al cargar los detalles del pago";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Ventas/Edit/5
        [HttpGet]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    TempData["ErrorMessage"] = "ID de pago no especificado";
                    return RedirectToAction(nameof(Index));
                }

                var pago = await _repositorioVenta.ObtenerPagoVentaConDetallesAsync(id.Value);
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si está anulado
                if (pago.Estado.ToLower() == "anulado")
                {
                    TempData["ErrorMessage"] = "No se puede editar un pago anulado";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                await CargarViewBagParaFormulario(pago.IdInmueble);
                return View(pago);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en Edit (GET): {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el formulario de edición";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Ventas/Edit/5
        [HttpPost]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pago pago)
        {
            try
            {
                if (id != pago.IdPago)
                {
                    TempData["ErrorMessage"] = "ID de pago no coincide";
                    return RedirectToAction(nameof(Index));
                }

                // Validar número de pago único
                if (await _repositorioVenta.ExisteNumeroPagoVentaAsync(pago.IdInmueble, pago.NumeroPago, pago.IdPago))
                {
                    ModelState.AddModelError("NumeroPago", "Ya existe un pago con este número para el inmueble seleccionado");
                }

                if (ModelState.IsValid)
                {
                    var resultado = await _repositorioVenta.ActualizarPagoVentaAsync(pago);
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Pago actualizado correctamente";
                        return RedirectToAction(nameof(Details), new { id = pago.IdPago });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Error al actualizar el pago";
                    }
                }

                await CargarViewBagParaFormulario(pago.IdInmueble);
                return View(pago);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en Edit (POST): {ex.Message}");
                TempData["ErrorMessage"] = "Error al actualizar el pago";

                await CargarViewBagParaFormulario(pago.IdInmueble);
                return View(pago);
            }
        }

        // GET: Ventas/Delete/5
        [HttpGet]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    TempData["ErrorMessage"] = "ID de pago no especificado";
                    return RedirectToAction(nameof(Index));
                }

                var pago = await _repositorioVenta.ObtenerPagoVentaConDetallesAsync(id.Value);
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Cargar datos adicionales para la vista
                await CargarDatosAdicionales(pago);

                return View(pago);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en Delete (GET): {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar la página de anulación";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Ventas/Delete/5
        [HttpPost]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string motivoAnulacion, string impactoInmueble)
        {
            try
            {
                var pago = await _repositorioVenta.ObtenerPagoVentaPorIdAsync(id);
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar que no esté ya anulado
                if (pago.Estado.ToLower() == "anulado")
                {
                    TempData["ErrorMessage"] = "El pago ya está anulado";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                // Obtener ID del usuario actual
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Anular el pago
                var resultado = await _repositorioVenta.AnularPagoVentaAsync(id, userId);

                if (resultado)
                {
                    // Manejar el impacto en el inmueble según la selección
                    await ManejarImpactoInmueble(pago.IdInmueble, impactoInmueble);

                    TempData["SuccessMessage"] = "Pago anulado correctamente";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo anular el pago";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en Delete (POST): {ex.Message}");
                TempData["ErrorMessage"] = "Error al anular el pago";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        // MÉTODOS PRIVADOS AUXILIARES

        private async Task CargarDatosAdicionales(Pago pago)
        {
            try
            {
                // Obtener estadísticas del inmueble
                var totalPagado = await _repositorioVenta.ObtenerTotalPagadoVentaAsync(pago.IdInmueble);

                // Buscar información del contrato de venta para este inmueble
                var contrato = await _repositorioContratoVenta.ObtenerContratoPorInmuebleAsync(pago.IdInmueble);

                ViewBag.EstadisticasInmueble = new
                {
                    TotalPagos = await ObtenerCantidadPagosInmueble(pago.IdInmueble),
                    MontoPagado = totalPagado,
                    PrecioTotal = contrato?.PrecioTotal ?? 0,
                    SaldoPendiente = contrato?.PrecioTotal - totalPagado ?? 0,
                    PorcentajePagado = contrato?.PrecioTotal > 0 ?
                        (totalPagado / contrato.PrecioTotal) * 100 : 0,
                    ContratoActivo = contrato != null && (contrato.Estado == "activo" || contrato.Estado == "seña_pagada")
                };

                // Obtener estado actual del inmueble desde el contrato
                if (contrato != null)
                {
                    ViewBag.EstadoInmueble = contrato.Estado switch
                    {
                        "activo" => "Reservado",
                        "seña_pagada" => "Señado",
                        "cancelada" => "Disponible",
                        "finalizada" => "Vendido",
                        _ => "Disponible"
                    };
                }
                else
                {
                    ViewBag.EstadoInmueble = "Disponible";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en CargarDatosAdicionales: {ex.Message}");
                // En caso de error, establecer valores por defecto
                ViewBag.EstadisticasInmueble = new
                {
                    TotalPagos = 1,
                    MontoPagado = pago.MontoTotal,
                    PrecioTotal = 0,
                    SaldoPendiente = 0,
                    PorcentajePagado = 0,
                    ContratoActivo = false
                };
                ViewBag.EstadoInmueble = "Desconocido";
            }
        }

        private async Task CargarViewBagParaFormulario(int idInmueble)
        {
            try
            {
                // Obtener último pago para sugerir número de pago
                var ultimoPago = await _repositorioVenta.ObtenerUltimoPagoVentaAsync(idInmueble);
                ViewBag.SiguienteNumeroPago = (ultimoPago?.NumeroPago ?? 0) + 1;

                // Obtener contrato para validaciones
                var contrato = await _repositorioContratoVenta.ObtenerContratoPorInmuebleAsync(idInmueble);
                ViewBag.PrecioTotalContrato = contrato?.PrecioTotal ?? 0;
                ViewBag.TotalPagado = await _repositorioVenta.ObtenerTotalPagadoVentaAsync(idInmueble);
                ViewBag.SaldoPendiente = (contrato?.PrecioTotal ?? 0) - ViewBag.TotalPagado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en CargarViewBagParaFormulario: {ex.Message}");
                ViewBag.SiguienteNumeroPago = 1;
                ViewBag.PrecioTotalContrato = 0;
                ViewBag.TotalPagado = 0;
                ViewBag.SaldoPendiente = 0;
            }
        }

        private async Task<int> ObtenerCantidadPagosInmueble(int idInmueble)
        {
            try
            {
                // Usar el método de paginación para obtener todos los pagos del inmueble
                var (pagos, totalRegistros) = await _repositorioVenta
                    .ObtenerPagosVentaConPaginacionAsync(1, "", "", 1000);

                // Filtrar por inmueble específico
                var pagosInmueble = pagos?.Where(p => p.IdInmueble == idInmueble).ToList();
                return pagosInmueble?.Count ?? 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en ObtenerCantidadPagosInmueble: {ex.Message}");
                return 1;
            }
        }

        private async Task ManejarImpactoInmueble(int idInmueble, string impactoInmueble)
        {
            try
            {
                Console.WriteLine($"Manejando impacto para inmueble {idInmueble}: {impactoInmueble}");

                switch (impactoInmueble?.ToLower())
                {
                    case "disponible":
                        await _repositorioVenta.RestaurarEstadoInmuebleAsync(idInmueble);
                        break;
                    case "reservado":
                        // Aquí necesitarías un método para marcar como reservado
                        // Por ahora usar RestaurarEstadoInmuebleAsync como alternativa
                        await _repositorioVenta.RestaurarEstadoInmuebleAsync(idInmueble);
                        break;
                    case "mantener":
                    default:
                        // No hacer cambios
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en ManejarImpactoInmueble: {ex.Message}");
            }
        }
        //Por Inmueble 

        // GET: Ventas/PorInmueble
        [HttpGet]
        public async Task<IActionResult> PorInmueble(int inmuebleId)
        {
            try
            {
                Console.WriteLine($"=== DEBUG POR INMUEBLE ===");
                Console.WriteLine($"Inmueble ID: {inmuebleId}");

                // Obtener todos los pagos del inmueble usando paginación con límite alto
                var (pagos, totalRegistros) = await _repositorioVenta
                    .ObtenerPagosVentaConPaginacionAsync(1, "", "", 1000);

                // Filtrar por inmueble específico
                var pagosInmueble = pagos?.Where(p => p.IdInmueble == inmuebleId).ToList();

                Console.WriteLine($"Pagos encontrados para inmueble {inmuebleId}: {pagosInmueble?.Count ?? 0}");

                if (pagosInmueble == null || !pagosInmueble.Any())
                {
                    Console.WriteLine("No se encontraron pagos para este inmueble");
                    ViewBag.InmuebleId = inmuebleId;
                    return View(new List<Pago>());
                }

                // Obtener información del contrato de venta
                var contrato = await _repositorioContratoVenta.ObtenerContratoPorInmuebleAsync(inmuebleId);

                // CALCULAR ESTADOS DEL PROCESO DE VENTA
                var tieneSeña = pagosInmueble.Any(p =>
                    p.Concepto.ToLower().Contains("seña") &&
                    p.Estado.ToLower() == "pagado");

                var tieneAnticipo = pagosInmueble.Any(p =>
                    p.Concepto.ToLower().Contains("anticipo") &&
                    p.Estado.ToLower() == "pagado");

                var tieneTotal = pagosInmueble.Any(p =>
                    (p.Concepto.ToLower().Contains("total") || p.Concepto.ToLower().Contains("completo")) &&
                    p.Estado.ToLower() == "pagado");

                var estaVendido = contrato?.Estado?.ToLower() == "finalizada" ||
                                 ViewBag.EstadoInmueble?.ToString().ToLower() == "vendido";

                // Cargar datos para la vista
                ViewBag.InmuebleId = inmuebleId;
                ViewBag.EstadoInmueble = contrato?.Estado ?? "Disponible";
                ViewBag.TieneSeña = tieneSeña;
                ViewBag.TieneAnticipo = tieneAnticipo;
                ViewBag.TieneTotal = tieneTotal;
                ViewBag.EstaVendido = estaVendido;

                // Obtener datos del inmueble desde el primer pago
                var primerPago = pagosInmueble.First();
                ViewBag.InmuebleDatos = new
                {
                    Direccion = primerPago.Inmueble?.Direccion,
                    Precio = contrato?.PrecioTotal ?? 0,
                    TipoInmueble = primerPago.Inmueble?.TipoInmueble?.Nombre ?? "No especificado",

                };

                Console.WriteLine($"Proceso de venta - Seña: {tieneSeña}, Anticipo: {tieneAnticipo}, Total: {tieneTotal}, Vendido: {estaVendido}");
                Console.WriteLine($"=== FIN DEBUG ===");

                return View(pagosInmueble);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en PorInmueble: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "Error al cargar el historial de pagos del inmueble";
                ViewBag.InmuebleId = inmuebleId;
                return View(new List<Pago>());
            }
        }

        // POST: Ventas/MarcarComoPagado
        [HttpPost]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoPagado(int idPago)
        {
            try
            {
                var pago = await _repositorioVenta.ObtenerPagoVentaPorIdAsync(idPago);
                if (pago == null)
                {
                    return Json(new { success = false, message = "Pago no encontrado" });
                }

                // Verificar que no esté anulado
                if (pago.Estado.ToLower() == "anulado")
                {
                    return Json(new { success = false, message = "No se puede marcar como pagado un pago anulado" });
                }

                // Actualizar estado a pagado
                pago.Estado = "pagado";
                var resultado = await _repositorioVenta.ActualizarPagoVentaAsync(pago);

                if (resultado)
                {
                    // Verificar si con este pago se completa la venta
                    await VerificarCompletacionVenta(pago.IdInmueble);

                    return Json(new { success = true, message = "Pago marcado como pagado correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al actualizar el pago" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en MarcarComoPagado: {ex.Message}");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        // GET: Ventas/ExportarPorInmueble
        [HttpGet]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> ExportarPorInmueble(int inmuebleId)
        {
            try
            {
                // Obtener todos los pagos del inmueble
                var (pagos, totalRegistros) = await _repositorioVenta
                    .ObtenerPagosVentaConPaginacionAsync(1, "", "", 1000);

                var pagosInmueble = pagos?.Where(p => p.IdInmueble == inmuebleId).ToList();

                if (pagosInmueble == null || !pagosInmueble.Any())
                {
                    TempData["ErrorMessage"] = "No hay datos para exportar";
                    return RedirectToAction(nameof(PorInmueble), new { inmuebleId });
                }

                // Crear contenido CSV
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("ID;Fecha;Concepto;Monto;Estado;Tipo;Observaciones");

                foreach (var pago in pagosInmueble.OrderBy(p => p.FechaPago))
                {
                    csv.AppendLine($"{pago.IdPago};{pago.FechaPago:dd/MM/yyyy};{pago.Concepto};{pago.MontoTotal};{pago.Estado};{GetTipoPagoFromConcepto(pago.Concepto)};{pago.Observaciones}");
                }

                // Obtener información del inmueble
                var primerPago = pagosInmueble.First();
                var nombreArchivo = $"Pagos_Inmueble_{inmuebleId}_{primerPago.Inmueble?.Direccion?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", nombreArchivo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en ExportarPorInmueble: {ex.Message}");
                TempData["ErrorMessage"] = "Error al exportar los datos";
                return RedirectToAction(nameof(PorInmueble), new { inmuebleId });
            }
        }

        // MÉTODOS PRIVADOS AUXILIARES

        private async Task VerificarCompletacionVenta(int idInmueble)
        {
            try
            {
                // Obtener contrato de venta
                var contrato = await _repositorioContratoVenta.ObtenerContratoPorInmuebleAsync(idInmueble);
                if (contrato == null) return;

                // Obtener total pagado
                var totalPagado = await _repositorioVenta.ObtenerTotalPagadoVentaAsync(idInmueble);

                // Si se pagó el total, marcar como vendido
                if (totalPagado >= contrato.PrecioTotal)
                {
                    await _repositorioVenta.MarcarInmuebleComoVendidoAsync(idInmueble);

                    // También podrías actualizar el estado del contrato aquí
                    Console.WriteLine($"Inmueble {idInmueble} marcado como vendido - Total pagado: {totalPagado}, Precio: {contrato.PrecioTotal}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ERROR en VerificarCompletacionVenta: {ex.Message}");
            }
        }

        private string GetTipoPagoFromConcepto(string concepto)
        {
            var conceptoLower = concepto?.ToLower() ?? "";

            if (conceptoLower.Contains("seña")) return "SEÑA";
            if (conceptoLower.Contains("anticipo")) return "ANTICIPO";
            if (conceptoLower.Contains("total") || conceptoLower.Contains("completo")) return "TOTAL";
            if (conceptoLower.Contains("reserva")) return "RESERVA";

            return "VENTA";
        }

    }
}

using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Services;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class AlquileresController : Controller
    {
        private readonly IRepositorioAlquiler _repositorioAlquiler;
        private readonly ISearchService _searchService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const int ITEMS_POR_PAGINA = 10;
        private const int USUARIO_SISTEMA_TEMPORAL = 1;

        // CONSTRUCTOR
        public AlquileresController(
            IRepositorioAlquiler repositorioAlquiler,
            ISearchService searchService,
            IWebHostEnvironment webHostEnvironment)
        {
            _repositorioAlquiler = repositorioAlquiler;
            _searchService = searchService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Alquileres - Index con paginación
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                var (pagos, totalRegistros) = await _repositorioAlquiler
                    .ObtenerPagosAlquilerConPaginacionAsync(pagina, buscar, estado, ITEMS_POR_PAGINA);

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
                TempData["ErrorMessage"] = $"Error al cargar los pagos de alquiler: {ex.Message}";
                return View(new List<Pago>());
            }
        }

        // GET: Alquileres/Create
        public IActionResult Create()
        {
            var pago = new Pago
            {
                TipoPago = "alquiler",
                FechaPago = DateTime.Now.Date,
                Estado = "pagado",
                IdUsuarioCreador = USUARIO_SISTEMA_TEMPORAL
            };

            return View(pago);
        }

        // POST: Alquileres/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Pago pago, IFormFile? ComprobanteArchivo)
{
    try
    {
        Console.WriteLine("=== DEBUG CREATE POST ===");
        Console.WriteLine($"IdContrato: {pago.IdContrato}");
        Console.WriteLine($"MontoBase: {pago.MontoBase}");
        Console.WriteLine($"NumeroPago: {pago.NumeroPago}");

        // Forzar valores específicos para alquileres
        pago.TipoPago = "alquiler";
        pago.IdUsuarioCreador = USUARIO_SISTEMA_TEMPORAL;

        // Validaciones específicas de alquileres
        if (!pago.IdContrato.HasValue || pago.IdContrato <= 0)
        {
            ModelState.AddModelError("IdContrato", "Debe seleccionar un contrato válido");
        }

        if (pago.MontoBase <= 0)
        {
            ModelState.AddModelError("MontoBase", "El monto debe ser mayor a 0");
        }

        if (pago.NumeroPago <= 0)
        {
            ModelState.AddModelError("NumeroPago", "El número de pago debe ser mayor a 0");
        }

        Console.WriteLine("Punto 1: Validaciones básicas OK");

    
        if (pago.IdContrato.HasValue)
        {
            Console.WriteLine("=== VALIDACIÓN EXTRA REFORZADA ===");
            
            var datosContrato = await _repositorioAlquiler.ObtenerDatosContratoParaPagoAsync(pago.IdContrato.Value);

            if (datosContrato != null)
            {
                Console.WriteLine($"InfoPagos: {datosContrato.InfoPagos}");
                Console.WriteLine($"Próximo pago: {datosContrato.ProximoNumeroPago}, Total: {datosContrato.TotalMeses}");
                Console.WriteLine($"Pagos realizados: {datosContrato.PagosRealizados}");

               
                if (pago.NumeroPago > datosContrato.TotalMeses)
                {
                    ModelState.AddModelError("NumeroPago", 
                        $" NO se puede crear el pago #{pago.NumeroPago}. " +
                        $"El contrato solo permite {datosContrato.TotalMeses} pagos totales. " +
                        $"*Nota: Los contratos se cobran por meses completos.*");
                }

                
                if (datosContrato.PagosRealizados >= datosContrato.TotalMeses)
                {
                    ModelState.AddModelError("IdContrato", 
                        $" CONTRATO COMPLETADO. " +
                        $"Ya se registraron {datosContrato.PagosRealizados} de {datosContrato.TotalMeses} pagos. " +
                        $"No se pueden registrar más pagos para este contrato.");
                }

                
                if (pago.NumeroPago != datosContrato.ProximoNumeroPago)
                {
                    ModelState.AddModelError("NumeroPago", 
                        $" El número de pago debe ser {datosContrato.ProximoNumeroPago} " +
                        $"(próximo pago pendiente). " +
                        $"Actualmente: {datosContrato.PagosRealizados}/{datosContrato.TotalMeses} pagos.");
                }

                
                if (datosContrato.PagosRealizados == datosContrato.TotalMeses - 1)
                {
                    Console.WriteLine(" ADVERTENCIA: Este es el ÚLTIMO pago del contrato");
                    // Puedes agregar un mensaje informativo si lo deseas
                    TempData["InfoMessage"] = $" Este es el último pago del contrato ({datosContrato.TotalMeses}/{datosContrato.TotalMeses})";
                }

                
                if (datosContrato.PagosRealizados > datosContrato.TotalMeses)
                {
                    ModelState.AddModelError("", 
                        $" ERROR CRÍTICO: El contrato tiene {datosContrato.PagosRealizados} pagos " +
                        $"pero solo debería tener {datosContrato.TotalMeses}. " +
                        $"Contacte al administrador del sistema.");
                }
            }
            else
            {
                ModelState.AddModelError("IdContrato", "No se pudieron obtener los datos del contrato seleccionado.");
            }
        }
      
        // Validar que el contrato esté vigente
        if (pago.IdContrato.HasValue)
        {
            Console.WriteLine("Verificando si contrato está vigente...");
            var contratoVigente = await _repositorioAlquiler.ContratoVigenteAsync(pago.IdContrato.Value);
            Console.WriteLine($"Contrato vigente: {contratoVigente}");

            if (!contratoVigente)
            {
                ModelState.AddModelError("IdContrato", "El contrato seleccionado no está vigente o ha vencido");
            }
        }

       

        // Validar número de pago único para el contrato
        if (pago.IdContrato.HasValue)
        {
            Console.WriteLine("Verificando número de pago...");
            var existePago = await _repositorioAlquiler.ExistePagoMesContratoAsync(pago.IdContrato.Value, pago.NumeroPago);
            Console.WriteLine($"Existe pago: {existePago}");

            if (existePago)
            {
                ModelState.AddModelError("NumeroPago", 
                    $"Ya existe un pago con el número {pago.NumeroPago} para este contrato. " +
                    $"Verifique el número de pago en el historial del contrato.");
            }
        }

        // Validar que el contrato permita más pagos (redundante pero segura)
        if (pago.IdContrato.HasValue)
        {
            Console.WriteLine("Verificando si permite más pagos...");
            var permiteMasPagos = await _repositorioAlquiler.ContratoPermiteMasPagosAsync(pago.IdContrato.Value);
            Console.WriteLine($"Permite más pagos: {permiteMasPagos}");

            if (!permiteMasPagos)
            {
                ModelState.AddModelError("IdContrato", 
                    "El contrato seleccionado ya tiene todos los pagos registrados o ha vencido. " +
                    "No se permiten nuevos pagos.");
            }
        }

        Console.WriteLine("Punto 4: Validación más pagos OK");

        // Procesar archivo de comprobante si se subió
        if (ComprobanteArchivo != null && ComprobanteArchivo.Length > 0)
        {
            Console.WriteLine($"Procesando comprobante: {ComprobanteArchivo.FileName}");
            var resultadoArchivo = await ProcesarComprobanteAsync(ComprobanteArchivo);
            Console.WriteLine($"Resultado comprobante: {resultadoArchivo.exito}");

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

        Console.WriteLine("Punto 5: Comprobante procesado OK");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState NO válido, errores:");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"  - {error.ErrorMessage}");
            }
            
            // Cargar datos del contrato para mostrar en la vista
            if (pago.IdContrato.HasValue)
            {
                var datosContrato = await _repositorioAlquiler.ObtenerDatosContratoParaPagoAsync(pago.IdContrato.Value);
                if (datosContrato != null)
                {
                    ViewBag.DatosContrato = datosContrato;
                    ViewBag.InfoPagos = $"{datosContrato.PagosRealizados}/{datosContrato.TotalMeses}";
                }
            }
            
            return View(pago);
        }

        Console.WriteLine("Punto 6: ModelState válido, creando pago...");

        var resultado = await _repositorioAlquiler.CrearPagoAlquilerAsync(pago);
        Console.WriteLine($"Resultado crear pago: {resultado}");

        if (resultado)
        {
            // Actualizar el estado del contrato después del pago
            if (pago.IdContrato.HasValue)
            {
                await _repositorioAlquiler.ActualizarEstadoContratoAsync(pago.IdContrato.Value);
            }

            TempData["SuccessMessage"] = pago.DiasMora > 0
                ? $" Pago de alquiler #{pago.NumeroPago} registrado con mora de {pago.DiasMora} días (${pago.RecargoMora:N2})"
                : $" Pago de alquiler #{pago.NumeroPago} registrado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            ModelState.AddModelError("", 
                " Error al registrar el pago. " +
                "Verifique que el contrato esté vigente y los datos sean correctos.");
            return View(pago);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ERROR COMPLETO ===");
        Console.WriteLine($"Mensaje: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
        Console.WriteLine($"InnerException: {ex.InnerException?.Message}");

        ModelState.AddModelError("", 
            $" Error crítico al registrar pago de alquiler: {ex.Message}");
        return View(pago);
    }
}

// GET: Alquileres/Edit/5      
[Authorize(Policy = "Administrador")]
public async Task<IActionResult> Edit(int id)
{
    if (id <= 0) return NotFound();

    var pago = await _repositorioAlquiler.ObtenerPagoAlquilerConDetallesAsync(id);
    if (pago == null) return NotFound();

    return View(pago);
}

        // POST: Alquileres/Edit/5
[Authorize(Policy = "Administrador")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Pago pago, IFormFile? ComprobanteArchivo)
{
    if (id != pago.IdPago) return NotFound();

    // Validaciones básicas
    if (pago.MontoBase <= 0)
        ModelState.AddModelError("MontoBase", "El monto debe ser mayor a 0");

    if (pago.NumeroPago <= 0)
        ModelState.AddModelError("NumeroPago", "El número de pago debe ser mayor a 0");

    // Validar que no exista otro pago con el mismo número
    if (pago.IdContrato.HasValue && 
        await _repositorioAlquiler.ExistePagoMesContratoAsync(pago.IdContrato.Value, pago.NumeroPago, pago.IdPago))
    {
        ModelState.AddModelError("NumeroPago", "Ya existe otro pago con este número para el contrato");
    }

    if (!ModelState.IsValid)
        return View(pago);

    try
    {
        // Procesar comprobante si se subió uno nuevo
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
                return View(pago);
            }
        }

        var resultado = await _repositorioAlquiler.ActualizarPagoAlquilerAsync(pago);

        if (resultado)
        {
            TempData["SuccessMessage"] = "Pago actualizado correctamente";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            ModelState.AddModelError("", "Error al actualizar el pago");
            return View(pago);
        }
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", $"Error: {ex.Message}");
        return View(pago);
    }
}
      public async Task<IActionResult> Delete(int id)
{
    if (id <= 0)
    {
        return NotFound();
    }

    try
    {
        var pago = await _repositorioAlquiler.ObtenerPagoAlquilerConDetallesAsync(id);

        if (pago == null)
        {
            TempData["ErrorMessage"] = "Pago de alquiler no encontrado";
            return RedirectToAction(nameof(Index));
        }

        // Verificar si ya está anulado
        if (pago.Estado == "anulado")
        {
            TempData["WarningMessage"] = "Este pago ya se encuentra anulado";
            return RedirectToAction(nameof(Index));
        }

        // Cargar datos del contrato para mostrar información
        if (pago.IdContrato.HasValue)
        {
            var datosContrato = await _repositorioAlquiler.ObtenerDatosContratoParaPagoAsync(pago.IdContrato.Value);
            ViewBag.DatosContrato = datosContrato;
        }

        return View(pago);
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"Error al cargar pago de alquiler: {ex.Message}";
        return RedirectToAction(nameof(Index));
    }
}

// POST: Alquileres/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id, string motivoAnulacion = "", int idUsuarioAnulador = USUARIO_SISTEMA_TEMPORAL)
{
    try
    {
        // Verificar que el pago existe
        var pago = await _repositorioAlquiler.ObtenerPagoAlquilerPorIdAsync(id);
        if (pago == null)
        {
            TempData["ErrorMessage"] = "Pago no encontrado";
            return RedirectToAction(nameof(Index));
        }

        // Verificar que no esté ya anulado
        if (pago.Estado == "anulado")
        {
            TempData["WarningMessage"] = "El pago ya se encuentra anulado";
            return RedirectToAction(nameof(Index));
        }

        // Agregar motivo de anulación a observaciones
        if (!string.IsNullOrEmpty(motivoAnulacion))
        {
            pago.Observaciones = string.IsNullOrEmpty(pago.Observaciones) 
                ? $"ANULADO - Motivo: {motivoAnulacion}"
                : $"{pago.Observaciones} | ANULADO - Motivo: {motivoAnulacion}";
        }
        else
        {
            pago.Observaciones = string.IsNullOrEmpty(pago.Observaciones)
                ? "ANULADO - Sin motivo especificado"
                : $"{pago.Observaciones} | ANULADO";
        }

        var resultado = await _repositorioAlquiler.AnularPagoAlquilerAsync(id, idUsuarioAnulador);

        if (resultado)
        {
            TempData["SuccessMessage"] = $"Pago #{pago.NumeroPago} anulado exitosamente";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al anular el pago de alquiler";
        }
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"Error al anular pago de alquiler: {ex.Message}";
    }

    return RedirectToAction(nameof(Index));
}
        // GET: Alquileres/DetailsPago/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Usar el método que obtiene detalles completos
                var pago = await _repositorioAlquiler.ObtenerPagoAlquilerConDetallesAsync(id);
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener datos adicionales del contrato si existe
                if (pago.IdContrato.HasValue)
                {
                    var contratoDatos = await _repositorioAlquiler.ObtenerDatosContratoAsync(pago.IdContrato.Value);
                    ViewBag.ContratoDatos = contratoDatos;
                }

                return View(pago);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los detalles del pago: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Alquileres/Historial/5
public async Task<IActionResult> Historial(int contratoId)
{
    try
    {
        Console.WriteLine($"=== CARGANDO HISTORIAL PARA CONTRATO {contratoId} ===");

        // Verificar que el contrato existe y es válido
        var contratoExiste = await _repositorioAlquiler.ContratoVigenteAsync(contratoId);
        if (!contratoExiste)
        {
            TempData["ErrorMessage"] = "El contrato especificado no existe o no está vigente";
            return RedirectToAction(nameof(Index));
        }

        // Obtener el historial de pagos del contrato
        var historialPagos = await _repositorioAlquiler.ObtenerHistorialPagosContratoAsync(contratoId);
        Console.WriteLine($"Pagos encontrados: {historialPagos?.Count ?? 0}");

        // Obtener datos COMPLETOS del contrato (con infoPagos)
        var datosContrato = await _repositorioAlquiler.ObtenerDatosContratoParaPagoAsync(contratoId);
        Console.WriteLine($"Datos contrato: {datosContrato?.InfoPagos ?? "No disponible"}");

        // =============================================
        // CÁLCULO DE ESTADÍSTICAS MEJORADAS
        // =============================================
        var pagosPagados = historialPagos.Count(p => p.Estado?.ToLower() == "pagado");
        var pagosPendientes = historialPagos.Count(p => p.Estado?.ToLower() == "pendiente");
        var pagosAnulados = historialPagos.Count(p => p.Estado?.ToLower() == "anulado");
        var pagosConMora = historialPagos.Count(p => p.DiasMora > 0);
        var totalPagos = historialPagos.Count;
        
        // Calcular porcentajes (excluyendo anulados del total efectivo)
        var totalEfectivo = pagosPagados + pagosPendientes;
        var porcentajePagados = totalEfectivo > 0 ? (pagosPagados * 100.0 / totalEfectivo) : 0;
        var porcentajeConMora = totalPagos > 0 ? (pagosConMora * 100.0 / totalPagos) : 0;
        var porcentajeAnulados = totalPagos > 0 ? (pagosAnulados * 100.0 / totalPagos) : 0;

        var estadisticas = new
        {
            PagosPagados = pagosPagados,
            PagosPendientes = pagosPendientes,
            PagosAnulados = pagosAnulados,
            PagosConMora = pagosConMora,
            TotalPagos = totalPagos,
            TotalEfectivo = totalEfectivo,
            PorcentajePagados = Math.Round(porcentajePagados, 1),
            PorcentajeConMora = Math.Round(porcentajeConMora, 1),
            PorcentajeAnulados = Math.Round(porcentajeAnulados, 1),
            InfoPagos = datosContrato?.InfoPagos ?? "0/0",
            ProximoPago = datosContrato?.ProximoNumeroPago ?? 0,
            TotalMeses = datosContrato?.TotalMeses ?? 0,
            MontoTotalPagado = historialPagos.Where(p => p.Estado?.ToLower() == "pagado")
                                           .Sum(p => p.MontoTotal),
            MontoTotalMora = historialPagos.Sum(p => p.RecargoMora)
        };

        Console.WriteLine($"Estadísticas calculadas: {pagosPagados} pagados, {pagosPendientes} pendientes, {pagosAnulados} anulados");

        // =============================================
        //  PREPARAR DATOS PARA LA VISTA
        // =============================================
        ViewBag.ContratoDatos = datosContrato;
        ViewBag.ContratoId = contratoId;
        ViewBag.Estadisticas = estadisticas;
        ViewBag.EsVigente = contratoExiste;

        // Información para el modal o alertas
        if (datosContrato != null)
        {
            if (datosContrato.PagosRealizados >= datosContrato.TotalMeses)
            {
                ViewBag.MensajeEstado = "✅ CONTRATO COMPLETADO - Todos los pagos realizados";
                ViewBag.TipoMensaje = "success";
            }
            else if (pagosConMora > 0)
            {
                ViewBag.MensajeEstado = "⚠️ CONTRATO CON MORA - Hay pagos atrasados";
                ViewBag.TipoMensaje = "warning";
            }
            else
            {
                ViewBag.MensajeEstado = "📋 CONTRATO EN CURSO - Pagos al día";
                ViewBag.TipoMensaje = "info";
            }
        }

        return View(historialPagos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ERROR EN HISTORIAL: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        
        TempData["ErrorMessage"] = $"Error al cargar el historial de pagos: {ex.Message}";
        return RedirectToAction(nameof(Index));
    }
}

        // GET: Alquileres/MarcarComoPagado/5
[HttpPost]
public async Task<IActionResult> MarcarComoPagado(int idPago)
{
    try
    {
        var pago = await _repositorioAlquiler.ObtenerPagoAlquilerPorIdAsync(idPago);
        if (pago == null)
        {
            return Json(new { success = false, message = "Pago no encontrado" });
        }

        if (pago.Estado.ToLower() == "pagado")
        {
            return Json(new { success = false, message = "El pago ya está marcado como pagado" });
        }

        // Actualizar estado a pagado
        pago.Estado = "pagado";
        pago.FechaPago = DateTime.Now;

        var resultado = await _repositorioAlquiler.ActualizarPagoAlquilerAsync(pago);
        
        if (resultado)
        {
            return Json(new { success = true, message = "Pago marcado como pagado correctamente" });
        }
        else
        {
            return Json(new { success = false, message = "Error al actualizar el pago" });
        }
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = $"Error: {ex.Message}" });
    }
}

// GET: Alquileres/ExportarHistorial
public async Task<IActionResult> ExportarHistorial(int contratoId)
{
    try
    {
        var historialPagos = await _repositorioAlquiler.ObtenerHistorialPagosContratoAsync(contratoId);
        var contratoDatos = await _repositorioAlquiler.ObtenerDatosContratoAsync(contratoId);

        // lógica para exportar a Excel, PDF, etc.
        // Por ahora redirigimos al historial normal
        TempData["InfoMessage"] = "Función de exportación en desarrollo";
        return RedirectToAction("Historial", new { contratoId });
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"Error al exportar historial: {ex.Message}";
        return RedirectToAction("Historial", new { contratoId });
    }
}


        // ========================
        // ENDPOINTS AJAX PARA AUTOCOMPLETADO
        // ========================
        /// <summary>
        /// Busca contratos para crear pagos (específico para formulario de pagos)
        /// NO confundir con BuscarContratosVigentes que usa UsuariosController
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarContratosParaPago(string termino, int limite = 10)
        {
            try
            {
                Console.WriteLine("Entrando al buscarContratosParaPAgo ");
                var contratos = await _repositorioAlquiler.BuscarContratosParaPagoAsync(termino, limite);

                var resultados = contratos.Select(c => new
                {
                    idContrato = c.IdContrato,
                    idInmueble = c.IdInmueble,
                    texto = c.Texto,
                    montoFormateado = c.MontoFormateado,
                    inquilinoCompleto = c.InquilinoCompleto,
                    propietarioCompleto = c.PropietarioCompleto,
                    montoMensual = c.MontoMensual,
                    montoDiarioMora = c.MontoDiarioMora,
                    infoPagos = c.InfoPagos,
                    textoConPagos = c.TextoConPagos
                });

                return Json(resultados);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return Json(new List<object>());
            }
        }
        /// <summary>
        /// Obtiene datos completos del contrato para llenar formulario de pago ObtenerDatosContratoParaPago
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerDatosContratoParaPago(int idContrato)
        {
            try
            {
                Console.WriteLine("entrando a ObtenerDatosContratoParaPago ...");
                var datos = await _repositorioAlquiler.ObtenerDatosContratoParaPagoAsync(idContrato);

                if (datos == null)
                    return Json(new { success = false, error = "Contrato no encontrado" });

                return Json(new
                {
                    success = true,
                    idContrato = datos.IdContrato,
                    idInmueble = datos.IdInmueble,
                    inmuebleDireccion = datos.InmuebleDireccion,
                    inquilinoNombreCompleto = datos.InquilinoNombreCompleto,
                    propietarioNombreCompleto = datos.PropietarioNombreCompleto,
                    montoMensual = datos.MontoMensual,
                    montoDiarioMora = datos.MontoDiarioMora,
                    proximoNumeroPago = datos.ProximoNumeroPago,
                    proximaFechaVencimiento = datos.ProximaFechaVencimiento.ToString("yyyy-MM-dd"),
                    periodoPago = datos.PeriodoPago,
                    periodoAño = datos.PeriodoAño,
                    periodoMes = datos.PeriodoMes,
                    totalMeses = datos.TotalMeses,
                    infoPagos = datos.InfoPagos,
                    concepto = $"Alquiler {datos.PeriodoPago} - {datos.InmuebleDireccion}"
                });
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return Json(new { success = false, error = "Error al obtener datos del contrato" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> BuscarContratosVigentes(string termino, int limite = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                {
                    return Json(new List<object>());
                }

                var contratos = await _repositorioAlquiler.ObtenerContratosVigentesAsync(limite);

                var contratosFiltrados = contratos
                    .Where(c => c.Inmueble.Direccion.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                               c.Inquilino.Usuario.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                               c.Inquilino.Usuario.Apellido.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                               $"{c.Inquilino.Usuario.Nombre} {c.Inquilino.Usuario.Apellido}".Contains(termino, StringComparison.OrdinalIgnoreCase))
                    .Select(c => new
                    {
                        id = c.IdContrato,
                        texto = $"{c.Inmueble.Direccion} - {c.Inquilino.Usuario.Apellido}, {c.Inquilino.Usuario.Nombre}",
                        info = $"${c.MontoMensual:N2}/mes"
                    });

                return Json(contratosFiltrados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosContrato(int idContrato)
        {
            try
            {
                if (idContrato <= 0)
                {
                    return Json(new { error = "ID de contrato inválido" });
                }

                var contrato = await _repositorioAlquiler.ObtenerDatosContratoAsync(idContrato);

                if (contrato == null)
                {
                    return Json(new { error = "Contrato no encontrado" });
                }

                // Obtener próximo número de pago
                var proximoNumero = await _repositorioAlquiler.ObtenerProximoNumeroPagoAsync(idContrato);

                // Calcular fecha de vencimiento sugerida
                var fechaVencimiento = await _repositorioAlquiler.CalcularFechaVencimientoAsync(idContrato, proximoNumero);

                return Json(new
                {
                    success = true,
                    montoBase = contrato.MontoMensual,
                    proximoNumeroPago = proximoNumero,
                    fechaVencimiento = fechaVencimiento.ToString("yyyy-MM-dd"),
                    concepto = $"Alquiler #{proximoNumero:D2} - {contrato.Inmueble?.Direccion ?? "Inmueble"}"
                });
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

                var usuarios = await _repositorioAlquiler.ObtenerUsuariosActivosAsync(limite);

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
        // ENDPOINTS PARA ALERTAS Y REPORTES
        // ========================

        [HttpGet]
        public async Task<IActionResult> AlertasMora()
        {
            try
            {
                var pagosConMora = await _repositorioAlquiler.ObtenerPagosConMoraAsync(1);

                ViewBag.TotalPagosConMora = pagosConMora.Count;
                ViewBag.TotalMonteMora = pagosConMora.Sum(p => p.RecargoMora);

                return View(pagosConMora);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar alertas de mora: {ex.Message}";
                return View(new List<Pago>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ContratosProximosVencer(int dias = 30)
        {
            try
            {
                var contratos = await _repositorioAlquiler.ObtenerContratosProximosVencerAsync(dias);

                ViewBag.DiasAnticipacion = dias;
                ViewBag.TotalContratos = contratos.Count;

                return View(contratos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contratos próximos a vencer: {ex.Message}";
                return View(new List<Contrato>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResumenContrato(int idContrato)
        {
            try
            {
                if (idContrato <= 0)
                {
                    return NotFound();
                }

                var resumen = await _repositorioAlquiler.ObtenerResumenPagosPorContratoAsync(idContrato);
                var contrato = await _repositorioAlquiler.ObtenerDatosContratoAsync(idContrato);

                ViewBag.Contrato = contrato;
                return View(resumen);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar resumen del contrato: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========================
        // MÉTODOS PRIVADOS DE APOYO
        // ========================

        private async Task<(bool exito, string ruta, string nombre, string error)> ProcesarComprobanteAsync(IFormFile archivo)
        {
            try
            {
                var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    return (false, "", "", "Solo se permiten archivos PDF, JPG, JPEG o PNG");
                }

                if (archivo.Length > 5 * 1024 * 1024)
                {
                    return (false, "", "", "El archivo no puede superar 5MB");
                }

                // Usar _webHostEnvironment (tu convención)
                var rutaComprobantes = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "comprobantes");

                if (!Directory.Exists(rutaComprobantes))
                {
                    Directory.CreateDirectory(rutaComprobantes);
                }

                var nombreArchivo = $"comprobante_{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(rutaComprobantes, nombreArchivo);
                var rutaRelativa = $"/uploads/comprobantes/{nombreArchivo}";

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

                var rutaCompleta = Path.Combine(_webHostEnvironment.WebRootPath, rutaComprobante.TrimStart('/'));

                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar comprobante: {ex.Message}");
            }
        }

        // ========================
        // MÉTODO PARA DESCARGAR COMPROBANTE
        // ========================

        public async Task<IActionResult> DescargarComprobante(int id)
        {
            try
            {
                var pago = await _repositorioAlquiler.ObtenerPagoAlquilerPorIdAsync(id);

                if (pago == null || string.IsNullOrEmpty(pago.ComprobanteRuta))
                {
                    return NotFound();
                }

                var rutaCompleta = Path.Combine(_webHostEnvironment.WebRootPath, pago.ComprobanteRuta.TrimStart('/'));

                if (!System.IO.File.Exists(rutaCompleta))
                {
                    return NotFound();
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(rutaCompleta);
                var contentType = GetContentType(pago.ComprobanteRuta);
                var nombreDescarga = pago.ComprobanteNombre ?? $"comprobante_alquiler_{id}{Path.GetExtension(pago.ComprobanteRuta)}";

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

        // ========================
        // ACCIÓN PARA ACTUALIZAR MORA MANUALMENTE
        // ========================

        [HttpPost]
        public async Task<IActionResult> ActualizarMora(int idPago)
        {
            try
            {
                var resultado = await _repositorioAlquiler.ActualizarMoraAsync(idPago);

                if (resultado)
                {
                    return Json(new { success = true, message = "Mora actualizada correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al actualizar la mora" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Alquileres/Historial
        /* [HttpGet]
        public async Task<IActionResult> Historial(int contratoId)
        {
            try
            {
                // Obtener los pagos del contrato
                //var pagos = await _repositorioAlquiler.ObtenerPagosPorContratoAsync(contratoId);

                // Obtener los datos del contrato
                var datosContrato = await _repositorioAlquiler.ObtenerDatosContratoParaPagoAsync(contratoId);

                // Pasar los datos a la vista
                ViewBag.ContratoId = contratoId;
                ViewBag.ContratoDatos = datosContrato; 

                return View(pagos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Historial: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el historial de pagos";
                return RedirectToAction("Index");
            }
        } */
    }
}
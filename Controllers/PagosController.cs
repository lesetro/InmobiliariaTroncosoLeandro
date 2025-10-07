using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Services;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class PagosController : Controller
    {
        private readonly IRepositorioReporte _repositorioReporte;
        private readonly IRepositorioAlquiler _repositorioAlquiler;
        private readonly IRepositorioVenta _repositorioVenta;
        private readonly ISearchService _searchService;
        private const int ITEMS_POR_PAGINA = 10;

        public PagosController(IRepositorioReporte repositorioReporte,
                              IRepositorioAlquiler repositorioAlquiler,
                              IRepositorioVenta repositorioVenta,
                              ISearchService searchService)
        {
            _repositorioReporte = repositorioReporte;
            _repositorioAlquiler = repositorioAlquiler;
            _repositorioVenta = repositorioVenta;
            _searchService = searchService;
        }

        // GET: Pagos - Dashboard principal con datos reales
        public async Task<IActionResult> Index()
        {
            try
            {
                // Obtener datos reales de la base de datos usando repositorios
                var resumenGeneral = await _repositorioReporte.ObtenerResumenGeneralAsync();
                var ingresosMensuales = await _repositorioReporte.ObtenerIngresosPorMesAsync(3);
                var topInmuebles = await _repositorioReporte.ObtenerTopInmueblesAsync(3);

                // Obtener alertas reales
                var pagosConMora = await _repositorioReporte.ObtenerPagosConMoraAsync();
                var contratosVenciendo = await _repositorioReporte.ObtenerContratosProximosVencerAsync();

                // Preparar datos para la vista
                ViewBag.ResumenGeneral = resumenGeneral;
                ViewBag.IngresosMensuales = ingresosMensuales;
                ViewBag.TopInmuebles = topInmuebles;
                ViewBag.AlertasMora = pagosConMora.Count();
                ViewBag.AlertasContratos = contratosVenciendo.Count();
                ViewBag.TotalAlertas = pagosConMora.Count() + contratosVenciendo.Count();

                // Datos adicionales para el dashboard
                ViewBag.PagosConMora = pagosConMora;
                ViewBag.ContratosVenciendo = contratosVenciendo;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el dashboard de pagos: {ex.Message}";
                return View();
            }
        }

        // GET: Pagos/Crear - Selector de tipo de pago
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Pagos/Crear - Redirige al controlador específico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(string tipoPago)
        {
            if (string.IsNullOrWhiteSpace(tipoPago))
            {
                TempData["ErrorMessage"] = "Debe seleccionar un tipo de pago.";
                return View();
            }

            return tipoPago.ToLower() switch
            {
                "alquiler" => RedirectToAction("Create", "Alquileres"),
                "venta" => RedirectToAction("Create", "Ventas"),
                "seña" => RedirectToAction("Create", "Ventas", new { tipo = "seña" }),
                "reserva" => RedirectToAction("Create", "Ventas", new { tipo = "reserva" }),
                _ => RedirectToAction(nameof(Crear))
            };
        }

        // GET: Pagos/Alertas - Vista de alertas con datos reales
        public async Task<IActionResult> Alertas()
        {
            try
            {
                // Obtener alertas reales desde el repositorio
                var pagosConMora = await _repositorioReporte.ObtenerPagosConMoraAsync();
                var contratosVenciendo = await _repositorioReporte.ObtenerContratosProximosVencerAsync();

                var alertas = new
                {
                    PagosConMora = pagosConMora,
                    ContratosProximosVencer = contratosVenciendo,
                    TotalAlertas = pagosConMora.Count() + contratosVenciendo.Count()
                };

                ViewBag.PagosConMora = pagosConMora;
                ViewBag.ContratosVenciendo = contratosVenciendo;
                ViewBag.TotalAlertas = alertas.TotalAlertas;

                return View(alertas);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar alertas: {ex.Message}";
                return View();
            }
        }

        // ========================
        // ENDPOINTS AJAX PARA BÚSQUEDA
        // ========================

        [HttpGet]
        public async Task<IActionResult> BuscarPagos(string termino, int limite = 15)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                {
                    return Json(new List<object>());
                }

                // Buscar en contratos (para alquileres)
                var contratos = await _searchService.BuscarContratosAsync(termino, limite / 2);
                var inmuebles = await _searchService.BuscarInmueblesAsync(termino, limite / 2);

                var resultados = new List<object>();

                // Agregar contratos como potenciales pagos de alquiler
                foreach (var contrato in contratos)
                {
                    resultados.Add(new
                    {
                        id = contrato.Id,
                        tipo = "alquiler",
                        texto = contrato.Texto,
                        info = "Contrato vigente",
                        controller = "Alquileres",
                        icono = "bi-house-door"
                    });
                }

                // Agregar inmuebles como potenciales pagos de venta
                foreach (var inmueble in inmuebles)
                {
                    resultados.Add(new
                    {
                        id = inmueble.Id,
                        tipo = "venta",
                        texto = inmueble.Texto,
                        info = "Inmueble disponible",
                        controller = "Ventas",
                        icono = "bi-currency-dollar"
                    });
                }

                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EstadisticasRapidas()
        {
            try
            {
                // Usar datos reales del repositorio de reportes
                var resumenGeneral = await _repositorioReporte.ObtenerResumenGeneralAsync();
                var estadoInmuebles = await _repositorioReporte.ObtenerEstadoInmueblesAsync();

                return Json(new
                {
                    resumen = resumenGeneral,
                    estados_inmuebles = estadoInmuebles,
                    ultima_actualizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ========================
        // NAVEGACIÓN RÁPIDA
        // ========================

        public IActionResult IrAAlquileres()
        {
            return RedirectToAction("Index", "Alquileres");
        }

        public IActionResult IrAVentas()
        {
            return RedirectToAction("Index", "Ventas");
        }

        public IActionResult IrAReportes()
        {
            return RedirectToAction("Index", "Reportes");
        }

        public IActionResult CrearPagoAlquiler()
        {
            return RedirectToAction("Create", "Alquileres");
        }

        public IActionResult CrearPagoVenta()
        {
            return RedirectToAction("Create", "Ventas");
        }

        // ========================
        // FILTROS RÁPIDOS
        // ========================

        public IActionResult PagosAlquiler()
        {
            return RedirectToAction("Index", "Alquileres");
        }

        public IActionResult PagosVenta()
        {
            return RedirectToAction("Index", "Ventas");
        }

        public IActionResult PagosConMora()
        {
            return RedirectToAction("AlertasMora", "Alquileres");
        }

        public IActionResult ContratosVenciendo()
        {
            return RedirectToAction("ContratosProximosVencer", "Contratos");
        }

        // ========================
        // UTILIDADES
        // ========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarVistaPreferida(string vista, string returnUrl)
        {
            // Guardar preferencia en sesión
            HttpContext.Session.SetString("VistaPreferidaPagos", vista);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportarDatos(string formato = "csv")
        {
            try
            {
                if (formato.ToLower() == "csv")
                {
                    // Obtener datos reales de ambos tipos de pagos
                    var (pagosAlquiler, _) = await _repositorioAlquiler.ObtenerPagosAlquilerConPaginacionAsync(1, "", "", 1000);
                    var (pagosVenta, _) = await _repositorioVenta.ObtenerPagosVentaConPaginacionAsync(1, "", "", 1000);

                    var contenido = "ID,Tipo,Concepto,Monto,Fecha,Estado,Inmueble\n";

                    // Agregar pagos de alquiler
                    foreach (var pago in pagosAlquiler)
                    {
                        contenido += $"\"{pago.IdPago}\",\"alquiler\",\"{pago.Concepto}\",\"{pago.MontoTotal}\",\"{pago.FechaPago:yyyy-MM-dd}\",\"{pago.Estado}\",\"{pago.Inmueble?.Direccion ?? ""}\"\n";
                    }

                    // Agregar pagos de venta
                    foreach (var pago in pagosVenta)
                    {
                        contenido += $"\"{pago.IdPago}\",\"venta\",\"{pago.Concepto}\",\"{pago.MontoTotal}\",\"{pago.FechaPago:yyyy-MM-dd}\",\"{pago.Estado}\",\"{pago.Inmueble?.Direccion ?? ""}\"\n";
                    }

                    var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
                    return File(bytes, "text/csv", $"pagos_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                }

                TempData["ErrorMessage"] = "Formato no soportado. Use CSV.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========================
        // CONFIGURACIÓN
        // ========================

        public IActionResult Configuracion()
        {
            var config = new
            {
                ItemsPorPaginaDefault = ITEMS_POR_PAGINA,
                TiposComprobantePermitidos = new[] { ".pdf", ".jpg", ".jpeg", ".png" },
                TamañoMaximoArchivo = "5MB",
                DiasAlertaVencimiento = 30,
                CalculoMoraAutomatico = true
            };

            ViewBag.Configuracion = config;
            return View();
        }

        // ========================
        // MÉTODOS DE APOYO
        // ========================

        private static string ExtraerDireccionDeTexto(string texto)
        {
            var partes = texto.Split(" - ");
            return partes.Length > 0 ? partes[0].Trim() : texto.Trim();
        }

        private static string DeterminarTipoDesdeSearch(SearchResult searchResult)
        {
            var tipo = searchResult.Tipo.ToLower();
            var texto = searchResult.Texto.ToLower();

            if (tipo.Contains("vigente") || tipo.Contains("contrato") || texto.Contains("alquiler"))
                return "alquiler";

            if (tipo.Contains("vendido") || tipo.Contains("venta") || texto.Contains("venta"))
                return "venta";

            return "desconocido";
        }


        // GET: Pagos/Ejecutivo - Dashboard ejecutivo específico
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Usar métodos existentes del repositorio de reportes
                var resumenGeneral = await _repositorioReporte.ObtenerResumenGeneralAsync();
                var estadoInmuebles = await _repositorioReporte.ObtenerEstadoInmueblesAsync();

                // Obtener últimos pagos usando métodos existentes de paginación
                var (ultimosPagosAlquiler, _) = await _repositorioAlquiler.ObtenerPagosAlquilerConPaginacionAsync(
                    pagina: 1, buscar: "", estado: "pagado", itemsPorPagina: 10);

                var (ultimosPagosVenta, _) = await _repositorioVenta.ObtenerPagosVentaConPaginacionAsync(
                    pagina: 1, buscar: "", estado: "pagado", itemsPorPagina: 10);

                // Preparar datos para la vista
                ViewBag.ResumenGeneral = resumenGeneral;
                ViewBag.EstadoInmuebles = estadoInmuebles;
                ViewBag.UltimosPagosAlquiler = ultimosPagosAlquiler;
                ViewBag.UltimosPagosVenta = ultimosPagosVenta;

                // Estadísticas para usar en la vista
                ViewBag.Estadisticas = new
                {
                    TotalPagos = ObtenerPropiedadSegura(resumenGeneral, "TotalPagos", 0),
                    PagosAlquiler = ObtenerPropiedadSegura(resumenGeneral, "PagosAlquiler", 0),
                    PagosVenta = ObtenerPropiedadSegura(resumenGeneral, "PagosVenta", 0),
                    MontoTotal = ObtenerPropiedadSegura(resumenGeneral, "MontoTotal", 0m)
                };

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el dashboard ejecutivo: {ex.Message}";
                return View();
            }
        }

        // Método auxiliar para obtener propiedades de objetos dinámicos de forma segura
        private T ObtenerPropiedadSegura<T>(object obj, string propiedad, T valorPorDefecto)
        {
            try
            {
                if (obj == null) return valorPorDefecto;

                var prop = obj.GetType().GetProperty(propiedad);
                if (prop != null && prop.GetValue(obj) is T valor)
                {
                    return valor;
                }
                return valorPorDefecto;
            }
            catch
            {
                return valorPorDefecto;
            }
        }
    }
}
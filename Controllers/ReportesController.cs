using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class ReportesController : Controller
    {
        private readonly IRepositorioReporte _repositorioReporte;

        public ReportesController(IRepositorioReporte repositorioReporte)
        {
            _repositorioReporte = repositorioReporte;
        }

        // GET: Reportes - Dashboard principal
        public async Task<IActionResult> Index()
        {
            try
            {
                // Obtener datos para el dashboard
                var resumenGeneral = await _repositorioReporte.ObtenerResumenGeneralAsync();
                var ingresosPorMes = await _repositorioReporte.ObtenerIngresosPorMesAsync(6);
                var topInmuebles = await _repositorioReporte.ObtenerTopInmueblesAsync(5);
                var estadoInmuebles = await _repositorioReporte.ObtenerEstadoInmueblesAsync();

                // Preparar ViewBag con los datos
                ViewBag.ResumenGeneral = resumenGeneral;
                ViewBag.IngresosPorMes = ingresosPorMes;
                ViewBag.TopInmuebles = topInmuebles;
                ViewBag.EstadoInmuebles = estadoInmuebles;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar reportes: {ex.Message}";
                return View();
            }
        }

        // GET: Reportes/Ingresos
        public async Task<IActionResult> Ingresos(int meses = 6)
        {
            try
            {
                var ingresosPorMes = await _repositorioReporte.ObtenerIngresosPorMesAsync(meses);
                
                ViewBag.Meses = meses;
                ViewBag.TotalMeses = ingresosPorMes.Count();
                
                return View(ingresosPorMes);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar reporte de ingresos: {ex.Message}";
                return View(new List<object>());
            }
        }

        // GET: Reportes/TopInmuebles
        public async Task<IActionResult> TopInmuebles(int limite = 10)
        {
            try
            {
                var topInmuebles = await _repositorioReporte.ObtenerTopInmueblesAsync(limite);
                
                ViewBag.Limite = limite;
                
                return View(topInmuebles);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar top inmuebles: {ex.Message}";
                return View(new List<object>());
            }
        }

        // GET: Reportes/Alertas
        public async Task<IActionResult> Alertas()
        {
            try
            {
                var pagosConMora = await _repositorioReporte.ObtenerPagosConMoraAsync();
                var contratosVenciendo = await _repositorioReporte.ObtenerContratosProximosVencerAsync();

                ViewBag.PagosConMora = pagosConMora;
                ViewBag.ContratosVenciendo = contratosVenciendo;
                ViewBag.TotalAlertas = pagosConMora.Count() + contratosVenciendo.Count();

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar alertas: {ex.Message}";
                return View();
            }
        }

        // GET: Reportes/EstadoInmuebles
        public async Task<IActionResult> EstadoInmuebles()
        {
            try
            {
                var estadoInmuebles = await _repositorioReporte.ObtenerEstadoInmueblesAsync();
                
                return View(estadoInmuebles);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar estado de inmuebles: {ex.Message}";
                return View(new List<object>());
            }
        }

        // ========================
        // ENDPOINTS AJAX PARA GRÁFICOS
        // ========================

        [HttpGet]
        public async Task<IActionResult> DatosGraficoIngresos(int meses = 6)
        {
            try
            {
                var ingresos = await _repositorioReporte.ObtenerIngresosPorMesAsync(meses);
                
                var datosGrafico = ingresos.Select(i => new
                {
                    mes = GetPropertyValue(i, "NombreMes"),
                    ingresos = GetPropertyValue(i, "TotalIngresos"),
                    pagos = GetPropertyValue(i, "CantidadPagos")
                }).ToList();

                return Json(datosGrafico);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DatosGraficoEstados()
        {
            try
            {
                var estados = await _repositorioReporte.ObtenerEstadoInmueblesAsync();
                
                if (estados is IEnumerable<object> listaEstados)
                {
                    var datosGrafico = listaEstados.Select(e => new
                    {
                        estado = GetPropertyValue(e, "Estado"),
                        cantidad = GetPropertyValue(e, "Cantidad")
                    }).ToList();

                    return Json(datosGrafico);
                }

                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResumenRapido()
        {
            try
            {
                var resumen = await _repositorioReporte.ObtenerResumenGeneralAsync();
                return Json(resumen);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ========================
        // MÉTODOS AUXILIARES
        // ========================

        private static object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj) ?? "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        // ========================
        // MÉTODOS DE NAVEGACIÓN
        // ========================

        public IActionResult IrAPagos()
        {
            return RedirectToAction("Index", "Pagos");
        }

        public IActionResult IrAAlquileres()
        {
            return RedirectToAction("Index", "Alquileres");
        }

        public IActionResult IrAVentas()
        {
            return RedirectToAction("Index", "Ventas");
        }

        public IActionResult IrAContratos()
        {
            return RedirectToAction("Index", "Contratos");
        }
    }
}
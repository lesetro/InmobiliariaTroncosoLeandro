
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using System.Security.Claims;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")] 
    public class AdminController : Controller
    {
        private readonly IRepositorioAdmin _repositorioAdmin;

        public AdminController(IRepositorioAdmin repositorioAdmin)
        {
            _repositorioAdmin = repositorioAdmin;
        }

        // GET: Admin/Index - Dashboard principal del administrador
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = await _repositorioAdmin.GetDashboardDataAsync();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el dashboard: {ex.Message}";
                return View(new AdminDashboardDto());
            }
        }

        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Estadisticas()
        {
            try
            {
                var estadisticas = new
                {
                    UsuariosPorRol = await _repositorioAdmin.GetUsuariosPorRolAsync(),
                    TotalUsuarios = await _repositorioAdmin.GetTotalUsuariosAsync(),
                    PropietariosActivos = await _repositorioAdmin.GetTotalPropietariosActivosAsync(),
                    InquilinosActivos = await _repositorioAdmin.GetTotalInquilinosActivosAsync(),
                    InmueblesTotal = await _repositorioAdmin.GetTotalInmueblesAsync(),
                    InmueblesDisponibles = await _repositorioAdmin.GetInmueblesDisponiblesAsync(),
                    ContratosTotal = await _repositorioAdmin.GetTotalContratosAsync(),
                    ContratosVigentes = await _repositorioAdmin.GetContratosVigentesAsync(),
                    InteresesTotal = await _repositorioAdmin.GetTotalInteresesAsync(),
                    InteresesPendientes = await _repositorioAdmin.GetInteresesPendientesAsync()
                };

                ViewBag.Estadisticas = estadisticas;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar estadísticas: {ex.Message}";
                return View();
            }
        }
        [Authorize(Policy = "Administrador")]
        // GET: Admin/Configuracion - Configuración del sistema
        public IActionResult Configuracion()
        {
            var configuracion = new
            {
                SistemaInfo = new
                {
                    Version = "1.0.0",
                    Nombre = "Sistema Inmobiliario CIMA",
                    Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                },
                // AGREGAR ESTAS PROPIEDADES QUE FALTAN:
                Paginacion = new
                {
                    ItemsPorPaginaDefault = 10,
                    MaximoItemsPorPagina = 50
                },
                Archivos = new
                {
                    TamañoMaximoMB = 5,
                    FormatosPermitidos = new[] { ".pdf", ".jpg", ".jpeg", ".png" },
                    RutaAlmacenamiento = "/uploads"
                },
                Contratos = new
                {
                    DiasAlertaVencimiento = 30,
                    DuracionMinimaMeses = 6,
                    RenovacionAutomatica = true
                },
                Pagos = new
                {
                    TasaMoraMensual = 2.5m,
                    DiasGracia = 5,
                    CalculoMoraAutomatico = true
                },
                Usuarios = new
                {
                    TiempoSesionHoras = 8,
                    PasswordTemporal = "PasswordTemporal123",
                    ForzarCambioPassword = true,
                    PermitirRegistroPublico = false,
                    RequiereActivacionEmail = false
                },
                Email = new
                {
                    ServidorSmtp = "smtp.gmail.com",
                    Puerto = 587,
                    EmailEmpresa = "inmobiliaria@troncoso.com",
                    EnviarNotificaciones = true
                }
            };

            ViewBag.Configuracion = configuracion;
            return View();
        }

        // GET: Admin/GestionRapida - Accesos rápidos a gestión
        public IActionResult GestionRapida()
        {
            var opcionesGestion = new[]
            {
                new { Titulo = "Usuarios", Controlador = "Usuario", Accion = "Index", Icono = "bi-people", Descripcion = "Gestionar usuarios del sistema" },
                new { Titulo = "Propietarios", Controlador = "Propietarios", Accion = "Index", Icono = "bi-person-badge", Descripcion = "Administrar propietarios" },
                new { Titulo = "Inquilinos", Controlador = "Inquilinos", Accion = "Index", Icono = "bi-people-fill", Descripcion = "Administrar inquilinos" },
                new { Titulo = "Inmuebles", Controlador = "Inmuebles", Accion = "Index", Icono = "bi-building", Descripcion = "Gestionar propiedades" },
                new { Titulo = "Contratos", Controlador = "Contratos", Accion = "Index", Icono = "bi-file-earmark-text", Descripcion = "Administrar contratos" },
                new { Titulo = "Pagos", Controlador = "Pagos", Accion = "Index", Icono = "bi-currency-dollar", Descripcion = "Gestionar pagos" },
                new { Titulo = "Intereses", Controlador = "InteresInmueble", Accion = "Index", Icono = "bi-envelope-heart", Descripcion = "Gestionar contactos de interés" },
                new { Titulo = "Tipos Inmuebles", Controlador = "TipoInmueble", Accion = "Index", Icono = "bi-tags", Descripcion = "Configurar tipos de propiedades" },
                new { Titulo = "Reportes", Controlador = "Admin", Accion = "Reportes", Icono = "bi-graph-up", Descripcion = "Ver reportes del sistema" }
            };

            ViewBag.OpcionesGestion = opcionesGestion;
            return View();
        }
        [Authorize(Policy = "Administrador")]
        // GET: Admin/Reportes - Vista de reportes
        public async Task<IActionResult> Reportes()
        {
            try
            {
                var reporteData = await _repositorioAdmin.GetReporteGeneralAsync();
                ViewBag.ReporteData = reporteData;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reportes: {ex.Message}";
                return View();
            }
        }
        
        // GET: Admin/SistemaInfo - Información del sistema
        public IActionResult SistemaInfo()
        {
            var sistemaInfo = new
            {
                Aplicacion = new
                {
                    Nombre = "Sistema Inmobiliario CIMA",
                    Version = "1.0.0",
                    Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    FechaCompilacion = DateTime.Now.ToString("yyyy-MM-dd"),
                    Framework = ".NET 9.0"
                },
                Servidor = new
                {
                    Sistema = Environment.OSVersion.ToString(),
                    Procesador = Environment.ProcessorCount + " cores",
                    MemoriaTotal = GC.GetTotalMemory(false) / (1024 * 1024) + " MB",
                    TiempoActividad = Environment.TickCount64 / (1000 * 60) + " minutos"
                },
                Usuario = new
                {
                    Nombre = User.FindFirst("FullName")?.Value ?? "N/A",
                    Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "N/A",
                    Rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "N/A",
                    IdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A"
                }
            };

            ViewBag.SistemaInfo = sistemaInfo;
            return View();
        }

        // POST: Admin/LimpiarCache - Limpiar cache del sistema
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LimpiarCache()
        {
            try
            {
                // Aquí puedes agregar lógica para limpiar cache si la implementas
                GC.Collect();
                TempData["SuccessMessage"] = "Cache del sistema limpiado exitosamente";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al limpiar cache: {ex.Message}";
            }

            return RedirectToAction(nameof(Configuracion));
        }

        // GET: Admin/Alertas - Vista de alertas del sistema
        public async Task<IActionResult> Alertas()
        {
            try
            {
                var alertas = new
                {
                    ContratosProximosVencer = await _repositorioAdmin.GetContratosProximosVencerAsync(30),
                    PagosVencidos = await _repositorioAdmin.GetPagosVencidosAsync(),
                    InteresesPendientes = await _repositorioAdmin.GetInteresesPendientesAsync(),
                    AlertasDelSistema = await _repositorioAdmin.GetAlertasDelSistemaAsync()
                };

                ViewBag.Alertas = alertas;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar alertas: {ex.Message}";
                return View();
            }
        }

        // GET: Admin/Metricas - Vista de métricas detalladas
        public async Task<IActionResult> Metricas()
        {
            try
            {
                var añoActual = DateTime.Now.Year;
                var metricas = new
                {
                    InteresesPorMes = await _repositorioAdmin.GetInteresesPorMesAsync(añoActual),
                    ContratosPorMes = await _repositorioAdmin.GetContratosPorMesAsync(añoActual),
                    IngresosPorMes = await _repositorioAdmin.GetIngresosPorMesAsync(añoActual),
                    EstadisticasComparativas = await _repositorioAdmin.GetEstadisticasComparativasAsync()
                };

                ViewBag.Metricas = metricas;
                ViewBag.AñoActual = añoActual;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar métricas: {ex.Message}";
                return View();
            }
        }

        // API Endpoints para AJAX

        // GET: Admin/Api/DashboardData - Datos del dashboard en JSON
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var data = await _repositorioAdmin.GetDashboardDataAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Admin/Api/Estadisticas - Estadísticas rápidas
        [HttpGet]
        public async Task<IActionResult> GetEstadisticasRapidas()
        {
            try
            {
                var stats = new
                {
                    TotalUsuarios = await _repositorioAdmin.GetTotalUsuariosAsync(),
                    TotalContratos = await _repositorioAdmin.GetTotalContratosAsync(),
                    InteresesPendientes = await _repositorioAdmin.GetInteresesPendientesAsync(),
                    ContratosVigentes = await _repositorioAdmin.GetContratosVigentesAsync()
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Admin/Api/ContratosRecientes - Últimos contratos
        [HttpGet]
        public async Task<IActionResult> GetContratosRecientes(int limite = 10)
        {
            try
            {
                var contratos = await _repositorioAdmin.GetContratosRecientesAsync(limite);
                return Json(contratos);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Admin/Api/InteresesRecientes - Últimos intereses
        [HttpGet]
        public async Task<IActionResult> GetInteresesRecientes(int limite = 10)
        {
            try
            {
                var intereses = await _repositorioAdmin.GetInteresesRecientesAsync(limite);
                return Json(intereses);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        

    }
}
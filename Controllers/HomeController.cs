using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using System.Diagnostics;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [AllowAnonymous] // Todo el controlador es público
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioInteresInmueble _repositorioIntereses;
        private readonly IRepositorioContacto _repositorioContacto;

        public HomeController(
            ILogger<HomeController> logger,
            IRepositorioInmueble repositorioInmueble,
            IRepositorioInteresInmueble repositorioIntereses,
            IRepositorioContacto repositorioContacto)
        {
            _logger = logger;
            _repositorioInmueble = repositorioInmueble;
            _repositorioIntereses = repositorioIntereses;
            _repositorioContacto = repositorioContacto;
        }

        // GET: Home/Index - Página principal pública
        public async Task<IActionResult> Index()
        {
            try
            {
                // Obtener estadísticas públicas REALES
                var (inmuebles, totalRegistros) = await _repositorioInmueble.ObtenerConPaginacionYBusquedaAsync(1, "", "disponible", 50);
                var inmueblesDisponibles = inmuebles.Count;
                var estadisticasIntereses = await _repositorioIntereses.ObtenerEstadisticasInteresesAsync();

                // Estadísticas para mostrar en landing page
                ViewBag.TotalPropiedades = totalRegistros;
                ViewBag.PropiedadesDisponibles = inmueblesDisponibles;
                ViewBag.ClientesInteresados = estadisticasIntereses.GetValueOrDefault("Total", 0);
                ViewBag.AñosExperiencia = 15;

                // Propiedades destacadas (las más recientes disponibles)
                var propiedadesDestacadas = inmuebles
                    .Where(i => i.Estado == "disponible")
                    .OrderByDescending(i => i.FechaAlta)
                    .Take(6)
                    .ToList();

                ViewBag.PropiedadesDestacadas = propiedadesDestacadas;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la página principal");
                
                // Valores por defecto en caso de error
                ViewBag.TotalPropiedades = 0;
                ViewBag.PropiedadesDisponibles = 0;
                ViewBag.ClientesInteresados = 0;
                ViewBag.AñosExperiencia = 15;
                ViewBag.PropiedadesDestacadas = new List<Inmueble>();
                
                return View();
            }
        }

        // GET: Home/Catalogo - Catálogo público de inmuebles
        public async Task<IActionResult> Catalogo(string buscar = "", int pagina = 1)
        {
            try
            {
                const int itemsPorPagina = 12;

                // Usar el método existente con filtro para solo inmuebles disponibles
                var (inmuebles, totalRegistros) = await _repositorioInmueble.ObtenerConPaginacionYBusquedaAsync(
                    pagina: pagina,
                    buscar: buscar ?? "",
                    estado: "Disponible",
                    itemsPorPagina: itemsPorPagina
                );

                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);

                ViewBag.FiltroBuscar = buscar;
                ViewBag.TotalResultados = totalRegistros;
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;

                // Obtener tipos de inmueble para filtros públicos
                var tiposInmueble = await _repositorioInmueble.ObtenerTiposInmuebleActivosAsync();
                ViewBag.TiposInmueble = tiposInmueble;

                return View(inmuebles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el catálogo");
                TempData["Error"] = "Error al cargar las propiedades";
                return View(new List<Inmueble>());
            }
        }

        // GET: Home/DetallePropiedad/{id} - Ver detalles de una propiedad específica
        public async Task<IActionResult> DetallePropiedad(int id)
        {
            try
            {
                // Usar método EXISTENTE con detalles completos
                var propiedad = await _repositorioInmueble.ObtenerInmuebleConDetallesAsync(id);
                if (propiedad == null || propiedad.Estado != "disponible")
                {
                    TempData["Error"] = "Propiedad no encontrada o no disponible";
                    return RedirectToAction("Catalogo");
                }

                // Obtener propiedades similares del mismo tipo (disponibles)
                var (propiedadesSimilares, _) = await _repositorioInmueble.ObtenerConPaginacionYBusquedaAsync(1, "", "disponible", 10);
                var propiedadesRelacionadas = propiedadesSimilares
                    .Where(p => p.IdInmueble != id && p.IdTipoInmueble == propiedad.IdTipoInmueble)
                    .Take(3)
                    .ToList();

                ViewBag.PropiedadesRelacionadas = propiedadesRelacionadas;

                return View(propiedad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles de la propiedad {Id}", id);
                TempData["Error"] = "Error al cargar los detalles de la propiedad";
                return RedirectToAction("Catalogo");
            }
        }

        // POST: Home/ExpresarInteres - Expresar interés en una propiedad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpresarInteres(int propiedadId, string nombre, string email, string telefono, string mensaje)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Nombre y email son obligatorios";
                    return RedirectToAction("DetallePropiedad", new { id = propiedadId });
                }

                // Crear objeto de interés según el modelo
                var interes = new InteresInmueble
                {
                    IdInmueble = propiedadId,
                    Nombre = nombre.Trim(),
                    Email = email.Trim(),
                    Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(mensaje) ? null : mensaje.Trim(),
                    Fecha = DateTime.Now,
                    Contactado = false,
                    FechaContacto = null
                };

                // Usar el método del repositorio (necesita implementarse)
                 await _repositorioIntereses.CrearInteresAsync(interes);
                
                _logger.LogInformation($"Nueva consulta de interés: {nombre} ({email}) - Propiedad {propiedadId}");

                TempData["Success"] = "¡Gracias por tu interés! Nos pondremos en contacto contigo pronto.";
                return RedirectToAction("DetallePropiedad", new { id = propiedadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar expresión de interés");
                TempData["Error"] = "Ocurrió un error al enviar tu consulta. Inténtalo nuevamente.";
                return RedirectToAction("DetallePropiedad", new { id = propiedadId });
            }
        }

        // GET: Home/SobreNosotros - Información de la empresa
        public IActionResult SobreNosotros()
        {
            // Información pública de la empresa
            var equipoTrabajo = new List<dynamic>
            {
                new {
                    Nombre = "Leandro Troncoso",
                    Cargo = "Director General",
                    Experiencia = "15 años",
                    Email = "leandro@inmobiliaria.com",
                    Foto = "/images/equipo/leandro.jpg",
                    Descripcion = "Fundador de la empresa con amplia experiencia en el sector inmobiliario de Villa Mercedes."
                },
                new {
                    Nombre = "María González",
                    Cargo = "Gerente de Ventas",
                    Experiencia = "8 años",
                    Email = "maria@inmobiliaria.com",
                    Foto = "/images/equipo/maria.jpg",
                    Descripcion = "Especialista en ventas y atención al cliente con gran trayectoria en el sector."
                },
                new {
                    Nombre = "Carlos Mendoza",
                    Cargo = "Asesor Inmobiliario",
                    Experiencia = "5 años",
                    Email = "carlos@inmobiliaria.com",
                    Foto = "/images/equipo/carlos.jpg",
                    Descripcion = "Experto en tasaciones y asesoramiento jurídico inmobiliario."
                }
            };

            ViewBag.EquipoTrabajo = equipoTrabajo;
            return View();
        }

        // GET: Home/Contacto - Formulario de contacto
        public IActionResult Contacto()
        {
            return View();
        }

        // POST: Home/Contacto - Procesar formulario de contacto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contacto(string nombre, string email, string telefono, string asunto, string mensaje)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(mensaje))
                {
                    TempData["Error"] = "Nombre, email y mensaje son obligatorios";
                    return View();
                }

                // Crear contacto usando el repositorio REAL
                var contacto = new Contacto
                {
                    Nombre = nombre.Trim(),
                    Email = email.Trim(),
                    Telefono = telefono?.Trim(),
                    Asunto = asunto?.Trim() ?? "Consulta general",
                    Mensaje = mensaje.Trim(),
                    //Fecha = DateTime.Now,
                    Estado = "Pendiente"
                };

                var resultado = await _repositorioContacto.CrearContactoAsync(contacto);
                if (resultado)
                {
                    _logger.LogInformation($"Nuevo contacto registrado: {nombre} ({email})");
                    TempData["Success"] = "¡Mensaje enviado correctamente! Nos pondremos en contacto contigo pronto.";
                }
                else
                {
                    TempData["Error"] = "Ocurrió un error al enviar el mensaje. Inténtalo nuevamente.";
                }

                return RedirectToAction("Contacto");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar formulario de contacto");
                TempData["Error"] = "Ocurrió un error al enviar el mensaje. Inténtalo nuevamente.";
                return View();
            }
        }

        // GET: Home/Servicios - Servicios que ofrece la inmobiliaria
        public IActionResult Servicios()
        {
            var servicios = new List<dynamic>
            {
                new {
                    Titulo = "Alquiler de Propiedades",
                    Descripcion = "Gestionamos el alquiler de tu propiedad de manera integral con garantías",
                    Icono = "fas fa-key",
                    Color = "primary",
                    Caracteristicas = new[] { "Búsqueda de inquilinos", "Gestión de contratos", "Cobro de alquileres", "Mantenimiento preventivo" }
                },
                new {
                    Titulo = "Venta de Inmuebles",
                    Descripcion = "Te ayudamos a vender tu propiedad al mejor precio del mercado actual",
                    Icono = "fas fa-home",
                    Color = "success",
                    Caracteristicas = new[] { "Tasación gratuita", "Marketing digital", "Asesoramiento legal", "Negociación profesional" }
                },
                new {
                    Titulo = "Administración",
                    Descripcion = "Administramos consorcios y propiedades de manera profesional y transparente",
                    Icono = "fas fa-building",
                    Color = "info",
                    Caracteristicas = new[] { "Gestión financiera", "Mantenimiento integral", "Reuniones organizadas", "Informes mensuales" }
                },
                new {
                    Titulo = "Asesoramiento",
                    Descripcion = "Brindamos consultoría experta en todos los aspectos del mercado inmobiliario",
                    Icono = "fas fa-handshake",
                    Color = "warning",
                    Caracteristicas = new[] { "Consultoría legal", "Análisis de mercado", "Oportunidades de inversión", "Financiamiento" }
                }
            };

            ViewBag.Servicios = servicios;
            return View();
        }

        // GET: Home/Privacy - Política de privacidad
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Error - Página de error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
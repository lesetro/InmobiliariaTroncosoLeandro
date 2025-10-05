using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using System.Security.Claims;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "EmpleadoOSuperior")] // Empleados y superiores pueden acceder
    public class EmpleadoController : Controller
    {
        private readonly IRepositorioEmpleado _repositorioEmpleado;
        private readonly IWebHostEnvironment _environment;

        public EmpleadoController(IRepositorioEmpleado repositorioEmpleado, IWebHostEnvironment environment)
        {
            _repositorioEmpleado = repositorioEmpleado;
            _environment = environment;
        }

        // GET: Empleado/Index - Dashboard del empleado
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = await _repositorioEmpleado.GetDashboardEmpleadoDataAsync();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el dashboard: {ex.Message}";
                return View(new EmpleadoDashboardDto());
            }
        }

        // GESTIÓN DE USUARIOS (Solo crear + ver), para limitar riesgos
        

        // GET: Empleado/Usuarios
        public async Task<IActionResult> Usuarios(int pagina = 1, string buscar = "", string rol = "")
        {
            try
            {
                // Solo puede ver propietarios e inquilinos
                var rolesPermitidos = new[] { "propietario", "inquilino" };
                if (!string.IsNullOrEmpty(rol) && !rolesPermitidos.Contains(rol.ToLower()))
                {
                    rol = ""; // Limpiar rol no permitido
                }

                var (usuarios, totalRegistros) = await _repositorioEmpleado
                    .ObtenerUsuariosParaEmpleadoAsync(pagina, buscar, rol, 10);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / 10);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.RolSeleccionado = rol;

                return View(usuarios);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar usuarios: {ex.Message}";
                return View(new List<Usuario>());
            }
        }

        // GET: Empleado/CrearPropietario
        public IActionResult CrearPropietario()
        {
            return View();
        }

        // POST: Empleado/CrearPropietario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPropietario([Bind("Nombre,Apellido,Dni,Email,Telefono,Direccion")] Usuario propietario)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var nuevoPropietario = Usuario.CrearPropietario(
                        propietario.Nombre, propietario.Apellido, propietario.Dni,
                        propietario.Email, propietario.Telefono, propietario.Direccion);

                    await _repositorioEmpleado.CrearPropietarioAsync(nuevoPropietario);
                    TempData["SuccessMessage"] = $"Propietario {nuevoPropietario.NombreCompleto} creado exitosamente";
                    return RedirectToAction(nameof(Usuarios));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear propietario: {ex.Message}";
            }
            return View(propietario);
        }

        // GET: Empleado/CrearInquilino
        public IActionResult CrearInquilino()
        {
            return View();
        }

        // POST: Empleado/CrearInquilino
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearInquilino([Bind("Nombre,Apellido,Dni,Email,Telefono,Direccion")] Usuario inquilino)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var nuevoInquilino = Usuario.CrearInquilino(
                        inquilino.Nombre, inquilino.Apellido, inquilino.Dni,
                        inquilino.Email, inquilino.Telefono, inquilino.Direccion);

                    await _repositorioEmpleado.CrearInquilinoAsync(nuevoInquilino);
                    TempData["SuccessMessage"] = $"Inquilino {nuevoInquilino.NombreCompleto} creado exitosamente";
                    return RedirectToAction(nameof(Usuarios));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear inquilino: {ex.Message}";
            }
            return View(inquilino);
        }

        // ==========================================
        // GESTIÓN DE INMUEBLES (Crear + Ver)
        // ==========================================

        // GET: Empleado/Inmuebles
        public async Task<IActionResult> Inmuebles(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                var (inmuebles, totalRegistros) = await _repositorioEmpleado
                    .ObtenerInmueblesConPaginacionAsync(pagina, buscar, estado, 10);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / 10);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.EstadoSeleccionado = estado;

                return View(inmuebles);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar inmuebles: {ex.Message}";
                return View(new List<Inmueble>());
            }
        }

        // GET: Empleado/CrearInmueble
        public async Task<IActionResult> CrearInmueble()
        {
            try
            {
                ViewBag.Propietarios = await _repositorioEmpleado.ObtenerPropietariosActivosAsync();
                ViewBag.TiposInmueble = await _repositorioEmpleado.ObtenerTiposInmuebleAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar datos: {ex.Message}";
                return RedirectToAction(nameof(Inmuebles));
            }
        }

        // POST: Empleado/CrearInmueble
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearInmueble(Inmueble inmueble, IFormFile? archivoPortada)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var resultado = await _repositorioEmpleado.CrearInmuebleAsync(inmueble, archivoPortada, _environment);
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Inmueble creado exitosamente";
                        return RedirectToAction(nameof(Inmuebles));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear inmueble: {ex.Message}";
            }

            ViewBag.Propietarios = await _repositorioEmpleado.ObtenerPropietariosActivosAsync();
            ViewBag.TiposInmueble = await _repositorioEmpleado.ObtenerTiposInmuebleAsync();
            return View(inmueble);
        }

        // GET: Empleado/DetallesInmueble/5
        public async Task<IActionResult> DetallesInmueble(int id)
        {
            try
            {
                var inmueble = await _repositorioEmpleado.ObtenerInmuebleConDetallesAsync(id);
                if (inmueble == null)
                {
                    TempData["ErrorMessage"] = "Inmueble no encontrado";
                    return RedirectToAction(nameof(Inmuebles));
                }
                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar inmueble: {ex.Message}";
                return RedirectToAction(nameof(Inmuebles));
            }
        }

        // ==========================================
        // GESTIÓN DE CONTRATOS (Crear + Ver)
        // ==========================================

        // GET: Empleado/Contratos
        public async Task<IActionResult> Contratos(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                var (contratos, totalRegistros) = await _repositorioEmpleado
                    .ObtenerContratosConPaginacionAsync(pagina, buscar, estado, 10);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / 10);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.EstadoSeleccionado = estado;

                return View(contratos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contratos: {ex.Message}";
                return View(new List<Contrato>());
            }
        }

        // GET: Empleado/CrearContrato
        public async Task<IActionResult> CrearContrato()
        {
            try
            {
                ViewBag.InmueblesDisponibles = await _repositorioEmpleado.ObtenerInmueblesDisponiblesAsync();
                ViewBag.Inquilinos = await _repositorioEmpleado.ObtenerInquilinosAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar datos: {ex.Message}";
                return RedirectToAction(nameof(Contratos));
            }
        }

        // POST: Empleado/CrearContrato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearContrato(Contrato contrato)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Establecer usuario creador
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        contrato.IdUsuarioCreador = userId;
                    }

                    var resultado = await _repositorioEmpleado.CrearContratoAsync(contrato);
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Contrato creado exitosamente";
                        return RedirectToAction(nameof(Contratos));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear contrato: {ex.Message}";
            }

            ViewBag.InmueblesDisponibles = await _repositorioEmpleado.ObtenerInmueblesDisponiblesAsync();
            ViewBag.Inquilinos = await _repositorioEmpleado.ObtenerInquilinosAsync();
            return View(contrato);
        }

        // ==========================================
        // GESTIÓN DE INTERESES (Ver + Marcar contactado)
        // ==========================================

        // GET: Empleado/Intereses
        public async Task<IActionResult> Intereses(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                var (intereses, totalRegistros) = await _repositorioEmpleado
                    .ObtenerInteresesConPaginacionAsync(pagina, buscar, estado, 10);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / 10);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.EstadoSeleccionado = estado;

                return View(intereses);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar intereses: {ex.Message}";
                return View(new List<InteresInmueble>());
            }
        }

        // POST: Empleado/MarcarContactado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarContactado(int id)
        {
            try
            {
                var resultado = await _repositorioEmpleado.MarcarInteresContactadoAsync(id);
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Interés marcado como contactado";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo actualizar el interés";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar interés: {ex.Message}";
            }

            return RedirectToAction(nameof(Intereses));
        }

        // ==========================================
        // REPORTES BÁSICOS (Sin información financiera)
        // ==========================================

        // GET: Empleado/Reportes
       // public async Task<IActionResult> Reportes()
        //{
           // try
           // {
                //var reportes = await _repositorioEmpleado.ObtenerReportesBasicosAsync();
                //return View(reportes);
           // }
            //catch (Exception ex)
            //{
                //TempData["ErrorMessage"] = $"Error al generar reportes: {ex.Message}";
                //return View();
            //}
        //}

        // ==========================================
        // GESTIÓN DE ARCHIVOS
        // ==========================================

        // POST: Empleado/SubirFotoInmueble/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirFotoInmueble(int idInmueble, IFormFile archivo)
        {
            try
            {
                if (archivo != null)
                {
                    var resultado = await _repositorioEmpleado.AgregarFotoInmuebleAsync(idInmueble, archivo, _environment);
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Foto agregada exitosamente";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No se pudo agregar la foto";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al subir foto: {ex.Message}";
            }

            return RedirectToAction(nameof(DetallesInmueble), new { id = idInmueble });
        }

        // ==========================================
        // PERFIL DEL EMPLEADO
        // ==========================================

        // GET: Empleado/MiPerfil
        public async Task<IActionResult> MiPerfil()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["ErrorMessage"] = "Error al obtener información del usuario";
                    return RedirectToAction(nameof(Index));
                }

                var usuario = await _repositorioEmpleado.ObtenerEmpleadoPorIdAsync(userId);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
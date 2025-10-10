using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Roles = "propietario")]
    public class PropietarioController : Controller
    {
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioAlquiler _repositorioAlquiler;
        private readonly IRepositorioContrato _repositorioContrato;
        private readonly IRepositorioPropietario _repositorioPropietario;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropietarioController(
            IRepositorioUsuario repositorioUsuario,
            IRepositorioInmueble repositorioInmueble,
            IRepositorioAlquiler repositorioAlquiler,
            IRepositorioContrato repositorioContrato,
            IRepositorioPropietario repositorioPropietario,
            IWebHostEnvironment webHostEnvironment)
        {
            _repositorioUsuario = repositorioUsuario;
            _repositorioInmueble = repositorioInmueble;
            _repositorioAlquiler = repositorioAlquiler;
            _repositorioPropietario = repositorioPropietario;
            _repositorioContrato = repositorioContrato;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Propietario/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Login", "Account");
                }

                // Obtener estadísticas usando métodos existentes
                var inmuebles = await _repositorioInmueble.ObtenerPorPropietarioAsync(userId);
                var contratos = await _repositorioContrato.ObtenerContratosPorPropietarioAsync(userId);

                ViewBag.TotalPropiedades = inmuebles.Count();
                ViewBag.PropiedadesAlquiladas = inmuebles.Count(i => i.Estado?.ToLower() == "alquilado");
                ViewBag.PropiedadesDisponibles = inmuebles.Count(i => i.Estado?.ToLower() == "disponible");
                ViewBag.ContratosActivos = contratos.Count(c => c.Estado?.ToLower() == "activo");
                ViewBag.IngresosMensuales = await CalcularIngresosMensuales(userId);

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar dashboard: {ex.Message}";
                return RedirectToAction("Login", "Account");
            }
        }

        // GET: Propietario/MiPerfil
        public async Task<IActionResult> MiPerfil()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener estadísticas para la vista
                var inmuebles = await _repositorioInmueble.ObtenerPorPropietarioAsync(userId);
                ViewBag.TotalPropiedades = inmuebles.Count();
                ViewBag.PropiedadesAlquiladas = inmuebles.Count(i => i.Estado?.ToLower() == "alquilado");
                ViewBag.PropiedadesDisponibles = inmuebles.Count(i => i.Estado?.ToLower() == "disponible");

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Propietario/EditarPerfil
        public async Task<IActionResult> EditarPerfil()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var usuario = await _repositorioUsuario.GetByIdAsync(userId);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Propietario/EditarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(Usuario usuario, IFormFile? archivoAvatar)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (usuario.IdUsuario != userId)
                {
                    TempData["Error"] = "No tiene permisos para editar este perfil";
                    return RedirectToAction(nameof(Index));
                }

                var usuarioActual = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuarioActual == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Remover validaciones de campos que no se pueden editar
                ModelState.Remove("Email");
                ModelState.Remove("Dni");
                ModelState.Remove("Rol");
                ModelState.Remove("Estado");
                ModelState.Remove("Password");

                if (ModelState.IsValid)
                {
                    // Preservar campos que NO se pueden modificar
                    usuario.Email = usuarioActual.Email;
                    usuario.Dni = usuarioActual.Dni;
                    usuario.Rol = usuarioActual.Rol;
                    usuario.Estado = usuarioActual.Estado;
                    usuario.Password = usuarioActual.Password;

                    // Procesar avatar
                    if (archivoAvatar != null && archivoAvatar.Length > 0)
                    {
                        var avatarUrl = await GuardarAvatar(archivoAvatar, userId);
                        usuario.Avatar = avatarUrl;
                    }
                    else if (string.IsNullOrEmpty(usuario.Avatar))
                    {
                        usuario.Avatar = usuarioActual.Avatar;
                    }

                    await _repositorioUsuario.UpdateAsync(usuario);
                    TempData["Success"] = "Perfil actualizado exitosamente";
                    return RedirectToAction(nameof(MiPerfil));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar perfil: {ex.Message}";
                return View(usuario);
            }
        }

        // GET: Propietario/MisInmuebles
        public async Task<IActionResult> MisInmuebles()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Obtener el id_propietario a partir del id_usuario
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);

                if (propietario == null)
                {
                    TempData["Error"] = "No se encontró información del propietario.";
                    return RedirectToAction(nameof(Index));
                }

                var inmuebles = await _repositorioInmueble.ObtenerPorPropietarioAsync(propietario.IdPropietario);
                return View(inmuebles);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar propiedades: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }


        // GET: Propietario/DetalleInmueble/5
        public async Task<IActionResult> DetalleInmueble(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Obtener el id_propietario del usuario logueado
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);
                if (propietario == null)
                {
                    TempData["Error"] = "No se encontró información del propietario";
                    return RedirectToAction(nameof(MisInmuebles));
                }

                // Obtener el inmueble con galería
                var inmueble = await _repositorioInmueble.ObtenerInmuebleConGaleriaAsync(id);

                // Verificar que el inmueble existe y pertenece al propietario
                if (inmueble == null || inmueble.IdPropietario != propietario.IdPropietario)
                {
                    TempData["Error"] = "Propiedad no encontrada o no tiene permisos";
                    return RedirectToAction(nameof(MisInmuebles));
                }

                // Asignar el propietario completo al inmueble
                inmueble.Propietario = propietario;

                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar propiedad: {ex.Message}";
                return RedirectToAction(nameof(MisInmuebles));
            }
        }

        // GET: Propietario/CrearInmueble
        public async Task<IActionResult> CrearInmueble()
        {
            try
            {
                await PopulateViewDataAsync();

                // Pre-configurar valores por defecto para propietario
                var model = new Inmueble
                {
                    Estado = "disponible",
                    Uso = "Residencial",
                    Ambientes = 1,
                    Precio = 0
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction(nameof(MisInmuebles));
            }
        }


        // POST: Propietario/CrearInmueble  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearInmueble(Inmueble inmueble)
        {
            Console.WriteLine($"=== INICIANDO CREACIÓN DE INMUEBLE ===");

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener ID del usuario logueado
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    {
                        ModelState.AddModelError("", "Usuario no autenticado.");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }

                    Console.WriteLine($"=== Usuario ID: {userId} ===");

                    // OBTENER EL ID_PROPIETARIO CORRESPONDIENTE AL USUARIO
                    var idPropietario = await _repositorioPropietario.ObtenerIdPropietarioPorUsuarioAsync(userId);

                    if (idPropietario == 0)
                    {
                        Console.WriteLine($"=== ERROR: El usuario {userId} no es un propietario registrado ===");
                        ModelState.AddModelError("", "Su usuario no está registrado como propietario. Contacte al administrador.");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }

                    Console.WriteLine($"=== ID Propietario encontrado: {idPropietario} ===");

                    // Asignar IDs correctos - ¡IMPORTANTE! Usar idPropietario, no userId
                    inmueble.IdPropietario = idPropietario;
                    inmueble.IdUsuarioCreador = userId;
                    inmueble.FechaAlta = DateTime.Now;


                    inmueble.Uso = inmueble.Uso?.ToLower() ?? "residencial";

                    // DEBUG: Mostrar datos que se enviarán a la BD
                    Console.WriteLine($"=== DATOS PARA INSERCIÓN ===");
                    Console.WriteLine($"IdPropietario: {inmueble.IdPropietario}");
                    Console.WriteLine($"IdUsuarioCreador: {inmueble.IdUsuarioCreador}");
                    Console.WriteLine($"Dirección: {inmueble.Direccion}");
                    Console.WriteLine($"Tipo Inmueble: {inmueble.IdTipoInmueble}");
                    Console.WriteLine($"Uso: {inmueble.Uso}");
                    Console.WriteLine($"Ambientes: {inmueble.Ambientes}");
                    Console.WriteLine($"Precio: {inmueble.Precio}");
                    Console.WriteLine($"Estado: {inmueble.Estado}");

                    // Verificar dirección única
                    bool direccionExiste = await _repositorioInmueble.ExisteDireccionAsync(inmueble.Direccion);
                    Console.WriteLine($"=== Dirección existe: {direccionExiste} ===");

                    if (direccionExiste)
                    {
                        ModelState.AddModelError("Direccion", "Ya existe un inmueble con esta dirección");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }

                    // Crear el inmueble
                    bool resultado = await _repositorioInmueble.CrearInmuebleConPortadaAsync(inmueble, _webHostEnvironment);
                    Console.WriteLine($"=== Resultado guardado: {resultado} ===");

                    if (resultado)
                    {
                        Console.WriteLine($"=== ÉXITO: Inmueble creado correctamente ===");
                        TempData["Success"] = "¡Propiedad creada exitosamente!";
                        return RedirectToAction(nameof(MisInmuebles));
                    }
                    else
                    {
                        Console.WriteLine($"=== FALLO: No se pudo crear el inmueble ===");
                        ModelState.AddModelError("", "No se pudo crear el inmueble. Verifique los datos ingresados.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== EXCEPCIÓN: {ex.Message} ===");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    ModelState.AddModelError("", $"Error al crear el inmueble: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"=== MODELO INVÁLIDO ===");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
            }

            await PopulateViewDataAsync();
            return View(inmueble);
        }

        
        private async Task<string> GuardarArchivoPortadaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            try
            {
                // Crear estructura de carpetas
                var propiedadesFolder = Path.Combine("uploads", "propiedades", idInmueble.ToString(), "portada");
                var uploadsFolder = Path.Combine(environment.WebRootPath, propiedadesFolder);

                // Asegurar que existe el directorio
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generar nombre único del archivo
                var fileName = $"portada_{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // Retornar URL relativa (ej: /uploads/propiedades/1/portada/portada_abc123.jpg)
                return $"/{Path.Combine("uploads", "propiedades", idInmueble.ToString(), "portada", fileName).Replace("\\", "/")}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR Guardando portada: {ex.Message} ===");
                return null;
            }
        }

        // Método auxiliar para cargar datos de vista (similar al de administrador)
        private async Task PopulateViewDataAsync()
        {
            try
            {
                // Cargar tipos de inmueble usando tu método existente
                var tiposInmueble = await _repositorioInmueble.ObtenerTiposInmuebleActivosAsync();
                ViewBag.TiposInmueble = new SelectList(tiposInmueble, "IdTipoInmueble", "Nombre");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en PopulateViewDataAsync: {ex.Message}");
            }
        }

        // Método de validación de coordenadas (ya lo tienes)
        private bool IsValidCoordinates(string coordinates)
        {
            if (string.IsNullOrEmpty(coordinates)) return true;

            var pattern = @"^-?\d{1,2}(\.\d{1,6})?,-?\d{1,3}(\.\d{1,6})?$";
            return System.Text.RegularExpressions.Regex.IsMatch(coordinates, pattern);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarInmueble(int id, Inmueble inmueble, IFormFileCollection imagenes)
        {
            if (id != inmueble.IdInmueble)
            {
                TempData["Error"] = "Error en la identificación de la propiedad";
                return RedirectToAction(nameof(MisInmuebles));
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var inmuebleActual = await _repositorioInmueble.ObtenerInmueblePorIdAsync(id);

                if (inmuebleActual == null || inmuebleActual.IdPropietario != userId)
                {
                    TempData["Error"] = "Propiedad no encontrada o no tiene permisos";
                    return RedirectToAction(nameof(MisInmuebles));
                }

                // Preservar datos del propietario y estado
                inmueble.IdPropietario = userId;
                inmueble.Estado = inmuebleActual.Estado;
                inmueble.FechaAlta = inmuebleActual.FechaAlta;
                inmueble.IdUsuarioCreador = inmuebleActual.IdUsuarioCreador;
                inmueble.UrlPortada = inmuebleActual.UrlPortada; // Preservar portada existente

                if (ModelState.IsValid)
                {
                    // Procesar nuevas imágenes
                    if (imagenes != null && imagenes.Count > 0)
                    {
                        inmueble.Imagenes = inmuebleActual.Imagenes ?? new List<ImagenInmueble>();
                        int totalImagenesActuales = inmueble.Imagenes.Count;

                        foreach (var imagen in imagenes.Take(10 - totalImagenesActuales))
                        {
                            if (imagen.Length > 0)
                            {
                                var imagenUrl = await GuardarImagenInmueble(imagen);

                                // Si no hay portada actual, establecer la primera nueva como portada
                                if (string.IsNullOrEmpty(inmueble.UrlPortada) && totalImagenesActuales == 0)
                                {
                                    inmueble.UrlPortada = imagenUrl;
                                }

                                inmueble.Imagenes.Add(new ImagenInmueble
                                {
                                    Url = imagenUrl
                                    // Eliminado: EsPortada ya que no existe en el modelo
                                });

                                totalImagenesActuales++;
                            }
                        }
                    }
                    else
                    {
                        inmueble.Imagenes = inmuebleActual.Imagenes;
                    }

                    await _repositorioInmueble.ActualizarInmuebleAsync(inmueble);
                    TempData["Success"] = "Propiedad actualizada exitosamente";
                    return RedirectToAction(nameof(DetalleInmueble), new { id = inmueble.IdInmueble });
                }

                // Recargar tipos de inmueble si hay error
                var tiposInmueble = await _repositorioInmueble.ObtenerTiposInmuebleActivosAsync();
                ViewBag.TiposInmueble = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(tiposInmueble, "IdTipoInmueble", "Nombre", inmueble.IdTipoInmueble);
                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar propiedad: {ex.Message}";

                var tiposInmueble = await _repositorioInmueble.ObtenerTiposInmuebleActivosAsync();
                ViewBag.TiposInmueble = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(tiposInmueble, "IdTipoInmueble", "Nombre", inmueble.IdTipoInmueble);
                return View(inmueble);
            }
        }

        // GET: Propietario/EditarInmueble/5
        public async Task<IActionResult> EditarInmueble(int id)
        {
            try
            {

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var inmueble = await _repositorioInmueble.ObtenerInmuebleConGaleriaAsync(id);

                if (inmueble == null || inmueble.IdPropietario != userId)
                {
                    TempData["Error"] = "Propiedad no encontrada o no tiene permisos";
                    return RedirectToAction(nameof(MisInmuebles));
                }

                // Cargar tipos de inmueble
                var tiposInmueble = await _repositorioInmueble.ObtenerTiposInmuebleActivosAsync();
                ViewBag.TiposInmueble = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(tiposInmueble, "IdTipoInmueble", "Nombre", inmueble.IdTipoInmueble); // CORREGIDO: IdTipoInmueble

                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar propiedad: {ex.Message}";
                return RedirectToAction(nameof(MisInmuebles));
            }
        }



        public async Task<IActionResult> MisContratos()
        {
            try
            {
                var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(nameIdentifier, out int userId) || userId <= 0)
                {
                    TempData["Error"] = "No se pudo identificar al usuario correctamente.";
                    return RedirectToAction("Login", "Usuarios");
                }
                Console.WriteLine($"=== DEBUG: UserId: {userId} ===");

                var contratos = await _repositorioPropietario.ObtenerContratosPorPropietarioAsync(userId);
                Console.WriteLine($"=== DEBUG: Contratos obtenidos: {contratos?.Count} ===");

                // Cargar datos relacionados
                foreach (var contrato in contratos)
                {
                    contrato.Inmueble = await _repositorioInmueble.ObtenerInmueblePorIdAsync(contrato.IdInmueble);
                    var inquilino = await _repositorioPropietario.ObtenerInquilinoPorIdAsync(contrato.IdInquilino);
                    contrato.Inquilino = inquilino;
                }

                Console.WriteLine($"=== DEBUG: Retornando vista con {contratos.Count} contratos ===");
                return View(contratos);
            }
            catch (Exception ex)
            {
                // LOG DETALLADO DEL ERROR
                Console.WriteLine($"=== ERROR EN MISCONTRATOS: {ex.Message} ===");
                Console.WriteLine($"=== STACK TRACE: {ex.StackTrace} ===");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"=== INNER EXCEPTION: {ex.InnerException.Message} ===");
                }

                TempData["Error"] = $"Error al cargar contratos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Propietario/MisPagos
        public async Task<IActionResult> MisPagos()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var pagos = await _repositorioPropietario.ObtenerPagosPorPropietarioAsync(userId);

                // Cargar datos relacionados
                foreach (var pago in pagos)
                {
                    if (pago.Contrato != null)
                    {
                        pago.Contrato.Inmueble = await _repositorioInmueble.ObtenerInmueblePorIdAsync(pago.Contrato.IdInmueble);


                        var inquilino = await _repositorioPropietario.ObtenerInquilinoPorIdAsync(pago.Contrato.IdInquilino);
                        pago.Contrato.Inquilino = inquilino;
                    }
                }

                return View(pagos);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar pagos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // MÉTODOS AUXILIARES
        private async Task<string> GuardarAvatar(IFormFile archivo, int userId)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var nombreArchivo = $"avatar_{userId}_{Guid.NewGuid()}{extension}";
            var rutaAvatars = Path.Combine(_webHostEnvironment.WebRootPath, "avatars");

            if (!Directory.Exists(rutaAvatars))
                Directory.CreateDirectory(rutaAvatars);

            var rutaCompleta = Path.Combine(rutaAvatars, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return $"/avatars/{nombreArchivo}";
        }

        private async Task<string> GuardarImagenInmueble(IFormFile archivo)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var nombreArchivo = $"inmueble_{Guid.NewGuid()}{extension}";
            var rutaImagenes = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes-inmuebles");

            if (!Directory.Exists(rutaImagenes))
                Directory.CreateDirectory(rutaImagenes);

            var rutaCompleta = Path.Combine(rutaImagenes, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return $"/imagenes-inmuebles/{nombreArchivo}";
        }

        private async Task<decimal> CalcularIngresosMensuales(int propietarioId)
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            var pagos = await _repositorioPropietario.ObtenerPagosPorPropietarioAsync(propietarioId, inicioMes, finMes);
            return pagos.Where(p => p.Estado?.ToLower() == "pagado").Sum(p => p.MontoTotal);
        }
        //gestionar la galeria de imagenes 
        // GET: Propietario/GestionarImagenes/5
        public async Task<IActionResult> GestionarImagenes(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);

                if (propietario == null)
                {
                    TempData["Error"] = "No se encontró información del propietario";
                    return RedirectToAction(nameof(MisInmuebles));
                }

                var inmueble = await _repositorioInmueble.ObtenerInmuebleConGaleriaAsync(id);

                if (inmueble == null || inmueble.IdPropietario != propietario.IdPropietario)
                {
                    TempData["Error"] = "Propiedad no encontrada o no tiene permisos";
                    return RedirectToAction(nameof(MisInmuebles));
                }

                inmueble.Propietario = propietario;
                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar galería: {ex.Message}";
                return RedirectToAction(nameof(MisInmuebles));
            }
        }

        // POST: Propietario/AgregarImagenGaleria/5
        [HttpPost]
        public async Task<IActionResult> AgregarImagenGaleria(int id, List<IFormFile> archivos)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);

                var inmueble = await _repositorioInmueble.ObtenerPorIdAsync(id);
                if (inmueble == null || inmueble.IdPropietario != propietario.IdPropietario)
                {
                    return Json(new { success = false, message = "No tiene permisos" });
                }

                if (archivos == null || !archivos.Any())
                {
                    return Json(new { success = false, message = "No se seleccionaron archivos" });
                }

                var imagenesGuardadas = new List<object>();

                foreach (var archivo in archivos)
                {
                    if (archivo.Length > 0)
                    {
                        var urlImagen = await GuardarImagenGaleriaAsync(archivo, id, _webHostEnvironment);
                        if (!string.IsNullOrEmpty(urlImagen))
                        {
                            var imagenId = await _repositorioInmueble.GuardarImagenGaleriaAsync(id, urlImagen);
                            if (imagenId > 0)
                            {
                                imagenesGuardadas.Add(new
                                {
                                    id = imagenId,
                                    url = urlImagen
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Se agregaron {imagenesGuardadas.Count} imágenes"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Propietario/EliminarImagenGaleria/5
        [HttpPost]
        public async Task<IActionResult> EliminarImagenGaleria(int idImagen, int idInmueble)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);

                var inmueble = await _repositorioInmueble.ObtenerPorIdAsync(idInmueble);
                if (inmueble == null || inmueble.IdPropietario != propietario.IdPropietario)
                {
                    return Json(new { success = false, message = "No tiene permisos" });
                }

                var imagen = await _repositorioInmueble.ObtenerImagenPorIdAsync(idImagen);
                if (imagen != null && !string.IsNullOrEmpty(imagen.Url))
                {

                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, imagen.Url.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Eliminar de la base de datos
                    await _repositorioInmueble.EliminarImagenAsync(idImagen);

                    return Json(new { success = true, message = "Imagen eliminada" });
                }

                return Json(new { success = false, message = "No se pudo eliminar" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> EliminarInmueble(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);

                // Obtener el inmueble con sus imágenes
                var inmueble = await _repositorioInmueble.ObtenerPorIdConImagenesAsync(id);

                if (inmueble == null || inmueble.IdPropietario != propietario.IdPropietario)
                {
                    return Json(new { success = false, message = "No tiene permisos para eliminar esta propiedad" });
                }

                // Verificar si tiene contratos activos
                var tieneContratosActivos = await _repositorioContrato.TieneContratosActivosAsync(id);
                if (tieneContratosActivos)
                {
                    return Json(new { success = false, message = "No se puede eliminar la propiedad porque tiene contratos activos" });
                }

                // Eliminar imágenes físicas del servidor
                if (inmueble.Imagenes != null && inmueble.Imagenes.Any())
                {
                    foreach (var imagen in inmueble.Imagenes)
                    {
                        if (!string.IsNullOrEmpty(imagen.Url))
                        {
                            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, imagen.Url.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }

                    // Eliminar imágenes de la base de datos
                    await _repositorioInmueble.EliminarImagenesPorInmuebleAsync(id);
                }

                // Eliminar imagen de portada si existe
                if (!string.IsNullOrEmpty(inmueble.UrlPortada))
                {
                    var portadaPath = Path.Combine(_webHostEnvironment.WebRootPath, inmueble.UrlPortada.TrimStart('/'));
                    if (System.IO.File.Exists(portadaPath))
                    {
                        System.IO.File.Delete(portadaPath);
                    }
                }

                // Eliminar de la base de datos (cambiar estado a "inactivo")
                var eliminado = await _repositorioInmueble.EliminarLogicamenteAsync(id);

                if (eliminado)
                {
                    return Json(new { success = true, message = "Propiedad eliminada correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo eliminar la propiedad" });
                }
            }
            catch (Exception ex)
            {
                
                return Json(new { success = false, message = $"Error al eliminar la propiedad: {ex.Message}" });
            }
        }
        // POST: Propietario/ActualizarPortada/5
        [HttpPost]
        public async Task<IActionResult> ActualizarPortada(int id, IFormFile portadaFile)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var propietario = await _repositorioPropietario.ObtenerPorUsuarioIdAsync(userId);

                var inmueble = await _repositorioInmueble.ObtenerPorIdAsync(id);
                if (inmueble == null || inmueble.IdPropietario != propietario.IdPropietario)
                {
                    return Json(new { success = false, message = "No tiene permisos" });
                }

                if (portadaFile == null || portadaFile.Length == 0)
                {
                    return Json(new { success = false, message = "No se seleccionó archivo" });
                }

                // Eliminar portada anterior si existe
                if (!string.IsNullOrEmpty(inmueble.UrlPortada))
                {
                    var portadaAnteriorPath = Path.Combine(_webHostEnvironment.WebRootPath, inmueble.UrlPortada.TrimStart('/'));
                    if (System.IO.File.Exists(portadaAnteriorPath))
                    {
                        System.IO.File.Delete(portadaAnteriorPath);
                    }
                }

                // Guardar nueva portada
                var urlPortada = await GuardarPortadaAsync(portadaFile, id, _webHostEnvironment);
                if (!string.IsNullOrEmpty(urlPortada))
                {
                    await _repositorioInmueble.ActualizarPortadaAsync(id, urlPortada);
                    return Json(new
                    {
                        success = true,
                        message = "Portada actualizada",
                        urlPortada = urlPortada
                    });
                }

                return Json(new { success = false, message = "Error al actualizar" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Métodos auxiliares para guardar archivos
        private async Task<string> GuardarPortadaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            try
            {
                var portadaFolder = Path.Combine("uploads", "propiedades", idInmueble.ToString(), "portada");
                var uploadsFolder = Path.Combine(environment.WebRootPath, portadaFolder);

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"portada_{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                return $"/{Path.Combine("uploads", "propiedades", idInmueble.ToString(), "portada", fileName).Replace("\\", "/")}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando portada: {ex.Message}");
                return null;
            }
        }

        private async Task<string> GuardarImagenGaleriaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            try
            {
                var galeriaFolder = Path.Combine("uploads", "propiedades", idInmueble.ToString(), "galeria");
                var uploadsFolder = Path.Combine(environment.WebRootPath, galeriaFolder);

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"galeria_{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                return $"/{Path.Combine("uploads", "propiedades", idInmueble.ToString(), "galeria", fileName).Replace("\\", "/")}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando galería: {ex.Message}");
                return null;
            }
        }
    }
}
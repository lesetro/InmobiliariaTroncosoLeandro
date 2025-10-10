using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using System.Security.Claims;


namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class UsuarioController : Controller
    {
        // DEPENDENCIAS CORRECTAS 
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly IRepositorioAlquiler _repositorioAlquiler;
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioVenta _repositorioVenta;

        private readonly IWebHostEnvironment _webHostEnvironment;

        // CONSTRUCTOR CORRECTO 
        public UsuarioController(
            IRepositorioUsuario repositorioUsuario,
            IRepositorioAlquiler repositorioAlquiler,
            IRepositorioInmueble repositorioInmueble,
            IRepositorioVenta repositorioVenta,
            IWebHostEnvironment webHostEnvironment)
        {
            _repositorioUsuario = repositorioUsuario;
            _repositorioAlquiler = repositorioAlquiler;
            _repositorioInmueble = repositorioInmueble;
            _repositorioVenta = repositorioVenta;
            _webHostEnvironment = webHostEnvironment;
        }


        public async Task<IActionResult> Index(
    int pagina = 1,
    string buscar = "",
    string rol = "",
    string estadoFiltro = "activos")
        {
            const int itemsPorPagina = 10;

            // USAR SOLO UN MÉTODO
            var (usuarios, totalRegistros) = await _repositorioUsuario
                .ObtenerConPaginacionBusquedaYRolAsync(pagina, buscar, rol, itemsPorPagina, estadoFiltro);

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)itemsPorPagina);
            ViewBag.TotalRegistros = totalRegistros;
            ViewBag.BuscarActual = buscar;
            ViewBag.RolActual = rol;
            ViewBag.EstadoFiltro = estadoFiltro;

            return View(usuarios);
        }

        // GET: Usuario/Create
        
        public IActionResult Create()
        {
            ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
            return View();
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Apellido,Dni,Email,Telefono,Direccion,Rol,Avatar")] Usuario usuario,
            IFormFile? archivoAvatar)
        {
            try
            {
                Console.WriteLine("========== INICIO CREACIÓN USUARIO ==========");
                Console.WriteLine($"Nombre: {usuario.Nombre}");
                Console.WriteLine($"Apellido: {usuario.Apellido}");
                Console.WriteLine($"DNI: {usuario.Dni}");
                Console.WriteLine($"Email: {usuario.Email}");
                Console.WriteLine($"Teléfono: {usuario.Telefono}");
                Console.WriteLine($"Dirección: {usuario.Direccion}");
                Console.WriteLine($"Rol: {usuario.Rol}");
                Console.WriteLine($"Avatar URL: {usuario.Avatar ?? "Sin URL"}");
                Console.WriteLine($"Archivo Avatar: {(archivoAvatar != null ? archivoAvatar.FileName : "Sin archivo")}");

                // Remover validaciones de campos autogenerados
                ModelState.Remove("Password");
                ModelState.Remove("Estado");
                ModelState.Remove("Avatar");
                ModelState.Remove("IdUsuario");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine(" ModelState NO es válido:");
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            Console.WriteLine($"  Campo: {key}");
                            foreach (var error in state.Errors)
                            {
                                Console.WriteLine($"    Error: {error.ErrorMessage}");
                            }
                        }
                    }

                    ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
                    return View(usuario);
                }

                Console.WriteLine(" ModelState es válido");

                // Validar rol
                var rolesValidos = new[] { "administrador", "empleado", "propietario", "inquilino" };
                if (!rolesValidos.Contains(usuario.Rol?.ToLower()))
                {
                    Console.WriteLine($" Rol inválido: {usuario.Rol}");
                    ModelState.AddModelError("Rol", "Rol no válido");
                    ViewBag.Roles = rolesValidos.ToList();
                    return View(usuario);
                }

                Console.WriteLine($" Rol válido: {usuario.Rol}");

                // Crear usuario según el rol
                Usuario nuevoUsuario;
                switch (usuario.Rol?.ToLower() ?? "")
                {
                    case "administrador":
                        nuevoUsuario = Usuario.CrearAdministrador(usuario.Nombre, usuario.Apellido,
                            usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                        break;
                    case "empleado":
                        nuevoUsuario = Usuario.CrearEmpleado(usuario.Nombre, usuario.Apellido,
                            usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                        break;
                    case "propietario":
                        nuevoUsuario = Usuario.CrearPropietario(usuario.Nombre, usuario.Apellido,
                            usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                        break;
                    case "inquilino":
                        nuevoUsuario = Usuario.CrearInquilino(usuario.Nombre, usuario.Apellido,
                            usuario.Dni, usuario.Email, usuario.Telefono, usuario.Direccion);
                        break;
                    default:
                        throw new InvalidOperationException("Rol no válido");
                }

                Console.WriteLine($" Usuario creado: {nuevoUsuario.NombreCompleto}");
                Console.WriteLine($"   Avatar autogenerado: {nuevoUsuario.Avatar}");

                // PROCESAR AVATAR CON PRIORIDADES
                if (archivoAvatar != null && archivoAvatar.Length > 0)
                {
                    Console.WriteLine(" Procesando archivo de avatar...");

                    // Validar tipo de archivo
                    var tiposPermitidos = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(archivoAvatar.FileName).ToLower();

                    if (!tiposPermitidos.Contains(extension))
                    {
                        ModelState.AddModelError("", "Solo se permiten imágenes (jpg, jpeg, png, gif)");
                        ViewBag.Roles = rolesValidos.ToList();
                        return View(usuario);
                    }

                    // Validar tamaño (máximo 5MB)
                    if (archivoAvatar.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "El archivo no debe superar los 5MB");
                        ViewBag.Roles = rolesValidos.ToList();
                        return View(usuario);
                    }

                    // Generar nombre único para el archivo
                    var nombreArchivo = $"avatar_{Guid.NewGuid()}{extension}";
                    var rutaAvatars = Path.Combine(_webHostEnvironment.WebRootPath, "avatars");

                    // Crear directorio si no existe
                    if (!Directory.Exists(rutaAvatars))
                    {
                        Directory.CreateDirectory(rutaAvatars);
                        Console.WriteLine("Directorio avatars creado");
                    }

                    var rutaCompleta = Path.Combine(rutaAvatars, nombreArchivo);

                    // Guardar archivo
                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await archivoAvatar.CopyToAsync(stream);
                    }

                    // PRIORIDAD 1: Archivo subido
                    nuevoUsuario.ActualizarAvatar($"/avatars/{nombreArchivo}");
                    Console.WriteLine($" Avatar personalizado guardado: {nuevoUsuario.Avatar}");
                }
                else if (!string.IsNullOrWhiteSpace(usuario.Avatar))
                {
                    // PRIORIDAD 2: URL ingresada
                    nuevoUsuario.ActualizarAvatar(usuario.Avatar);
                    Console.WriteLine($" Avatar desde URL: {nuevoUsuario.Avatar}");
                }
                else
                {
                    // PRIORIDAD 3: Avatar autogenerado con iniciales (ya fue creado por el método Crear{Rol})
                    Console.WriteLine(" Usando avatar autogenerado con iniciales");
                }

                // Guardar en BD
                await _repositorioUsuario.CreateAsync(nuevoUsuario);

                Console.WriteLine(" Usuario guardado en BD exitosamente");
                Console.WriteLine("========== FIN CREACIÓN USUARIO ==========");

                TempData["Success"] = $"Usuario {nuevoUsuario.NombreCompleto} creado exitosamente. Contraseña temporal: PasswordTemporal123";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($" InvalidOperationException: {ex.Message}");
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Error al crear usuario: {ex.Message}";
            }

            ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
            return View(usuario);
        }

        // GET: Usuario/Edit/5
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(id);
                if (usuario == null || usuario.Estado == "eliminado")
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Los empleados solo pueden editar propietarios e inquilinos
                if (User.IsInRole("empleado"))
                {
                    if (usuario.Rol == "administrador" || usuario.Rol == "empleado")
                    {
                        TempData["Error"] = "No tiene permisos para editar este usuario";
                        return RedirectToAction(nameof(Index));
                    }
                }

                ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar usuario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Usuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Edit(int id,
            [Bind("IdUsuario,Nombre,Apellido,Dni,Email,Telefono,Direccion,Rol,Estado,Avatar")] Usuario usuario,
            IFormFile? archivoAvatar)
        {
            if (id != usuario.IdUsuario)
            {
                TempData["Error"] = "Error en la identificación del usuario";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Obtener usuario actual de la BD
                var usuarioActual = await _repositorioUsuario.GetByIdAsync(id);
                if (usuarioActual == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Remover validaciones de Password del ModelState ya que no la estamos editando
                ModelState.Remove("Password");

                // Validación adicional: si el DNI no cambió, no validarlo
                if (usuario.Dni == usuarioActual.Dni)
                {
                    ModelState.Remove("Dni");
                }

                // Validación adicional: si el Email no cambió, no validarlo
                if (usuario.Email == usuarioActual.Email)
                {
                    ModelState.Remove("Email");
                }

                if (ModelState.IsValid)
                {
                    // IMPORTANTE: Preservar la contraseña actual
                    usuario.Password = usuarioActual.Password;

                    // Los empleados no pueden cambiar roles ni estado
                    if (User.IsInRole("empleado"))
                    {
                        usuario.Rol = usuarioActual.Rol;
                        usuario.Estado = usuarioActual.Estado;
                    }

                    // Procesar avatar si se subió un archivo
                    if (archivoAvatar != null && archivoAvatar.Length > 0)
                    {
                        // Validar tipo de archivo
                        var tiposPermitidos = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(archivoAvatar.FileName).ToLower();

                        if (!tiposPermitidos.Contains(extension))
                        {
                            ModelState.AddModelError("Avatar", "Solo se permiten imágenes (jpg, jpeg, png, gif)");
                            ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
                            return View(usuario);
                        }

                        // Validar tamaño (máximo 5MB)
                        if (archivoAvatar.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("Avatar", "El archivo no debe superar los 5MB");
                            ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
                            return View(usuario);
                        }

                        // Generar nombre único para el archivo
                        var nombreArchivo = $"avatar_{usuario.IdUsuario}_{Guid.NewGuid()}{extension}";
                        var rutaAvatars = Path.Combine(_webHostEnvironment.WebRootPath, "avatars");

                        // Crear directorio si no existe
                        if (!Directory.Exists(rutaAvatars))
                        {
                            Directory.CreateDirectory(rutaAvatars);
                        }

                        var rutaCompleta = Path.Combine(rutaAvatars, nombreArchivo);

                        // Eliminar avatar anterior si existe y es un archivo local
                        if (!string.IsNullOrEmpty(usuarioActual.Avatar) && usuarioActual.Avatar.StartsWith("/avatars/"))
                        {
                            var rutaAnterior = Path.Combine(_webHostEnvironment.WebRootPath, usuarioActual.Avatar.TrimStart('/'));
                            if (System.IO.File.Exists(rutaAnterior))
                            {
                                System.IO.File.Delete(rutaAnterior);
                            }
                        }

                        // Guardar nuevo archivo
                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await archivoAvatar.CopyToAsync(stream);
                        }

                        // Actualizar ruta del avatar
                        usuario.Avatar = $"/avatars/{nombreArchivo}";
                    }
                    else if (string.IsNullOrEmpty(usuario.Avatar))
                    {
                        // Si no se subió archivo y el campo está vacío, mantener el avatar actual
                        usuario.Avatar = usuarioActual.Avatar;
                    }
                    // Si usuario.Avatar tiene valor pero no hay archivo, es una URL externa

                    // Actualizar usuario en la base de datos
                    await _repositorioUsuario.UpdateAsync(usuario);

                    TempData["Success"] = "Usuario actualizado exitosamente";

                    // IMPORTANTE: Redirigir a Details en lugar de Index para ver los cambios
                    return RedirectToAction(nameof(Details), new { id = usuario.IdUsuario });
                }

                // Si el modelo no es válido, mostrar errores para debugging
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // Esto te ayudará a debuggear si hay problemas de validación
                    Console.WriteLine($"Error de validación: {modelError.ErrorMessage}");
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar usuario: {ex.Message}";
                // Log del error completo para debugging
                Console.WriteLine($"Error completo: {ex}");
            }

            ViewBag.Roles = new List<string> { "administrador", "empleado", "propietario", "inquilino" };
            return View(usuario);
        }

        // GET: Usuario/Details/5
        [Authorize(Policy = "AdminOEmpleado")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(id);
                if (usuario == null || usuario.Estado == "eliminado")
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar usuario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Método auxiliar para eliminar avatar (opcional, pero útil)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOEmpleado")]
        public async Task<IActionResult> EliminarAvatar(int id)
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(id);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                // Verificar permisos
                if (User.IsInRole("empleado"))
                {
                    if (usuario.Rol == "administrador" || usuario.Rol == "empleado")
                    {
                        return Json(new { success = false, message = "Sin permisos para modificar este usuario" });
                    }
                }

                // Eliminar archivo si es local
                if (!string.IsNullOrEmpty(usuario.Avatar) && usuario.Avatar.StartsWith("/avatars/"))
                {
                    var rutaArchivo = Path.Combine(_webHostEnvironment.WebRootPath, usuario.Avatar.TrimStart('/'));
                    if (System.IO.File.Exists(rutaArchivo))
                    {
                        System.IO.File.Delete(rutaArchivo);
                    }
                }

                // Actualizar usuario sin avatar
                usuario.Avatar = null;
                await _repositorioUsuario.UpdateAsync(usuario);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Usuario/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(id);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                usuario.Estado = nuevoEstado;
                await _repositorioUsuario.UpdateAsync(usuario);

                TempData["Success"] = $"Usuario {(nuevoEstado == "activo" ? "activado" : "desactivado")} exitosamente";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Usuario/ResetearPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> ResetearPassword(int id)
        {
            try
            {
                var resultado = await _repositorioUsuario.ResetearPasswordAsync(id);
                if (resultado)
                {
                    TempData["Success"] = "Contraseña reseteada exitosamente a 'PasswordTemporal123'";
                }
                else
                {
                    TempData["Error"] = "No se pudo resetear la contraseña";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al resetear contraseña: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Usuario/Delete/5 - MODIFICADO PARA MOSTRAR RELACIONES
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(id);
                if (usuario == null || usuario.Estado == "eliminado")
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // ⚠️ VERIFICAR RELACIONES ANTES DE MOSTRAR LA VISTA DE ELIMINACIÓN
                var tieneRelaciones = await VerificarRelacionesUsuario(id);

                if (tieneRelaciones.TieneRelaciones)
                {
                    // ❌ NO permitir eliminación - redirigir con mensaje explicativo
                    TempData["Warning"] = $"No se puede eliminar el usuario porque tiene {tieneRelaciones.MensajeRelaciones}. " +
                                         "Como alternativa, puede desactivar el usuario para que no pueda acceder al sistema.";

                    // Agregar botón para desactivar como alternativa
                    TempData["MostrarDesactivar"] = true;
                    TempData["UsuarioId"] = id;

                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar usuario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Usuario/Delete/5 - MODIFICADO PARA VERIFICACIÓN FINAL
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // VERIFICACIÓN FINAL ANTES DE ELIMINAR
                var tieneRelaciones = await VerificarRelacionesUsuario(id);

                if (tieneRelaciones.TieneRelaciones)
                {
                    TempData["Error"] = $"No se puede eliminar el usuario porque tiene {tieneRelaciones.MensajeRelaciones}. " +
                                       "Debe desactivar el usuario en su lugar.";
                    return RedirectToAction(nameof(Index));
                }

                var resultado = await _repositorioUsuario.DeleteAsync(id);
                if (resultado)
                {
                    TempData["Success"] = "Usuario eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el usuario. Puede ser el último administrador del sistema.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar usuario: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Usuario/Perfil - Para que cualquier usuario vea su propio perfil
        public async Task<IActionResult> Perfil()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["Error"] = "Error al obtener información del usuario";
                    return RedirectToAction("Index", "Home");
                }

                var usuario = await _repositorioUsuario.GetByIdAsync(userId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Home");
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar perfil: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Usuario/Estadisticas - Solo para admin
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Estadisticas()
        {
            try
            {
                var estadisticas = await _repositorioUsuario.GetEstadisticasPorRolAsync();
                var usuariosRecientes = await _repositorioUsuario.GetUsuariosRecientesAsync();

                ViewBag.EstadisticasPorRol = estadisticas;
                ViewBag.UsuariosRecientes = usuariosRecientes;
                ViewBag.TotalUsuarios = await _repositorioUsuario.GetTotalUsuariosAsync();
                ViewBag.UsuariosActivos = await _repositorioUsuario.GetNumeroUsuariosActivosAsync();

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar estadísticas: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }



        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(id);

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Cambiar el estado al contrario
                usuario.Estado = usuario.Estado == "activo" ? "inactivo" : "activo";

                // Usar UpdateAsync que sí existe en tu interfaz
                var usuarioActualizado = await _repositorioUsuario.UpdateAsync(usuario);

                if (usuarioActualizado != null)
                {
                    TempData["SuccessMessage"] = $"Usuario {usuario.Estado} exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo cambiar el estado del usuario";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        // 🔧 MÉTODO PARA VERIFICAR RELACIONES DE USUARIO
        private async Task<(bool TieneRelaciones, string MensajeRelaciones)> VerificarRelacionesUsuario(int usuarioId)
        {
            var mensajes = new List<string>();

            try
            {
                var usuario = await _repositorioUsuario.GetByIdAsync(usuarioId);
                if (usuario == null) return (false, "");

                //  VERIFICAR PAGOS DE ALQUILER (últimos 6 meses)
                try
                {
                    var fechaLimite = DateTime.Now.AddMonths(-6);
                    var (pagosAlquiler, _) = await _repositorioAlquiler.ObtenerPagosAlquilerConPaginacionAsync(1, "", "", 1000);

                    // Filtrar pagos del usuario (como creador o vinculado a contratos del usuario)
                    var pagosUsuario = pagosAlquiler.Where(p =>
                    (p.IdUsuarioCreador == usuarioId ||
                    (p.Contrato != null && (p.Contrato.IdPropietario == usuarioId || p.Contrato.IdInquilino == usuarioId))) &&
                    p.FechaPago >= fechaLimite).ToList();

                    if (pagosUsuario.Any())
                    {
                        var pagosPendientes = pagosUsuario.Where(p => p.Estado?.ToLower() == "pendiente" || p.Estado?.ToLower() == "vencido").Count();
                        var pagosPagados = pagosUsuario.Where(p => p.Estado?.ToLower() == "pagado").Count();

                        if (pagosPendientes > 0)
                        {
                            mensajes.Add($"{pagosPendientes} pago(s) de alquiler pendiente(s)");
                        }
                        if (pagosPagados > 0)
                        {
                            mensajes.Add($"{pagosPagados} pago(s) de alquiler reciente(s)");
                        }
                    }
                }
                catch (Exception)
                {
                    mensajes.Add("pagos de alquiler que no pudieron verificarse");
                }

                //  VERIFICAR INMUEBLES (solo propietarios)
                if (usuario.Rol?.ToLower() == "propietario")
                {
                    try
                    {
                        var (inmuebles, _) = await _repositorioInmueble.ObtenerConPaginacionYBusquedaAsync(1, "", "", 1000);
                        var inmueblesDelUsuario = inmuebles.Where(i => i.IdPropietario == usuarioId).ToList();

                        if (inmueblesDelUsuario.Any())
                        {
                            var alquilados = inmueblesDelUsuario.Where(i => i.Estado?.ToLower() == "alquilado").Count();
                            var disponibles = inmueblesDelUsuario.Where(i => i.Estado?.ToLower() == "disponible").Count();
                            var vendidos = inmueblesDelUsuario.Where(i => i.Estado?.ToLower() == "vendido").Count();

                            if (alquilados > 0)
                            {
                                mensajes.Add($"{alquilados} inmueble(s) alquilado(s)");
                            }
                            if (disponibles > 0)
                            {
                                mensajes.Add($"{disponibles} inmueble(s) disponible(s)");
                            }
                            if (vendidos > 0)
                            {
                                mensajes.Add($"{vendidos} inmueble(s) vendido(s)");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        mensajes.Add("inmuebles que no pudieron verificarse");
                    }
                }

                // VERIFICAR PAGOS DE VENTAS
                try
                {
                    var (pagosVenta, _) = await _repositorioVenta.ObtenerPagosVentaConPaginacionAsync(1, "", "", 1000);
                    var pagosVentaUsuario = pagosVenta.Where(p =>
                        p.IdUsuarioCreador == usuarioId ||
                        (p.Inmueble != null && p.Inmueble.IdPropietario == usuarioId)
                    ).ToList();

                    if (pagosVentaUsuario.Any())
                    {
                        var ventasPendientes = pagosVentaUsuario.Where(p => p.Estado?.ToLower() == "pendiente").Count();
                        var ventasCompletadas = pagosVentaUsuario.Where(p => p.Estado?.ToLower() == "pagado").Count();

                        if (ventasPendientes > 0)
                        {
                            mensajes.Add($"{ventasPendientes} venta(s) pendiente(s)");
                        }
                        if (ventasCompletadas > 0)
                        {
                            mensajes.Add($"{ventasCompletadas} venta(s) completada(s)");
                        }
                    }
                }
                catch (Exception)
                {
                    mensajes.Add("ventas que no pudieron verificarse");
                }

                //  VERIFICAR CONTRATOS VIGENTES
                try
                {
                    var contratosVigentes = await _repositorioAlquiler.ObtenerContratosVigentesAsync(1000);
                    var contratosDelUsuario = contratosVigentes.Where(c =>
                        c.IdPropietario == usuarioId ||
                        c.IdInquilino == usuarioId ||
                        c.IdUsuarioCreador == usuarioId
                    ).ToList();

                    if (contratosDelUsuario.Any())
                    {
                        var activos = contratosDelUsuario.Where(c => c.Estado?.ToLower() == "activo").Count();
                        if (activos > 0)
                        {
                            mensajes.Add($"{activos} contrato(s) vigente(s)");
                        }
                    }
                }
                catch (Exception)
                {
                    mensajes.Add("contratos que no pudieron verificarse");
                }

                //  VERIFICAR SI ES EL ÚLTIMO ADMINISTRADOR
                if (usuario.Rol?.ToLower() == "administrador")
                {
                    try
                    {
                        var totalAdmins = await _repositorioUsuario.GetTotalUsuariosPorRolAsync("administrador");
                        if (totalAdmins <= 1)
                        {
                            mensajes.Add("es el último administrador del sistema");
                        }
                    }
                    catch (Exception)
                    {
                        mensajes.Add("verificación de administradores falló");
                    }
                }
            }
            catch (Exception ex)
            {
                // Error general - ser conservador
                mensajes.Add($"relaciones críticas no verificables: {ex.Message}");
            }

            bool tieneRelaciones = mensajes.Any();
            string mensajeCompleto = tieneRelaciones ? string.Join(", ", mensajes) : "";

            return (tieneRelaciones, mensajeCompleto);
        }

        // Pa validaciones con ajax 

        /// <summary>
        /// Verifica si un DNI ya existe en la base de datos
        /// GET: /Usuario/VerificarDni?dni=12345678
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> VerificarDni(string dni)
        {
            try
            {
                // Validar que venga el DNI
                if (string.IsNullOrWhiteSpace(dni))
                {
                    return Json(new { existe = false, mensaje = "DNI vacío" });
                }

                // Consultar en la base de datos si existe
                bool existe = await _repositorioUsuario.DniExistsAsync(dni);

                // Retornar JSON con el resultado
                return Json(new
                {
                    existe = existe,
                    mensaje = existe ? "Este DNI ya está registrado" : "DNI disponible"
                });
            }
            catch (Exception ex)
            {
                // En caso de error, retornar que no existe para no bloquear
                return Json(new { existe = false, mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Verifica si un Email ya existe en la base de datos
        /// GET: /Usuario/VerificarEmail?email=usuario@ejemplo.com
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> VerificarEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { existe = false, mensaje = "Email vacío" });
                }

                bool existe = await _repositorioUsuario.EmailExistsAsync(email);

                return Json(new
                {
                    existe = existe,
                    mensaje = existe ? "Este email ya está registrado" : "Email disponible"
                });
            }
            catch (Exception ex)
            {
                return Json(new { existe = false, mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Verifica si un Teléfono ya existe en la base de datos
        /// GET: /Usuario/VerificarTelefono?telefono=1234567890
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> VerificarTelefono(string telefono)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(telefono))
                {
                    return Json(new { existe = false, mensaje = "Teléfono vacío" });
                }

                // Como no tienes un método específico para teléfono, 
                // tendrías que agregarlo o hacer una búsqueda manual
                var usuarios = await _repositorioUsuario.GetAllAsync();
                bool existe = usuarios.Any(u => u.Telefono == telefono);

                return Json(new
                {
                    existe = existe,
                    mensaje = existe ? "Este teléfono ya está registrado" : "Teléfono disponible"
                });
            }
            catch (Exception ex)
            {
                return Json(new { existe = false, mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Verifica múltiples campos a la vez (opcional, más eficiente)
        /// POST: /Usuario/VerificarDuplicados
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> VerificarDuplicados([FromBody] ValidacionDuplicadosDto datos)
        {
            try
            {
                var resultado = new
                {
                    dniExiste = !string.IsNullOrWhiteSpace(datos.Dni) &&
                                await _repositorioUsuario.DniExistsAsync(datos.Dni),
                    emailExiste = !string.IsNullOrWhiteSpace(datos.Email) &&
                                  await _repositorioUsuario.EmailExistsAsync(datos.Email),
                    telefonoExiste = false // Implementar si es necesario
                };

                return Json(resultado);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, mensaje = ex.Message });
            }
        }

        // Clase auxiliar para recibir datos en POST
        public class ValidacionDuplicadosDto
        {
            public string? Dni { get; set; }
            public string? Email { get; set; }
            public string? Telefono { get; set; }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class PropietariosController : Controller
    {
        private readonly IRepositorioPropietario _repositorioPropietario;

        public PropietariosController(IRepositorioPropietario repositorioPropietario)
        {
            _repositorioPropietario = repositorioPropietario;
        }

        // GET: Propietarios
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", int itemsPorPagina = 10, string estado = "activos")
        {
            try
            {
                var (propietarios, totalRegistros) = await _repositorioPropietario
                    .ObtenerConPaginacionYBusquedaAsync(pagina, buscar, itemsPorPagina, estado);

                // Calcular información de paginación
                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.ITEMS_POR_PAGINA = itemsPorPagina;
                ViewBag.EstadoFiltro = estado;

                return View(propietarios);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los propietarios: {ex.Message}";
                return View(new List<Propietario>());
            }
        }

        // GET: Propietarios/Create
        public IActionResult Create()
        {
            var propietario = new Propietario
            {
                Usuario = new Usuario
                {
                    Nombre = "",
                    Apellido = "",
                    Dni = "",
                    Email = "",
                    Telefono = "",
                    Direccion = ""
                }
            };
            return View(propietario);
        }

        // POST: Propietarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Propietario propietario)
        {
            // PARA REMOVER VALIDACIÓN DE CAMPOS QUE SE ASIGNAN AUTOMÁTICAMENTE
            ModelState.Remove("Usuario.Rol");
            ModelState.Remove("Usuario.Password");
            ModelState.Remove("Usuario.Estado");
            ModelState.Remove("Usuario.Avatar");

            Console.WriteLine("=== INICIO DEBUG CREATE ===");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            // Verificar si Usuario es null
            if (propietario.Usuario == null)
            {
                Console.WriteLine("ERROR: propietario.Usuario es NULL");
                ModelState.AddModelError("", "Error: datos de usuario no recibidos");
                return View(propietario);
            }

            Console.WriteLine($"Usuario recibido:");
            Console.WriteLine($"- Nombre: '{propietario.Usuario.Nombre}'");
            Console.WriteLine($"- Apellido: '{propietario.Usuario.Apellido}'");
            Console.WriteLine($"- DNI: '{propietario.Usuario.Dni}'");
            Console.WriteLine($"- Email: '{propietario.Usuario.Email}'");
            Console.WriteLine($"- Telefono: '{propietario.Usuario.Telefono}'");
            Console.WriteLine($"- Direccion: '{propietario.Usuario.Direccion}'");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== ERRORES DE VALIDACIÓN ===");
                foreach (var error in ModelState)
                {
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"Campo: {error.Key} | Error: {err.ErrorMessage}");
                    }
                }

                return View(propietario);
            }

            try
            {
                Console.WriteLine("=== VERIFICANDO DNI ===");

                if (!string.IsNullOrEmpty(propietario.Usuario.Dni))
                {
                    Console.WriteLine($"Verificando DNI: '{propietario.Usuario.Dni}'");
                    bool dniExiste = await _repositorioPropietario.ExisteDniAsync(propietario.Usuario.Dni);
                    Console.WriteLine($"DNI existe: {dniExiste}");

                    if (dniExiste)
                    {
                        Console.WriteLine("DNI duplicado - devolviendo vista con error");
                        ModelState.AddModelError("Usuario.Dni", "Ya existe un propietario con este DNI");
                        return View(propietario);
                    }
                }

                Console.WriteLine("=== VERIFICANDO EMAIL ===");

                if (!string.IsNullOrEmpty(propietario.Usuario.Email))
                {
                    Console.WriteLine($"Verificando Email: '{propietario.Usuario.Email}'");
                    bool emailExiste = await _repositorioPropietario.ExisteEmailAsync(propietario.Usuario.Email);
                    Console.WriteLine($"Email existe: {emailExiste}");

                    if (emailExiste)
                    {
                        Console.WriteLine("Email duplicado - devolviendo vista con error");
                        ModelState.AddModelError("Usuario.Email", "Ya existe un propietario con este email");
                        return View(propietario);
                    }
                }

                Console.WriteLine("=== LLAMANDO AL REPOSITORIO PARA CREAR ===");
                bool resultado = await _repositorioPropietario.CrearPropietarioConTransaccionAsync(propietario);
                Console.WriteLine($"Resultado del repositorio: {resultado}");

                if (resultado)
                {
                    Console.WriteLine("=== ÉXITO - REDIRIGIENDO AL INDEX ===");
                    TempData["SuccessMessage"] = "Propietario creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    Console.WriteLine("=== ERROR: REPOSITORIO DEVOLVIÓ FALSE ===");
                    ModelState.AddModelError("", "No se pudo crear el propietario. Error en la base de datos.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPCIÓN: {ex.Message} ===");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            Console.WriteLine("=== DEVOLVIENDO VISTA CON ERRORES ===");
            return View(propietario);
        }

        // GET: Propietarios/Edit/5
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(id);

                if (propietario == null)
                {
                    return NotFound();
                }

                return View(propietario);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el propietario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Propietarios/Edit/5
        [HttpPost]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Propietario propietario)
        {
            if (id != propietario.IdPropietario)
            {
                return NotFound();
            }

            // Remover validación de campos automáticos
            ModelState.Remove("Usuario.Rol");
            ModelState.Remove("Usuario.Password");
            ModelState.Remove("Usuario.Avatar");

            if (ModelState.IsValid)
            {
                try
                {
                    // OBTENER EL PROPIETARIO ACTUAL 
                    var propietarioActual = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(id);

                    if (propietarioActual == null)
                    {
                        return NotFound();
                    }

                    // Verificar DNI único
                    if (!string.IsNullOrEmpty(propietario.Usuario?.Dni))
                    {
                        bool dniExiste = await _repositorioPropietario.ExisteDniAsync(
                            propietario.Usuario.Dni,
                            propietarioActual.IdUsuario
                        );

                        if (dniExiste)
                        {
                            ModelState.AddModelError("Usuario.Dni", "Ya existe otro propietario con este DNI");
                            return View(propietario);
                        }
                    }

                    // Verificar Email único
                    if (!string.IsNullOrEmpty(propietario.Usuario?.Email))
                    {
                        bool emailExiste = await _repositorioPropietario.ExisteEmailAsync(
                            propietario.Usuario.Email,
                            propietarioActual.IdUsuario
                        );

                        if (emailExiste)
                        {
                            ModelState.AddModelError("Usuario.Email", "Ya existe otro propietario con este email");
                            return View(propietario);
                        }
                    }

                    bool resultado = await _repositorioPropietario.ActualizarPropietarioConTransaccionAsync(propietario);

                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Propietario actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar el propietario.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el propietario: {ex.Message}");
                }
            }


            return View(propietario);
        }

        // GET: Propietarios/Delete/5
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(id);

                if (propietario == null)
                {
                    return NotFound();
                }

                return View(propietario);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el propietario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Propietarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool resultado = await _repositorioPropietario.EliminarPropietarioConTransaccionAsync(id);

                if (resultado)
                {
                    TempData["SuccessMessage"] = "Propietario eliminado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo eliminar el propietario";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el propietario: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Propietarios/ToggleEstado/5
        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            try
            {
                var propietario = await _repositorioPropietario.ObtenerPropietarioPorIdAsync(id);

                if (propietario == null)
                {
                    TempData["ErrorMessage"] = "Propietario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Cambiar el estado al contrario
                propietario.Estado = !propietario.Estado;

                bool resultado = await _repositorioPropietario.ActualizarPropietarioConTransaccionAsync(propietario);

                if (resultado)
                {
                    string nuevoEstado = propietario.Estado ? "activado" : "desactivado";
                    TempData["SuccessMessage"] = $"Propietario {nuevoEstado} exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo cambiar el estado del propietario";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Services;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class InmueblesController : Controller
    {
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly ISearchService _searchService;
        private readonly IWebHostEnvironment _environment;

        public InmueblesController(IRepositorioInmueble repositorioInmueble, ISearchService searchService, IWebHostEnvironment environment)
        {
            _repositorioInmueble = repositorioInmueble;
            _searchService = searchService;
            _environment = environment;

        }

        // GET: Inmuebles
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "", int itemsPorPagina = 10)
        {
            try
            {
                var (inmuebles, totalRegistros) = await _repositorioInmueble
                    .ObtenerConPaginacionYBusquedaAsync(pagina, buscar, estado, itemsPorPagina);

                // Calcular información de paginación
                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;
                ViewBag.ITEMS_POR_PAGINA = itemsPorPagina;

                return View(inmuebles);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los inmuebles: {ex.Message}";
                return View(new List<Inmueble>());
            }
        }

        // GET: Inmuebles/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await PopulateViewDataAsync();
                return View(new Inmueble());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar datos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inmuebles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inmueble inmueble)

        {
            Console.WriteLine($"PortadaFile: {inmueble.PortadaFile?.FileName ?? "NULL"}");
            Console.WriteLine($"Tamaño: {inmueble.PortadaFile?.Length ?? 0} bytes");
            if (ModelState.IsValid)
            {
                try
                {
                    // Validar formato de coordenadas
                    if (!string.IsNullOrEmpty(inmueble.Coordenadas) && !IsValidCoordinates(inmueble.Coordenadas))
                    {
                        ModelState.AddModelError("Coordenadas", "Formato de coordenadas inválido (ej. -34.6037,-58.3816)");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }

                    // Verificar dirección única
                    if (await _repositorioInmueble.ExisteDireccionAsync(inmueble.Direccion))
                    {
                        ModelState.AddModelError("Direccion", "Ya existe un inmueble con esta dirección");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }
                    bool resultado = await _repositorioInmueble.CrearInmuebleConPortadaAsync(inmueble, _environment);

                    Console.WriteLine($"Resultado guardado: {resultado}");

                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Inmueble creado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo crear el inmueble. Verifique los datos ingresados.");
                    }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        ModelState.AddModelError("", $"Error al crear el inmueble: {ex.Message}");
                    }
                }

                await PopulateViewDataAsync();
                return View(inmueble);
            }


        // GET: Inmuebles/Edit
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var inmueble = await _repositorioInmueble.ObtenerInmueblePorIdAsync(id);

                if (inmueble == null)
                {
                    return NotFound();
                }
                Console.WriteLine($"Inmueble cargado: ID={inmueble.IdInmueble}, IdTipoInmueble={inmueble.IdTipoInmueble}");

                await PopulateViewDataAsync();
                return View(inmueble);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inmuebles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inmueble inmueble)
        {
            if (id != inmueble.IdInmueble)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validar formato de coordenadas
                    if (!string.IsNullOrEmpty(inmueble.Coordenadas) && !IsValidCoordinates(inmueble.Coordenadas))
                    {
                        ModelState.AddModelError("Coordenadas", "Formato de coordenadas inválido (ej. -34.6037,-58.3816)");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }

                    // Verificar dirección única (excluyendo el actual)
                    if (await _repositorioInmueble.ExisteDireccionAsync(inmueble.Direccion, inmueble.IdInmueble))
                    {
                        ModelState.AddModelError("Direccion", "Ya existe otro inmueble con esta dirección");
                        await PopulateViewDataAsync();
                        return View(inmueble);
                    }

                    bool resultado = await _repositorioInmueble.ActualizarInmuebleConPortadaAsync(inmueble, _environment);

                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Inmueble actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar el inmueble.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el inmueble: {ex.Message}");
                }
            }

            await PopulateViewDataAsync();
            return View(inmueble);
        }

        // GET: Inmuebles/Delete
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var inmueble = await _repositorioInmueble.ObtenerInmuebleConDetallesAsync(id);

                if (inmueble == null)
                {
                    return NotFound();
                }

                // Verificar si tiene contratos vigentes
                var contratosVigentes = await _repositorioInmueble.ContarContratosVigentesAsync(id);
                ViewBag.ContratosVigentes = contratosVigentes;

                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inmuebles/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Verificar contratos vigentes antes de eliminar
                var contratosVigentes = await _repositorioInmueble.ContarContratosVigentesAsync(id);
                if (contratosVigentes > 0)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el inmueble porque tiene contratos vigentes";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                bool resultado = await _repositorioInmueble.EliminarInmuebleAsync(id);

                if (resultado)
                {
                    TempData["SuccessMessage"] = "Inmueble eliminado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo eliminar el inmueble";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el inmueble: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // APIs para búsqueda y autocompletado


        [HttpGet]
        public async Task<IActionResult> BuscarInmuebles(string termino, int limite = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarInmueblesAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPropietariosParaAutocompletar(string termino, int limite = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarPropietariosAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]

        [HttpGet]
        public async Task<IActionResult> BuscarTiposInmuebleParaAutocompletar(string termino, int limite = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarTiposInmueblesAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var inmueble = await _repositorioInmueble.ObtenerInmuebleConDetallesAsync(id);

                if (inmueble == null)
                {
                    return NotFound();
                }

                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        


        // Métodos auxiliares
        private async Task PopulateViewDataAsync()
        {
            try
            {
                var propietarios = await _repositorioInmueble.ObtenerPropietariosActivosAsync();
                var tiposInmueble = await _repositorioInmueble.ObtenerTiposInmuebleActivosAsync();


                // ENVIAR COMO LISTAS SIMPLES, NO COMO SELECTLIST
                ViewData["Propietarios"] = propietarios; // List<Propietario>
                ViewData["TiposInmueble"] = tiposInmueble; // List<TipoInmueble>

                // DEBUGGING
                Console.WriteLine($"Propietarios cargados: {propietarios.Count}");
                Console.WriteLine($"Tipos inmueble cargados: {tiposInmueble.Count}");

                // Mostrar algunos propietarios para debug
                foreach (var p in propietarios.Take(3))
                {
                    Console.WriteLine($"Propietario: ID={p.IdPropietario}, Nombre={p.Usuario?.Apellido}, {p.Usuario?.Nombre}");
                }
                foreach (var t in tiposInmueble.Take(5))
                {
                    Console.WriteLine($"Tipo Inmueble: ID={t.IdTipoInmueble}, Nombre={t.Nombre}");
                }
            }
            catch (Exception ex)
            {
                ViewData["Propietarios"] = new List<Propietario>();
                ViewData["TiposInmueble"] = new List<TipoInmueble>();
                Console.WriteLine($"Error en PopulateViewDataAsync: {ex.Message}");
            }
        }
        private bool IsValidCoordinates(string coordinates)
        {
            if (string.IsNullOrEmpty(coordinates)) return true;
            var pattern = @"^-?\d{1,2}(\.\d{1,6})?,-?\d{1,3}(\.\d{1,6})?$";
            return System.Text.RegularExpressions.Regex.IsMatch(coordinates, pattern);
        }
    }
}
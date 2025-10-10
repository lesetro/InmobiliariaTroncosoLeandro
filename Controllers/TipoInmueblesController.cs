using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class TipoInmueblesController : Controller
    {
        private readonly IRepositorioTipoInmueble _repositorioTipoInmueble;

        public TipoInmueblesController(IRepositorioTipoInmueble repositorioTipoInmueble)
        {
            _repositorioTipoInmueble = repositorioTipoInmueble;
        }

        // GET: TipoInmuebles
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "", int itemsPorPagina = 10)
        {
            try
            {
                var (tiposInmueble, totalRegistros) = await _repositorioTipoInmueble
                    .ObtenerConPaginacionYBusquedaAsync(pagina, buscar, estado, itemsPorPagina);

                // Calcular información de paginación
                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);
                
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;
                ViewBag.ITEMS_POR_PAGINA = itemsPorPagina;

                return View(tiposInmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los tipos de inmueble: {ex.Message}";
                return View(new List<TipoInmueble>());
            }
        }

        // GET: TipoInmuebles/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var tipoInmueble = await _repositorioTipoInmueble.ObtenerTipoInmuebleConDetallesAsync(id);
                
                if (tipoInmueble == null)
                {
                    return NotFound();
                }

                return View(tipoInmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el tipo de inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: TipoInmuebles/Create
        public IActionResult Create()
        {
            return View(new TipoInmueble());
        }

        // POST: TipoInmuebles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoInmueble tipoInmueble)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar nombre único
                    if (await _repositorioTipoInmueble.ExisteNombreAsync(tipoInmueble.Nombre))
                    {
                        ModelState.AddModelError("Nombre", "Ya existe un tipo de inmueble con este nombre");
                        return View(tipoInmueble);
                    }

                    // Asegurar que la fecha de creación sea la actual
                    tipoInmueble.FechaCreacion = DateTime.Now;
                    
                    bool resultado = await _repositorioTipoInmueble.CrearTipoInmuebleAsync(tipoInmueble);
                    
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Tipo de inmueble creado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo crear el tipo de inmueble. Verifique los datos ingresados.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al crear el tipo de inmueble: {ex.Message}");
                }
            }
            
            return View(tipoInmueble);
        }

        // GET: TipoInmuebles/Edit/5
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var tipoInmueble = await _repositorioTipoInmueble.ObtenerTipoInmueblePorIdAsync(id);

                if (tipoInmueble == null)
                {
                    return NotFound();
                }

                return View(tipoInmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el tipo de inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: TipoInmuebles/Edit/5
        [HttpPost]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoInmueble tipoInmueble)
        {
            if (id != tipoInmueble.IdTipoInmueble)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar nombre único (excluyendo el actual)
                    if (await _repositorioTipoInmueble.ExisteNombreAsync(tipoInmueble.Nombre, tipoInmueble.IdTipoInmueble))
                    {
                        ModelState.AddModelError("Nombre", "Ya existe otro tipo de inmueble con este nombre");
                        return View(tipoInmueble);
                    }

                    bool resultado = await _repositorioTipoInmueble.ActualizarTipoInmuebleAsync(tipoInmueble);
                    
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Tipo de inmueble actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar el tipo de inmueble.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el tipo de inmueble: {ex.Message}");
                }
            }
            
            return View(tipoInmueble);
        }

        // GET: TipoInmuebles/Delete/5
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var tipoInmueble = await _repositorioTipoInmueble.ObtenerTipoInmuebleConDetallesAsync(id);
                
                if (tipoInmueble == null)
                {
                    return NotFound();
                }

                // Verificar si tiene inmuebles asociados
                var inmueblesAsociados = await _repositorioTipoInmueble.ContarInmueblesAsociadosAsync(id);
                ViewBag.InmueblesAsociados = inmueblesAsociados;

                return View(tipoInmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el tipo de inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: TipoInmuebles/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Verificar inmuebles asociados antes de eliminar
                var inmueblesAsociados = await _repositorioTipoInmueble.ContarInmueblesAsociadosAsync(id);
                
                if (inmueblesAsociados > 0)
                {
                    TempData["ErrorMessage"] = $"No se puede eliminar el tipo de inmueble porque tiene {inmueblesAsociados} inmueble(s) asociado(s).";
                    return RedirectToAction(nameof(Index));
                }

                bool resultado = await _repositorioTipoInmueble.EliminarTipoInmuebleAsync(id);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Tipo de inmueble eliminado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo eliminar el tipo de inmueble";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el tipo de inmueble: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para obtener tipos de inmueble activos (para uso en otros controladores)
        [HttpGet]
        public async Task<JsonResult> ObtenerTiposActivosAsync()
        {
            try
            {
                var tiposActivos = await _repositorioTipoInmueble.ObtenerTiposInmuebleActivosAsync();
                
                var resultado = tiposActivos.Select(t => new
                {
                    id = t.IdTipoInmueble,
                    nombre = t.Nombre,
                    descripcion = t.Descripcion
                });

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Acción para cambiar el estado (activar/desactivar)
        [HttpPost]
        [Authorize(Policy = "Administrador")]
        public async Task<JsonResult> CambiarEstado(int id)
        {
            try
            {
                var tipoInmueble = await _repositorioTipoInmueble.ObtenerTipoInmueblePorIdAsync(id);

                if (tipoInmueble == null)
                {
                    return Json(new { success = false, message = "Tipo de inmueble no encontrado" });
                }

                // Si se quiere desactivar, verificar que no tenga inmuebles asociados
                if (tipoInmueble.Estado)
                {
                    var inmueblesAsociados = await _repositorioTipoInmueble.ContarInmueblesAsociadosAsync(id);
                    if (inmueblesAsociados > 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"No se puede desactivar el tipo de inmueble porque tiene {inmueblesAsociados} inmueble(s) asociado(s)."
                        });
                    }
                }

                // Cambiar el estado
                tipoInmueble.Estado = !tipoInmueble.Estado;
                bool resultado = await _repositorioTipoInmueble.ActualizarTipoInmuebleAsync(tipoInmueble);

                if (resultado)
                {
                    var nuevoEstado = tipoInmueble.Estado ? "activado" : "desactivado";
                    return Json(new
                    {
                        success = true,
                        message = $"Tipo de inmueble {nuevoEstado} exitosamente",
                        nuevoEstado = tipoInmueble.Estado
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo cambiar el estado" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
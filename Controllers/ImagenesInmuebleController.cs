using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Authorize(Policy = "AdminOEmpleado")]
    public class ImagenesInmuebleController : Controller
    {
        private readonly IRepositorioImagen _repositorioImagen;
        private readonly IWebHostEnvironment _environment;

        public ImagenesInmuebleController(IRepositorioImagen repositorioImagen, IWebHostEnvironment environment)
        {
            _repositorioImagen = repositorioImagen;
            _environment = environment;
        }

        // GET: ImagenesInmueble/Index 
        public async Task<IActionResult> Index(int idInmueble)
        {
            if (idInmueble <= 0)
            {
                return NotFound("ID de inmueble inválido");
            }

            try
            {
                // Verificar que el inmueble existe
                if (!await _repositorioImagen.ExisteInmuebleAsync(idInmueble))
                {
                    TempData["ErrorMessage"] = "El inmueble especificado no existe";
                    return RedirectToAction("Index", "Inmuebles");
                }

                // Obtener imágenes del inmueble
                var imagenes = await _repositorioImagen.ObtenerImagenesPorInmuebleAsync(idInmueble);
                
                // Obtener estadísticas para mostrar en la vista
                var estadisticas = await _repositorioImagen.ObtenerEstadisticasGaleriaAsync(idInmueble);
                
                ViewData["IdInmueble"] = idInmueble;
                ViewData["EstadisticasGaleria"] = estadisticas;
                ViewData["TotalImagenes"] = imagenes.Count;
                
                return View(imagenes);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar imágenes: {ex.Message}";
                return RedirectToAction("Details", "Inmuebles", new { id = idInmueble });
            }
        }

        // GET: ImagenesInmueble/Create
        public async Task<IActionResult> Create(int idInmueble)
        {
            if (idInmueble <= 0)
            {
                return NotFound("ID de inmueble inválido");
            }

            try
            {
                // Verificar que el inmueble existe
                if (!await _repositorioImagen.ExisteInmuebleAsync(idInmueble))
                {
                    TempData["ErrorMessage"] = "El inmueble especificado no existe";
                    return RedirectToAction("Index", "Inmuebles");
                }

                // Obtener el siguiente orden disponible
                int siguienteOrden = await _repositorioImagen.ObtenerSiguienteOrdenAsync(idInmueble);

                var imagen = new ImagenInmueble 
                { 
                    IdInmueble = idInmueble,
                    Orden = siguienteOrden,
                    Url = "" // Valor por defecto para satisfacer el required
                };

                ViewData["IdInmueble"] = idInmueble;
                return View(imagen);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al preparar formulario: {ex.Message}";
                return RedirectToAction(nameof(Index), new { idInmueble });
            }
        }

        // POST: ImagenesInmueble/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ImagenInmueble imagen, IFormFile? ImagenFile)
        {
            // Validar que se envió un archivo
            if (ImagenFile == null || ImagenFile.Length == 0)
            {
                ModelState.AddModelError("ImagenFile", "Debe seleccionar una imagen válida");
                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }

            try
            {
                // Validar archivo usando el repositorio
                if (!await _repositorioImagen.ValidarArchivoImagenAsync(ImagenFile))
                {
                    ModelState.AddModelError("ImagenFile", 
                        "Archivo inválido. Solo se permiten imágenes JPG, JPEG, PNG, GIF, BMP o WEBP de máximo 5MB");
                    ViewData["IdInmueble"] = imagen.IdInmueble;
                    return View(imagen);
                }

                // Verificar que el inmueble existe
                if (!await _repositorioImagen.ExisteInmuebleAsync(imagen.IdInmueble))
                {
                    ModelState.AddModelError("", "El inmueble especificado no existe");
                    ViewData["IdInmueble"] = imagen.IdInmueble;
                    return View(imagen);
                }

                // Crear imagen con archivo
                bool resultado = await _repositorioImagen.CrearImagenConArchivoAsync(imagen, ImagenFile, _environment);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Imagen agregada exitosamente a la galería";
                    return RedirectToAction(nameof(Index), new { idInmueble = imagen.IdInmueble });
                }
                else
                {
                    ModelState.AddModelError("", "Error al guardar la imagen. Intente nuevamente.");
                    ViewData["IdInmueble"] = imagen.IdInmueble;
                    return View(imagen);
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("ImagenFile", ex.Message);
                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error inesperado: {ex.Message}");
                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }
        }

        // GET: ImagenesInmueble/Edit
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var imagen = await _repositorioImagen.ObtenerImagenPorIdAsync(id);
                
                if (imagen == null)
                {
                    TempData["ErrorMessage"] = "Imagen no encontrada";
                    return RedirectToAction("Index", "Inmuebles");
                }

                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar imagen: {ex.Message}";
                return RedirectToAction("Index", "Inmuebles");
            }
        }

        // POST: ImagenesInmueble/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ImagenInmueble imagen, IFormFile? ImagenFile)
        {
            if (id != imagen.IdImagen)
            {
                return NotFound();
            }

            // Validar modelo 
            ModelState.Remove("ImagenFile"); // No es requerido en edición
            if (!ModelState.IsValid)
            {
                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }

            try
            {
                // Si hay nuevo archivo, validarlo
                if (ImagenFile != null && ImagenFile.Length > 0)
                {
                    if (!await _repositorioImagen.ValidarArchivoImagenAsync(ImagenFile))
                    {
                        ModelState.AddModelError("ImagenFile", 
                            "Archivo inválido. Solo se permiten imágenes JPG, JPEG, PNG, GIF, BMP o WEBP de máximo 5MB");
                        ViewData["IdInmueble"] = imagen.IdInmueble;
                        return View(imagen);
                    }
                }

                // Actualizar imagen
                bool resultado = await _repositorioImagen.ActualizarImagenConArchivoAsync(imagen, ImagenFile, _environment);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Imagen actualizada exitosamente";
                    return RedirectToAction(nameof(Index), new { idInmueble = imagen.IdInmueble });
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar la imagen");
                    ViewData["IdInmueble"] = imagen.IdInmueble;
                    return View(imagen);
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("ImagenFile", ex.Message);
                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar imagen: {ex.Message}");
                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }
        }

        // GET: ImagenesInmueble/Delete
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var imagen = await _repositorioImagen.ObtenerImagenPorIdAsync(id);
                
                if (imagen == null)
                {
                    TempData["ErrorMessage"] = "Imagen no encontrada";
                    return RedirectToAction("Index", "Inmuebles");
                }

                // Verificar si se puede eliminar
                if (!await _repositorioImagen.PuedeEliminarImagenAsync(id))
                {
                    TempData["ErrorMessage"] = "No se puede eliminar esta imagen";
                    return RedirectToAction(nameof(Index), new { idInmueble = imagen.IdInmueble });
                }

                ViewData["IdInmueble"] = imagen.IdInmueble;
                return View(imagen);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar imagen: {ex.Message}";
                return RedirectToAction("Index", "Inmuebles");
            }
        }

        // POST: ImagenesInmueble/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int idInmueble)
        {
            try
            {
                // Obtener información de la imagen antes de eliminar (para mensajes)
                var imagen = await _repositorioImagen.ObtenerImagenPorIdAsync(id);
                if (imagen == null)
                {
                    TempData["ErrorMessage"] = "Imagen no encontrada";
                    return RedirectToAction(nameof(Index), new { idInmueble });
                }

                // Guardar orden para reorganización posterior
                int ordenEliminado = imagen.Orden;
                string descripcionImagen = imagen.Descripcion ?? "Sin descripción";

                // Eliminar imagen (esto incluye archivo físico y registro en BD)
                bool resultado = await _repositorioImagen.EliminarImagenAsync(id);
                
                if (resultado)
                {
                    // Reorganizar el orden de las imágenes restantes
                    await _repositorioImagen.ReorganizarOrdenDespuesDeEliminarAsync(idInmueble, ordenEliminado);
                    
                    // Limpiar posibles archivos huérfanos
                    await _repositorioImagen.LimpiarImagenesHuerfanasAsync(idInmueble, _environment);
                    
                    TempData["SuccessMessage"] = $"Imagen '{descripcionImagen}' eliminada exitosamente de la galería";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo eliminar la imagen";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar imagen: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index), new { idInmueble });
        }

        // MÉTODO ADICIONAL: Reordenar imágenes (AJAX)
        [HttpPost]
        public async Task<IActionResult> ReordenarImagenes(int idInmueble, [FromBody] Dictionary<int, int> nuevosOrdenes)
        {
            try
            {
                if (!await _repositorioImagen.ExisteInmuebleAsync(idInmueble))
                {
                    return Json(new { success = false, message = "Inmueble no encontrado" });
                }

                bool resultado = await _repositorioImagen.ActualizarOrdenImagenesAsync(idInmueble, nuevosOrdenes);
                
                if (resultado)
                {
                    return Json(new { success = true, message = "Orden actualizado exitosamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al actualizar el orden" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // MÉTODO ADICIONAL: Obtener estadísticas de galería (AJAX)
        [HttpGet]
        public async Task<IActionResult> EstadisticasGaleria(int idInmueble)
        {
            try
            {
                var estadisticas = await _repositorioImagen.ObtenerEstadisticasGaleriaAsync(idInmueble);
                return Json(estadisticas);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // MÉTODO ADICIONAL: Limpiar imágenes huérfanas 
        [HttpPost]
        public async Task<IActionResult> LimpiarImagenesHuerfanas(int idInmueble)
        {
            try
            {
                bool resultado = await _repositorioImagen.LimpiarImagenesHuerfanasAsync(idInmueble, _environment);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Limpieza de archivos huérfanos completada";
                }
                else
                {
                    TempData["WarningMessage"] = "No se pudieron limpiar todos los archivos huérfanos";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error durante la limpieza: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index), new { idInmueble });
        }

        // MÉTODO ADICIONAL: Eliminar todas las imágenes de un inmueble
        [HttpPost]
        public async Task<IActionResult> EliminarTodasLasImagenes(int idInmueble)
        {
            try
            {
                if (!await _repositorioImagen.ExisteInmuebleAsync(idInmueble))
                {
                    TempData["ErrorMessage"] = "Inmueble no encontrado";
                    return RedirectToAction("Index", "Inmuebles");
                }

                int totalImagenes = await _repositorioImagen.ContarImagenesPorInmuebleAsync(idInmueble);
                
                if (totalImagenes == 0)
                {
                    TempData["InfoMessage"] = "No hay imágenes para eliminar";
                    return RedirectToAction(nameof(Index), new { idInmueble });
                }

                bool resultado = await _repositorioImagen.EliminarTodasLasImagenesInmuebleAsync(idInmueble, _environment);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = $"Se eliminaron {totalImagenes} imágenes de la galería exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al eliminar las imágenes de la galería";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar imágenes: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index), new { idInmueble });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class InquilinosController : Controller
    {
        private readonly IRepositorioInquilino _repositorioInquilino;

        public InquilinosController(IRepositorioInquilino repositorioInquilino)
        {
            _repositorioInquilino = repositorioInquilino;
        }

        // GET: Inquilinos
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", int itemsPorPagina = 10)
        {
            try
            {
                var (inquilinos, totalRegistros) = await _repositorioInquilino
                    .ObtenerConPaginacionYBusquedaAsync(pagina, buscar, itemsPorPagina);

                // Calcular información de paginación
                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / itemsPorPagina);
                
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.ITEMS_POR_PAGINA = itemsPorPagina;

                return View(inquilinos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los inquilinos: {ex.Message}";
                return View(new List<Inquilino>());
            }
        }

        // GET: Inquilinos/Create
        public IActionResult Create()
        {
            var inquilino = new Inquilino
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
            return View(inquilino);
        }

        // POST: Inquilinos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inquilino inquilino)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar DNI único (solo si se proporciona)
                    if (!string.IsNullOrEmpty(inquilino.Usuario?.Dni))
                    {
                        bool dniExiste = await _repositorioInquilino.ExisteDniAsync(inquilino.Usuario.Dni);
                        if (dniExiste)
                        {
                            ModelState.AddModelError("Usuario.Dni", "Ya existe un inquilino con este DNI");
                            return View(inquilino);
                        }
                    }

                    // Verificar Email único (si se proporciona)
                    if (!string.IsNullOrEmpty(inquilino.Usuario?.Email))
                    {
                        bool emailExiste = await _repositorioInquilino.ExisteEmailAsync(inquilino.Usuario.Email);
                        if (emailExiste)
                        {
                            ModelState.AddModelError("Usuario.Email", "Ya existe un inquilino con este email");
                            return View(inquilino);
                        }
                    }

                    bool resultado = await _repositorioInquilino.CrearInquilinoConTransaccionAsync(inquilino);
                    
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Inquilino creado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo crear el inquilino. Verifique los datos ingresados.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al crear el inquilino: {ex.Message}");
                }
            }
            
            return View(inquilino);
        }

        // GET: Inquilinos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var inquilino = await _repositorioInquilino.ObtenerInquilinoPorIdAsync(id);
                
                if (inquilino == null)
                {
                    return NotFound();
                }

                return View(inquilino);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inquilino: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inquilinos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inquilino inquilino)
        {
            if (id != inquilino.IdInquilino)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar DNI único (excluyendo el actual por IdUsuario)
                    if (!string.IsNullOrEmpty(inquilino.Usuario?.Dni))
                    {
                        bool dniExiste = await _repositorioInquilino.ExisteDniAsync(inquilino.Usuario.Dni, inquilino.IdUsuario);
                        if (dniExiste)
                        {
                            ModelState.AddModelError("Usuario.Dni", "Ya existe otro inquilino con este DNI");
                            return View(inquilino);
                        }
                    }

                    // Verificar Email único (si se proporciona, excluyendo el actual)
                    if (!string.IsNullOrEmpty(inquilino.Usuario?.Email))
                    {
                        bool emailExiste = await _repositorioInquilino.ExisteEmailAsync(inquilino.Usuario.Email, inquilino.IdUsuario);
                        if (emailExiste)
                        {
                            ModelState.AddModelError("Usuario.Email", "Ya existe otro inquilino con este email");
                            return View(inquilino);
                        }
                    }

                    bool resultado = await _repositorioInquilino.ActualizarInquilinoConTransaccionAsync(inquilino);
                    
                    if (resultado)
                    {
                        TempData["SuccessMessage"] = "Inquilino actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar el inquilino.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el inquilino: {ex.Message}");
                }
            }
            
            return View(inquilino);
        }

        // GET: Inquilinos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var inquilino = await _repositorioInquilino.ObtenerInquilinoPorIdAsync(id);
                
                if (inquilino == null)
                {
                    return NotFound();
                }

                return View(inquilino);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inquilino: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inquilinos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool resultado = await _repositorioInquilino.EliminarInquilinoConTransaccionAsync(id);
                
                if (resultado)
                {
                    TempData["SuccessMessage"] = "Inquilino eliminado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo eliminar el inquilino";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el inquilino: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
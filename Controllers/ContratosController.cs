using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Services;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class ContratosController : Controller
    {
        private readonly IRepositorioContrato _repositorioContrato;
        private const int ITEMS_POR_PAGINA = 10;

        public ContratosController(IRepositorioContrato repositorioContrato)
        {
            _repositorioContrato = repositorioContrato;
        }

        // GET: Contratos - Index con paginación
        public async Task<IActionResult> Index(int pagina = 1, string buscar = "", string estado = "")
        {
            try
            {
                // repositorio directamente para paginación y búsqueda
                var (contratos, totalRegistros) = await _repositorioContrato
                    .ObtenerConPaginacionYBusquedaAsync(pagina, buscar, estado, ITEMS_POR_PAGINA);

                // Calcular información de paginación
                var totalPaginas = (int)Math.Ceiling((double)totalRegistros / ITEMS_POR_PAGINA);

                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;

                return View(contratos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los contratos: {ex.Message}";
                return View(new List<Contrato>());
            }
        }

        // GET: Contratos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var contrato = await _repositorioContrato.ObtenerContratoConDetallesAsync(id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Contratos/Create
        public IActionResult Create()
        {
            try
            {
                return View(new Contrato());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar formulario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Contratos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contrato contrato)
        {
            // Validaciones adicionales de negocio
            if (contrato.FechaFin <= contrato.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");
            }

            if (contrato.MontoMensual <= 0)
            {
                ModelState.AddModelError("MontoMensual", "El monto debe ser mayor a 0");
            }

            if (contrato.FechaInicio < DateTime.Today)
            {
                ModelState.AddModelError("FechaInicio", "La fecha de inicio no puede ser anterior a hoy");
            }

            // Verificar existencia de registros relacionados
            if (contrato.IdInmueble > 0)
            {
                if (!await _repositorioContrato.ExisteInmuebleDisponibleAsync(contrato.IdInmueble, contrato.FechaInicio, contrato.FechaFin))
                {
                    ModelState.AddModelError("IdInmueble", "El inmueble no está disponible en las fechas seleccionadas");
                }
            }

            if (contrato.IdInquilino > 0 && !await _repositorioContrato.ExisteInquilinoActivoAsync(contrato.IdInquilino))
            {
                ModelState.AddModelError("IdInquilino", "El inquilino seleccionado no existe o no está activo");
            }

            if (contrato.IdPropietario > 0 && !await _repositorioContrato.ExistePropietarioActivoAsync(contrato.IdPropietario))
            {
                ModelState.AddModelError("IdPropietario", "El propietario seleccionado no existe o no está activo");
            }

            if (contrato.IdUsuarioCreador > 0 && !await _repositorioContrato.ExisteUsuarioActivoAsync(contrato.IdUsuarioCreador))
            {
                ModelState.AddModelError("IdUsuarioCreador", "El usuario seleccionado no existe o no está activo");
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewDataAsync();
                return View(contrato);
            }

            try
            {
                var resultado = await _repositorioContrato.CrearContratoAsync(contrato);

                if (resultado)
                {
                    TempData["SuccessMessage"] = "Contrato creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Error al crear el contrato. Verifique que el inmueble esté disponible.");
                    await PopulateViewDataAsync();
                    return View(contrato);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear contrato: {ex.Message}");
                await PopulateViewDataAsync();
                return View(contrato);
            }
        }

        // GET: Contratos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var contrato = await _repositorioContrato.ObtenerContratoConDetallesAsync(id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateViewDataAsync();
                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Contratos/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Contrato contrato)
        {
            if (id != contrato.IdContrato)
            {
                return NotFound();
            }

            // Validaciones adicionales de negocio
            if (contrato.FechaFin <= contrato.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");
            }

            if (contrato.MontoMensual <= 0)
            {
                ModelState.AddModelError("MontoMensual", "El monto debe ser mayor a 0");
            }

            // Verificar disponibilidad del inmueble (excluyendo el contrato actual)
            if (contrato.IdInmueble > 0)
            {
                if (!await _repositorioContrato.ExisteInmuebleDisponibleAsync(
                    contrato.IdInmueble, contrato.FechaInicio, contrato.FechaFin, contrato.IdContrato))
                {
                    ModelState.AddModelError("IdInmueble", "El inmueble no está disponible en las fechas seleccionadas");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewDataAsync();
                return View(contrato);
            }

            try
            {
                var resultado = await _repositorioContrato.ActualizarContratoAsync(contrato);

                if (resultado)
                {
                    TempData["SuccessMessage"] = "Contrato actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el contrato");
                    await PopulateViewDataAsync();
                    return View(contrato);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar contrato: {ex.Message}");
                await PopulateViewDataAsync();
                return View(contrato);
            }
        }

        // GET: Contratos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var contrato = await _repositorioContrato.ObtenerContratoConDetallesAsync(id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(contrato);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Contratos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var resultado = await _repositorioContrato.EliminarContratoAsync(id);

                if (resultado)
                {
                    TempData["SuccessMessage"] = "Contrato finalizado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al finalizar el contrato";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al finalizar contrato: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // MÉTODOS AUXILIARES PARA LLENAR LOS DROPDOWNS

        private async Task PopulateViewDataAsync()
        {
            ViewData["Inmuebles"] = await _repositorioContrato.ObtenerInmueblesDisponiblesAsync();
            ViewData["Inquilinos"] = await _repositorioContrato.ObtenerInquilinosActivosAsync();
            ViewData["Propietarios"] = await _repositorioContrato.ObtenerPropietariosActivosAsync();
            ViewData["Usuarios"] = await _repositorioContrato.ObtenerUsuariosActivosAsync();
        }


        // NUEVOS ENDPOINTS PARA AUTOCOMPLETADO EN CREATE

        [HttpGet]
        public async Task<IActionResult> BuscarInmueblesParaContrato(string termino, int limite = 10)
        {
            try
            {
                // Validar término de búsqueda mínimo
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 3)
                {
                    return Json(new { error = "Debe ingresar al menos 3 caracteres para buscar" });
                }

                var resultados = await _repositorioContrato.BuscarInmueblesParaContratoAsync(termino, limite);
                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error al buscar inmuebles: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPropietariosParaContrato(string termino, int limite = 10)
        {
            try
            {
                // Validar término de búsqueda mínimo
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 3)
                {
                    return Json(new { error = "Debe ingresar al menos 3 caracteres para buscar" });
                }

                var resultados = await _repositorioContrato.BuscarPropietariosParaContratoAsync(termino, limite);
                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error al buscar propietarios: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarInquilinosParaContrato(string termino, int limite = 10)
        {
            try
            {
                // Validar término de búsqueda mínimo
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 3)
                {
                    return Json(new { error = "Debe ingresar al menos 3 caracteres para buscar" });
                }

                var resultados = await _repositorioContrato.BuscarInquilinosParaContratoAsync(termino, limite);
                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error al buscar inquilinos: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerInmueblesPorPropietario(int propietarioId, int limite = 15)
        {
            try
            {
                if (propietarioId <= 0)
                {
                    return Json(new { error = "ID de propietario inválido" });
                }

                var resultados = await _repositorioContrato.ObtenerInmueblesPorPropietarioAsync(propietarioId, limite);
                return Json(new { success = true, data = resultados });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error al obtener inmuebles del propietario: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPropietarioDeInmueble(int inmuebleId)
        {
            try
            {
                if (inmuebleId <= 0)
                {
                    return Json(new { error = "ID de inmueble inválido" });
                }

                var resultado = await _repositorioContrato.ObtenerPropietarioDeInmuebleAsync(inmuebleId);

                if (resultado == null)
                {
                    return Json(new { error = "No se encontró el propietario del inmueble" });
                }

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error al obtener propietario del inmueble: {ex.Message}" });
            }
        }
    }
}
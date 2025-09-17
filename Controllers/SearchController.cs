
using Microsoft.AspNetCore.Mvc;
using Inmobiliaria_troncoso_leandro.Services;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        //  Búsqueda unificada de usuarios
        [HttpGet("usuarios")]
        public async Task<IActionResult> BuscarUsuarios([FromQuery] string termino, [FromQuery] int limite = 20)
        {
            try
            {
                var resultados = await _searchService.BuscarUsuariosAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        // Mantener endpoints específicos para compatibilidad
        [HttpGet("propietarios")]
        public async Task<IActionResult> BuscarPropietarios([FromQuery] string termino, [FromQuery] int limite = 20)
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

           
            //  Búsqueda específica de inmuebles para contratos
            [HttpGet("inmuebleEspecifico")]
            public async Task<IActionResult> BuscarInmueblesEspecifico(string termino, int limite = 20, int? propietarioId = null)
            {
                try
                {
                    var resultados = await _searchService.BuscarInmueblesAsync(termino, limite, propietarioId);
                    return Json(resultados);
                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message });
                }
            }

            // Obtener propietario de un inmueble específico
            [HttpGet("inmuebles/{idInmueble:int}/propietario")]
            public async Task<IActionResult> ObtenerPropietarioDelInmueble(int idInmueble)
            {
                try
                {
                    var propietario = await _searchService.ObtenerPropietarioDelInmuebleAsync(idInmueble);

                    if (propietario == null)
                    {
                        return Json(new { error = "No se encontró propietario para este inmueble" });
                    }

                    return Json(propietario);
                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message });
                }
            }



            // TIPOS DE INMUEBLES
            [HttpGet("tipos-inmuebles")]
            public async Task<IActionResult> BuscarTiposInmuebles(string termino, int limite = 20)
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


            // INTERESES INMUEBLES
            [HttpGet("intereses-inmuebles")]
            public async Task<IActionResult> BuscarInteresesInmuebles(string termino, int limite = 20)
            {
                try
                {
                    var resultados = await _searchService.BuscarInteresesInmueblesAsync(termino, limite);
                    return Json(resultados);
                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message });
                }
            }


            // CONTRATOS
            [HttpGet("contratos")]
            public async Task<IActionResult> BuscarContratos(string termino, int limite = 20)
            {
                try
                {
                    var resultados = await _searchService.BuscarContratosAsync(termino, limite);
                    return Json(resultados);
                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message });
                }
            }



        }
}

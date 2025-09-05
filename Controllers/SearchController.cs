
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

        [HttpGet("usuarios/paginado")]
        public async Task<IActionResult> BuscarUsuariosPaginado(
            [FromQuery] string termino,
            [FromQuery] int pagina = 1,
            [FromQuery] int porPagina = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarUsuariosPaginadoAsync(termino, pagina, porPagina);
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

        [HttpGet("inquilinos")]
        public async Task<IActionResult> BuscarInquilinos([FromQuery] string termino, [FromQuery] int limite = 20)
        {
            try
            {
                var resultados = await _searchService.BuscarInquilinosAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("propietarios/paginado")]
        public async Task<IActionResult> BuscarPropietariosPaginado(
            [FromQuery] string termino,
            [FromQuery] int pagina = 1,
            [FromQuery] int porPagina = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarPropietariosPaginadoAsync(termino, pagina, porPagina);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("inquilinos/paginado")]
        public async Task<IActionResult> BuscarInquilinosPaginado(
            [FromQuery] string termino,
            [FromQuery] int pagina = 1,
            [FromQuery] int porPagina = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarInquilinosPaginadoAsync(termino, pagina, porPagina);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        // INMUEBLES
[HttpGet("inmuebles")]
public async Task<IActionResult> BuscarInmuebles(string termino, int limite = 20)
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

[HttpGet("inmuebles/paginado")]
public async Task<IActionResult> BuscarInmueblesPaginado(string termino, int pagina = 1, int porPagina = 10)
{
    try
    {
        var resultado = await _searchService.BuscarInmueblesPaginadoAsync(termino, pagina, porPagina);
        return Json(resultado);
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

[HttpGet("tipos-inmuebles/paginado")]
public async Task<IActionResult> BuscarTiposInmueblesPaginado(string termino, int pagina = 1, int porPagina = 10)
{
    try
    {
        var resultado = await _searchService.BuscarTiposInmueblesPaginadoAsync(termino, pagina, porPagina);
        return Json(resultado);
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

[HttpGet("intereses-inmuebles/paginado")]
public async Task<IActionResult> BuscarInteresesInmueblesPaginado(string termino, int pagina = 1, int porPagina = 10)
{
    try
    {
        var resultado = await _searchService.BuscarInteresesInmueblesPaginadoAsync(termino, pagina, porPagina);
        return Json(resultado);
    }
    catch (Exception ex)
    {
        return Json(new { error = ex.Message });
    }
}
    }
}
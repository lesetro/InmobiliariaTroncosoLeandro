using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Services
{
    public interface ISearchService
    {
        // USUARIOS - Métodos existentes
        Task<List<SearchResult>> BuscarUsuariosAsync(string termino, int limite = 20);
        Task<SearchPaginatedResult> BuscarUsuariosPaginadoAsync(string termino, int pagina, int porPagina);
        
        // PROPIETARIOS - Métodos existentes
        Task<List<SearchResult>> BuscarPropietariosAsync(string termino, int limite = 20);
        Task<SearchPaginatedResult> BuscarPropietariosPaginadoAsync(string termino, int pagina, int porPagina);
        
        // INQUILINOS - Métodos existentes
        Task<List<SearchResult>> BuscarInquilinosAsync(string termino, int limite = 20);
        Task<SearchPaginatedResult> BuscarInquilinosPaginadoAsync(string termino, int pagina, int porPagina);
        
        
        // INMUEBLES - Nuevos métodos
        Task<List<SearchResult>> BuscarInmueblesAsync(string termino, int limite = 20);
        Task<SearchPaginatedResult> BuscarInmueblesPaginadoAsync(string termino, int pagina, int porPagina);
        
        // TIPOS DE INMUEBLES - Nuevos métodos
        Task<List<SearchResult>> BuscarTiposInmueblesAsync(string termino, int limite = 20);
        Task<SearchPaginatedResult> BuscarTiposInmueblesPaginadoAsync(string termino, int pagina, int porPagina);
        
        // INTERESES INMUEBLES - Nuevos métodos
        Task<List<SearchResult>> BuscarInteresesInmueblesAsync(string termino, int limite = 20);
        Task<SearchPaginatedResult> BuscarInteresesInmueblesPaginadoAsync(string termino, int pagina, int porPagina);
    }
}
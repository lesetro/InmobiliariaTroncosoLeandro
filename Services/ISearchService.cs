using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Services
{
    public interface ISearchService
    {

        Task<List<SearchResult>> BuscarUsuariosAsync(string termino, int limite = 20);
        Task<List<SearchResult>> BuscarPropietariosAsync(string termino, int limite = 20);
        Task<List<SearchResult>> BuscarInquilinosAsync(string termino, int limite = 20);
        Task<List<SearchResult>> BuscarInmueblesAsync(string termino, int limite = 20);
        Task<List<SearchResult>> BuscarTiposInmueblesAsync(string termino, int limite = 20);
        Task<List<SearchResult>> BuscarInteresesInmueblesAsync(string termino, int limite = 20);
        Task<List<SearchResult>> BuscarContratosAsync(string termino, int limite = 20);

        //para los contrato que busque de forma especifica
        Task<List<SearchResult>> BuscarInmueblesAsync(string termino, int limite = 20, int? propietarioId = null);


        Task<SearchResult?> ObtenerPropietarioDelInmuebleAsync(int idInmueble);
        
       

        
        



        
    }
}
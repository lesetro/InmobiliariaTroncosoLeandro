using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioTipoInmueble
    {
        // Métodos CRUD básicos
        Task<bool> CrearTipoInmuebleAsync(TipoInmueble tipoInmueble);
        Task<bool> ActualizarTipoInmuebleAsync(TipoInmueble tipoInmueble);
        Task<bool> EliminarTipoInmuebleAsync(int id);
        Task<TipoInmueble?> ObtenerTipoInmueblePorIdAsync(int id);
        Task<TipoInmueble?> ObtenerTipoInmuebleConDetallesAsync(int id);
        
        // Métodos de validación
        Task<bool> ExisteNombreAsync(string nombre, int idExcluir = 0);
        Task<int> ContarInmueblesAsociadosAsync(int idTipoInmueble);
        
        // Método para Index con paginación y búsqueda
        Task<(IList<TipoInmueble> tiposInmueble, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);
        
        // Métodos auxiliares para vistas
        Task<IList<TipoInmueble>> ObtenerTiposInmuebleActivosAsync();
    }
}
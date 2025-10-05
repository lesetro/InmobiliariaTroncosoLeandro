using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioInteresInmueble
    {
        // Métodos de consulta
        Task<InteresInmueble?> ObtenerInteresPorIdAsync(int id);
        Task<InteresInmueble?> ObtenerInteresConDetallesAsync(int id);

        // Método para Index con paginación y búsqueda
        Task<(IList<InteresInmueble> intereses, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, int? idInmueble, DateTime? fechaDesde, DateTime? fechaHasta, int itemsPorPagina);

        // Métodos de gestión de contactos
        Task<bool> MarcarComoContactadoAsync(int idInteres, string? observaciones = null);
        Task<bool> DesmarcarContactadoAsync(int idInteres);
        Task<bool> ActualizarObservacionesAsync(int idInteres, string observaciones);
        //Metodos para el administrador 
        Task<int> GetTotalInteresesAsync();
        Task<IEnumerable<InteresInmueble>> GetInteresesPendientesAsync();
        Task<IEnumerable<InteresInmueble>> GetInteresesRecientesAsync();
        Task<int> GetInteresesPendientesCountAsync();
        Task<int> GetInteresesHoyAsync();
        Task<int> GetInteresesSemanaAsync();
        // Métodos estadísticos
        Task<int> ContarInteresesPendientesAsync();
        Task<int> ContarInteresesContactadosAsync();
        Task<int> ContarInteresesPorInmuebleAsync(int idInmueble);
        Task<IList<InteresInmueble>> ObtenerInteresesRecientesAsync(int cantidad = 5);

        // Métodos auxiliares para vistas
        Task<IList<Inmueble>> ObtenerInmueblesConInteresesAsync();

        // Dashboard/Estadísticas
        Task<Dictionary<string, int>> ObtenerEstadisticasInteresesAsync();
        Task<IList<InteresInmueble>> ObtenerInteresesUrgentesAsync(); // Más de X días sin contactar
        
        // Método para crear un nuevo interés
        Task<bool> CrearInteresAsync(InteresInmueble nuevoInteres);
    }
}
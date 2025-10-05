using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioContacto
    {
        Task<bool> CrearContactoAsync(Contacto contacto);
        Task<Contacto?> ObtenerContactoPorIdAsync(int id);
        Task<(IList<Contacto> contactos, int totalRegistros)> ObtenerContactosConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);
        Task<bool> ActualizarEstadoContactoAsync(int id, string nuevoEstado);
        Task<bool> EliminarContactoAsync(int id);
        Task<IList<Contacto>> ObtenerContactosRecientesAsync(int cantidad = 5);
        Task<Dictionary<string, int>> ObtenerEstadisticasContactosAsync();
    }
}
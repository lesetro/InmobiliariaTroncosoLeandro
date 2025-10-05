using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioInquilino
    {
        // Métodos CRUD básicos
        Task<bool> CrearInquilinoConTransaccionAsync(Inquilino inquilino);
        Task<bool> ActualizarInquilinoConTransaccionAsync(Inquilino inquilino);
        Task<bool> EliminarInquilinoConTransaccionAsync(int id);
        Task<Inquilino?> ObtenerInquilinoPorIdAsync(int id);
        
        // Métodos auxiliares de validación - NULLABLE
        Task<bool> ExisteDniAsync(string? dni, int idExcluir = 0);
        Task<bool> ExisteEmailAsync(string? email, int idExcluir = 0);
        
        // Método para Index con paginación y búsqueda completa
        Task<(IList<Inquilino> inquilinos, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, int itemsPorPagina,string estadoFiltro = "activos");
    }
}
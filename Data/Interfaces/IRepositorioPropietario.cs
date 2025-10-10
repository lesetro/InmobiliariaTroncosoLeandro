using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioPropietario
    {
        // Métodos CRUD básicos
        Task<bool> CrearPropietarioConTransaccionAsync(Propietario propietario);
        Task<bool> ActualizarPropietarioConTransaccionAsync(Propietario propietario);
        Task<bool> EliminarPropietarioConTransaccionAsync(int id);
        Task<Propietario?> ObtenerPropietarioPorIdAsync(int id);

        // Métodos auxiliares de validación - NULLABLE
        Task<bool> ExisteDniAsync(string? dni, int idExcluir = 0);
        Task<bool> ExisteEmailAsync(string? email, int idExcluir = 0);

        // Método para Index con paginación y búsqueda completa
        Task<(IList<Propietario> propietarios, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, int itemsPorPagina, string estadoFiltro = "activos");
        Task<IEnumerable<Propietario>> ObtenerTodosAsync();
        //Propietario
        Task<IList<Pago>> ObtenerPagosPorPropietarioAsync(int propietarioId, DateTime? fechaInicio = null, DateTime? fechaFin = null);
        Task<IList<Contrato>> ObtenerContratosPorPropietarioAsync(int propietarioId);
        Task<IEnumerable<Inmueble>> ObtenerInmueblesPorPropietarioAsync(int propietarioId);

        Task<Propietario?> GetByIdAsync(int id);

        Task<Inquilino?> ObtenerInquilinoPorIdAsync(int idInquilino);
        Task<Propietario?> ObtenerPorUsuarioIdAsync(int usuarioId);
        Task<int> ObtenerIdPropietarioPorUsuarioAsync(int idUsuario);

        
    }
}
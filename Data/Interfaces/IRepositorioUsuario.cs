using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioUsuario
    {
        // CRUD básico
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(Usuario usuario);
        Task<bool> DeleteAsync(int id);

        // Métodos específicos para autenticación
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> ValidateUserAsync(string email, string password);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> DniExistsAsync(string dni);

        // Métodos específicos para autorización por roles
        Task<IEnumerable<Usuario>> GetByRolAsync(string rol);
        Task<IEnumerable<Usuario>> GetAdministradoresAsync();
        Task<IEnumerable<Usuario>> GetEmpleadosAsync();
        Task<IEnumerable<Usuario>> GetPropietariosAsync();
        Task<IEnumerable<Usuario>> GetInquilinosAsync();
        Task<IEnumerable<Usuario>> GetUsuariosActivosAsync();
        Task<IEnumerable<Usuario>> GetUsuariosInactivosAsync();
        Task<int> GetNumeroUsuariosActivosAsync();

        // Métodos para gestión de estado
        Task<bool> ActivarUsuarioAsync(int id);
        Task<bool> DesactivarUsuarioAsync(int id);
        Task<bool> CambiarRolAsync(int id, string nuevoRol);

        // Métodos para gestión de contraseñas
        Task<bool> CambiarPasswordAsync(int id, string nuevaPassword);
        Task<bool> ResetearPasswordAsync(int id);

        // Métodos para búsqueda y filtrado
        Task<IEnumerable<Usuario>> BuscarUsuariosAsync(string termino);
        Task<IEnumerable<Usuario>> GetUsuariosPaginadosAsync(int pagina, int registrosPorPagina);
        Task<int> GetTotalUsuariosAsync();
        Task<int> GetTotalUsuariosPorRolAsync(string rol);

        // Métodos de validación
        Task<bool> PuedeEliminarUsuarioAsync(int id);
        Task<bool> TienePermisosAdminAsync(int id);

        // Métodos para estadísticas
        Task<Dictionary<string, int>> GetEstadisticasPorRolAsync();
        Task<IEnumerable<Usuario>> GetUsuariosRecientesAsync(int cantidad = 5);

        //  MÉTODO PRINCIPAL DE PAGINACIÓN (UNIFICADO)
        /// <summary>
        /// Obtiene usuarios con paginación, búsqueda, filtro por rol y estado
        /// </summary>
        Task<(IList<Usuario> usuarios, int totalRegistros)> ObtenerConPaginacionBusquedaYRolAsync(
            int pagina,
            string buscar,
            string rol,
            int itemsPorPagina,
            string estadoFiltro = "activos");

        // Otros métodos útiles
        Task<int> GetTotalUsuariosInactivosAsync();
        Task<Usuario?> GetUsuarioPorPropietarioIdAsync(int idPropietario);
        
    }
}
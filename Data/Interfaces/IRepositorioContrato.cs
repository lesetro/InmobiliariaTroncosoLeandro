using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioContrato
    {
        // ========================================
        // MÉTODOS CRUD BÁSICOS
        // ========================================

        Task<bool> CrearContratoAsync(Contrato contrato);
        Task<bool> ActualizarContratoAsync(Contrato contrato);
        Task<bool> EliminarContratoAsync(int id);
        Task<Contrato?> ObtenerContratoPorIdAsync(int id);
        Task<Contrato?> ObtenerContratoConDetallesAsync(int id);

        // ========================================
        // PAGINACIÓN Y BÚSQUEDA PARA INDEX
        // ========================================

        Task<(IList<Contrato> contratos, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, string tipoContrato, int itemsPorPagina);

        // ========================================
        // MÉTODOS DE VALIDACIÓN ESPECÍFICOS DE NEGOCIO
        // ========================================

        Task<bool> ExisteInmuebleDisponibleAsync(int idInmueble, DateTime fechaInicio, DateTime fechaFin, int idContratoExcluir = 0);
        Task<bool> ExisteInquilinoActivoAsync(int idInquilino);
        Task<bool> ExistePropietarioActivoAsync(int idPropietario);
        Task<bool> ExisteUsuarioActivoAsync(int idUsuario);

        // ========================================
        // MÉTODOS AUXILIARES PARA DROPDOWNS
        // ========================================

        Task<List<Inmueble>> ObtenerInmueblesDisponiblesAsync();
        Task<List<Inquilino>> ObtenerInquilinosActivosAsync();
        Task<List<Propietario>> ObtenerPropietariosActivosAsync();
        Task<List<Usuario>> ObtenerUsuariosActivosAsync();

        // MÉTODOS DE CONSULTA ESPECÍFICOS
        Task<List<Inmueble>> ObtenerInmueblesAsync(bool soloDisponibles = true);

        /// Verifica si un inmueble tiene contratos vigentes
        Task<bool> InmuebleTieneContratosVigentesAsync(int idInmueble);

        /// Obtiene los contratos vigentes de un inquilino
        Task<List<Contrato>> ObtenerContratosVigentesPorInquilinoAsync(int idInquilino);


        /// Obtiene los contratos de un propietario
        Task<List<Contrato>> ObtenerContratosPorPropietarioAsync(int idPropietario);

        // Métodos específicos para autocompletado en contratos
        Task<List<dynamic>> BuscarInmueblesParaContratoAsync(string termino, int limite = 10);
        Task<List<dynamic>> BuscarPropietariosParaContratoAsync(string termino, int limite = 10);
        Task<List<dynamic>> BuscarInquilinosParaContratoAsync(string termino, int limite = 10);

        // Métodos de relación para filtrado dinámico
        Task<List<dynamic>> ObtenerInmueblesPorPropietarioAsync(int propietarioId, int limite = 15);
        Task<dynamic?> ObtenerPropietarioDeInmuebleAsync(int inmuebleId);
        Task<bool> TieneContratosActivosAsync(int idInmueble);

        
    }
}
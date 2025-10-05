using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioEmpleado
    {
        // DASHBOARD
        Task<EmpleadoDashboardDto> GetDashboardEmpleadoDataAsync();

        // USUARIOS
        Task<(IList<Usuario> usuarios, int totalRegistros)> ObtenerUsuariosParaEmpleadoAsync(
            int pagina, string buscar, string rol, int itemsPorPagina);
        Task<Usuario> CrearPropietarioAsync(Usuario propietario);
        Task<Usuario> CrearInquilinoAsync(Usuario inquilino);
        Task<Usuario?> ObtenerEmpleadoPorIdAsync(int id);

        // INMUEBLES
        Task<(IList<Inmueble> inmuebles, int totalRegistros)> ObtenerInmueblesConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);
        Task<bool> CrearInmuebleAsync(Inmueble inmueble, IFormFile? archivoPortada, IWebHostEnvironment environment);
        Task<Inmueble?> ObtenerInmuebleConDetallesAsync(int id);
        Task<bool> AgregarFotoInmuebleAsync(int idInmueble, IFormFile archivo, IWebHostEnvironment environment);

        // CONTRATOS
        Task<(IList<Contrato> contratos, int totalRegistros)> ObtenerContratosConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);
        Task<bool> CrearContratoAsync(Contrato contrato);

        // INTERESES
        Task<(IList<InteresInmueble> intereses, int totalRegistros)> ObtenerInteresesConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);
        Task<bool> MarcarInteresContactadoAsync(int idInteres);

        // DROPDOWNS Y LISTAS
        Task<IList<Propietario>> ObtenerPropietariosActivosAsync();
        Task<IList<TipoInmueble>> ObtenerTiposInmuebleAsync();
        Task<IList<Inmueble>> ObtenerInmueblesDisponiblesAsync();
        Task<IList<Inquilino>> ObtenerInquilinosAsync();
    }

    // DTO simple para dashboard
    public class EmpleadoDashboardDto
    {
        public int TotalUsuarios { get; set; }
        public int TotalInmuebles { get; set; }
        public int TotalContratos { get; set; }
        public int TotalIntereses { get; set; }
    }
}
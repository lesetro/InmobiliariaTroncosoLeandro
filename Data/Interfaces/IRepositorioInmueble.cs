using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioInmueble
    {
        // Métodos CRUD básicos
        Task<bool> CrearInmuebleAsync(Inmueble inmueble);
        Task<bool> ActualizarInmuebleAsync(Inmueble inmueble);
        Task<bool> EliminarInmuebleAsync(int id);
        Task<Inmueble?> ObtenerInmueblePorIdAsync(int id);
        Task<Inmueble?> ObtenerInmuebleConDetallesAsync(int id);

        // NUEVOS MÉTODOS - Gestión de portada
        Task<bool> CrearInmuebleConPortadaAsync(Inmueble inmueble, IWebHostEnvironment environment);
        Task<bool> ActualizarInmuebleConPortadaAsync(Inmueble inmueble, IWebHostEnvironment environment);
        Task<bool> ActualizarSoloPortadaAsync(int idInmueble, IFormFile archivoPortada, IWebHostEnvironment environment);
        Task<bool> EliminarPortadaAsync(int idInmueble, IWebHostEnvironment environment);

        // Gestión de archivos de portada
        Task<string> GuardarArchivoPortadaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment);
        Task<bool> EliminarArchivoPortadaAsync(string? rutaArchivo, IWebHostEnvironment environment);
        Task<bool> ValidarArchivoPortadaAsync(IFormFile archivo);

        // Validaciones básicas existentes
        Task<bool> ExisteDireccionAsync(string direccion, int idExcluir = 0);
        Task<int> ContarContratosVigentesAsync(int idInmueble);

        // VALIDACIONES DE NEGOCIO - Coordenadas
        Task<bool> ExisteCoordenadasAsync(string coordenadas, int idExcluir = 0);
        Task<bool> PropietarioTieneInmuebleEnCoordenadasAsync(int idPropietario, string coordenadas, int idExcluir = 0);
        Task<Inmueble?> ObtenerInmueblePorCoordenadasAsync(string coordenadas, int idExcluir = 0);

        // VALIDACIONES DE NEGOCIO - Propietario
        Task<bool> PropietarioTieneOtrosInmueblesAsync(int idPropietario, int idInmuebleExcluir = 0);
        Task<bool> PuedeAsignarInmuebleAPropietarioAsync(int idInmueble, int idPropietario);

        // Método para Index con paginación y búsqueda
        Task<(IList<Inmueble> inmuebles, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);

        // Métodos auxiliares para vistas
        Task<IList<Propietario>> ObtenerPropietariosActivosAsync();
        Task<IList<TipoInmueble>> ObtenerTiposInmuebleActivosAsync();

        // NUEVOS MÉTODOS - Consultas con imágenes
        Task<Inmueble?> ObtenerInmuebleConGaleriaAsync(int id);
        Task<IList<Inmueble>> ObtenerInmueblesConPortadaAsync(int limite = 20);
        Task<IList<Inmueble>> ObtenerInmueblesSinPortadaAsync();
        Task<Dictionary<string, object>> ObtenerEstadisticasImagenesInmuebleAsync(int idInmueble);

        // Metodos que voy a usar en ContratoVenta
        Task<bool> VerificarDisponibilidadParaVenta(int idInmueble);
        Task<IEnumerable<Inmueble>> ObtenerDisponiblesParaVentaAsync();
        Task<IEnumerable<Inmueble>> BuscarParaVentaAsync(string termino, int limite = 10);
        Task<IEnumerable<Inmueble>> ObtenerPorPropietarioAsync(int propietarioId);
        Task<IList<Inmueble>> ObtenerPropiedadesDestacadasAsync(int limite = 6);

        //home 
        Task<List<Inmueble>> ObtenerCatalogoPublicoAsync(string? buscar = null, string? tipo = null,
        string? precio = null, string? ambientes = null, int pagina = 1, int itemsPorPagina = 12);

        Task<int> ObtenerTotalCatalogoAsync(string? buscar = null, string? tipo = null,
        string? precio = null, string? ambientes = null);
        //Inquilino 

        Task<Inmueble?> GetByIdAsync(int id);
        //gestionar imagenes del propietario

        Task<bool> EliminarImagenAsync(int idImagen);
        Task<ImagenInmueble> ObtenerImagenPorIdAsync(int idImagen);
        Task<int> GuardarImagenGaleriaAsync(int idInmueble, string urlImagen);
        Task<Inmueble> ObtenerPorIdAsync(int idInmueble);
        Task<bool> ActualizarPortadaAsync(int idInmueble, string urlPortada);

        //Eliminar inmueble del propietario
        Task<Inmueble> ObtenerPorIdConImagenesAsync(int id);
        Task<bool> EliminarLogicamenteAsync(int id);
        Task<bool> EliminarImagenesPorInmuebleAsync(int idInmueble);
        
        
        
    }
}
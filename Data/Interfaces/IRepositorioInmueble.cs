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
    }
}
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioImagen
    {
        // ===== MÉTODOS CRUD BÁSICOS =====
        Task<bool> CrearImagenAsync(ImagenInmueble imagen);
        Task<bool> ActualizarImagenAsync(ImagenInmueble imagen);
        Task<bool> EliminarImagenAsync(int id);
        Task<ImagenInmueble?> ObtenerImagenPorIdAsync(int id);

        // ===== MÉTODOS ESPECÍFICOS PARA GALERÍA =====
        Task<IList<ImagenInmueble>> ObtenerImagenesPorInmuebleAsync(int idInmueble);
        Task<bool> CrearImagenConArchivoAsync(ImagenInmueble imagen, IFormFile archivo, IWebHostEnvironment environment);
        Task<bool> ActualizarImagenConArchivoAsync(ImagenInmueble imagen, IFormFile? archivo, IWebHostEnvironment environment);

        // ===== MÉTODOS DE ORDEN Y CONTEO =====
        Task<int> ContarImagenesPorInmuebleAsync(int idInmueble);
        Task<int> ObtenerSiguienteOrdenAsync(int idInmueble);
        Task<bool> ActualizarOrdenImagenesAsync(int idInmueble, Dictionary<int, int> nuevosOrdenes);
        Task<bool> ReorganizarOrdenDespuesDeEliminarAsync(int idInmueble, int ordenEliminado);

        // ===== VALIDACIONES DE NEGOCIO =====
        Task<bool> ExisteImagenAsync(int id);
        Task<bool> ExisteInmuebleAsync(int idInmueble);
        Task<bool> PuedeEliminarImagenAsync(int id);

        // ===== GESTIÓN DE ARCHIVOS FÍSICOS =====
        Task<string> GuardarArchivoGaleriaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment);
        Task<bool> EliminarArchivoGaleriaAsync(string rutaArchivo, IWebHostEnvironment environment);
        Task<bool> EliminarTodasLasImagenesInmuebleAsync(int idInmueble, IWebHostEnvironment environment);
        Task<bool> ValidarArchivoImagenAsync(IFormFile archivo);

        // ===== MÉTODOS DE UTILIDAD =====
        Task<bool> LimpiarImagenesHuerfanasAsync(int idInmueble, IWebHostEnvironment environment);
        Task<Dictionary<string, object>> ObtenerEstadisticasGaleriaAsync(int idInmueble);
        Task<bool> ExistenImagenesParaInmuebleAsync(int idInmueble);
    }
}
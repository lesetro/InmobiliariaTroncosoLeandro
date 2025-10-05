using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorioAdmin
    {
        // Dashboard principal
        Task<AdminDashboardDto> GetDashboardDataAsync();
        
        // Estadísticas generales
        Task<int> GetTotalUsuariosAsync();
        Task<Dictionary<string, int>> GetUsuariosPorRolAsync();
        Task<int> GetTotalPropietariosActivosAsync();
        Task<int> GetTotalInquilinosActivosAsync();
        
        // Estadísticas de inmuebles
        Task<int> GetTotalInmueblesAsync();
        Task<int> GetInmueblesDisponiblesAsync();
        Task<int> GetInmueblesOcupadosAsync();
        
        // Estadísticas de contratos
        Task<int> GetTotalContratosAsync();
        Task<int> GetContratosVigentesAsync();
        Task<int> GetContratosFinalizadosAsync();
        
        // Estadísticas de intereses
        Task<int> GetTotalInteresesAsync();
        Task<int> GetInteresesPendientesAsync();
        Task<int> GetInteresesContactadosAsync();
        Task<int> GetInteresesRecientesAsync(); // Últimos 7 días
        
        // Datos recientes para dashboard
        Task<IEnumerable<ContratoRecenteDto>> GetContratosRecientesAsync(int limite = 5);
        Task<IEnumerable<InteresRecenteDto>> GetInteresesRecientesAsync(int limite = 5);
        Task<IEnumerable<UsuarioRecenteDto>> GetUsuariosRecientesAsync(int limite = 5);
        
        // Métricas por período
        Task<Dictionary<string, int>> GetInteresesPorMesAsync(int año);
        Task<Dictionary<string, int>> GetContratosPorMesAsync(int año);
        Task<Dictionary<string, decimal>> GetIngresosPorMesAsync(int año);
        
        // Alertas y notificaciones
        Task<int> GetContratosProximosVencerAsync(int dias = 30);
        Task<int> GetPagosVencidosAsync();
        Task<IEnumerable<AlertaDto>> GetAlertasDelSistemaAsync();
        
        // Reportes
        Task<ReporteGeneralDto> GetReporteGeneralAsync();
        Task<Dictionary<string, object>> GetEstadisticasComparativasAsync();
    }
}
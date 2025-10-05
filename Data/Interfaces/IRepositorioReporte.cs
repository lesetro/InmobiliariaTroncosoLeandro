public interface IRepositorioReporte
{
    Task<object> ObtenerResumenGeneralAsync();
    Task<IList<object>> ObtenerIngresosPorMesAsync(int meses = 6);
    Task<IList<object>> ObtenerTopInmueblesAsync(int limite = 5);
    Task<IList<object>> ObtenerPagosConMoraAsync();
    Task<IList<object>> ObtenerContratosProximosVencerAsync();
    Task<object> ObtenerEstadoInmueblesAsync();
}
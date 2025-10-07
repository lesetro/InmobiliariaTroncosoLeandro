using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    /// <summary>
    /// Interface específica para gestión de pagos de ventas
    /// </summary>
    public interface IRepositorioVenta
    {

        Task<bool> CrearPagoVentaAsync(Pago pago);

        Task<bool> ActualizarPagoVentaAsync(Pago pago);

        Task<bool> AnularPagoVentaAsync(int idPago, int idUsuarioAnulador);

        Task<Pago?> ObtenerPagoVentaPorIdAsync(int id);

        Task<Pago?> ObtenerPagoVentaConDetallesAsync(int id);

        // ========================
        // MÉTODOS DE LISTADO Y PAGINACIÓN
        // ========================
        Task<(IList<Pago> pagos, int totalRegistros)> ObtenerPagosVentaConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);

        // ========================
        // MÉTODOS DE VALIDACIÓN ESPECÍFICOS
        // ========================

        Task<bool> InmuebleDisponibleParaVentaAsync(int idInmueble);

        Task<bool> ExisteVentaParaInmuebleAsync(int idInmueble);

        Task<bool> ExisteNumeroPagoVentaAsync(int idInmueble, int numeroPago, int idPagoExcluir = 0);

        // ========================
        // MÉTODOS DE NEGOCIO ESPECÍFICOS
        // ========================

        Task<bool> MarcarInmuebleComoVendidoAsync(int idInmueble);

        Task<bool> RestaurarEstadoInmuebleAsync(int idInmueble);

        // ========================
        // MÉTODOS AUXILIARES PARA DROPDOWNS
        // ========================


        Task<List<Inmueble>> ObtenerInmueblesDisponiblesVentaAsync(string termino, int limite = 20);

        Task<List<Usuario>> ObtenerUsuariosActivosAsync(int limite = 20);

        //Metodos para trabajar con pagos 
        Task<decimal> ObtenerTotalPagadoVentaAsync(int idInmueble);
        Task<Pago?> ObtenerUltimoPagoVentaAsync(int idInmueble);

        //Buscar inmuebles para ventas 

        Task<List<SearchResultVenta>> BuscarInmueblesParaVentaAsync(string termino, int limite = 20);
    }
}
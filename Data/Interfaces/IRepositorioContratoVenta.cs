using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data;

namespace Inmobiliaria_troncoso_leandro.Data
{
    public interface IRepositorioContratoVenta
    {
        Task<ContratoVenta> ObtenerPorIdAsync(int id);
        Task<ContratoVenta> ObtenerCompletoPorIdAsync(int id);
        Task CrearAsync(ContratoVenta contratoVenta);
        Task ActualizarAsync(ContratoVenta contratoVenta);
        Task<bool> EliminarAsync(int id);
        //para mostrar en el contratos
        Task<IEnumerable<ContratoVenta>> ObtenerTodosAsync();
        //Pagos
        Task<ContratoVenta?> ObtenerContratoPorInmuebleAsync(int idInmueble);
        
    }
}
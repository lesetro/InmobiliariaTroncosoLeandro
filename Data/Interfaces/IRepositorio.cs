using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    public interface IRepositorio<T> where T : class
    {
        // Métodos básicos CRUD (como el profesor)
        Task<bool> AltaAsync(T entidad);
        Task<bool> BajaAsync(int id);  
        Task<bool> ModificacionAsync(T entidad);
        Task<IList<T>> ObtenerTodosAsync();
        Task<T?> ObtenerPorIdAsync(int id);
    }
}
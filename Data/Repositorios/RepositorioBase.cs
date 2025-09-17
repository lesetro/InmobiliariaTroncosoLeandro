using Microsoft.Extensions.Configuration;
using System;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioBase
    {
        protected readonly string connectionString;

        public RepositorioBase(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }
    }
}
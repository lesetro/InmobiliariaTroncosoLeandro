using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Services
{
    public class SearchService : ISearchService
    {
        private readonly string _connectionString;

        public SearchService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException("Connection string cannot be null");
        }

        // USUARIOS - Corregido para devolver id_usuario correcto
        public async Task<List<SearchResult>> BuscarUsuariosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT u.id_usuario,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       u.rol as tipo_usuario
                FROM usuario u
                WHERE u.estado = 'activo'
                AND (u.nombre LIKE @termino 
                     OR u.apellido LIKE @termino 
                     OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino
                     OR u.telefono LIKE @termino
                     OR u.email LIKE @termino
                     OR u.dni LIKE @termino)
                ORDER BY u.apellido, u.nombre
                LIMIT @limite";

            return await ExecuteSearchQueryAsync(query, termino, limite);
        }

        // PROPIETARIOS 
        public async Task<List<SearchResult>> BuscarPropietariosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT p.id_propietario,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       'Propietario' as tipo_usuario
                FROM usuario u
                INNER JOIN propietario p ON u.id_usuario = p.id_usuario
                WHERE u.estado = 'activo' AND p.estado = 1
                AND (u.nombre LIKE @termino 
                     OR u.apellido LIKE @termino 
                     OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino
                     OR u.telefono LIKE @termino
                     OR u.email LIKE @termino
                     OR u.dni LIKE @termino)
                ORDER BY u.apellido, u.nombre
                LIMIT @limite";

            return await ExecuteSearchQueryPropietariosAsync(query, termino, limite);
        }

        // INQUILINOS 
        public async Task<List<SearchResult>> BuscarInquilinosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT i.id_inquilino,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       'Inquilino' as tipo_usuario
                FROM usuario u
                INNER JOIN inquilino i ON u.id_usuario = i.id_usuario
                WHERE u.estado = 'activo' AND i.estado = 1
                AND (u.nombre LIKE @termino 
                     OR u.apellido LIKE @termino 
                     OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino
                     OR u.telefono LIKE @termino
                     OR u.email LIKE @termino
                     OR u.dni LIKE @termino)
                ORDER BY u.apellido, u.nombre
                LIMIT @limite";

            return await ExecuteSearchQueryInquilinosAsync(query, termino, limite);
        }

        // INMUEBLES 
        public async Task<List<SearchResult>> BuscarInmueblesAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT i.id_inmueble,
                       CONCAT(i.direccion, ' - $', FORMAT(i.precio, 0)) as nombre_completo,
                       CONCAT(i.ambientes, ' ambientes - ', i.uso) as telefono,
                       CONCAT('Estado: ', i.estado) as email,
                       CONCAT(up.nombre, ' ', up.apellido) as info_adicional,
                       'Inmueble' as tipo_usuario
                FROM inmueble i
                INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                WHERE i.estado = 'disponible' AND p.estado = 1 AND up.estado = 'activo'
                AND (i.direccion LIKE @termino 
                     OR up.nombre LIKE @termino
                     OR up.apellido LIKE @termino
                     OR CONCAT(up.nombre, ' ', up.apellido) LIKE @termino)
                ORDER BY i.direccion
                LIMIT @limite";

            return await ExecuteSearchQueryInmueblesAsync(query, termino, limite);
        }

        // TIPOS DE INMUEBLES - 
        public async Task<List<SearchResult>> BuscarTiposInmueblesAsync(string termino, int limite = 20)
        {

            if (string.IsNullOrWhiteSpace(termino))
                termino = "";

            const string query = @"
        SELECT DISTINCT t.id_tipo_inmueble,
               t.nombre as nombre_completo,
               IFNULL(t.descripcion, 'Sin descripción') as telefono,
               CONCAT('Creado: ', DATE_FORMAT(t.fecha_creacion, '%d/%m/%Y')) as email,
               'Tipo de Inmueble' as info_adicional,
               'Tipo de Inmueble' as tipo_usuario
        FROM tipo_inmueble t
        WHERE t.estado = 1
        AND (@termino = '' OR t.nombre LIKE @termino OR IFNULL(t.descripcion, '') LIKE @termino)
        ORDER BY t.nombre
        LIMIT @limite";

            return await ExecuteSearchQueryTiposInmueblesAsync(query, termino, limite);
        }
        // INTERESES INMUEBLES 
        public async Task<List<SearchResult>> BuscarInteresesInmueblesAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT ii.id_interes,
                       CONCAT(ii.nombre, ' - ', i.direccion) as nombre_completo,
                       ii.telefono as telefono,
                       ii.email as email,
                       CONCAT('Interés en: ', i.direccion) as info_adicional,
                       'Interés en Inmueble' as tipo_usuario
                FROM interes_inmueble ii
                INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                WHERE i.estado != 'inactivo'
                AND (ii.nombre LIKE @termino 
                     OR ii.email LIKE @termino 
                     OR ii.telefono LIKE @termino
                     OR i.direccion LIKE @termino)
                ORDER BY ii.fecha DESC
                LIMIT @limite";

            return await ExecuteSearchQueryInteresesAsync(query, termino, limite);
        }

        // CONTRATOS 
        public async Task<List<SearchResult>> BuscarContratosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT c.id_contrato,
                       CONCAT('Contrato #', c.id_contrato, ' - ', u1.apellido, ', ', u1.nombre) as nombre_completo,
                       CONCAT('$', FORMAT(c.monto_mensual, 0)) as telefono,
                       i.direccion as email,
                       CONCAT(c.estado, ' - ', DATE_FORMAT(c.fecha_inicio, '%d/%m/%Y'), ' a ', DATE_FORMAT(c.fecha_fin, '%d/%m/%Y')) as info_adicional,
                       'Contrato' as tipo_usuario
                FROM contrato c
                INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                INNER JOIN usuario u1 ON inq.id_usuario = u1.id_usuario
                WHERE (i.direccion LIKE @termino 
                       OR u1.nombre LIKE @termino 
                       OR u1.apellido LIKE @termino 
                       OR u1.dni LIKE @termino
                       OR c.estado LIKE @termino
                       OR CONCAT(u1.nombre, ' ', u1.apellido) LIKE @termino)
                ORDER BY c.id_contrato DESC
                LIMIT @limite";

            return await ExecuteSearchQueryContratosAsync(query, termino, limite);
        }

        // MÉTODOS AUXILIARES ESPECÍFICOS PARA CADA ENTIDAD

        private async Task<List<SearchResult>> ExecuteSearchQueryAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_usuario"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["dni"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda: {ex.Message}", ex);
            }

            return results;
        }

        private async Task<List<SearchResult>> ExecuteSearchQueryPropietariosAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_propietario"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["dni"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda de propietarios: {ex.Message}", ex);
            }

            return results;
        }

        private async Task<List<SearchResult>> ExecuteSearchQueryInquilinosAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_inquilino"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["dni"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda de inquilinos: {ex.Message}", ex);
            }

            return results;
        }

        private async Task<List<SearchResult>> ExecuteSearchQueryInmueblesAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_inmueble"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["info_adicional"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda de inmuebles: {ex.Message}", ex);
            }

            return results;
        }

        private async Task<List<SearchResult>> ExecuteSearchQueryTiposInmueblesAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_tipo_inmueble"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["info_adicional"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda de tipos de inmuebles: {ex.Message}", ex);
            }

            return results;
        }

        private async Task<List<SearchResult>> ExecuteSearchQueryInteresesAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_interes"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["info_adicional"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda de intereses: {ex.Message}", ex);
            }

            return results;
        }

        private async Task<List<SearchResult>> ExecuteSearchQueryContratosAsync(string query, string termino, int limite)
        {
            var results = new List<SearchResult>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var searchResult = new SearchResult
                    {
                        Id = reader["id_contrato"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"] as string ?? "",
                        Telefono = reader["telefono"] as string,
                        Email = reader["email"] as string,
                        InfoAdicional = reader["info_adicional"] as string,
                        Tipo = reader["tipo_usuario"] as string ?? ""
                    };
                    results.Add(searchResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda de contratos: {ex.Message}", ex);
            }

            return results;
        }
        // INMUEBLES - Método actualizado con filtro por propietario
        public async Task<List<SearchResult>> BuscarInmueblesAsync(string termino, int limite = 20, int? propietarioId = null)
        {
            var resultados = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return resultados;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Construir WHERE dinámico
                string whereClause = @"
            WHERE i.estado != 'inactivo' AND p.estado = true AND up.estado = 'activo'
            AND (i.direccion LIKE @termino 
                 OR t.nombre LIKE @termino 
                 OR i.uso LIKE @termino
                 OR up.nombre LIKE @termino
                 OR up.apellido LIKE @termino
                 OR CONCAT(up.nombre, ' ', up.apellido) LIKE @termino)";

                // Agregar filtro por propietario si se especifica
                if (propietarioId.HasValue)
                {
                    whereClause += " AND i.id_propietario = @propietarioId";
                }

                string query = $@"
            SELECT i.id_inmueble,
                   CONCAT(i.direccion, ' - ', t.nombre) as nombre_completo,
                   CONCAT('$', FORMAT(i.precio, 2)) as telefono,
                   CONCAT(i.ambientes, ' ambientes - ', i.uso) as email,
                   CONCAT(up.nombre, ' ', up.apellido) as info_adicional,
                   CONCAT(i.estado, ' - ', t.nombre) as tipo_usuario
            FROM inmueble i
            INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
            INNER JOIN propietario p ON i.id_propietario = p.id_propietario
            INNER JOIN usuario up ON p.id_usuario = up.id_usuario
            {whereClause}
            ORDER BY i.direccion
            LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                if (propietarioId.HasValue)
                {
                    command.Parameters.AddWithValue("@propietarioId", propietarioId.Value);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    resultados.Add(new SearchResult
                    {
                        Id = reader["id_inmueble"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"]?.ToString() ?? "Sin dirección",
                        Telefono = reader["telefono"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        InfoAdicional = reader["info_adicional"]?.ToString(),
                        Tipo = reader["tipo_usuario"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en BuscarInmueblesAsync: {ex.Message}");
            }

            return resultados;
        }

        // NUEVO - Obtener propietario específico de un inmueble
        public async Task<SearchResult?> ObtenerPropietarioDelInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
            SELECT p.id_propietario as id,
                   CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                   u.telefono,
                   u.email,
                   u.dni as info_adicional,
                   COALESCE('Propietario', '') as tipo_usuario
            FROM inmueble i
            INNER JOIN propietario p ON i.id_propietario = p.id_propietario
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario
            WHERE i.id_inmueble = @idInmueble 
            AND i.estado != 'inactivo' 
            AND p.estado = true 
            AND u.estado = 'activo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idInmueble", idInmueble);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SearchResult
                    {
                        Id = reader["id"]?.ToString() ?? "0",
                        Texto = reader["nombre_completo"]?.ToString() ?? "Sin nombre",
                        Telefono = reader["telefono"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        InfoAdicional = reader["info_adicional"]?.ToString(),
                        Tipo = reader["tipo_usuario"].ToString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerPropietarioDelInmuebleAsync: {ex.Message}");
            }

            return null;
        }
      
    }
}
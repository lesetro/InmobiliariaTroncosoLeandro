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

        public async Task<List<SearchResult>> BuscarUsuariosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT DISTINCT u.id_usuario,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       CASE 
                           WHEN p.id_propietario IS NOT NULL AND i.id_inquilino IS NOT NULL 
                           THEN 'Propietario e Inquilino'
                           WHEN p.id_propietario IS NOT NULL 
                           THEN 'Propietario'
                           WHEN i.id_inquilino IS NOT NULL 
                           THEN 'Inquilino'
                           ELSE 'Usuario'
                       END as tipo_usuario
                FROM usuario u
                LEFT JOIN propietario p ON u.id_usuario = p.id_usuario AND p.estado = true
                LEFT JOIN inquilino i ON u.id_usuario = i.id_usuario AND i.estado = true
                WHERE u.estado = 'activo'
                AND (p.id_propietario IS NOT NULL OR i.id_inquilino IS NOT NULL)
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

        public async Task<List<SearchResult>> BuscarPropietariosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT u.id_usuario,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       'Propietario' as tipo_usuario
                FROM usuario u
                INNER JOIN propietario p ON u.id_usuario = p.id_usuario
                WHERE u.estado = 'activo' AND p.estado = true
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

        public async Task<List<SearchResult>> BuscarInquilinosAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
                SELECT u.id_usuario,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       'Inquilino' as tipo_usuario
                FROM usuario u
                INNER JOIN inquilino i ON u.id_usuario = i.id_usuario
                WHERE u.estado = 'activo' AND i.estado = true
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

        public async Task<SearchPaginatedResult> BuscarUsuariosPaginadoAsync(string termino, int pagina, int porPagina)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new SearchPaginatedResult();

            const string countQuery = @"
                SELECT COUNT(DISTINCT u.id_usuario)
                FROM usuario u
                LEFT JOIN propietario p ON u.id_usuario = p.id_usuario AND p.estado = true
                LEFT JOIN inquilino i ON u.id_usuario = i.id_usuario AND i.estado = true
                WHERE u.estado = 'activo'
                AND (p.id_propietario IS NOT NULL OR i.id_inquilino IS NOT NULL)
                AND (u.nombre LIKE @termino 
                     OR u.apellido LIKE @termino 
                     OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino
                     OR u.telefono LIKE @termino
                     OR u.email LIKE @termino
                     OR u.dni LIKE @termino)";

            const string dataQuery = @"
                SELECT DISTINCT u.id_usuario,
                       CONCAT(u.apellido, ', ', u.nombre) as nombre_completo,
                       u.telefono,
                       u.email,
                       u.dni,
                       CASE 
                           WHEN p.id_propietario IS NOT NULL AND i.id_inquilino IS NOT NULL 
                           THEN 'Propietario e Inquilino'
                           WHEN p.id_propietario IS NOT NULL 
                           THEN 'Propietario'
                           WHEN i.id_inquilino IS NOT NULL 
                           THEN 'Inquilino'
                           ELSE 'Usuario'
                       END as tipo_usuario
                FROM usuario u
                LEFT JOIN propietario p ON u.id_usuario = p.id_usuario AND p.estado = true
                LEFT JOIN inquilino i ON u.id_usuario = i.id_usuario AND i.estado = true
                WHERE u.estado = 'activo'
                AND (p.id_propietario IS NOT NULL OR i.id_inquilino IS NOT NULL)
                AND (u.nombre LIKE @termino 
                     OR u.apellido LIKE @termino 
                     OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino
                     OR u.telefono LIKE @termino
                     OR u.email LIKE @termino
                     OR u.dni LIKE @termino)
                ORDER BY u.apellido, u.nombre
                LIMIT @limite OFFSET @offset";

            return await ExecutePaginatedQueryAsync(countQuery, dataQuery, termino, pagina, porPagina);
        }

        public async Task<SearchPaginatedResult> BuscarPropietariosPaginadoAsync(string termino, int pagina, int porPagina)
        {
            return await BuscarUsuariosPaginadoAsync(termino, pagina, porPagina);
        }

        public async Task<SearchPaginatedResult> BuscarInquilinosPaginadoAsync(string termino, int pagina, int porPagina)
        {
            return await BuscarUsuariosPaginadoAsync(termino, pagina, porPagina);
        }

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

        private async Task<SearchPaginatedResult> ExecutePaginatedQueryAsync(
            string countQuery, string dataQuery, string termino, int pagina, int porPagina)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var countCommand = new MySqlCommand(countQuery, connection);
                countCommand.Parameters.AddWithValue("@termino", $"%{termino}%");
                var totalItems = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

                var totalPaginas = (int)Math.Ceiling((double)totalItems / porPagina);
                var offset = (pagina - 1) * porPagina;

                var items = new List<SearchResult>();
                using var dataCommand = new MySqlCommand(dataQuery, connection);
                dataCommand.Parameters.AddWithValue("@termino", $"%{termino}%");
                dataCommand.Parameters.AddWithValue("@limite", porPagina);
                dataCommand.Parameters.AddWithValue("@offset", offset);

                using var reader = await dataCommand.ExecuteReaderAsync();
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
                    items.Add(searchResult);
                }

                return new SearchPaginatedResult
                {
                    Items = items,
                    TotalItems = totalItems,
                    TotalPaginas = totalPaginas,
                    PaginaActual = pagina
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en búsqueda paginada: {ex.Message}", ex);
            }
        }


        // INMUEBLES 
        public async Task<List<SearchResult>> BuscarInmueblesAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
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
        WHERE i.estado != 'inactivo' AND p.estado = true AND up.estado = 'activo'
        AND (i.direccion LIKE @termino 
             OR t.nombre LIKE @termino 
             OR i.uso LIKE @termino
             OR up.nombre LIKE @termino
             OR up.apellido LIKE @termino
             OR CONCAT(up.nombre, ' ', up.apellido) LIKE @termino)
        ORDER BY i.direccion
        LIMIT @limite";

            return await ExecuteSearchQueryAsync(query, termino, limite);
        }

        public async Task<SearchPaginatedResult> BuscarInmueblesPaginadoAsync(string termino, int pagina, int porPagina)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new SearchPaginatedResult();

            const string countQuery = @"
        SELECT COUNT(*)
        FROM inmueble i
        INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
        INNER JOIN propietario p ON i.id_propietario = p.id_propietario
        INNER JOIN usuario up ON p.id_usuario = up.id_usuario
        WHERE i.estado != 'inactivo' AND p.estado = true AND up.estado = 'activo'
        AND (i.direccion LIKE @termino 
             OR t.nombre LIKE @termino 
             OR i.uso LIKE @termino
             OR up.nombre LIKE @termino
             OR up.apellido LIKE @termino
             OR CONCAT(up.nombre, ' ', up.apellido) LIKE @termino)";

            const string dataQuery = @"
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
        WHERE i.estado != 'inactivo' AND p.estado = true AND up.estado = 'activo'
        AND (i.direccion LIKE @termino 
             OR t.nombre LIKE @termino 
             OR i.uso LIKE @termino
             OR up.nombre LIKE @termino
             OR up.apellido LIKE @termino
             OR CONCAT(up.nombre, ' ', up.apellido) LIKE @termino)
        ORDER BY i.direccion
        LIMIT @limite OFFSET @offset";

            return await ExecutePaginatedQueryAsync(countQuery, dataQuery, termino, pagina, porPagina);
        }

        // TIPOS DE INMUEBLES 
        public async Task<List<SearchResult>> BuscarTiposInmueblesAsync(string termino, int limite = 20)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new List<SearchResult>();

            const string query = @"
        SELECT t.id_tipo_inmueble,
               t.nombre as nombre_completo,
               IFNULL(t.descripcion, 'Sin descripción') as telefono,
               CONCAT('Creado: ', DATE_FORMAT(t.fecha_creacion, '%d/%m/%Y')) as email,
               CONCAT('Inmuebles: ', COUNT(i.id_inmueble)) as info_adicional,
               'Tipo de Inmueble' as tipo_usuario
        FROM tipo_inmueble t
        LEFT JOIN inmueble i ON t.id_tipo_inmueble = i.id_tipo_inmueble AND i.estado != 'inactivo'
        WHERE t.estado = true
        AND (t.nombre LIKE @termino OR t.descripcion LIKE @termino)
        GROUP BY t.id_tipo_inmueble, t.nombre, t.descripcion, t.fecha_creacion
        ORDER BY t.nombre
        LIMIT @limite";

            return await ExecuteSearchQueryAsync(query, termino, limite);
        }

        public async Task<SearchPaginatedResult> BuscarTiposInmueblesPaginadoAsync(string termino, int pagina, int porPagina)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new SearchPaginatedResult();

            const string countQuery = @"
        SELECT COUNT(*)
        FROM tipo_inmueble t
        WHERE t.estado = true
        AND (t.nombre LIKE @termino OR t.descripcion LIKE @termino)";

            const string dataQuery = @"
        SELECT t.id_tipo_inmueble,
               t.nombre as nombre_completo,
               IFNULL(t.descripcion, 'Sin descripción') as telefono,
               CONCAT('Creado: ', DATE_FORMAT(t.fecha_creacion, '%d/%m/%Y')) as email,
               CONCAT('Inmuebles: ', COUNT(i.id_inmueble)) as info_adicional,
               'Tipo de Inmueble' as tipo_usuario
        FROM tipo_inmueble t
        LEFT JOIN inmueble i ON t.id_tipo_inmueble = i.id_tipo_inmueble AND i.estado != 'inactivo'
        WHERE t.estado = true
        AND (t.nombre LIKE @termino OR t.descripcion LIKE @termino)
        GROUP BY t.id_tipo_inmueble, t.nombre, t.descripcion, t.fecha_creacion
        ORDER BY t.nombre
        LIMIT @limite OFFSET @offset";

            return await ExecutePaginatedQueryAsync(countQuery, dataQuery, termino, pagina, porPagina);
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

            return await ExecuteSearchQueryAsync(query, termino, limite);
        }

        public async Task<SearchPaginatedResult> BuscarInteresesInmueblesPaginadoAsync(string termino, int pagina, int porPagina)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return new SearchPaginatedResult();

            const string countQuery = @"
        SELECT COUNT(*)
        FROM interes_inmueble ii
        INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
        WHERE i.estado != 'inactivo'
        AND (ii.nombre LIKE @termino 
             OR ii.email LIKE @termino 
             OR ii.telefono LIKE @termino
             OR i.direccion LIKE @termino)";

            const string dataQuery = @"
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
        LIMIT @limite OFFSET @offset";

            return await ExecutePaginatedQueryAsync(countQuery, dataQuery, termino, pagina, porPagina);
        }
    }
}
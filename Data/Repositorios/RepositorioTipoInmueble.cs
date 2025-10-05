using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;


namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioTipoInmueble : IRepositorioTipoInmueble
    {
        private readonly string _connectionString;

        public RepositorioTipoInmueble(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                              throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        public async Task<bool> CrearTipoInmuebleAsync(TipoInmueble tipoInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO tipo_inmueble (nombre, descripcion, fecha_creacion, estado) 
                VALUES (@nombre, @descripcion, @fechaCreacion, @estado)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@nombre", tipoInmueble.Nombre);
            command.Parameters.AddWithValue("@descripcion", tipoInmueble.Descripcion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@fechaCreacion", tipoInmueble.FechaCreacion);
            command.Parameters.AddWithValue("@estado", tipoInmueble.Estado);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ActualizarTipoInmuebleAsync(TipoInmueble tipoInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE tipo_inmueble 
                SET nombre = @nombre, 
                    descripcion = @descripcion,
                    estado = @estado
                WHERE id_tipo_inmueble = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", tipoInmueble.IdTipoInmueble);
            command.Parameters.AddWithValue("@nombre", tipoInmueble.Nombre);
            command.Parameters.AddWithValue("@descripcion", tipoInmueble.Descripcion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@estado", tipoInmueble.Estado);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> EliminarTipoInmuebleAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Eliminación lógica - cambiar estado a false
            var query = "UPDATE tipo_inmueble SET estado = false WHERE id_tipo_inmueble = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<TipoInmueble?> ObtenerTipoInmueblePorIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id_tipo_inmueble, nombre, descripcion, fecha_creacion, estado 
                FROM tipo_inmueble 
                WHERE id_tipo_inmueble = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TipoInmueble
                {
                    IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                    Estado = reader.GetBoolean(reader.GetOrdinal("estado"))
                };
            }

            return null;
        }

        public async Task<TipoInmueble?> ObtenerTipoInmuebleConDetallesAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT ti.id_tipo_inmueble, ti.nombre, ti.descripcion, ti.fecha_creacion, ti.estado,
                       COUNT(i.id_inmueble) as cantidad_inmuebles
                FROM tipo_inmueble ti
                LEFT JOIN inmueble i ON ti.id_tipo_inmueble = i.id_tipo_inmueble
                WHERE ti.id_tipo_inmueble = @id
                GROUP BY ti.id_tipo_inmueble, ti.nombre, ti.descripcion, ti.fecha_creacion, ti.estado";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var tipoInmueble = new TipoInmueble
                {
                    IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                    Estado = reader.GetBoolean(reader.GetOrdinal("estado")),
                    
                };

                return tipoInmueble;
            }

            return null;
        }

        public async Task<bool> ExisteNombreAsync(string nombre, int idExcluir = 0)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) 
                FROM tipo_inmueble 
                WHERE LOWER(nombre) = LOWER(@nombre) 
                AND id_tipo_inmueble != @idExcluir 
                AND estado = true";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@nombre", nombre);
            command.Parameters.AddWithValue("@idExcluir", idExcluir);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<int> ContarInmueblesAsociadosAsync(int idTipoInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) 
                FROM inmueble 
                WHERE id_tipo_inmueble = @idTipoInmueble";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@idTipoInmueble", idTipoInmueble);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<(IList<TipoInmueble> tiposInmueble, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Construir condiciones WHERE dinámicamente
            var condiciones = new List<string>();
            var parametros = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(buscar))
            {
                condiciones.Add("(ti.nombre LIKE @buscar OR ti.descripcion LIKE @buscar)");
                parametros.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "activo")
                {
                    condiciones.Add("ti.estado = true");
                }
                else if (estado == "inactivo")
                {
                    condiciones.Add("ti.estado = false");
                }
            }

            var whereClause = condiciones.Count > 0 ? $"WHERE {string.Join(" AND ", condiciones)}" : "";

            // Consulta para obtener el total de registros
            var queryTotal = $@"
                SELECT COUNT(DISTINCT ti.id_tipo_inmueble)
                FROM tipo_inmueble ti
                {whereClause}";

            using var commandTotal = new MySqlCommand(queryTotal, connection);
            commandTotal.Parameters.AddRange(parametros.ToArray());
            var totalRegistros = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            // Consulta para obtener los datos paginados
            var queryDatos = $@"
                SELECT ti.id_tipo_inmueble, ti.nombre, ti.descripcion, ti.fecha_creacion, ti.estado,
                       COUNT(i.id_inmueble) as cantidad_inmuebles
                FROM tipo_inmueble ti
                LEFT JOIN inmueble i ON ti.id_tipo_inmueble = i.id_tipo_inmueble
                {whereClause}
                GROUP BY ti.id_tipo_inmueble, ti.nombre, ti.descripcion, ti.fecha_creacion, ti.estado
                ORDER BY ti.fecha_creacion DESC
                LIMIT @offset, @itemsPorPagina";

            using var commandDatos = new MySqlCommand(queryDatos, connection);
            commandDatos.Parameters.AddRange(parametros.Select(p => new MySqlParameter(p.ParameterName, p.Value)).ToArray());
            commandDatos.Parameters.AddWithValue("@offset", (pagina - 1) * itemsPorPagina);
            commandDatos.Parameters.AddWithValue("@itemsPorPagina", itemsPorPagina);

            var tiposInmueble = new List<TipoInmueble>();

            using var reader = await commandDatos.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var tipoInmueble = new TipoInmueble
                {
                    IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                    Estado = reader.GetBoolean(reader.GetOrdinal("estado"))
                };

                tiposInmueble.Add(tipoInmueble);
            }

            return (tiposInmueble, totalRegistros);
        }

        public async Task<IList<TipoInmueble>> ObtenerTiposInmuebleActivosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id_tipo_inmueble, nombre, descripcion, fecha_creacion, estado
                FROM tipo_inmueble 
                WHERE estado = true 
                ORDER BY nombre";

            using var command = new MySqlCommand(query, connection);
            var tiposInmueble = new List<TipoInmueble>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tiposInmueble.Add(new TipoInmueble
                {
                    IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                    Estado = reader.GetBoolean(reader.GetOrdinal("estado"))
                });
            }

            return tiposInmueble;
        }
    }
}
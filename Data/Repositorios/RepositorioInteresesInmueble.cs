using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioInteresInmueble : IRepositorioInteresInmueble
    {
        private readonly string _connectionString;

        public RepositorioInteresInmueble(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                              throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        public async Task<InteresInmueble?> ObtenerInteresPorIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id_interes, id_inmueble, nombre, email, telefono, fecha, 
                       contactado, fecha_contacto, observaciones
                FROM interes_inmueble 
                WHERE id_interes = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones"))
                };
            }

            return null;
        }

        public async Task<InteresInmueble?> ObtenerInteresConDetallesAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT ii.id_interes, ii.id_inmueble, ii.nombre, ii.email, ii.telefono, ii.fecha,
               ii.contactado, ii.fecha_contacto, ii.observaciones,
               i.direccion, i.precio, i.uso, i.ambientes,
               ti.nombre as tipo_inmueble,
               u.nombre as usuario_nombre, u.apellido as usuario_apellido
        FROM interes_inmueble ii
        INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
        INNER JOIN tipo_inmueble ti ON i.id_tipo_inmueble = ti.id_tipo_inmueble
        INNER JOIN propietario p ON i.id_propietario = p.id_propietario
        INNER JOIN usuario u ON p.id_usuario = u.id_usuario
        WHERE ii.id_interes = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var interes = new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        Uso = reader.GetString(reader.GetOrdinal("uso")),
                        Ambientes = reader.GetInt32(reader.GetOrdinal("ambientes")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_inmueble"))
                        },
                        Propietario = new Propietario
                        {
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("usuario_nombre")),
                                Apellido = reader.GetString(reader.GetOrdinal("usuario_apellido"))
                            }
                        }
                    }
                };

                return interes;
            }

            return null;
        }
        public async Task<(IList<InteresInmueble> intereses, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, int? idInmueble, DateTime? fechaDesde, DateTime? fechaHasta, int itemsPorPagina)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Construir condiciones WHERE dinámicamente
            var condiciones = new List<string>();
            var parametros = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(buscar))
            {
                condiciones.Add("(ii.nombre LIKE @buscar OR ii.email LIKE @buscar OR i.direccion LIKE @buscar)");
                parametros.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "contactado")
                {
                    condiciones.Add("ii.contactado = true");
                }
                else if (estado == "pendiente")
                {
                    condiciones.Add("ii.contactado = false");
                }
            }

            if (idInmueble.HasValue)
            {
                condiciones.Add("ii.id_inmueble = @idInmueble");
                parametros.Add(new MySqlParameter("@idInmueble", idInmueble.Value));
            }

            if (fechaDesde.HasValue)
            {
                condiciones.Add("ii.fecha >= @fechaDesde");
                parametros.Add(new MySqlParameter("@fechaDesde", fechaDesde.Value));
            }

            if (fechaHasta.HasValue)
            {
                condiciones.Add("ii.fecha <= @fechaHasta");
                parametros.Add(new MySqlParameter("@fechaHasta", fechaHasta.Value.AddDays(1).AddTicks(-1)));
            }

            var whereClause = condiciones.Count > 0 ? $"WHERE {string.Join(" AND ", condiciones)}" : "";

            // Consulta para obtener el total de registros
            var queryTotal = $@"
                SELECT COUNT(*)
                FROM interes_inmueble ii
                INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                {whereClause}";

            using var commandTotal = new MySqlCommand(queryTotal, connection);
            commandTotal.Parameters.AddRange(parametros.ToArray());
            var totalRegistros = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            // Consulta para obtener los datos paginados
            var queryDatos = $@"
                SELECT ii.id_interes, ii.id_inmueble, ii.nombre, ii.email, ii.telefono, ii.fecha,
                       ii.contactado, ii.fecha_contacto, ii.observaciones,
                       i.direccion, i.precio, i.uso,
                       ti.nombre as tipo_inmueble
                FROM interes_inmueble ii
                INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                INNER JOIN tipo_inmueble ti ON i.id_tipo_inmueble = ti.id_tipo_inmueble
                {whereClause}
                ORDER BY ii.contactado ASC, ii.fecha DESC
                LIMIT @offset, @itemsPorPagina";

            using var commandDatos = new MySqlCommand(queryDatos, connection);
            commandDatos.Parameters.AddRange(parametros.Select(p => new MySqlParameter(p.ParameterName, p.Value)).ToArray());
            commandDatos.Parameters.AddWithValue("@offset", (pagina - 1) * itemsPorPagina);
            commandDatos.Parameters.AddWithValue("@itemsPorPagina", itemsPorPagina);

            var intereses = new List<InteresInmueble>();

            using var reader = await commandDatos.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var interes = new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        Uso = reader.GetString(reader.GetOrdinal("uso")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_inmueble"))
                        }
                    }
                };

                intereses.Add(interes);
            }

            return (intereses, totalRegistros);
        }

        public async Task<bool> MarcarComoContactadoAsync(int idInteres, string? observaciones = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE interes_inmueble 
                SET contactado = true, 
                    fecha_contacto = @fechaContacto,
                    observaciones = @observaciones
                WHERE id_interes = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", idInteres);
            command.Parameters.AddWithValue("@fechaContacto", DateTime.Now);
            command.Parameters.AddWithValue("@observaciones", observaciones ?? (object)DBNull.Value);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DesmarcarContactadoAsync(int idInteres)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE interes_inmueble 
                SET contactado = false, 
                    fecha_contacto = NULL
                WHERE id_interes = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", idInteres);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ActualizarObservacionesAsync(int idInteres, string observaciones)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE interes_inmueble 
                SET observaciones = @observaciones
                WHERE id_interes = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", idInteres);
            command.Parameters.AddWithValue("@observaciones", observaciones);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int> ContarInteresesPendientesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM interes_inmueble WHERE contactado = false";

            using var command = new MySqlCommand(query, connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> ContarInteresesContactadosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM interes_inmueble WHERE contactado = true";

            using var command = new MySqlCommand(query, connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> ContarInteresesPorInmuebleAsync(int idInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM interes_inmueble WHERE id_inmueble = @idInmueble";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@idInmueble", idInmueble);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<IList<InteresInmueble>> ObtenerInteresesRecientesAsync(int cantidad = 5)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT ii.id_interes, ii.id_inmueble, ii.nombre, ii.email, ii.telefono, ii.fecha,
                       ii.contactado, ii.fecha_contacto, ii.observaciones,
                       i.direccion, i.precio
                FROM interes_inmueble ii
                INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                ORDER BY ii.fecha DESC
                LIMIT @cantidad";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@cantidad", cantidad);

            var intereses = new List<InteresInmueble>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var interes = new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio"))
                    }
                };

                intereses.Add(interes);
            }

            return intereses;
        }

        public async Task<IList<Inmueble>> ObtenerInmueblesConInteresesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT i.id_inmueble, i.direccion, i.precio,
                       COUNT(ii.id_interes) as total_intereses
                FROM inmueble i
                INNER JOIN interes_inmueble ii ON i.id_inmueble = ii.id_inmueble
                GROUP BY i.id_inmueble, i.direccion, i.precio
                ORDER BY total_intereses DESC, i.direccion";

            using var command = new MySqlCommand(query, connection);
            var inmuebles = new List<Inmueble>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                inmuebles.Add(new Inmueble
                {
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                    Precio = reader.GetDecimal(reader.GetOrdinal("precio"))
                });
            }

            return inmuebles;
        }

        public async Task<Dictionary<string, int>> ObtenerEstadisticasInteresesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var estadisticas = new Dictionary<string, int>();

            // Total de intereses
            var queryTotal = "SELECT COUNT(*) FROM interes_inmueble";
            using var commandTotal = new MySqlCommand(queryTotal, connection);
            estadisticas["Total"] = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            // Pendientes
            var queryPendientes = "SELECT COUNT(*) FROM interes_inmueble WHERE contactado = false";
            using var commandPendientes = new MySqlCommand(queryPendientes, connection);
            estadisticas["Pendientes"] = Convert.ToInt32(await commandPendientes.ExecuteScalarAsync());

            // Contactados
            var queryContactados = "SELECT COUNT(*) FROM interes_inmueble WHERE contactado = true";
            using var commandContactados = new MySqlCommand(queryContactados, connection);
            estadisticas["Contactados"] = Convert.ToInt32(await commandContactados.ExecuteScalarAsync());

            // Hoy
            var queryHoy = "SELECT COUNT(*) FROM interes_inmueble WHERE DATE(fecha) = CURDATE()";
            using var commandHoy = new MySqlCommand(queryHoy, connection);
            estadisticas["Hoy"] = Convert.ToInt32(await commandHoy.ExecuteScalarAsync());

            // Esta semana
            var querySemana = "SELECT COUNT(*) FROM interes_inmueble WHERE fecha >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)";
            using var commandSemana = new MySqlCommand(querySemana, connection);
            estadisticas["EstaSemana"] = Convert.ToInt32(await commandSemana.ExecuteScalarAsync());

            return estadisticas;
        }

        public async Task<IList<InteresInmueble>> ObtenerInteresesUrgentesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT ii.id_interes, ii.id_inmueble, ii.nombre, ii.email, ii.telefono, ii.fecha,
                       ii.contactado, ii.fecha_contacto, ii.observaciones,
                       i.direccion, i.precio,
                       DATEDIFF(CURDATE(), ii.fecha) as dias_desde_interes
                FROM interes_inmueble ii
                INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                WHERE ii.contactado = false 
                AND DATEDIFF(CURDATE(), ii.fecha) >= 3
                ORDER BY ii.fecha ASC
                LIMIT 10";

            using var command = new MySqlCommand(query, connection);
            var intereses = new List<InteresInmueble>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var interes = new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio"))
                    }
                };

                intereses.Add(interes);
            }

            return intereses;
        }
        //metodos para el administrador
        public async Task<int> GetTotalInteresesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM interes_inmueble";
            using var command = new MySqlCommand(query, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<IEnumerable<InteresInmueble>> GetInteresesPendientesAsync()
        {
            var intereses = new List<InteresInmueble>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT ii.*, i.direccion as inmueble_direccion, i.precio, ti.nombre as tipo_inmueble
                     FROM interes_inmueble ii 
                     INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                     INNER JOIN tipo_inmueble ti ON i.id_tipo = ti.id_tipo
                     WHERE ii.contactado = 0 
                     ORDER BY ii.fecha DESC";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var interes = new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_inmueble"))
                        }
                    }
                };
                intereses.Add(interes);
            }

            return intereses;
        }

        public async Task<IEnumerable<InteresInmueble>> GetInteresesRecientesAsync()
        {
            var intereses = new List<InteresInmueble>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Últimos 7 días
            string query = @"SELECT ii.*, i.direccion as inmueble_direccion, i.precio, ti.nombre as tipo_inmueble
                     FROM interes_inmueble ii 
                     INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                     INNER JOIN tipo_inmueble ti ON i.id_tipo = ti.id_tipo
                     WHERE ii.fecha >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                     ORDER BY ii.fecha DESC
                     LIMIT 10";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var interes = new InteresInmueble
                {
                    IdInteres = reader.GetInt32(reader.GetOrdinal("id_interes")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                    Contactado = reader.GetBoolean(reader.GetOrdinal("contactado")),
                    FechaContacto = reader.IsDBNull(reader.GetOrdinal("fecha_contacto")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_inmueble"))
                        }
                    }
                };
                intereses.Add(interes);
            }

            return intereses;
        }

        // También agregar estos métodos útiles para estadísticas

        public async Task<int> GetInteresesPendientesCountAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM interes_inmueble WHERE contactado = 0";
            using var command = new MySqlCommand(query, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> GetInteresesHoyAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM interes_inmueble WHERE DATE(fecha) = CURDATE()";
            using var command = new MySqlCommand(query, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> GetInteresesSemanaAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM interes_inmueble WHERE fecha >= DATE_SUB(NOW(), INTERVAL 7 DAY)";
            using var command = new MySqlCommand(query, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        // IMPLEMENTACIÓN EN RepositorioInteresInmueble
        public async Task<bool> CrearInteresAsync(InteresInmueble interes)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
            INSERT INTO interes_inmueble (
                id_inmueble, 
                nombre, 
                email, 
                telefono, 
                fecha, 
                contactado,
                fecha_contacto,
                observaciones
            ) VALUES (
                @idInmueble, 
                @nombre, 
                @email, 
                @telefono, 
                @fecha, 
                @contactado,
                @fechaContacto,
                @observaciones
            )";

                using var command = new MySqlCommand(query, connection);

                // Parámetros según el modelo InteresInmueble
                command.Parameters.AddWithValue("@idInmueble", interes.IdInmueble);
                command.Parameters.AddWithValue("@nombre", interes.Nombre);
                command.Parameters.AddWithValue("@email", interes.Email);
                command.Parameters.AddWithValue("@fecha", interes.Fecha);
                command.Parameters.AddWithValue("@contactado", interes.Contactado);

                // Parámetros opcionales (pueden ser null)
                command.Parameters.AddWithValue("@telefono",
                    string.IsNullOrWhiteSpace(interes.Telefono) ? (object)DBNull.Value : interes.Telefono);
                command.Parameters.AddWithValue("@fechaContacto",
                    interes.FechaContacto.HasValue ? interes.FechaContacto.Value : (object)DBNull.Value);
                command.Parameters.AddWithValue("@observaciones",
                    string.IsNullOrWhiteSpace(interes.Observaciones) ? (object)DBNull.Value : interes.Observaciones);

                var filasAfectadas = await command.ExecuteNonQueryAsync();
                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                // Log del error si tienes un logger disponible
                throw new Exception($"Error al crear interés en inmueble: {ex.Message}", ex);
            }
        }


    }
}
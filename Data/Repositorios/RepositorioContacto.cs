using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioContacto : IRepositorioContacto
    {
        private readonly string _connectionString;

        public RepositorioContacto(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                              throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<bool> CrearContactoAsync(Contacto contacto)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO contacto 
                    (nombre, apellido, email, telefono, asunto, mensaje, fecha_contacto, estado, id_inmueble, ip_origen, user_agent)
                    VALUES (@nombre, @apellido, @email, @telefono, @asunto, @mensaje, @fechaContacto, @estado, @idInmueble, @ipOrigen, @userAgent)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@nombre", contacto.Nombre);
                command.Parameters.AddWithValue("@apellido", contacto.Apellido);
                command.Parameters.AddWithValue("@email", contacto.Email);
                command.Parameters.AddWithValue("@telefono", contacto.Telefono ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@asunto", contacto.Asunto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@mensaje", contacto.Mensaje);
                command.Parameters.AddWithValue("@fechaContacto", contacto.FechaContacto);
                command.Parameters.AddWithValue("@estado", contacto.Estado);
                command.Parameters.AddWithValue("@idInmueble", contacto.IdInmueble ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ipOrigen", contacto.IpOrigen ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@userAgent", contacto.UserAgent ?? (object)DBNull.Value);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(IList<Contacto> contactos, int totalRegistros)> ObtenerContactosConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            var contactos = new List<Contacto>();
            int totalRegistros = 0;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Query para contar total
                string queryCount = @"
                    SELECT COUNT(*) FROM contacto c
                    LEFT JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    WHERE (@buscar = '' OR c.nombre LIKE @buscarParam OR c.apellido LIKE @buscarParam OR c.email LIKE @buscarParam)
                    AND (@estado = '' OR c.estado = @estado)";

                using (var commandCount = new MySqlCommand(queryCount, connection))
                {
                    commandCount.Parameters.AddWithValue("@buscar", buscar ?? "");
                    commandCount.Parameters.AddWithValue("@buscarParam", $"%{buscar ?? ""}%");
                    commandCount.Parameters.AddWithValue("@estado", estado ?? "");
                    totalRegistros = Convert.ToInt32(await commandCount.ExecuteScalarAsync());
                }

                // Query para obtener datos
                string queryData = @"
                    SELECT c.id_contacto, c.nombre, c.apellido, c.email, c.telefono, c.asunto, 
                           c.mensaje, c.fecha_contacto, c.estado, c.id_inmueble,
                           i.direccion as inmueble_direccion
                    FROM contacto c
                    LEFT JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    WHERE (@buscar = '' OR c.nombre LIKE @buscarParam OR c.apellido LIKE @buscarParam OR c.email LIKE @buscarParam)
                    AND (@estado = '' OR c.estado = @estado)
                    ORDER BY c.fecha_contacto DESC
                    LIMIT @offset, @limit";

                using (var commandData = new MySqlCommand(queryData, connection))
                {
                    commandData.Parameters.AddWithValue("@buscar", buscar ?? "");
                    commandData.Parameters.AddWithValue("@buscarParam", $"%{buscar ?? ""}%");
                    commandData.Parameters.AddWithValue("@estado", estado ?? "");
                    commandData.Parameters.AddWithValue("@offset", (pagina - 1) * itemsPorPagina);
                    commandData.Parameters.AddWithValue("@limit", itemsPorPagina);

                    using var reader = await commandData.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        contactos.Add(new Contacto
                        {
                            IdContacto = reader.GetInt32(reader.GetOrdinal("id_contacto")),
                            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                            Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                            Email = reader.GetString(reader.GetOrdinal("email")),
                            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                            Asunto = reader.IsDBNull(reader.GetOrdinal("asunto")) ? null : reader.GetString(reader.GetOrdinal("asunto")),
                            Mensaje = reader.GetString(reader.GetOrdinal("mensaje")),
                            FechaContacto = reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                            Estado = reader.GetString(reader.GetOrdinal("estado")),
                            IdInmueble = reader.IsDBNull(reader.GetOrdinal("id_inmueble")) ? null : reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                            Inmueble = reader.IsDBNull(reader.GetOrdinal("inmueble_direccion")) ? null : new Inmueble 
                            { 
                                Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")) 
                            }
                        });
                    }
                }
            }
            catch
            {
                // Log error
            }

            return (contactos, totalRegistros);
        }

        public async Task<bool> ActualizarEstadoContactoAsync(int id, string nuevoEstado)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "UPDATE contacto SET estado = @estado WHERE id_contacto = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@estado", nuevoEstado);
                command.Parameters.AddWithValue("@id", id);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Contacto?> ObtenerContactoPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.*, i.direccion as inmueble_direccion
                    FROM contacto c
                    LEFT JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    WHERE c.id_contacto = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Contacto
                    {
                        IdContacto = reader.GetInt32(reader.GetOrdinal("id_contacto")),
                        Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        Email = reader.GetString(reader.GetOrdinal("email")),
                        Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                        Asunto = reader.IsDBNull(reader.GetOrdinal("asunto")) ? null : reader.GetString(reader.GetOrdinal("asunto")),
                        Mensaje = reader.GetString(reader.GetOrdinal("mensaje")),
                        FechaContacto = reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        IdInmueble = reader.IsDBNull(reader.GetOrdinal("id_inmueble")) ? null : reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        IpOrigen = reader.IsDBNull(reader.GetOrdinal("ip_origen")) ? null : reader.GetString(reader.GetOrdinal("ip_origen")),
                        UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent"))? null : reader.GetString(reader.GetOrdinal("user_agent")),
                        Inmueble = reader.IsDBNull(reader.GetOrdinal("inmueble_direccion")) ? null : new Inmueble 
                        { 
                            Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")) 
                        }
                    };
                }
            }
            catch
            {
                // Log error
            }

            return null;
        }

        public async Task<bool> EliminarContactoAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM contacto WHERE id_contacto = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IList<Contacto>> ObtenerContactosRecientesAsync(int cantidad = 5)
        {
            var contactos = new List<Contacto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.id_contacto, c.nombre, c.apellido, c.email, c.asunto, 
                           c.fecha_contacto, c.estado, i.direccion as inmueble_direccion
                    FROM contacto c
                    LEFT JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    ORDER BY c.fecha_contacto DESC
                    LIMIT @cantidad";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@cantidad", cantidad);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    contactos.Add(new Contacto
                    {
                        IdContacto = reader.GetInt32(reader.GetOrdinal("id_contacto")),
                        Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        Email = reader.GetString(reader.GetOrdinal("email")),
                        Asunto = reader.IsDBNull(reader.GetOrdinal("asunto")) ? null : reader.GetString(reader.GetOrdinal("asunto")),
                        FechaContacto = reader.GetDateTime(reader.GetOrdinal("fecha_contacto")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        Inmueble = reader.IsDBNull(reader.GetOrdinal("inmueble_direccion")) ? null : new Inmueble 
                        { 
                            Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")) 
                        }
                    });
                }
            }
            catch
            {
                // Log error
            }

            return contactos;
        }

        public async Task<Dictionary<string, int>> ObtenerEstadisticasContactosAsync()
        {
            var estadisticas = new Dictionary<string, int>
            {
                { "pendiente", 0 },
                { "respondido", 0 },
                { "cerrado", 0 },
                { "total", 0 }
            };

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT estado, COUNT(*) as cantidad
                    FROM contacto
                    GROUP BY estado
                    
                    UNION ALL
                    
                    SELECT 'total' as estado, COUNT(*) as cantidad
                    FROM contacto";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var estado = reader.GetString(reader.GetOrdinal("estado"));
                    var cantidad = reader.GetInt32(reader.GetOrdinal("cantidad"));
                    estadisticas[estado] = cantidad;
                }
            }
            catch
            {
                // Log error
            }

            return estadisticas;
        }
        
    }
}
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioInquilino : IRepositorioInquilino
    {
        private readonly string _connectionString;

        public RepositorioInquilino(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // MÉTODO Create
        public async Task<bool> CrearInquilinoConTransaccionAsync(Inquilino inquilino)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Verificar DNI único
                if (!string.IsNullOrEmpty(inquilino.Usuario.Dni) && await ExisteDniInternoAsync(inquilino.Usuario.Dni, connection, transaction))
                {
                    return false; // DNI ya existe
                }

                // Verificar Email único
                if (!string.IsNullOrEmpty(inquilino.Usuario.Email) && await ExisteEmailInternoAsync(inquilino.Usuario.Email, connection, transaction))
                {
                    return false; // Email ya existe
                }

                // Crear usuario - SIN fecha_creacion, solo fecha_alta para inquilino
                string queryUsuario = @"
                    INSERT INTO usuario 
                    (dni, nombre, apellido, telefono, email, direccion, password, rol, estado) 
                    VALUES (@dni, @nombre, @apellido, @telefono, @email, @direccion, @password, @rol, @estado);
                    SELECT LAST_INSERT_ID();";

                int idUsuario;
                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@dni", inquilino.Usuario.Dni);
                    commandUsuario.Parameters.AddWithValue("@nombre", inquilino.Usuario.Nombre);
                    commandUsuario.Parameters.AddWithValue("@apellido", inquilino.Usuario.Apellido);
                    commandUsuario.Parameters.AddWithValue("@telefono", inquilino.Usuario.Telefono ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@email", inquilino.Usuario.Email ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@direccion", inquilino.Usuario.Direccion ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword("passwordtemporal"));
                    commandUsuario.Parameters.AddWithValue("@rol", "inquilino");
                    commandUsuario.Parameters.AddWithValue("@estado", "activo");

                    idUsuario = Convert.ToInt32(await commandUsuario.ExecuteScalarAsync());
                }

                // Crear inquilino - USAR fecha_alta
                string queryInquilino = @"
                    INSERT INTO inquilino 
                    (id_usuario, fecha_alta, estado) 
                    VALUES (@id_usuario, @fecha_alta, @estado)";

                using (var commandInquilino = new MySqlCommand(queryInquilino, connection, transaction))
                {
                    commandInquilino.Parameters.AddWithValue("@id_usuario", idUsuario);
                    commandInquilino.Parameters.AddWithValue("@fecha_alta", DateTime.Now);
                    commandInquilino.Parameters.AddWithValue("@estado", true);

                    await commandInquilino.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // MÉTODO Edit
        public async Task<bool> ActualizarInquilinoConTransaccionAsync(Inquilino inquilino)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Verificar DNI único 
                string queryDni = "SELECT COUNT(*) FROM usuario WHERE dni = @dni AND id_usuario != @id_usuario";
                using (var commandDni = new MySqlCommand(queryDni, connection, transaction))
                {
                    commandDni.Parameters.AddWithValue("@dni", inquilino.Usuario.Dni);
                    commandDni.Parameters.AddWithValue("@id_usuario", inquilino.IdUsuario);
                    if (Convert.ToInt32(await commandDni.ExecuteScalarAsync()) > 0)
                    {
                        return false; // DNI duplicado
                    }
                }

                // Verificar Email único 
                if (!string.IsNullOrEmpty(inquilino.Usuario.Email))
                {
                    string queryEmail = "SELECT COUNT(*) FROM usuario WHERE email = @email AND id_usuario != @id_usuario";
                    using (var commandEmail = new MySqlCommand(queryEmail, connection, transaction))
                    {
                        commandEmail.Parameters.AddWithValue("@email", inquilino.Usuario.Email);
                        commandEmail.Parameters.AddWithValue("@id_usuario", inquilino.IdUsuario);
                        if (Convert.ToInt32(await commandEmail.ExecuteScalarAsync()) > 0)
                        {
                            return false; // Email duplicado
                        }
                    }
                }

                // Actualizar usuario
                string queryUsuario = @"
                    UPDATE usuario 
                    SET dni = @dni, nombre = @nombre, apellido = @apellido, 
                        telefono = @telefono, email = @email, direccion = @direccion
                    WHERE id_usuario = @id_usuario";

                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@dni", inquilino.Usuario.Dni);
                    commandUsuario.Parameters.AddWithValue("@nombre", inquilino.Usuario.Nombre);
                    commandUsuario.Parameters.AddWithValue("@apellido", inquilino.Usuario.Apellido);
                    commandUsuario.Parameters.AddWithValue("@telefono", inquilino.Usuario.Telefono ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@email", inquilino.Usuario.Email ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@direccion", inquilino.Usuario.Direccion ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@id_usuario", inquilino.IdUsuario);

                    await commandUsuario.ExecuteNonQueryAsync();
                }

                // Actualizar inquilino
                string queryInquilino = @"
                    UPDATE inquilino 
                    SET estado = @estado 
                    WHERE id_inquilino = @id_inquilino";

                using (var commandInquilino = new MySqlCommand(queryInquilino, connection, transaction))
                {
                    commandInquilino.Parameters.AddWithValue("@estado", inquilino.Estado);
                    commandInquilino.Parameters.AddWithValue("@id_inquilino", inquilino.IdInquilino);

                    await commandInquilino.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // MÉTODO Delete
        public async Task<bool> EliminarInquilinoConTransaccionAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Obtener id_usuario
                int idUsuario;
                string queryGetUsuario = "SELECT id_usuario FROM inquilino WHERE id_inquilino = @id_inquilino";
                using (var commandGet = new MySqlCommand(queryGetUsuario, connection, transaction))
                {
                    commandGet.Parameters.AddWithValue("@id_inquilino", id);
                    idUsuario = Convert.ToInt32(await commandGet.ExecuteScalarAsync());
                }

                // Actualizar estado en inquilino
                string queryInquilino = "UPDATE inquilino SET estado = @estado WHERE id_inquilino = @id_inquilino";
                using (var commandInquilino = new MySqlCommand(queryInquilino, connection, transaction))
                {
                    commandInquilino.Parameters.AddWithValue("@estado", false);
                    commandInquilino.Parameters.AddWithValue("@id_inquilino", id);
                    await commandInquilino.ExecuteNonQueryAsync();
                }

                // Actualizar estado en usuario
                string queryUsuario = "UPDATE usuario SET estado = @estado WHERE id_usuario = @id_usuario";
                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@estado", "inactivo");
                    commandUsuario.Parameters.AddWithValue("@id_usuario", idUsuario);
                    await commandUsuario.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // MÉTODO ObtenerPorId
        public async Task<Inquilino?> ObtenerInquilinoPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT i.id_inquilino, i.id_usuario, i.fecha_alta, i.estado,
                           u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion
                    FROM inquilino i 
                    INNER JOIN usuario u ON i.id_usuario = u.id_usuario 
                    WHERE i.id_inquilino = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Inquilino
                    {
                        IdInquilino = reader.GetInt32(0),  // i.id_inquilino
                        IdUsuario = reader.GetInt32(1),    // i.id_usuario
                        FechaAlta = reader.GetDateTime(2), // i.fecha_alta
                        Estado = reader.GetBoolean(3),     // i.estado
                        Usuario = new Usuario
                        {
                            IdUsuario = reader.GetInt32(4),  // u.id_usuario
                            Dni = reader.IsDBNull(5) ? null : reader.GetString(5),       // u.dni
                            Nombre = reader.IsDBNull(6) ? null : reader.GetString(6),    // u.nombre
                            Apellido = reader.IsDBNull(7) ? null : reader.GetString(7),  // u.apellido
                            Telefono = reader.IsDBNull(8) ? null : reader.GetString(8),  // u.telefono
                            Email = reader.IsDBNull(9) ? null : reader.GetString(9),     // u.email
                            Direccion = reader.IsDBNull(10) ? null : reader.GetString(10) // u.direccion
                        }
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // MÉTODO para Index con paginación
        public async Task<(IList<Inquilino> inquilinos, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, int itemsPorPagina)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // Construir la consulta base con JOIN
                string baseQuery = @"
                    FROM inquilino i 
                    INNER JOIN usuario u ON i.id_usuario = u.id_usuario 
                    WHERE i.estado = true";
                
                // Agregar filtro de búsqueda si se proporciona
                string whereClause = baseQuery;
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    whereClause += " AND (u.nombre LIKE @buscar OR u.apellido LIKE @buscar OR u.dni LIKE @buscar OR u.email LIKE @buscar)";
                }
                
                // 1. Contar total de registros
                string countQuery = $"SELECT COUNT(*) {whereClause}";
                int totalRegistros;
                
                using (var countCommand = new MySqlCommand(countQuery, connection))
                {
                    if (!string.IsNullOrWhiteSpace(buscar))
                    {
                        countCommand.Parameters.AddWithValue("@buscar", $"%{buscar}%");
                    }
                    totalRegistros = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }
                
                // 2. Obtener registros con paginación
                int offset = (pagina - 1) * itemsPorPagina;
                string dataQuery = $@"
                    SELECT i.id_inquilino, i.id_usuario, i.fecha_alta, i.estado,
                           u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion
                    {whereClause}
                    ORDER BY i.id_inquilino 
                    LIMIT @limit OFFSET @offset";
                
                var inquilinos = new List<Inquilino>();
                
                using (var dataCommand = new MySqlCommand(dataQuery, connection))
                {
                    if (!string.IsNullOrWhiteSpace(buscar))
                    {
                        dataCommand.Parameters.AddWithValue("@buscar", $"%{buscar}%");
                    }
                    dataCommand.Parameters.AddWithValue("@limit", itemsPorPagina);
                    dataCommand.Parameters.AddWithValue("@offset", offset);
                    
                    using var reader = await dataCommand.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        inquilinos.Add(new Inquilino
                        {
                            IdInquilino = reader.GetInt32(0),  // i.id_inquilino
                            IdUsuario = reader.GetInt32(1),    // i.id_usuario
                            FechaAlta = reader.GetDateTime(2), // i.fecha_alta
                            Estado = reader.GetBoolean(3),     // i.estado
                            Usuario = new Usuario
                            {
                                IdUsuario = reader.GetInt32(4),  // u.id_usuario
                                Dni = reader.IsDBNull(5) ? null : reader.GetString(5),       // u.dni
                                Nombre = reader.IsDBNull(6) ? null : reader.GetString(6),    // u.nombre
                                Apellido = reader.IsDBNull(7) ? null : reader.GetString(7),  // u.apellido
                                Telefono = reader.IsDBNull(8) ? null : reader.GetString(8),  // u.telefono
                                Email = reader.IsDBNull(9) ? null : reader.GetString(9),     // u.email
                                Direccion = reader.IsDBNull(10) ? null : reader.GetString(10) // u.direccion
                            }
                        });
                    }
                }
                
                return (inquilinos, totalRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener inquilinos con paginación: {ex.Message}", ex);
            }
        }

        // MÉTODOS AUXILIARES PÚBLICOS PARA VALIDACIONES
        public async Task<bool> ExisteDniAsync(string? dni, int idExcluir = 0)
        {
            if (string.IsNullOrEmpty(dni)) return false;
            
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await ExisteDniInternoAsync(dni, connection, null, idExcluir);
        }

        public async Task<bool> ExisteEmailAsync(string? email, int idExcluir = 0)
        {
            if (string.IsNullOrEmpty(email)) return false;
            
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await ExisteEmailInternoAsync(email, connection, null, idExcluir);
        }

        // MÉTODOS AUXILIARES PRIVADOS 
        private async Task<bool> ExisteDniInternoAsync(string dni, MySqlConnection connection, MySqlTransaction? transaction = null, int idExcluir = 0)
        {
            string query = idExcluir == 0
                ? "SELECT COUNT(*) FROM usuario WHERE dni = @dni"
                : "SELECT COUNT(*) FROM usuario WHERE dni = @dni AND id_usuario != @id";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@dni", dni);
            if (idExcluir != 0)
            {
                command.Parameters.AddWithValue("@id", idExcluir);
            }
            
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteEmailInternoAsync(string email, MySqlConnection connection, MySqlTransaction? transaction = null, int idExcluir = 0)
        {
            if (string.IsNullOrEmpty(email)) return false;

            string query = idExcluir == 0
                ? "SELECT COUNT(*) FROM usuario WHERE email = @email"
                : "SELECT COUNT(*) FROM usuario WHERE email = @email AND id_usuario != @id";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@email", email);
            if (idExcluir != 0)
            {
                command.Parameters.AddWithValue("@id", idExcluir);
            }
            
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }
    }
}
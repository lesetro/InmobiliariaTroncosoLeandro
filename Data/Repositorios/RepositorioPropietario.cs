
using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Data.Repositorios;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioPropietario : IRepositorioPropietario
    {
        private readonly string _connectionString;

        public RepositorioPropietario(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // MÉTODO Create
        public async Task<bool> CrearPropietarioConTransaccionAsync(Propietario propietario)
        {
            Console.WriteLine("=== INICIO REPOSITORIO CREATE ===");

            using var connection = new MySqlConnection(_connectionString);

            try
            {
                Console.WriteLine("Abriendo conexión...");
                await connection.OpenAsync();
                Console.WriteLine("Conexión abierta exitosamente");

                using var transaction = await connection.BeginTransactionAsync();
                Console.WriteLine("Transacción iniciada");

                try
                {
                    Console.WriteLine("=== VERIFICACIONES INTERNAS ===");

                    // Verificar DNI único
                    if (!string.IsNullOrEmpty(propietario.Usuario.Dni))
                    {
                        Console.WriteLine($"Verificando DNI interno: {propietario.Usuario.Dni}");
                        bool dniExiste = await ExisteDniInternoAsync(propietario.Usuario.Dni, connection, transaction);
                        Console.WriteLine($"DNI existe (interno): {dniExiste}");

                        if (dniExiste)
                        {
                            Console.WriteLine("DNI ya existe - retornando false");
                            return false;
                        }
                    }

                    // Verificar Email único
                    if (!string.IsNullOrEmpty(propietario.Usuario.Email))
                    {
                        Console.WriteLine($"Verificando Email interno: {propietario.Usuario.Email}");
                        bool emailExiste = await ExisteEmailInternoAsync(propietario.Usuario.Email, connection, transaction);
                        Console.WriteLine($"Email existe (interno): {emailExiste}");

                        if (emailExiste)
                        {
                            Console.WriteLine("Email ya existe - retornando false");
                            return false;
                        }
                    }

                    Console.WriteLine("=== CREANDO USUARIO CON MÉTODO ESTÁTICO ===");

                    // ✅ CREAR USUARIO USANDO MÉTODO ESTÁTICO (esto es lo clave)
                    var usuarioCompleto = Usuario.CrearPropietario(
                        propietario.Usuario.Nombre,
                        propietario.Usuario.Apellido,
                        propietario.Usuario.Dni,
                        propietario.Usuario.Email,
                        propietario.Usuario.Telefono,
                        propietario.Usuario.Direccion
                    );

                    Console.WriteLine($"Usuario creado con rol: {usuarioCompleto.Rol}"); // Debería mostrar "propietario"
                    Console.WriteLine($"Usuario creado con avatar: {usuarioCompleto.Avatar}"); // Avatar con iniciales
                    Console.WriteLine($"Password ya hasheado: {!string.IsNullOrEmpty(usuarioCompleto.Password)}");

                    // Query actualizada con avatar
                    string queryUsuario = @"
                        INSERT INTO usuario 
                        (dni, nombre, apellido, telefono, email, direccion, password, rol, estado, avatar) 
                        VALUES (@dni, @nombre, @apellido, @telefono, @email, @direccion, @password, @rol, @estado, @avatar);
                        SELECT LAST_INSERT_ID();";

                    Console.WriteLine($"Query usuario: {queryUsuario}");

                    int idUsuario;
                    using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                    {
                        // ✅ USAR LOS DATOS DEL USUARIO CREADO POR EL MÉTODO ESTÁTICO
                        commandUsuario.Parameters.AddWithValue("@dni", usuarioCompleto.Dni);
                        commandUsuario.Parameters.AddWithValue("@nombre", usuarioCompleto.Nombre);
                        commandUsuario.Parameters.AddWithValue("@apellido", usuarioCompleto.Apellido);
                        commandUsuario.Parameters.AddWithValue("@telefono", usuarioCompleto.Telefono ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@email", usuarioCompleto.Email ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@direccion", usuarioCompleto.Direccion ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@password", usuarioCompleto.Password); // Ya viene hasheado del método estático
                        commandUsuario.Parameters.AddWithValue("@rol", usuarioCompleto.Rol); //  Será "propietario"
                        commandUsuario.Parameters.AddWithValue("@estado", usuarioCompleto.Estado); // "activo"
                        commandUsuario.Parameters.AddWithValue("@avatar", usuarioCompleto.Avatar ?? (object)DBNull.Value); // Avatar con iniciales

                        Console.WriteLine($"Insertando usuario con rol: {usuarioCompleto.Rol}");
                        Console.WriteLine($"Insertando usuario con avatar: {usuarioCompleto.Avatar}");

                        var result = await commandUsuario.ExecuteScalarAsync();
                        Console.WriteLine($"Resultado ExecuteScalar: {result}");

                        if (result == null)
                        {
                            Console.WriteLine("ERROR: ExecuteScalar devolvió NULL");
                            return false;
                        }

                        idUsuario = Convert.ToInt32(result);
                        Console.WriteLine($"ID Usuario creado: {idUsuario}");
                    }

                    Console.WriteLine("=== CREANDO PROPIETARIO ===");

                    // Crear propietario (esta parte no cambia)
                    string queryPropietario = @"
                        INSERT INTO propietario 
                        (id_usuario, fecha_alta, estado) 
                        VALUES (@id_usuario, @fecha_alta, @estado)";

                    Console.WriteLine($"Query propietario: {queryPropietario}");

                    using (var commandPropietario = new MySqlCommand(queryPropietario, connection, transaction))
                    {
                        commandPropietario.Parameters.AddWithValue("@id_usuario", idUsuario);
                        commandPropietario.Parameters.AddWithValue("@fecha_alta", DateTime.Now);
                        commandPropietario.Parameters.AddWithValue("@estado", true);

                        Console.WriteLine("Ejecutando INSERT propietario...");
                        int rowsAffected = await commandPropietario.ExecuteNonQueryAsync();
                        Console.WriteLine($"Filas afectadas en propietario: {rowsAffected}");
                    }

                    Console.WriteLine("Haciendo COMMIT...");
                    await transaction.CommitAsync();
                    Console.WriteLine("=== REPOSITORIO: ÉXITO ===");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== ERROR EN TRANSACCIÓN: {ex.Message} ===");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    Console.WriteLine("Haciendo ROLLBACK...");
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN CONEXIÓN: {ex.Message} ===");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }
        // MÉTODO Edit
        public async Task<bool> ActualizarPropietarioConTransaccionAsync(Propietario propietario)
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
                    commandDni.Parameters.AddWithValue("@dni", propietario.Usuario.Dni);
                    commandDni.Parameters.AddWithValue("@id_usuario", propietario.IdUsuario);
                    if (Convert.ToInt32(await commandDni.ExecuteScalarAsync()) > 0)
                    {
                        return false; // DNI duplicado
                    }
                }

                // Verificar Email único 
                if (!string.IsNullOrEmpty(propietario.Usuario.Email))
                {
                    string queryEmail = "SELECT COUNT(*) FROM usuario WHERE email = @email AND id_usuario != @id_usuario";
                    using (var commandEmail = new MySqlCommand(queryEmail, connection, transaction))
                    {
                        commandEmail.Parameters.AddWithValue("@email", propietario.Usuario.Email);
                        commandEmail.Parameters.AddWithValue("@id_usuario", propietario.IdUsuario);
                        if (Convert.ToInt32(await commandEmail.ExecuteScalarAsync()) > 0)
                        {
                            return false; // Email duplicado
                        }
                    }
                }

                // Actualizar usuario
                string queryUsuario = @"
            UPDATE usuario 
            SET dni = @dni, nombre = @nombre, apellido = @apellido, telefono = @telefono, 
                email = @email, direccion = @direccion, estado = @estado
            WHERE id_usuario = @id_usuario";

                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@dni", propietario.Usuario.Dni);
                    commandUsuario.Parameters.AddWithValue("@nombre", propietario.Usuario.Nombre);
                    commandUsuario.Parameters.AddWithValue("@apellido", propietario.Usuario.Apellido);
                    commandUsuario.Parameters.AddWithValue("@telefono", propietario.Usuario.Telefono ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@email", propietario.Usuario.Email ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@direccion", propietario.Usuario.Direccion ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@estado", propietario.Estado ? "activo" : "inactivo");
                    commandUsuario.Parameters.AddWithValue("@id_usuario", propietario.IdUsuario);

                    await commandUsuario.ExecuteNonQueryAsync();
                }

                // Actualizar propietario
                string queryPropietario = @"
            UPDATE propietario 
            SET estado = @estado 
            WHERE id_propietario = @id_propietario";

                using (var commandPropietario = new MySqlCommand(queryPropietario, connection, transaction))
                {
                    commandPropietario.Parameters.AddWithValue("@estado", propietario.Estado);
                    commandPropietario.Parameters.AddWithValue("@id_propietario", propietario.IdPropietario);

                    await commandPropietario.ExecuteNonQueryAsync();
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
        public async Task<bool> EliminarPropietarioConTransaccionAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(); // ← CORREGIDO: usar await
            using var transaction = await connection.BeginTransactionAsync(); // ← CORREGIDO: usar await

            try
            {
                // Obtener id_usuario
                int idUsuario;
                string queryGetUsuario = "SELECT id_usuario FROM propietario WHERE id_propietario = @id_propietario";
                using (var commandGet = new MySqlCommand(queryGetUsuario, connection, transaction))
                {
                    commandGet.Parameters.AddWithValue("@id_propietario", id);
                    idUsuario = Convert.ToInt32(await commandGet.ExecuteScalarAsync()); // ← CORREGIDO: usar await
                }

                // Actualizar estado en propietario
                string queryPropietario = "UPDATE propietario SET estado = @estado WHERE id_propietario = @id_propietario";
                using (var commandPropietario = new MySqlCommand(queryPropietario, connection, transaction))
                {
                    commandPropietario.Parameters.AddWithValue("@estado", false);
                    commandPropietario.Parameters.AddWithValue("@id_propietario", id);
                    await commandPropietario.ExecuteNonQueryAsync(); // ← CORREGIDO: usar await
                }

                // Actualizar estado en usuario
                string queryUsuario = "UPDATE usuario SET estado = @estado WHERE id_usuario = @id_usuario";
                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@estado", "inactivo");
                    commandUsuario.Parameters.AddWithValue("@id_usuario", idUsuario);
                    await commandUsuario.ExecuteNonQueryAsync(); // ← CORREGIDO: usar await
                }

                await transaction.CommitAsync(); // ← CORREGIDO: usar await
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(); // ← CORREGIDO: usar await
                return false;
            }
        }

        public async Task<Propietario?> ObtenerPropietarioPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
                   u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion
            FROM propietario p 
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
            WHERE p.id_propietario = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Propietario
                    {
                        IdPropietario = reader.GetInt32(0),  // p.id_propietario
                        IdUsuario = reader.GetInt32(1),      // p.id_usuario
                        FechaAlta = reader.IsDBNull(2) ? null : reader.GetDateTime(2), // p.fecha_alta
                        Estado = reader.GetBoolean(3),       // p.estado
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
        public async Task<(IList<Propietario> propietarios, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
     int pagina, string buscar, int itemsPorPagina, string estadoFiltro = "activos")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Construir la consulta base con JOIN y WHERE inicial
                string baseQuery = @"
            FROM propietario p 
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
            WHERE 1=1 ";  

                if (estadoFiltro == "activos")
                    baseQuery += " AND p.estado = true";
                else if (estadoFiltro == "inactivos")
                    baseQuery += " AND p.estado = false";

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
            SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
                   u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion
            {whereClause}
            ORDER BY p.id_propietario 
            LIMIT @limit OFFSET @offset";

                var propietarios = new List<Propietario>();

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
                        propietarios.Add(new Propietario
                        {
                            IdPropietario = reader.GetInt32(0),
                            IdUsuario = reader.GetInt32(1),
                            FechaAlta = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                            Estado = reader.GetBoolean(3),
                            Usuario = new Usuario
                            {
                                IdUsuario = reader.GetInt32(4),
                                Dni = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Nombre = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Apellido = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Telefono = reader.IsDBNull(8) ? null : reader.GetString(8),
                                Email = reader.IsDBNull(9) ? null : reader.GetString(9),
                                Direccion = reader.IsDBNull(10) ? null : reader.GetString(10)
                            }
                        });
                    }
                }

                return (propietarios, totalRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener propietarios con paginación: {ex.Message}", ex);
            }
        }
        // PAra el contrato VEnta

        public async Task<IEnumerable<Propietario>> ObtenerTodosAsync()
{
    using var connection = new MySqlConnection(_connectionString);
    await connection.OpenAsync();

    string query = @"
        SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
               u.nombre, u.apellido, u.dni, u.email, u.telefono, u.direccion, u.avatar
        FROM propietario p
        INNER JOIN usuario u ON p.id_usuario = u.id_usuario
        WHERE p.estado = true AND u.estado = 'activo'
        ORDER BY u.nombre, u.apellido";

    using var command = new MySqlCommand(query, connection);
    
    var propietarios = new List<Propietario>();
    using var reader = await command.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        propietarios.Add(new Propietario
        {
            IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
            IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
            FechaAlta = reader.GetDateTime(reader.GetOrdinal("fecha_alta")),
            Estado = reader.GetBoolean(reader.GetOrdinal("estado")),
            Usuario = new Usuario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                Dni = reader.GetString(reader.GetOrdinal("dni")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Telefono = reader.GetString(reader.GetOrdinal("telefono")),
                Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                Avatar = reader.IsDBNull(reader.GetOrdinal("avatar")) ? null : reader.GetString(reader.GetOrdinal("avatar"))
            }
        });
    }

    return propietarios;
}
    }

}
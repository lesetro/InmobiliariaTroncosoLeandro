
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

            // QUERY MODIFICADA - Sin filtros de estado
            string query = @"
        SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
               u.id_usuario, u.nombre, u.apellido, u.dni, u.email, u.telefono, u.direccion, u.avatar, u.estado as usuario_estado
        FROM propietario p
        INNER JOIN usuario u ON p.id_usuario = u.id_usuario
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
                        Avatar = reader.IsDBNull(reader.GetOrdinal("avatar")) ? null : reader.GetString(reader.GetOrdinal("avatar")),
                        Estado = reader.GetString(reader.GetOrdinal("usuario_estado"))
                    }
                });
            }

            return propietarios;
        }

        //Propietario
        public async Task<IList<Pago>> ObtenerPagosPorPropietarioAsync(int propietarioId, DateTime? fechaInicio = null, DateTime? fechaFin = null)
{
    var pagos = new List<Pago>();

    try
    {
        using var connection = new MySqlConnection(_connectionString);

        // CORREGIR NOMBRES DE TABLAS (usar singular y minúsculas como en tu modelo)
        var query = @"
            SELECT p.*, c.*, i.*, 
                   u_inq.id_usuario AS inq_id, u_inq.nombre AS inq_nombre, u_inq.apellido AS inq_apellido, u_inq.email AS inq_email,
                   u_prop.id_usuario AS prop_id, u_prop.nombre AS prop_nombre, u_prop.apellido AS prop_apellido, u_prop.email AS prop_email
            FROM pago p  -- ← CORREGIDO: 'pago' en lugar de 'Pagos'
            INNER JOIN contrato c ON p.id_contrato = c.id_contrato  
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble  
            INNER JOIN usuario u_inq ON c.id_inquilino = u_inq.id_usuario 
            INNER JOIN usuario u_prop ON i.id_propietario = u_prop.id_usuario  
            WHERE i.id_propietario = @PropietarioId";  

        if (fechaInicio.HasValue)
        {
            query += " AND p.fecha_pago >= @FechaInicio";  
        }

        if (fechaFin.HasValue)
        {
            query += " AND p.fecha_pago <= @FechaFin";  
        }

        query += " ORDER BY p.fecha_pago DESC";  

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PropietarioId", propietarioId);

        if (fechaInicio.HasValue)
        {
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
        }

        if (fechaFin.HasValue)
        {
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value);
        }

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var pago = new Pago
            {
                IdPago = reader.GetInt32(reader.GetOrdinal("id_pago")),  
                IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),  
                NumeroPago = reader.GetInt32(reader.GetOrdinal("numero_pago")),  
                FechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago")),  
                MontoBase = reader.GetDecimal(reader.GetOrdinal("monto_base")),  
                MontoTotal = reader.GetDecimal(reader.GetOrdinal("monto_total")),  
                Estado = reader.GetString(reader.GetOrdinal("estado")), 
                DiasMora = reader.IsDBNull(reader.GetOrdinal("dias_mora")) ? 0 : reader.GetInt32(reader.GetOrdinal("dias_mora")),  
                RecargoMora = reader.IsDBNull(reader.GetOrdinal("recargo_mora")) ? 0 : reader.GetDecimal(reader.GetOrdinal("recargo_mora")),  
                FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("fecha_anulacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_anulacion")),  
                IdUsuarioAnulador = reader.IsDBNull(reader.GetOrdinal("id_usuario_anulador")) ? null : reader.GetInt32(reader.GetOrdinal("id_usuario_anulador"))  
            };

            
            pago.Contrato = new Contrato
            {
                IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                IdInquilino = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                Estado = reader.GetString(reader.GetOrdinal("estado"))
            };

            // ... el resto del mapeo igual pero con nombres de campos en minúsculas
            pago.Contrato.Inmueble = new Inmueble
            {
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                FechaAlta = reader.GetDateTime(reader.GetOrdinal("fecha_alta"))
            };

            pagos.Add(pago);
        }
    }
    catch (Exception ex)
    {
        throw new Exception($"Error al obtener pagos del propietario: {ex.Message}", ex);
    }

    return pagos;
}
        public async Task<IList<Contrato>> ObtenerContratosPorPropietarioAsync(int propietarioId)
{
    var contratos = new List<Contrato>();

    try
    {
        using var connection = new MySqlConnection(_connectionString);

        // QUERY CORREGIDA - usando los nombres reales de las tablas y columnas
        var query = @"
            SELECT c.*, 
                   i.id_inmueble, i.direccion, i.estado AS inmueble_estado, i.fecha_alta,
                   u_inq.id_usuario AS inq_id_usuario, u_inq.nombre AS inq_nombre, u_inq.apellido AS inq_apellido, u_inq.email AS inq_email,
                   u_prop.id_usuario AS prop_id_usuario, u_prop.nombre AS prop_nombre, u_prop.apellido AS prop_apellido, u_prop.email AS prop_email
            FROM contrato c
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario u_inq ON inq.id_usuario = u_inq.id_usuario
            INNER JOIN propietario p ON c.id_propietario = p.id_propietario
            INNER JOIN usuario u_prop ON p.id_usuario = u_prop.id_usuario
            WHERE c.id_propietario = @PropietarioId
            ORDER BY c.fecha_inicio DESC";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PropietarioId", propietarioId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var contrato = new Contrato
            {
                IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                IdInquilino = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                // Campos opcionales - manejar nulos
                FechaFinAnticipada = reader.IsDBNull(reader.GetOrdinal("fecha_fin_anticipada")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_fin_anticipada")),
                MultaAplicada = reader.GetDecimal(reader.GetOrdinal("multa_aplicada")),
                IdUsuarioCreador = reader.GetInt32(reader.GetOrdinal("id_usuario_creador")),
                IdUsuarioTerminador = reader.IsDBNull(reader.GetOrdinal("id_usuario_terminador")) ? null : reader.GetInt32(reader.GetOrdinal("id_usuario_terminador")),
                TipoContrato = reader.GetString(reader.GetOrdinal("tipo_contrato"))
            };

            // Mapear Inmueble
            contrato.Inmueble = new Inmueble
            {
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                Estado = reader.GetString(reader.GetOrdinal("inmueble_estado")),
                FechaAlta = reader.GetDateTime(reader.GetOrdinal("fecha_alta"))
            };

            // Mapear Inquilino
            contrato.Inquilino = new Inquilino
            {
                IdInquilino = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("inq_id_usuario")),
                Usuario = new Usuario
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("inq_id_usuario")),
                    Nombre = reader.GetString(reader.GetOrdinal("inq_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("inq_apellido")),
                    Email = reader.GetString(reader.GetOrdinal("inq_email"))
                }
            };

            // Mapear Propietario
            contrato.Propietario = new Propietario
            {
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("prop_id_usuario")),
                Usuario = new Usuario
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("prop_id_usuario")),
                    Nombre = reader.GetString(reader.GetOrdinal("prop_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("prop_apellido")),
                    Email = reader.GetString(reader.GetOrdinal("prop_email"))
                }
            };

            contratos.Add(contrato);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ERROR EN ObtenerContratosPorPropietarioAsync: {ex.Message} ===");
        Console.WriteLine($"=== StackTrace: {ex.StackTrace} ===");
        throw new Exception($"Error al obtener contratos del propietario: {ex.Message}", ex);
    }

    return contratos;
}
        public async Task<IEnumerable<Inmueble>> ObtenerInmueblesPorPropietarioAsync(int propietarioId)
{
    var inmuebles = new List<Inmueble>();

    try
    {
        using var connection = new MySqlConnection(_connectionString);

        // QUERY CORREGIDA - usando nombres reales de tablas y columnas
        var query = @"
            SELECT i.*, 
                   p.id_propietario, p.id_usuario AS prop_id_usuario,
                   u.nombre AS prop_nombre, u.apellido AS prop_apellido, u.email AS prop_email,
                   ti.id_tipo_inmueble, ti.nombre AS tipo_nombre
            FROM inmueble i
            INNER JOIN propietario p ON i.id_propietario = p.id_propietario
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario
            INNER JOIN tipo_inmueble ti ON i.id_tipo_inmueble = ti.id_tipo_inmueble
            WHERE i.id_propietario = @PropietarioId
            ORDER BY i.fecha_alta DESC";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PropietarioId", propietarioId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var inmueble = new Inmueble
            {
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                Uso = reader.GetString(reader.GetOrdinal("uso")),
                Ambientes = reader.GetInt32(reader.GetOrdinal("ambientes")),
                Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                FechaAlta = reader.GetDateTime(reader.GetOrdinal("fecha_alta"))
            };

            // Campos opcionales - manejar nulos
            if (!reader.IsDBNull(reader.GetOrdinal("coordenadas")))
                inmueble.Coordenadas = reader.GetString(reader.GetOrdinal("coordenadas"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("url_portada")))
                inmueble.UrlPortada = reader.GetString(reader.GetOrdinal("url_portada"));

            if (!reader.IsDBNull(reader.GetOrdinal("id_usuario_creador")))
                inmueble.IdUsuarioCreador = reader.GetInt32(reader.GetOrdinal("id_usuario_creador"));

            // Mapear Propietario
            inmueble.Propietario = new Propietario
            {
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("prop_id_usuario")),
                Usuario = new Usuario
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("prop_id_usuario")),
                    Nombre = reader.GetString(reader.GetOrdinal("prop_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("prop_apellido")),
                    Email = reader.GetString(reader.GetOrdinal("prop_email"))
                }
            };

            // Mapear TipoInmueble
            inmueble.TipoInmueble = new TipoInmueble
            {
                IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                Nombre = reader.GetString(reader.GetOrdinal("tipo_nombre"))
            };

            inmuebles.Add(inmueble);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ERROR EN ObtenerInmueblesPorPropietarioAsync: {ex.Message} ===");
        Console.WriteLine($"=== StackTrace: {ex.StackTrace} ===");
        throw new Exception($"Error al obtener inmuebles del propietario: {ex.Message}", ex);
    }

    return inmuebles;
}

        public async Task<Propietario?> GetByIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var query = @"
            SELECT p.*, u.*
            FROM Propietario p
            INNER JOIN Usuario u ON p.IdUsuario = u.IdUsuario
            WHERE p.IdPropietario = @Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var propietario = new Propietario
                    {
                        IdPropietario = reader.GetInt32(reader.GetOrdinal("IdPropietario")),
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                        FechaAlta = reader.GetDateTime(reader.GetOrdinal("FechaAlta")),
                        Estado = reader.GetBoolean(reader.GetOrdinal("Estado"))
                    };

                    // Mapear Usuario
                    propietario.Usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("Apellido")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Dni = reader.GetString(reader.GetOrdinal("Dni")),
                        Telefono = reader.GetString(reader.GetOrdinal("Telefono")),
                        Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado"))
                    };

                    return propietario;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener propietario por ID: {ex.Message}", ex);
            }
        }



        public async Task<Inquilino?> ObtenerInquilinoPorIdAsync(int idInquilino)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var query = @"
            SELECT i.*, u.*
            FROM Inquilino i
            INNER JOIN Usuario u ON i.IdUsuario = u.IdUsuario
            WHERE i.IdInquilino = @IdInquilino";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdInquilino", idInquilino);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var inquilino = new Inquilino
                    {
                        IdInquilino = reader.GetInt32(reader.GetOrdinal("IdInquilino")),
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                        FechaAlta = reader.GetDateTime(reader.GetOrdinal("FechaAlta")),
                        Estado = reader.GetBoolean(reader.GetOrdinal("Estado"))
                    };

                    // Mapear Usuario
                    inquilino.Usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("Apellido")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Dni = reader.GetString(reader.GetOrdinal("Dni")),
                        Telefono = reader.GetString(reader.GetOrdinal("Telefono")),
                        Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado"))
                    };

                    return inquilino;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener inquilino por ID: {ex.Message}", ex);
            }
        }

        public async Task<Propietario?> ObtenerPorUsuarioIdAsync(int usuarioId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
                   u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion, u.rol, u.estado as usuario_estado, u.avatar
            FROM propietario p 
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
            WHERE p.id_usuario = @usuarioId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@usuarioId", usuarioId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Propietario
                    {
                        IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                        FechaAlta = reader.IsDBNull(reader.GetOrdinal("fecha_alta")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_alta")),
                        Estado = reader.GetBoolean(reader.GetOrdinal("estado")),
                        Usuario = new Usuario
                        {
                            IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                            Dni = reader.IsDBNull(reader.GetOrdinal("dni")) ? null : reader.GetString(reader.GetOrdinal("dni")),
                            Nombre = reader.IsDBNull(reader.GetOrdinal("nombre")) ? null : reader.GetString(reader.GetOrdinal("nombre")),
                            Apellido = reader.IsDBNull(reader.GetOrdinal("apellido")) ? null : reader.GetString(reader.GetOrdinal("apellido")),
                            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                            Direccion = reader.IsDBNull(reader.GetOrdinal("direccion")) ? null : reader.GetString(reader.GetOrdinal("direccion")),
                            Rol = reader.IsDBNull(reader.GetOrdinal("rol")) ? null : reader.GetString(reader.GetOrdinal("rol")),
                            Estado = reader.IsDBNull(reader.GetOrdinal("usuario_estado")) ? null : reader.GetString(reader.GetOrdinal("usuario_estado")),
                            Avatar = reader.IsDBNull(reader.GetOrdinal("avatar")) ? null : reader.GetString(reader.GetOrdinal("avatar"))
                        }
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener propietario por usuario ID: {ex.Message}", ex);
            }
        }

        public async Task<int> ObtenerIdPropietarioPorUsuarioAsync(int idUsuario)
{
    try
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = "SELECT id_propietario FROM propietario WHERE id_usuario = @idUsuario";
        
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@idUsuario", idUsuario);

        var result = await command.ExecuteScalarAsync();
        
        if (result != null)
        {
            int idPropietario = Convert.ToInt32(result);
            Console.WriteLine($"=== REPOSITORIO: id_usuario={idUsuario} -> id_propietario={idPropietario} ===");
            return idPropietario;
        }
        
        Console.WriteLine($"=== REPOSITORIO: No se encontró propietario para usuario {idUsuario} ===");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en ObtenerIdPropietarioPorUsuarioAsync: {ex.Message}");
        return 0;
    }
}
    }

}
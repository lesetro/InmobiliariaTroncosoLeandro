using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;
using BCrypt.Net;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioUsuario : IRepositorioUsuario
    {
        private readonly string _connectionString;

        public RepositorioUsuario(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // CRUD básico
        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE estado != 'eliminado'
                            ORDER BY apellido, nombre";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE id_usuario = @id AND estado != 'eliminado'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapearUsuario((MySqlDataReader)reader);
            }

            return null;
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            // Validar que no exista el email o DNI
            if (await EmailExistsAsync(usuario.Email))
                throw new InvalidOperationException("Ya existe un usuario con este email");

            if (await DniExistsAsync(usuario.Dni))
                throw new InvalidOperationException("Ya existe un usuario con este DNI");

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO usuario 
                            (email, password, rol, nombre, apellido, dni, direccion, telefono, estado, avatar) 
                            VALUES (@email, @password, @rol, @nombre, @apellido, @dni, @direccion, @telefono, @estado, @avatar);
                            SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", usuario.Email);
            command.Parameters.AddWithValue("@password", usuario.Password);
            command.Parameters.AddWithValue("@rol", usuario.Rol);
            command.Parameters.AddWithValue("@nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@apellido", usuario.Apellido);
            command.Parameters.AddWithValue("@dni", usuario.Dni);
            command.Parameters.AddWithValue("@direccion", usuario.Direccion ?? "Sin dirección especificada");
            command.Parameters.AddWithValue("@telefono", usuario.Telefono ?? "Sin especificar");
            command.Parameters.AddWithValue("@estado", usuario.Estado);
            command.Parameters.AddWithValue("@avatar", usuario.Avatar ?? (object)DBNull.Value);

            var id = await command.ExecuteScalarAsync();
            usuario.IdUsuario = Convert.ToInt32(id);

            return usuario;
        }

        public async Task<Usuario> UpdateAsync(Usuario usuario)
        {
            // IMPORTANTE: Limpiar espacios en blanco
            usuario.Email = usuario.Email?.Trim() ?? "";
            usuario.Dni = usuario.Dni?.Trim() ?? "";
            usuario.Nombre = usuario.Nombre?.Trim() ?? "";
            usuario.Apellido = usuario.Apellido?.Trim() ?? "";

            var usuarioExistente = await GetByIdAsync(usuario.IdUsuario);
            if (usuarioExistente == null)
                throw new InvalidOperationException("Usuario no encontrado");

            // DEBUG: Imprimir valores para verificar
            Console.WriteLine($"=== ACTUALIZACIÓN USUARIO {usuario.IdUsuario} ===");
            Console.WriteLine($"DNI Existente: '{usuarioExistente.Dni}' | DNI Nuevo: '{usuario.Dni}'");
            Console.WriteLine($"Email Existente: '{usuarioExistente.Email}' | Email Nuevo: '{usuario.Email}'");
            Console.WriteLine($"¿DNI cambió?: {usuarioExistente.Dni != usuario.Dni}");
            Console.WriteLine($"¿Email cambió?: {usuarioExistente.Email != usuario.Email}");

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Solo validar si el DNI cambió
            if (usuarioExistente.Dni != usuario.Dni)
            {
                string queryDni = @"SELECT COUNT(*) FROM usuario 
                           WHERE TRIM(dni) = @dni 
                           AND id_usuario != @id 
                           AND estado != 'eliminado'";

                using var commandDni = new MySqlCommand(queryDni, connection);
                commandDni.Parameters.AddWithValue("@dni", usuario.Dni);
                commandDni.Parameters.AddWithValue("@id", usuario.IdUsuario);

                var countDni = Convert.ToInt32(await commandDni.ExecuteScalarAsync());

                Console.WriteLine($"Usuarios con DNI '{usuario.Dni}' (excluyendo ID {usuario.IdUsuario}): {countDni}");

                if (countDni > 0)
                {
                    // Buscar quién tiene ese DNI para más información
                    string queryWho = @"SELECT id_usuario, nombre, apellido FROM usuario 
                               WHERE TRIM(dni) = @dni 
                               AND id_usuario != @id 
                               AND estado != 'eliminado'";

                    using var cmdWho = new MySqlCommand(queryWho, connection);
                    cmdWho.Parameters.AddWithValue("@dni", usuario.Dni);
                    cmdWho.Parameters.AddWithValue("@id", usuario.IdUsuario);

                    using var readerWho = await cmdWho.ExecuteReaderAsync();
                    if (await readerWho.ReadAsync())
                    {
                        var conflictId = readerWho.GetInt32(0);
                        var conflictNombre = readerWho.GetString(1);
                        var conflictApellido = readerWho.GetString(2);
                        Console.WriteLine($"DNI ya usado por: Usuario ID {conflictId} - {conflictNombre} {conflictApellido}");
                    }

                    throw new InvalidOperationException($"Ya existe otro usuario con el DNI {usuario.Dni}");
                }
            }

            // Solo validar si el Email cambió
            if (usuarioExistente.Email != usuario.Email)
            {
                string queryEmail = @"SELECT COUNT(*) FROM usuario 
                             WHERE TRIM(LOWER(email)) = LOWER(@email) 
                             AND id_usuario != @id 
                             AND estado != 'eliminado'";

                using var commandEmail = new MySqlCommand(queryEmail, connection);
                commandEmail.Parameters.AddWithValue("@email", usuario.Email);
                commandEmail.Parameters.AddWithValue("@id", usuario.IdUsuario);

                var countEmail = Convert.ToInt32(await commandEmail.ExecuteScalarAsync());

                Console.WriteLine($"Usuarios con Email '{usuario.Email}' (excluyendo ID {usuario.IdUsuario}): {countEmail}");

                if (countEmail > 0)
                    throw new InvalidOperationException($"Ya existe otro usuario con el email {usuario.Email}");
            }

            // Si pasa las validaciones, actualizar el usuario
            string query = @"UPDATE usuario SET 
                    email = @email, 
                    password = @password, 
                    rol = @rol, 
                    nombre = @nombre, 
                    apellido = @apellido, 
                    dni = @dni, 
                    direccion = @direccion, 
                    telefono = @telefono, 
                    estado = @estado, 
                    avatar = @avatar
                    WHERE id_usuario = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", usuario.IdUsuario);
            command.Parameters.AddWithValue("@email", usuario.Email);
            command.Parameters.AddWithValue("@password", usuario.Password);
            command.Parameters.AddWithValue("@rol", usuario.Rol);
            command.Parameters.AddWithValue("@nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@apellido", usuario.Apellido);
            command.Parameters.AddWithValue("@dni", usuario.Dni);
            command.Parameters.AddWithValue("@direccion", usuario.Direccion ?? "Sin dirección especificada");
            command.Parameters.AddWithValue("@telefono", usuario.Telefono ?? "Sin especificar");
            command.Parameters.AddWithValue("@estado", usuario.Estado);
            command.Parameters.AddWithValue("@avatar", string.IsNullOrEmpty(usuario.Avatar) ? (object)DBNull.Value : usuario.Avatar);

            var filasAfectadas = await command.ExecuteNonQueryAsync();

            Console.WriteLine($"Filas actualizadas: {filasAfectadas}");

            if (filasAfectadas == 0)
                throw new InvalidOperationException("No se pudo actualizar el usuario");

            return usuario;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!await PuedeEliminarUsuarioAsync(id))
                return false;

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "UPDATE usuario SET estado = 'eliminado' WHERE id_usuario = @id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            var filas = await command.ExecuteNonQueryAsync();
            return filas > 0;
        }

        // Métodos específicos para autenticación
        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE email = @email AND estado = 'activo'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", email);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapearUsuario((MySqlDataReader)reader);
            }

            return null;
        }

        public async Task<Usuario?> ValidateUserAsync(string email, string password)
        {
            var usuario = await GetByEmailAsync(email);
            if (usuario == null)
                return null;

            // Verificar contraseña con BCrypt
            if (BCrypt.Net.BCrypt.Verify(password, usuario.Password))
                return usuario;

            return null;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM usuario WHERE email = @email AND estado != 'eliminado'";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", email);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<bool> DniExistsAsync(string dni)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM usuario WHERE dni = @dni AND estado != 'eliminado'";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@dni", dni);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        // Métodos específicos para autorización por roles
        public async Task<IEnumerable<Usuario>> GetByRolAsync(string rol)
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE rol = @rol AND estado = 'activo'
                            ORDER BY apellido, nombre";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@rol", rol);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        public async Task<IEnumerable<Usuario>> GetAdministradoresAsync() => await GetByRolAsync("administrador");
        public async Task<IEnumerable<Usuario>> GetEmpleadosAsync() => await GetByRolAsync("empleado");
        public async Task<IEnumerable<Usuario>> GetPropietariosAsync() => await GetByRolAsync("propietario");
        public async Task<IEnumerable<Usuario>> GetInquilinosAsync() => await GetByRolAsync("inquilino");

        public async Task<IEnumerable<Usuario>> GetUsuariosActivosAsync()
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE estado = 'activo'
                            ORDER BY apellido, nombre";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        //Contar los usuarios activos 
        public async Task<int> GetNumeroUsuariosActivosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM usuario WHERE estado = 'activo'";

            using var command = new MySqlCommand(query, connection);
            var count = await command.ExecuteScalarAsync();

            return Convert.ToInt32(count);
        }

        public async Task<IEnumerable<Usuario>> GetUsuariosInactivosAsync()
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE estado = 'inactivo'
                            ORDER BY apellido, nombre";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        // Métodos para gestión de estado
        public async Task<bool> ActivarUsuarioAsync(int id)
        {
            return await CambiarEstadoUsuarioAsync(id, "activo");
        }

        public async Task<bool> DesactivarUsuarioAsync(int id)
        {
            return await CambiarEstadoUsuarioAsync(id, "inactivo");
        }

        public async Task<bool> CambiarRolAsync(int id, string nuevoRol)
        {
            var rolesValidos = new[] { "administrador", "empleado", "propietario", "inquilino" };
            if (!rolesValidos.Contains(nuevoRol.ToLower()))
                return false;

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "UPDATE usuario SET rol = @rol WHERE id_usuario = @id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@rol", nuevoRol.ToLower());

            var filas = await command.ExecuteNonQueryAsync();
            return filas > 0;
        }

        // Métodos para gestión de contraseñas
        public async Task<bool> CambiarPasswordAsync(int id, string nuevaPassword)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "UPDATE usuario SET password = @password WHERE id_usuario = @id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(nuevaPassword));

            var filas = await command.ExecuteNonQueryAsync();
            return filas > 0;
        }

        public async Task<bool> ResetearPasswordAsync(int id)
        {
            return await CambiarPasswordAsync(id, "PasswordTemporal123");
        }

        // Métodos para búsqueda y filtrado
        public async Task<IEnumerable<Usuario>> BuscarUsuariosAsync(string termino)
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE estado != 'eliminado' AND 
                                  (nombre LIKE @termino OR apellido LIKE @termino OR 
                                   email LIKE @termino OR dni LIKE @termino)
                            ORDER BY apellido, nombre";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@termino", $"%{termino}%");
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        public async Task<IEnumerable<Usuario>> GetUsuariosPaginadosAsync(int pagina, int registrosPorPagina)
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            int offset = (pagina - 1) * registrosPorPagina;
            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE estado != 'eliminado'
                            ORDER BY apellido, nombre
                            LIMIT @limit OFFSET @offset";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@limit", registrosPorPagina);
            command.Parameters.AddWithValue("@offset", offset);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        public async Task<int> GetTotalUsuariosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM usuario WHERE estado != 'eliminado'";
            using var command = new MySqlCommand(query, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> GetTotalUsuariosPorRolAsync(string rol)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM usuario WHERE rol = @rol AND estado = 'activo'";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@rol", rol);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        // Métodos de validación
        public async Task<bool> PuedeEliminarUsuarioAsync(int id)
        {
            var usuario = await GetByIdAsync(id);
            if (usuario == null)
                return false;

            // No permitir eliminar administradores si es el último
            if (usuario.Rol == "administrador")
            {
                var totalAdmins = await GetTotalUsuariosPorRolAsync("administrador");
                if (totalAdmins <= 1)
                    return false;
            }

            return true;
        }

        public async Task<bool> TienePermisosAdminAsync(int id)
        {
            var usuario = await GetByIdAsync(id);
            return usuario?.Rol == "administrador" && usuario.Estado == "activo";
        }

        // Métodos para estadísticas
        public async Task<Dictionary<string, int>> GetEstadisticasPorRolAsync()
        {
            var estadisticas = new Dictionary<string, int>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT rol, COUNT(*) as cantidad 
                            FROM usuario 
                            WHERE estado = 'activo' 
                            GROUP BY rol";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            // Inicializar con ceros
            estadisticas["administrador"] = 0;
            estadisticas["empleado"] = 0;
            estadisticas["propietario"] = 0;
            estadisticas["inquilino"] = 0;

            while (await reader.ReadAsync())
            {
                var rol = reader.GetString(reader.GetOrdinal("rol"));
                var cantidad = reader.GetInt32(reader.GetOrdinal("cantidad"));
                estadisticas[rol] = cantidad;
            }

            return estadisticas;
        }

        public async Task<IEnumerable<Usuario>> GetUsuariosRecientesAsync(int cantidad = 5)
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
                            direccion, telefono, estado, avatar 
                            FROM usuario 
                            WHERE estado = 'activo'
                            ORDER BY id_usuario DESC
                            LIMIT @cantidad";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@cantidad", cantidad);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(MapearUsuario((MySqlDataReader)reader));
            }

            return usuarios;
        }

        // Métodos auxiliares privados
        private Usuario MapearUsuario(MySqlDataReader reader)
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32("id_usuario"),
                Email = reader.GetString("email").Trim(),
                Password = reader.GetString("password"),
                Rol = reader.GetString("rol").Trim(),
                Nombre = reader.GetString("nombre").Trim(),
                Apellido = reader.GetString("apellido").Trim(),
                Dni = reader.GetString("dni").Trim(),
                Direccion = reader.GetString("direccion").Trim(),
                Telefono = reader.GetString("telefono").Trim(),
                Estado = reader.GetString("estado").Trim(),
                Avatar = reader.IsDBNull(reader.GetOrdinal("avatar")) ? null : reader.GetString(reader.GetOrdinal("avatar")).Trim()
            };
        }

        private async Task<bool> CambiarEstadoUsuarioAsync(int id, string nuevoEstado)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "UPDATE usuario SET estado = @estado WHERE id_usuario = @id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@estado", nuevoEstado);

            var filas = await command.ExecuteNonQueryAsync();
            return filas > 0;
        }

        private async Task<bool> EmailExistsExceptAsync(string email, int exceptoId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT COUNT(*) FROM usuario 
                    WHERE email = @email 
                    AND id_usuario != @id 
                    AND estado != 'eliminado'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@id", exceptoId);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        private async Task<bool> DniExistsExceptAsync(string dni, int exceptoId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT COUNT(*) FROM usuario 
                    WHERE dni = @dni 
                    AND id_usuario != @id 
                    AND estado != 'eliminado'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@dni", dni);
            command.Parameters.AddWithValue("@id", exceptoId);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }


        public async Task<int> GetTotalUsuariosInactivosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM usuario WHERE estado = 'inactivo'";
            using var command = new MySqlCommand(query, connection);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        // MÉTODO OPTIMIZADO PARA BÚSQUEDA + FILTRO POR ROL CON PAGINACIÓN
        public async Task<(IList<Usuario> usuarios, int totalRegistros)> ObtenerConPaginacionBusquedaYRolAsync(
    int pagina, string buscar, string rol, int itemsPorPagina, string estadoFiltro = "activos")
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Construir WHERE dinámico
            string whereClause = "WHERE u.estado != 'eliminado'";
            var parameters = new List<MySqlParameter>();

            // Filtro de ESTADO
            if (estadoFiltro == "activos")
                whereClause += " AND u.estado = 'activo'";
            else if (estadoFiltro == "inactivos")
                whereClause += " AND u.estado = 'inactivo'";
            // Si es "todos", no agregamos filtro adicional

            // Filtro de BÚSQUEDA
            if (!string.IsNullOrEmpty(buscar))
            {
                whereClause += @" AND (u.nombre LIKE @buscar 
                     OR u.apellido LIKE @buscar 
                     OR u.dni LIKE @buscar 
                     OR u.email LIKE @buscar)";
                parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            // Filtro por ROL
            if (!string.IsNullOrEmpty(rol) && rol.ToLower() != "todos los roles")
            {
                whereClause += " AND u.rol = @rol";
                parameters.Add(new MySqlParameter("@rol", rol));
            }

            // Obtener total de registros
            string countQuery = $"SELECT COUNT(*) FROM usuario u {whereClause}";

            int totalRegistros = 0;
            using (var countCommand = new MySqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                }
                var result = await countCommand.ExecuteScalarAsync();
                totalRegistros = Convert.ToInt32(result);
            }

            // Obtener registros con paginación
            int offset = (pagina - 1) * itemsPorPagina;
            string dataQuery = $@"
        SELECT u.id_usuario, u.email, u.password, u.rol, u.nombre, u.apellido, 
               u.dni, u.direccion, u.telefono, u.estado, u.avatar
        FROM usuario u
        {whereClause}
        ORDER BY u.nombre, u.apellido
        LIMIT @limit OFFSET @offset";

            var usuarios = new List<Usuario>();
            using (var dataCommand = new MySqlCommand(dataQuery, connection))
            {
                foreach (var param in parameters)
                {
                    dataCommand.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                }
                dataCommand.Parameters.AddWithValue("@limit", itemsPorPagina);
                dataCommand.Parameters.AddWithValue("@offset", offset);

                using var reader = await dataCommand.ExecuteReaderAsync();

                var idUsuarioOrdinal = reader.GetOrdinal("id_usuario");
                var emailOrdinal = reader.GetOrdinal("email");
                var passwordOrdinal = reader.GetOrdinal("password");
                var rolOrdinal = reader.GetOrdinal("rol");
                var nombreOrdinal = reader.GetOrdinal("nombre");
                var apellidoOrdinal = reader.GetOrdinal("apellido");
                var dniOrdinal = reader.GetOrdinal("dni");
                var direccionOrdinal = reader.GetOrdinal("direccion");
                var telefonoOrdinal = reader.GetOrdinal("telefono");
                var estadoOrdinal = reader.GetOrdinal("estado");
                var avatarOrdinal = reader.GetOrdinal("avatar");

                while (await reader.ReadAsync())
                {
                    usuarios.Add(new Usuario
                    {
                        IdUsuario = reader.GetInt32(idUsuarioOrdinal),
                        Email = reader.GetString(emailOrdinal),
                        Password = reader.GetString(passwordOrdinal),
                        Rol = reader.GetString(rolOrdinal),
                        Nombre = reader.GetString(nombreOrdinal),
                        Apellido = reader.GetString(apellidoOrdinal),
                        Dni = reader.GetString(dniOrdinal),
                        Direccion = reader.GetString(direccionOrdinal),
                        Telefono = reader.GetString(telefonoOrdinal),
                        Estado = reader.GetString(estadoOrdinal),
                        Avatar = reader.IsDBNull(avatarOrdinal) ? null : reader.GetString(avatarOrdinal)
                    });
                }
            }

            return (usuarios, totalRegistros);
        }
        //para el contrato venta 
        public async Task<Usuario> ObtenerPorEmailAsync(string email)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
        SELECT id_usuario, email, password, rol, nombre, apellido, dni, 
               direccion, telefono, estado, avatar
        FROM usuario 
        WHERE email = @Email AND estado = 'activo'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Usuario
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Password = reader.GetString(reader.GetOrdinal("password")),
                    Rol = reader.GetString(reader.GetOrdinal("rol")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                    Dni = reader.GetString(reader.GetOrdinal("dni")),
                    Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                    Telefono = reader.GetString(reader.GetOrdinal("telefono")),
                    Estado = reader.GetString(reader.GetOrdinal("estado")),
                    Avatar = reader.IsDBNull(reader.GetOrdinal("avatar")) ? null : reader.GetString(reader.GetOrdinal("avatar"))
                };
            }

            return null;
        }
        public async Task<Usuario?> GetUsuarioPorPropietarioIdAsync(int idPropietario)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var query = @"
            SELECT u.*
            FROM Usuario u
            INNER JOIN Propietario p ON u.IdUsuario = p.IdUsuario
            WHERE p.IdPropietario = @IdPropietario";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdPropietario", idPropietario);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("Apellido")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Dni = reader.GetString(reader.GetOrdinal("Dni")),
                        Telefono = reader.GetString(reader.GetOrdinal("Telefono")),
                        Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                        Rol = reader.GetString(reader.GetOrdinal("Rol")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado")),
                        Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString(reader.GetOrdinal("Avatar"))
                    };

                    return usuario;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener usuario por ID de propietario: {ex.Message}", ex);
            }
        }


    }
}
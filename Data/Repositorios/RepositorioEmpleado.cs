using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioEmpleado : IRepositorioEmpleado
    {
        private readonly string _connectionString;

        public RepositorioEmpleado(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // DASHBOARD
        public async Task<EmpleadoDashboardDto> GetDashboardEmpleadoDataAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var dashboard = new EmpleadoDashboardDto();

            // Contar usuarios (propietarios e inquilinos)
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM usuario WHERE rol IN ('propietario', 'inquilino') AND estado = 'activo'", connection))
            {
                dashboard.TotalUsuarios = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Contar inmuebles
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM inmueble WHERE estado != 'eliminado'", connection))
            {
                dashboard.TotalInmuebles = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Contar contratos
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM contrato", connection))
            {
                dashboard.TotalContratos = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Contar intereses
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM interes_inmueble", connection))
            {
                dashboard.TotalIntereses = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            return dashboard;
        }

        // USUARIOS
        public async Task<(IList<Usuario> usuarios, int totalRegistros)> ObtenerUsuariosParaEmpleadoAsync(
            int pagina, string buscar, string rol, int itemsPorPagina)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string whereClause = "WHERE u.estado = 'activo' AND u.rol IN ('propietario', 'inquilino')";
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(buscar))
            {
                whereClause += " AND (u.nombre LIKE @buscar OR u.apellido LIKE @buscar OR u.email LIKE @buscar)";
                parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            if (!string.IsNullOrEmpty(rol) && (rol == "propietario" || rol == "inquilino"))
            {
                whereClause += " AND u.rol = @rol";
                parameters.Add(new MySqlParameter("@rol", rol));
            }

            // Contar total
            string countQuery = $"SELECT COUNT(*) FROM usuario u {whereClause}";
            int totalRegistros;
            using (var countCmd = new MySqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                    countCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                totalRegistros = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            // Obtener datos paginados
            int offset = (pagina - 1) * itemsPorPagina;
            string dataQuery = $@"
                SELECT u.id_usuario, u.email, u.password, u.rol, u.nombre, u.apellido, u.dni, u.direccion, u.telefono, u.estado
                FROM usuario u {whereClause}
                ORDER BY u.nombre, u.apellido
                LIMIT @limit OFFSET @offset";

            var usuarios = new List<Usuario>();
            using (var dataCmd = new MySqlCommand(dataQuery, connection))
            {
                foreach (var param in parameters)
                    dataCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                dataCmd.Parameters.AddWithValue("@limit", itemsPorPagina);
                dataCmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await dataCmd.ExecuteReaderAsync();

                // GetOrdinal para mapeo optimizado
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
                        Estado = reader.GetString(estadoOrdinal)
                    });
                }
            }

            return (usuarios, totalRegistros);
        }

        public async Task<Usuario> CrearPropietarioAsync(Usuario propietario)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO usuario (email, password, rol, nombre, apellido, dni, direccion, telefono, estado) 
                            VALUES (@email, @password, 'propietario', @nombre, @apellido, @dni, @direccion, @telefono, 'activo');
                            SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@email", propietario.Email);
            cmd.Parameters.AddWithValue("@password", propietario.Password);
            cmd.Parameters.AddWithValue("@nombre", propietario.Nombre);
            cmd.Parameters.AddWithValue("@apellido", propietario.Apellido);
            cmd.Parameters.AddWithValue("@dni", propietario.Dni);
            cmd.Parameters.AddWithValue("@direccion", propietario.Direccion ?? "");
            cmd.Parameters.AddWithValue("@telefono", propietario.Telefono ?? "");

            var nuevoId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            propietario.IdUsuario = nuevoId;
            propietario.Rol = "propietario";
            propietario.Estado = "activo";
            
            return propietario;
        }

        public async Task<Usuario> CrearInquilinoAsync(Usuario inquilino)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO usuario (email, password, rol, nombre, apellido, dni, direccion, telefono, estado) 
                            VALUES (@email, @password, 'inquilino', @nombre, @apellido, @dni, @direccion, @telefono, 'activo');
                            SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@email", inquilino.Email);
            cmd.Parameters.AddWithValue("@password", inquilino.Password);
            cmd.Parameters.AddWithValue("@nombre", inquilino.Nombre);
            cmd.Parameters.AddWithValue("@apellido", inquilino.Apellido);
            cmd.Parameters.AddWithValue("@dni", inquilino.Dni);
            cmd.Parameters.AddWithValue("@direccion", inquilino.Direccion ?? "");
            cmd.Parameters.AddWithValue("@telefono", inquilino.Telefono ?? "");

            var nuevoId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            inquilino.IdUsuario = nuevoId;
            inquilino.Rol = "inquilino";
            inquilino.Estado = "activo";
            
            return inquilino;
        }

        public async Task<Usuario?> ObtenerEmpleadoPorIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, email, password, rol, nombre, apellido, dni, direccion, telefono, estado 
                            FROM usuario WHERE id_usuario = @id AND rol = 'empleado'";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                // GetOrdinal para mapeo optimizado
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

                return new Usuario
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
                    Estado = reader.GetString(estadoOrdinal)
                };
            }
            return null;
        }

        // INMUEBLES
        public async Task<(IList<Inmueble> inmuebles, int totalRegistros)> ObtenerInmueblesConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string whereClause = "WHERE i.estado != 'eliminado'";
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(buscar))
            {
                whereClause += " AND i.direccion LIKE @buscar";
                parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                whereClause += " AND i.estado = @estado";
                parameters.Add(new MySqlParameter("@estado", estado));
            }

            // Contar total
            string countQuery = $"SELECT COUNT(*) FROM inmueble i {whereClause}";
            int totalRegistros;
            using (var countCmd = new MySqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                    countCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                totalRegistros = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            // Obtener datos paginados
            int offset = (pagina - 1) * itemsPorPagina;
            string dataQuery = $@"
                SELECT i.id_inmueble, i.direccion, i.uso, i.ambientes, i.precio, i.coordenadas, i.estado, 
                       i.id_propietario, i.id_tipo_inmueble,
                       p.nombre as prop_nombre, p.apellido as prop_apellido
                FROM inmueble i
                LEFT JOIN usuario p ON i.id_propietario = p.id_usuario
                {whereClause}
                ORDER BY i.direccion
                LIMIT @limit OFFSET @offset";

            var inmuebles = new List<Inmueble>();
            using (var dataCmd = new MySqlCommand(dataQuery, connection))
            {
                foreach (var param in parameters)
                    dataCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                dataCmd.Parameters.AddWithValue("@limit", itemsPorPagina);
                dataCmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await dataCmd.ExecuteReaderAsync();

                // GetOrdinal para mapeo optimizado
                var idInmuebleOrdinal = reader.GetOrdinal("id_inmueble");
                var direccionOrdinal = reader.GetOrdinal("direccion");
                var usoOrdinal = reader.GetOrdinal("uso");
                var ambientesOrdinal = reader.GetOrdinal("ambientes");
                var precioOrdinal = reader.GetOrdinal("precio");
                var coordenadasOrdinal = reader.GetOrdinal("coordenadas");
                var estadoOrdinal = reader.GetOrdinal("estado");
                var idPropietarioOrdinal = reader.GetOrdinal("id_propietario");
                var idTipoInmuebleOrdinal = reader.GetOrdinal("id_tipo_inmueble");
                var propNombreOrdinal = reader.GetOrdinal("prop_nombre");
                var propApellidoOrdinal = reader.GetOrdinal("prop_apellido");

                while (await reader.ReadAsync())
                {
                    var inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32(idInmuebleOrdinal),
                        Direccion = reader.GetString(direccionOrdinal),
                        Uso = reader.GetString(usoOrdinal),
                        Ambientes = reader.GetInt32(ambientesOrdinal),
                        Precio = reader.GetDecimal(precioOrdinal),
                        Coordenadas = reader.IsDBNull(coordenadasOrdinal) ? null : reader.GetString(coordenadasOrdinal),
                        Estado = reader.GetString(estadoOrdinal),
                        IdPropietario = reader.GetInt32(idPropietarioOrdinal),
                        IdTipoInmueble = reader.GetInt32(idTipoInmuebleOrdinal)
                    };

                    // Agregar propietario si existe
                    if (!reader.IsDBNull(propNombreOrdinal))
                    {
                        inmueble.Propietario = new Propietario
                        {
                            Usuario = new Usuario
                            {
                                IdUsuario = reader.GetInt32(idPropietarioOrdinal),
                                Nombre = reader.GetString(propNombreOrdinal),
                                Apellido = reader.GetString(propApellidoOrdinal)
                            }
                        };
                    }

                    inmuebles.Add(inmueble);
                }
            }

            return (inmuebles, totalRegistros);
        }

        public async Task<bool> CrearInmuebleAsync(Inmueble inmueble, IFormFile? archivoPortada, IWebHostEnvironment environment)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO inmueble (direccion, uso, ambientes, precio, coordenadas, estado, id_propietario, id_tipo_inmueble) 
                            VALUES (@direccion, @uso, @ambientes, @precio, @coordenadas, @estado, @idPropietario, @idTipoInmueble)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@direccion", inmueble.Direccion);
            cmd.Parameters.AddWithValue("@uso", inmueble.Uso);
            cmd.Parameters.AddWithValue("@ambientes", inmueble.Ambientes);
            cmd.Parameters.AddWithValue("@precio", inmueble.Precio);
            cmd.Parameters.AddWithValue("@coordenadas", inmueble.Coordenadas ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@estado", inmueble.Estado ?? "disponible");
            cmd.Parameters.AddWithValue("@idPropietario", inmueble.IdPropietario);
            cmd.Parameters.AddWithValue("@idTipoInmueble", inmueble.IdTipoInmueble);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<Inmueble?> ObtenerInmuebleConDetallesAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT i.id_inmueble, i.direccion, i.uso, i.ambientes, i.precio, i.coordenadas, i.estado, 
                                   i.id_propietario, i.id_tipo_inmueble,
                                   p.nombre as prop_nombre, p.apellido as prop_apellido, p.email as prop_email, p.telefono as prop_telefono
                            FROM inmueble i
                            LEFT JOIN usuario p ON i.id_propietario = p.id_usuario
                            WHERE i.id_inmueble = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                // GetOrdinal para mapeo optimizado
                var idInmuebleOrdinal = reader.GetOrdinal("id_inmueble");
                var direccionOrdinal = reader.GetOrdinal("direccion");
                var usoOrdinal = reader.GetOrdinal("uso");
                var ambientesOrdinal = reader.GetOrdinal("ambientes");
                var precioOrdinal = reader.GetOrdinal("precio");
                var coordenadasOrdinal = reader.GetOrdinal("coordenadas");
                var estadoOrdinal = reader.GetOrdinal("estado");
                var idPropietarioOrdinal = reader.GetOrdinal("id_propietario");
                var idTipoInmuebleOrdinal = reader.GetOrdinal("id_tipo_inmueble");
                var propNombreOrdinal = reader.GetOrdinal("prop_nombre");
                var propApellidoOrdinal = reader.GetOrdinal("prop_apellido");
                var propEmailOrdinal = reader.GetOrdinal("prop_email");
                var propTelefonoOrdinal = reader.GetOrdinal("prop_telefono");

                var inmueble = new Inmueble
                {
                    IdInmueble = reader.GetInt32(idInmuebleOrdinal),
                    Direccion = reader.GetString(direccionOrdinal),
                    Uso = reader.GetString(usoOrdinal),
                    Ambientes = reader.GetInt32(ambientesOrdinal),
                    Precio = reader.GetDecimal(precioOrdinal),
                    Coordenadas = reader.IsDBNull(coordenadasOrdinal) ? null : reader.GetString(coordenadasOrdinal),
                    Estado = reader.GetString(estadoOrdinal),
                    IdPropietario = reader.GetInt32(idPropietarioOrdinal),
                    IdTipoInmueble = reader.GetInt32(idTipoInmuebleOrdinal)
                };

                if (!reader.IsDBNull(propNombreOrdinal))
                {
                    inmueble.Propietario = new Propietario
                    {
                        Usuario = new Usuario
                        {
                            IdUsuario = reader.GetInt32(idPropietarioOrdinal),
                            Nombre = reader.GetString(propNombreOrdinal),
                            Apellido = reader.GetString(propApellidoOrdinal),
                            Email = reader.IsDBNull(propEmailOrdinal) ? "" : reader.GetString(propEmailOrdinal),
                            Telefono = reader.IsDBNull(propTelefonoOrdinal) ? "" : reader.GetString(propTelefonoOrdinal)
                        }
                    };
                }

                return inmueble;
            }
            return null;
        }

        public async Task<bool> AgregarFotoInmuebleAsync(int idInmueble, IFormFile archivo, IWebHostEnvironment environment)
        {
            try
            {
                if (archivo == null || archivo.Length == 0) return false;

                // Crear directorio si no existe
                var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "inmuebles");
                Directory.CreateDirectory(uploadsPath);

                // Generar nombre único
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                var nombreArchivo = $"inmueble_{idInmueble}_{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(uploadsPath, nombreArchivo);

                // Guardar archivo
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // Guardar en base de datos
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "INSERT INTO imagen_inmueble (id_inmueble, url, descripcion) VALUES (@idInmueble, @url, @descripcion)";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@idInmueble", idInmueble);
                cmd.Parameters.AddWithValue("@url", $"/uploads/inmuebles/{nombreArchivo}");
                cmd.Parameters.AddWithValue("@descripcion", "Imagen agregada");

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        // CONTRATOS
        public async Task<(IList<Contrato> contratos, int totalRegistros)> ObtenerContratosConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string whereClause = "WHERE 1=1";
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(buscar))
            {
                whereClause += " AND (i.direccion LIKE @buscar OR u.nombre LIKE @buscar)";
                parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                whereClause += " AND c.estado = @estado";
                parameters.Add(new MySqlParameter("@estado", estado));
            }

            // Contar total
            string countQuery = $@"SELECT COUNT(*) FROM contrato c
                                  LEFT JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                                  LEFT JOIN usuario u ON c.id_inquilino = u.id_usuario
                                  {whereClause}";
            int totalRegistros;
            using (var countCmd = new MySqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                    countCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                totalRegistros = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            // Obtener datos paginados
            int offset = (pagina - 1) * itemsPorPagina;
            string dataQuery = $@"
                SELECT c.id_contrato, c.fecha_inicio, c.fecha_fin, c.monto_mensual, c.estado,
                       i.direccion as inmueble_direccion,
                       u.nombre as inquilino_nombre, u.apellido as inquilino_apellido
                FROM contrato c
                LEFT JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                LEFT JOIN usuario u ON c.id_inquilino = u.id_usuario
                {whereClause}
                ORDER BY c.fecha_inicio DESC
                LIMIT @limit OFFSET @offset";

            var contratos = new List<Contrato>();
            using (var dataCmd = new MySqlCommand(dataQuery, connection))
            {
                foreach (var param in parameters)
                    dataCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                dataCmd.Parameters.AddWithValue("@limit", itemsPorPagina);
                dataCmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await dataCmd.ExecuteReaderAsync();

                // GetOrdinal para mapeo optimizado
                var idContratoOrdinal = reader.GetOrdinal("id_contrato");
                var fechaInicioOrdinal = reader.GetOrdinal("fecha_inicio");
                var fechaFinOrdinal = reader.GetOrdinal("fecha_fin");
                var montoMensualOrdinal = reader.GetOrdinal("monto_mensual");
                var estadoOrdinal = reader.GetOrdinal("estado");
                var inmuebleDireccionOrdinal = reader.GetOrdinal("inmueble_direccion");
                var inquilinoNombreOrdinal = reader.GetOrdinal("inquilino_nombre");
                var inquilinoApellidoOrdinal = reader.GetOrdinal("inquilino_apellido");

                while (await reader.ReadAsync())
                {
                    contratos.Add(new Contrato
                    {
                        IdContrato = reader.GetInt32(idContratoOrdinal),
                        FechaInicio = reader.GetDateTime(fechaInicioOrdinal),
                        FechaFin = reader.GetDateTime(fechaFinOrdinal),
                        MontoMensual = reader.GetDecimal(montoMensualOrdinal),
                        Estado = reader.GetString(estadoOrdinal)
                    });
                }
            }

            return (contratos, totalRegistros);
        }

        public async Task<bool> CrearContratoAsync(Contrato contrato)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO contrato (id_inmueble, id_inquilino, id_usuario_creador, fecha_inicio, fecha_fin, monto_mensual, estado) 
                            VALUES (@idInmueble, @idInquilino, @idUsuarioCreador, @fechaInicio, @fechaFin, @montoMensual, @estado)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@idInmueble", contrato.IdInmueble);
            cmd.Parameters.AddWithValue("@idInquilino", contrato.IdInquilino);
            cmd.Parameters.AddWithValue("@idUsuarioCreador", contrato.IdUsuarioCreador);
            cmd.Parameters.AddWithValue("@fechaInicio", contrato.FechaInicio);
            cmd.Parameters.AddWithValue("@fechaFin", contrato.FechaFin);
            cmd.Parameters.AddWithValue("@montoMensual", contrato.MontoMensual);
            cmd.Parameters.AddWithValue("@estado", contrato.Estado ?? "vigente");

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }

        // INTERESES
        public async Task<(IList<InteresInmueble> intereses, int totalRegistros)> ObtenerInteresesConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string whereClause = "WHERE 1=1";
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrEmpty(buscar))
            {
                whereClause += " AND (int.nombre LIKE @buscar OR int.email LIKE @buscar)";
                parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
            }

            if (estado == "contactado")
                whereClause += " AND int.contactado = 1";
            else if (estado == "pendiente")
                whereClause += " AND int.contactado = 0";

            // Contar total
            string countQuery = $"SELECT COUNT(*) FROM interes_inmueble int {whereClause}";
            int totalRegistros;
            using (var countCmd = new MySqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                    countCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                totalRegistros = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            // Obtener datos paginados
            int offset = (pagina - 1) * itemsPorPagina;
            string dataQuery = $@"
                SELECT int.id_interes, int.id_inmueble, int.nombre, int.email, int.telefono, 
                       int.fecha, int.contactado, int.fecha_contacto, int.observaciones
                FROM interes_inmueble int
                {whereClause}
                ORDER BY int.fecha DESC
                LIMIT @limit OFFSET @offset";

            var intereses = new List<InteresInmueble>();
            using (var dataCmd = new MySqlCommand(dataQuery, connection))
            {
                foreach (var param in parameters)
                    dataCmd.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                dataCmd.Parameters.AddWithValue("@limit", itemsPorPagina);
                dataCmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await dataCmd.ExecuteReaderAsync();

                // GetOrdinal para mapeo optimizado
                var idInteresOrdinal = reader.GetOrdinal("id_interes");
                var idInmuebleOrdinal = reader.GetOrdinal("id_inmueble");
                var nombreOrdinal = reader.GetOrdinal("nombre");
                var emailOrdinal = reader.GetOrdinal("email");
                var telefonoOrdinal = reader.GetOrdinal("telefono");
                var fechaOrdinal = reader.GetOrdinal("fecha");
                var contactadoOrdinal = reader.GetOrdinal("contactado");
                var fechaContactoOrdinal = reader.GetOrdinal("fecha_contacto");
                var observacionesOrdinal = reader.GetOrdinal("observaciones");

                while (await reader.ReadAsync())
                {
                    intereses.Add(new InteresInmueble
                    {
                        IdInteres = reader.GetInt32(idInteresOrdinal),
                        IdInmueble = reader.GetInt32(idInmuebleOrdinal),
                        Nombre = reader.GetString(nombreOrdinal),
                        Email = reader.GetString(emailOrdinal),
                        Telefono = reader.IsDBNull(telefonoOrdinal) ? null : reader.GetString(telefonoOrdinal),
                        Fecha = reader.GetDateTime(fechaOrdinal),
                        Contactado = reader.GetBoolean(contactadoOrdinal),
                        FechaContacto = reader.IsDBNull(fechaContactoOrdinal) ? null : reader.GetDateTime(fechaContactoOrdinal),
                        Observaciones = reader.IsDBNull(observacionesOrdinal) ? null : reader.GetString(observacionesOrdinal)
                    });
                }
            }

            return (intereses, totalRegistros);
        }

        public async Task<bool> MarcarInteresContactadoAsync(int idInteres)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"UPDATE interes_inmueble 
                            SET contactado = 1, fecha_contacto = NOW() 
                            WHERE id_interes = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", idInteres);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }

        // DROPDOWNS Y LISTAS
        public async Task<IList<Propietario>> ObtenerPropietariosActivosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, nombre, apellido, email FROM usuario 
                            WHERE rol = 'propietario' AND estado = 'activo' 
                            ORDER BY nombre, apellido";

            var propietarios = new List<Propietario>();
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            // GetOrdinal para mapeo optimizado
            var idUsuarioOrdinal = reader.GetOrdinal("id_usuario");
            var nombreOrdinal = reader.GetOrdinal("nombre");
            var apellidoOrdinal = reader.GetOrdinal("apellido");
            var emailOrdinal = reader.GetOrdinal("email");

            while (await reader.ReadAsync())
            {
                propietarios.Add(new Propietario
                {
                    Usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32(idUsuarioOrdinal),
                        Nombre = reader.GetString(nombreOrdinal),
                        Apellido = reader.GetString(apellidoOrdinal),
                        Email = reader.GetString(emailOrdinal)
                    }
                });
            }

            return propietarios;
        }

        public async Task<IList<TipoInmueble>> ObtenerTiposInmuebleAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT id_tipo_inmueble, nombre FROM tipo_inmueble WHERE estado = 1 ORDER BY nombre";

            var tipos = new List<TipoInmueble>();
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            // GetOrdinal para mapeo optimizado
            var idTipoInmuebleOrdinal = reader.GetOrdinal("id_tipo_inmueble");
            var nombreOrdinal = reader.GetOrdinal("nombre");

            while (await reader.ReadAsync())
            {
                tipos.Add(new TipoInmueble
                {
                    IdTipoInmueble = reader.GetInt32(idTipoInmuebleOrdinal),
                    Nombre = reader.GetString(nombreOrdinal)
                });
            }

            return tipos;
        }

        public async Task<IList<Inmueble>> ObtenerInmueblesDisponiblesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_inmueble, direccion, uso, ambientes, precio 
                            FROM inmueble WHERE estado = 'disponible' 
                            ORDER BY direccion";

            var inmuebles = new List<Inmueble>();
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            // GetOrdinal para mapeo optimizado
            var idInmuebleOrdinal = reader.GetOrdinal("id_inmueble");
            var direccionOrdinal = reader.GetOrdinal("direccion");
            var usoOrdinal = reader.GetOrdinal("uso");
            var ambientesOrdinal = reader.GetOrdinal("ambientes");
            var precioOrdinal = reader.GetOrdinal("precio");

            while (await reader.ReadAsync())
            {
                inmuebles.Add(new Inmueble
                {
                    IdInmueble = reader.GetInt32(idInmuebleOrdinal),
                    Direccion = reader.GetString(direccionOrdinal),
                    Uso = reader.GetString(usoOrdinal),
                    Ambientes = reader.GetInt32(ambientesOrdinal),
                    Precio = reader.GetDecimal(precioOrdinal)
                });
            }

            return inmuebles;
        }

        public async Task<IList<Inquilino>> ObtenerInquilinosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT id_usuario, nombre, apellido, email FROM usuario 
                            WHERE rol = 'inquilino' AND estado = 'activo' 
                            ORDER BY nombre, apellido";

            var inquilinos = new List<Inquilino>();
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            // GetOrdinal para mapeo optimizado
            var idUsuarioOrdinal = reader.GetOrdinal("id_usuario");
            var nombreOrdinal = reader.GetOrdinal("nombre");
            var apellidoOrdinal = reader.GetOrdinal("apellido");
            var emailOrdinal = reader.GetOrdinal("email");

            while (await reader.ReadAsync())
            {
                inquilinos.Add(new Inquilino
                {
                    Usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32(idUsuarioOrdinal),
                        Nombre = reader.GetString(nombreOrdinal),
                        Apellido = reader.GetString(apellidoOrdinal),
                        Email = reader.GetString(emailOrdinal)
                    }
                });
            }

            return inquilinos;
        }
    }
}
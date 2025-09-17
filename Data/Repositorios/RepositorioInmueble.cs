using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioInmueble : IRepositorioInmueble
    {
        private readonly string _connectionString;

        public RepositorioInmueble(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // ===== MÉTODOS CRUD BÁSICOS =====

        public async Task<bool> CrearInmuebleAsync(Inmueble inmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO inmueble 
                                (id_propietario, id_tipo_inmueble, direccion, uso, ambientes, 
                                 precio, coordenadas, url_portada, estado, fecha_alta) 
                                VALUES (@id_propietario, @id_tipo_inmueble, @direccion, @uso, @ambientes, 
                                        @precio, @coordenadas, @url_portada, @estado, @fecha_alta)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_propietario", inmueble.IdPropietario);
                command.Parameters.AddWithValue("@id_tipo_inmueble", inmueble.IdTipoInmueble);
                command.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                command.Parameters.AddWithValue("@uso", inmueble.Uso);
                command.Parameters.AddWithValue("@ambientes", inmueble.Ambientes);
                command.Parameters.AddWithValue("@precio", inmueble.Precio);
                command.Parameters.AddWithValue("@coordenadas", inmueble.Coordenadas ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@url_portada", inmueble.UrlPortada ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@estado", "disponible");
                command.Parameters.AddWithValue("@fecha_alta", DateTime.Now);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarInmuebleAsync(Inmueble inmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"UPDATE inmueble 
                                SET id_propietario = @id_propietario, id_tipo_inmueble = @id_tipo_inmueble, 
                                    direccion = @direccion, uso = @uso, ambientes = @ambientes, 
                                    precio = @precio, coordenadas = @coordenadas, url_portada = @url_portada, 
                                    estado = @estado 
                                WHERE id_inmueble = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_propietario", inmueble.IdPropietario);
                command.Parameters.AddWithValue("@id_tipo_inmueble", inmueble.IdTipoInmueble);
                command.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                command.Parameters.AddWithValue("@uso", inmueble.Uso);
                command.Parameters.AddWithValue("@ambientes", inmueble.Ambientes);
                command.Parameters.AddWithValue("@precio", inmueble.Precio);
                command.Parameters.AddWithValue("@coordenadas", inmueble.Coordenadas ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@url_portada", inmueble.UrlPortada ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@estado", inmueble.Estado);
                command.Parameters.AddWithValue("@id", inmueble.IdInmueble);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch
            {
                return false;
            }
        }

        // ===== NUEVOS MÉTODOS - GESTIÓN DE PORTADA =====

        public async Task<bool> CrearInmuebleConPortadaAsync(Inmueble inmueble, IWebHostEnvironment environment)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Insertar inmueble sin portada primero para obtener ID
                    string queryInmueble = @"INSERT INTO inmueble 
                                            (id_propietario, id_tipo_inmueble, direccion, uso, ambientes, 
                                             precio, coordenadas, estado, fecha_alta) 
                                            VALUES (@id_propietario, @id_tipo_inmueble, @direccion, @uso, @ambientes, 
                                                    @precio, @coordenadas, @estado, @fecha_alta);
                                            SELECT LAST_INSERT_ID();";

                    int idInmueble;
                    using (var command = new MySqlCommand(queryInmueble, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@id_propietario", inmueble.IdPropietario);
                        command.Parameters.AddWithValue("@id_tipo_inmueble", inmueble.IdTipoInmueble);
                        command.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                        command.Parameters.AddWithValue("@uso", inmueble.Uso);
                        command.Parameters.AddWithValue("@ambientes", inmueble.Ambientes);
                        command.Parameters.AddWithValue("@precio", inmueble.Precio);
                        command.Parameters.AddWithValue("@coordenadas", inmueble.Coordenadas ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@estado", "disponible");
                        command.Parameters.AddWithValue("@fecha_alta", DateTime.Now);

                        idInmueble = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // 2. Si hay archivo de portada, guardarlo y actualizar URL
                    if (inmueble.PortadaFile != null && inmueble.PortadaFile.Length > 0)
                    {
                        string urlPortada = await GuardarArchivoPortadaAsync(inmueble.PortadaFile, idInmueble, environment);
                        
                        string queryActualizarPortada = "UPDATE inmueble SET url_portada = @url_portada WHERE id_inmueble = @id";
                        using var commandPortada = new MySqlCommand(queryActualizarPortada, connection, transaction);
                        commandPortada.Parameters.AddWithValue("@url_portada", urlPortada);
                        commandPortada.Parameters.AddWithValue("@id", idInmueble);
                        await commandPortada.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    inmueble.IdInmueble = idInmueble; // Asignar ID generado
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarInmuebleConPortadaAsync(Inmueble inmueble, IWebHostEnvironment environment)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Obtener URL de portada actual para eliminar archivo anterior si es necesario
                    string queryObtenerPortada = "SELECT url_portada FROM inmueble WHERE id_inmueble = @id";
                    string? portadaAnterior = null;
                    
                    using (var commandObtener = new MySqlCommand(queryObtenerPortada, connection, transaction))
                    {
                        commandObtener.Parameters.AddWithValue("@id", inmueble.IdInmueble);
                        var result = await commandObtener.ExecuteScalarAsync();
                        portadaAnterior = result as string;
                    }

                    // 2. Si hay nuevo archivo de portada, procesarlo
                    string? nuevaUrlPortada = inmueble.UrlPortada; // Mantener la actual
                    
                    if (inmueble.PortadaFile != null && inmueble.PortadaFile.Length > 0)
                    {
                        // Guardar nuevo archivo
                        nuevaUrlPortada = await GuardarArchivoPortadaAsync(inmueble.PortadaFile, inmueble.IdInmueble, environment);
                        
                        // Eliminar archivo anterior si existía
                        if (!string.IsNullOrEmpty(portadaAnterior))
                        {
                            await EliminarArchivoPortadaAsync(portadaAnterior, environment);
                        }
                    }

                    // 3. Actualizar inmueble con nueva información
                    string queryActualizar = @"UPDATE inmueble 
                                              SET id_propietario = @id_propietario, id_tipo_inmueble = @id_tipo_inmueble, 
                                                  direccion = @direccion, uso = @uso, ambientes = @ambientes, 
                                                  precio = @precio, coordenadas = @coordenadas, url_portada = @url_portada, 
                                                  estado = @estado 
                                              WHERE id_inmueble = @id";

                    using var commandActualizar = new MySqlCommand(queryActualizar, connection, transaction);
                    commandActualizar.Parameters.AddWithValue("@id_propietario", inmueble.IdPropietario);
                    commandActualizar.Parameters.AddWithValue("@id_tipo_inmueble", inmueble.IdTipoInmueble);
                    commandActualizar.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                    commandActualizar.Parameters.AddWithValue("@uso", inmueble.Uso);
                    commandActualizar.Parameters.AddWithValue("@ambientes", inmueble.Ambientes);
                    commandActualizar.Parameters.AddWithValue("@precio", inmueble.Precio);
                    commandActualizar.Parameters.AddWithValue("@coordenadas", inmueble.Coordenadas ?? (object)DBNull.Value);
                    commandActualizar.Parameters.AddWithValue("@url_portada", nuevaUrlPortada ?? (object)DBNull.Value);
                    commandActualizar.Parameters.AddWithValue("@estado", inmueble.Estado);
                    commandActualizar.Parameters.AddWithValue("@id", inmueble.IdInmueble);

                    int rowsAffected = await commandActualizar.ExecuteNonQueryAsync();
                    
                    await transaction.CommitAsync();
                    return rowsAffected > 0;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarSoloPortadaAsync(int idInmueble, IFormFile archivoPortada, IWebHostEnvironment environment)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Obtener portada actual
                    string queryObtenerPortada = "SELECT url_portada FROM inmueble WHERE id_inmueble = @id";
                    string? portadaAnterior = null;
                    
                    using (var commandObtener = new MySqlCommand(queryObtenerPortada, connection, transaction))
                    {
                        commandObtener.Parameters.AddWithValue("@id", idInmueble);
                        var result = await commandObtener.ExecuteScalarAsync();
                        portadaAnterior = result as string;
                    }

                    // 2. Guardar nuevo archivo
                    string nuevaUrlPortada = await GuardarArchivoPortadaAsync(archivoPortada, idInmueble, environment);

                    // 3. Actualizar URL en BD
                    string queryActualizar = "UPDATE inmueble SET url_portada = @url_portada WHERE id_inmueble = @id";
                    using var commandActualizar = new MySqlCommand(queryActualizar, connection, transaction);
                    commandActualizar.Parameters.AddWithValue("@url_portada", nuevaUrlPortada);
                    commandActualizar.Parameters.AddWithValue("@id", idInmueble);
                    
                    int rowsAffected = await commandActualizar.ExecuteNonQueryAsync();

                    // 4. Eliminar archivo anterior si existía
                    if (!string.IsNullOrEmpty(portadaAnterior))
                    {
                        await EliminarArchivoPortadaAsync(portadaAnterior, environment);
                    }

                    await transaction.CommitAsync();
                    return rowsAffected > 0;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarPortadaAsync(int idInmueble, IWebHostEnvironment environment)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Obtener URL de portada actual
                    string queryObtenerPortada = "SELECT url_portada FROM inmueble WHERE id_inmueble = @id";
                    string? portadaActual = null;
                    
                    using (var commandObtener = new MySqlCommand(queryObtenerPortada, connection, transaction))
                    {
                        commandObtener.Parameters.AddWithValue("@id", idInmueble);
                        var result = await commandObtener.ExecuteScalarAsync();
                        portadaActual = result as string;
                    }

                    // 2. Actualizar BD para quitar portada
                    string queryActualizar = "UPDATE inmueble SET url_portada = NULL WHERE id_inmueble = @id";
                    using var commandActualizar = new MySqlCommand(queryActualizar, connection, transaction);
                    commandActualizar.Parameters.AddWithValue("@id", idInmueble);
                    
                    int rowsAffected = await commandActualizar.ExecuteNonQueryAsync();

                    // 3. Eliminar archivo físico si existía
                    if (!string.IsNullOrEmpty(portadaActual))
                    {
                        await EliminarArchivoPortadaAsync(portadaActual, environment);
                    }

                    await transaction.CommitAsync();
                    return rowsAffected > 0;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch
            {
                return false;
            }
        }
        // ===== GESTIÓN DE ARCHIVOS DE PORTADA =====

        public async Task<string> GuardarArchivoPortadaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment)
        {
            try
            {
                // Crear estructura de directorios: wwwroot/images/inmuebles/{idInmueble}/portada/
                var uploadsDir = Path.Combine(environment.WebRootPath, "images", "inmuebles", idInmueble.ToString(), "portada");
                
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Eliminar portada anterior si existe
                if (Directory.Exists(uploadsDir))
                {
                    var archivosAnteriores = Directory.GetFiles(uploadsDir);
                    foreach (var archivoAnterior in archivosAnteriores)
                    {
                        File.Delete(archivoAnterior);
                    }
                }

                // Generar nombre para portada
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                var fileName = $"portada{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Guardar archivo
                using var stream = new FileStream(filePath, FileMode.Create);
                await archivo.CopyToAsync(stream);

                // Retornar URL relativa
                return $"/images/inmuebles/{idInmueble}/portada/{fileName}";
            }
            catch
            {
                throw new Exception("Error al guardar el archivo de portada");
            }
        }

        public async Task<bool> EliminarArchivoPortadaAsync(string? rutaArchivo, IWebHostEnvironment environment)
        {
            try
            {
                if (string.IsNullOrEmpty(rutaArchivo)) return true;

                var filePath = Path.Combine(environment.WebRootPath, rutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidarArchivoPortadaAsync(IFormFile archivo)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return false;

                // Validar extensión
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                
                if (!extensionesPermitidas.Contains(extension))
                    return false;

                // Validar tamaño (5MB máximo)
                if (archivo.Length > 5 * 1024 * 1024)
                    return false;

                // Validar que es realmente una imagen (cabecera de archivo)
                using var stream = archivo.OpenReadStream();
                var buffer = new byte[8];
                int bytesRead = await stream.ReadAsync(buffer, 0, 8);
                if (bytesRead != 8)
                {
                    throw new IOException("No se leyeron los 8 bytes esperados.");
                }
                
                return EsImagenValida(buffer);
            }
            catch
            {
                return false;
            }
        }

        // ===== MÉTODOS CRUD RESTANTES =====

        public async Task<bool> EliminarInmuebleAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "UPDATE inmueble SET estado = 'inactivo' WHERE id_inmueble = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Inmueble?> ObtenerInmueblePorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM inmueble WHERE id_inmueble = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearInmuebleBasico(reader);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Inmueble?> ObtenerInmuebleConDetallesAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT i.id_inmueble, i.id_propietario, i.id_tipo_inmueble, i.direccion, i.uso, 
                           i.ambientes, i.precio, i.coordenadas, i.url_portada, i.estado, i.fecha_alta,
                           p.id_usuario, u.nombre AS propietario_nombre, u.apellido AS propietario_apellido,
                           u.telefono AS propietario_telefono,
                           t.nombre AS tipo_inmueble_nombre
                    FROM inmueble i
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    WHERE i.id_inmueble = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearInmuebleCompleto(reader);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ===== NUEVOS MÉTODOS - CONSULTAS CON IMÁGENES =====

        public async Task<Inmueble?> ObtenerInmuebleConGaleriaAsync(int id)
        {
            try
            {
                var inmueble = await ObtenerInmuebleConDetallesAsync(id);
                if (inmueble == null) return null;

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Obtener imágenes de galería del inmueble
                string queryImagenes = @"
                    SELECT id_imagen, id_inmueble, url, descripcion, orden, fecha_creacion
                    FROM imagen_inmueble
                    WHERE id_inmueble = @id_inmueble
                    ORDER BY orden ASC, fecha_creacion ASC";

                var imagenes = new List<ImagenInmueble>();
                using var command = new MySqlCommand(queryImagenes, connection);
                command.Parameters.AddWithValue("@id_inmueble", id);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    imagenes.Add(new ImagenInmueble
                    {
                        IdImagen = reader.GetInt32(reader.GetOrdinal("id_imagen")),
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Url = reader.GetString(reader.GetOrdinal("url")),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                        Orden = reader.GetInt32(reader.GetOrdinal("orden")),
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion"))
                    });
                }

                inmueble.Imagenes = imagenes;
                return inmueble;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IList<Inmueble>> ObtenerInmueblesConPortadaAsync(int limite = 20)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inmuebles = new List<Inmueble>();
                string query = @"
                    SELECT i.id_inmueble, i.direccion, i.precio, i.estado, i.url_portada,
                           t.nombre as tipo_nombre,
                           up.nombre as propietario_nombre, up.apellido as propietario_apellido
                    FROM inmueble i
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                    WHERE i.estado != 'inactivo' AND i.url_portada IS NOT NULL
                    ORDER BY i.fecha_alta DESC
                    LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    inmuebles.Add(new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        UrlPortada = reader.GetString(reader.GetOrdinal("url_portada")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_nombre"))
                        },
                        Propietario = new Propietario
                        {
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                                Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido"))
                            }
                        }
                    });
                }

                return inmuebles;
            }
            catch
            {
                return new List<Inmueble>();
            }
        }

        public async Task<IList<Inmueble>> ObtenerInmueblesSinPortadaAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inmuebles = new List<Inmueble>();
                string query = @"
                    SELECT i.id_inmueble, i.direccion, i.precio, i.estado,
                           t.nombre as tipo_nombre,
                           up.nombre as propietario_nombre, up.apellido as propietario_apellido
                    FROM inmueble i
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                    WHERE i.estado != 'inactivo' AND (i.url_portada IS NULL OR i.url_portada = '')
                    ORDER BY i.fecha_alta DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    inmuebles.Add(new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_nombre"))
                        },
                        Propietario = new Propietario
                        {
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                                Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido"))
                            }
                        }
                    });
                }

                return inmuebles;
            }
            catch
            {
                return new List<Inmueble>();
            }
        }

        public async Task<Dictionary<string, object>> ObtenerEstadisticasImagenesInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var estadisticas = new Dictionary<string, object>();

                // Verificar si tiene portada
                string queryPortada = "SELECT url_portada FROM inmueble WHERE id_inmueble = @id";
                using (var command = new MySqlCommand(queryPortada, connection))
                {
                    command.Parameters.AddWithValue("@id", idInmueble);
                    var portada = await command.ExecuteScalarAsync() as string;
                    estadisticas["TienePortada"] = !string.IsNullOrEmpty(portada);
                    estadisticas["UrlPortada"] = portada ?? string.Empty;
                }

                // Contar imágenes de galería
                string queryGaleria = "SELECT COUNT(*) FROM imagen_inmueble WHERE id_inmueble = @id";
                using (var command = new MySqlCommand(queryGaleria, connection))
                {
                    command.Parameters.AddWithValue("@id", idInmueble);
                    estadisticas["TotalImagenesGaleria"] = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // Última imagen de galería agregada
                string queryUltima = @"
                    SELECT fecha_creacion FROM imagen_inmueble 
                    WHERE id_inmueble = @id 
                    ORDER BY fecha_creacion DESC LIMIT 1";
                using (var command = new MySqlCommand(queryUltima, connection))
                {
                    command.Parameters.AddWithValue("@id", idInmueble);
                    var resultado = await command.ExecuteScalarAsync();
                    estadisticas["UltimaImagenGaleria"] = resultado != null ? Convert.ToDateTime(resultado) : DateTime.MinValue; 
                }

                return estadisticas;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        // ===== MÉTODOS AUXILIARES PRIVADOS =====

        private static bool EsImagenValida(byte[] buffer)
        {
            // JPEG
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8)
                return true;

            // PNG
            if (buffer.Length >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && 
                buffer[2] == 0x4E && buffer[3] == 0x47)
                return true;

            // GIF
            if (buffer.Length >= 6 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
                return true;

            // BMP
            if (buffer.Length >= 2 && buffer[0] == 0x42 && buffer[1] == 0x4D)
                return true;

            // WEBP
            if (buffer.Length >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && 
                buffer[2] == 0x46 && buffer[3] == 0x46 && 
                buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                return true;

            return false;
        }

        private static Inmueble MapearInmuebleBasico(System.Data.Common.DbDataReader reader)
        {
            return new Inmueble
            {
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                Uso = reader.GetString(reader.GetOrdinal("uso")),
                Ambientes = reader.GetInt32(reader.GetOrdinal("ambientes")),
                Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                Coordenadas = reader.IsDBNull(reader.GetOrdinal("coordenadas")) ? null : reader.GetString(reader.GetOrdinal("coordenadas")),
                UrlPortada = reader.IsDBNull(reader.GetOrdinal("url_portada")) ? null : reader.GetString(reader.GetOrdinal("url_portada")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                FechaAlta = reader.GetDateTime(reader.GetOrdinal("fecha_alta"))
            };
        }

        private static Inmueble MapearInmuebleCompleto(System.Data.Common.DbDataReader reader)
        {
            var inmueble = MapearInmuebleBasico(reader);
            
            inmueble.Propietario = new Propietario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                Usuario = new Usuario
                {
                    Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("propietario_telefono")) ? null : reader.GetString(reader.GetOrdinal("propietario_telefono"))
                }
            };
            
            inmueble.TipoInmueble = new TipoInmueble
            {
                Nombre = reader.GetString(reader.GetOrdinal("tipo_inmueble_nombre"))
            };

            return inmueble;
        }
        // ===== VALIDACIONES BÁSICAS EXISTENTES =====

        public async Task<bool> ExisteDireccionAsync(string direccion, int idExcluir = 0)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = idExcluir == 0
                    ? "SELECT COUNT(*) FROM inmueble WHERE direccion = @direccion AND estado != 'inactivo'"
                    : "SELECT COUNT(*) FROM inmueble WHERE direccion = @direccion AND id_inmueble != @id AND estado != 'inactivo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@direccion", direccion);
                if (idExcluir != 0)
                {
                    command.Parameters.AddWithValue("@id", idExcluir);
                }

                int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> ContarContratosVigentesAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM contrato WHERE id_inmueble = @id AND estado = 'vigente'";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", idInmueble);

                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            catch
            {
                return 0;
            }
        }

        // ===== VALIDACIONES DE NEGOCIO - COORDENADAS =====

        public async Task<bool> ExisteCoordenadasAsync(string coordenadas, int idExcluir = 0)
        {
            if (string.IsNullOrEmpty(coordenadas)) return false;
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = idExcluir == 0
                    ? "SELECT COUNT(*) FROM inmueble WHERE coordenadas = @coordenadas AND estado != 'inactivo'"
                    : "SELECT COUNT(*) FROM inmueble WHERE coordenadas = @coordenadas AND id_inmueble != @id AND estado != 'inactivo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@coordenadas", coordenadas);
                if (idExcluir != 0)
                {
                    command.Parameters.AddWithValue("@id", idExcluir);
                }

                int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PropietarioTieneInmuebleEnCoordenadasAsync(int idPropietario, string coordenadas, int idExcluir = 0)
        {
            if (string.IsNullOrEmpty(coordenadas)) return false;
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = idExcluir == 0
                    ? @"SELECT COUNT(*) FROM inmueble 
                        WHERE id_propietario != @id_propietario 
                        AND coordenadas = @coordenadas 
                        AND estado != 'inactivo'"
                    : @"SELECT COUNT(*) FROM inmueble 
                        WHERE id_propietario != @id_propietario 
                        AND coordenadas = @coordenadas 
                        AND id_inmueble != @id_excluir 
                        AND estado != 'inactivo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_propietario", idPropietario);
                command.Parameters.AddWithValue("@coordenadas", coordenadas);
                if (idExcluir != 0)
                {
                    command.Parameters.AddWithValue("@id_excluir", idExcluir);
                }

                int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0; // TRUE = existe conflicto (otro propietario tiene esas coordenadas)
            }
            catch
            {
                return false;
            }
        }

        public async Task<Inmueble?> ObtenerInmueblePorCoordenadasAsync(string coordenadas, int idExcluir = 0)
        {
            if (string.IsNullOrEmpty(coordenadas)) return null;
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = idExcluir == 0
                    ? @"SELECT i.*, p.id_usuario, u.nombre as propietario_nombre, u.apellido as propietario_apellido
                        FROM inmueble i
                        INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                        INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                        WHERE i.coordenadas = @coordenadas AND i.estado != 'inactivo'
                        LIMIT 1"
                    : @"SELECT i.*, p.id_usuario, u.nombre as propietario_nombre, u.apellido as propietario_apellido
                        FROM inmueble i
                        INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                        INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                        WHERE i.coordenadas = @coordenadas AND i.id_inmueble != @id_excluir AND i.estado != 'inactivo'
                        LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@coordenadas", coordenadas);
                if (idExcluir != 0)
                {
                    command.Parameters.AddWithValue("@id_excluir", idExcluir);
                }

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var inmueble = MapearInmuebleBasico(reader);
                    inmueble.Propietario = new Propietario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                        Usuario = new Usuario
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                            Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido"))
                        }
                    };
                    return inmueble;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ===== VALIDACIONES DE NEGOCIO - PROPIETARIO =====

        public async Task<bool> PropietarioTieneOtrosInmueblesAsync(int idPropietario, int idInmuebleExcluir = 0)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = idInmuebleExcluir == 0
                    ? "SELECT COUNT(*) FROM inmueble WHERE id_propietario = @id_propietario AND estado != 'inactivo'"
                    : "SELECT COUNT(*) FROM inmueble WHERE id_propietario = @id_propietario AND id_inmueble != @id_excluir AND estado != 'inactivo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_propietario", idPropietario);
                if (idInmuebleExcluir != 0)
                {
                    command.Parameters.AddWithValue("@id_excluir", idInmuebleExcluir);
                }

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PuedeAsignarInmuebleAPropietarioAsync(int idInmueble, int idPropietario)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Verificar que el inmueble no pertenezca ya a otro propietario
                string query = @"
                    SELECT COUNT(*) FROM inmueble 
                    WHERE id_inmueble = @id_inmueble 
                    AND id_propietario != @id_propietario 
                    AND estado != 'inactivo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);
                command.Parameters.AddWithValue("@id_propietario", idPropietario);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) == 0;
            }
            catch
            {
                return false;
            }
        }

        // ===== MÉTODO PARA INDEX CON PAGINACIÓN Y BÚSQUEDA =====

        public async Task<(IList<Inmueble> inmuebles, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Construir WHERE dinámico
                var whereConditions = new List<string> { "i.estado != 'inactivo'" };
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(buscar))
                {
                    whereConditions.Add(@"(i.direccion LIKE @buscar 
                                          OR t.nombre LIKE @buscar 
                                          OR i.uso LIKE @buscar
                                          OR up.nombre LIKE @buscar
                                          OR up.apellido LIKE @buscar
                                          OR CONCAT(up.nombre, ' ', up.apellido) LIKE @buscar)");
                    parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    whereConditions.Add("i.estado = @estado");
                    parameters.Add(new MySqlParameter("@estado", estado));
                }

                string whereClause = "WHERE " + string.Join(" AND ", whereConditions);

                // 1. Contar total de registros
                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM inmueble i
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                    {whereClause}";

                int totalRegistros;
                using (var countCommand = new MySqlCommand(countQuery, connection))
                {
                    foreach (var param in parameters)
                    {
                        countCommand.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                    }
                    totalRegistros = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                // 2. Obtener registros con paginación incluyendo portada
                int offset = (pagina - 1) * itemsPorPagina;
                string dataQuery = $@"
                    SELECT i.id_inmueble, i.id_propietario, i.id_tipo_inmueble, i.direccion, 
                           i.uso, i.ambientes, i.precio, i.coordenadas, i.url_portada, i.estado, i.fecha_alta,
                           t.nombre as tipo_nombre,
                           up.nombre as propietario_nombre, up.apellido as propietario_apellido
                    FROM inmueble i
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                    {whereClause}
                    ORDER BY i.direccion
                    LIMIT @limit OFFSET @offset";

                var inmuebles = new List<Inmueble>();
                using (var dataCommand = new MySqlCommand(dataQuery, connection))
                {
                    foreach (var param in parameters)
                    {
                        dataCommand.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                    }
                    dataCommand.Parameters.AddWithValue("@limit", itemsPorPagina);
                    dataCommand.Parameters.AddWithValue("@offset", offset);

                    using var reader = await dataCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        inmuebles.Add(new Inmueble
                        {
                            IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                            IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                            IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                            Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                            Uso = reader.GetString(reader.GetOrdinal("uso")),
                            Ambientes = reader.GetInt32(reader.GetOrdinal("ambientes")),
                            Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                            Coordenadas = reader.IsDBNull(reader.GetOrdinal("coordenadas")) ? null : reader.GetString(reader.GetOrdinal("coordenadas")),
                            UrlPortada = reader.IsDBNull(reader.GetOrdinal("url_portada")) ? null : reader.GetString(reader.GetOrdinal("url_portada")),
                            Estado = reader.GetString(reader.GetOrdinal("estado")),
                            FechaAlta = reader.GetDateTime(reader.GetOrdinal("fecha_alta")),
                            TipoInmueble = new TipoInmueble
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("tipo_nombre"))
                            },
                            Propietario = new Propietario
                            {
                                Usuario = new Usuario
                                {
                                    Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                                    Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido"))
                                }
                            }
                        });
                    }
                }

                return (inmuebles, totalRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener inmuebles con paginación: {ex.Message}", ex);
            }
        }

        // ===== MÉTODOS AUXILIARES PARA VISTAS =====

        public async Task<IList<Propietario>> ObtenerPropietariosActivosAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var propietarios = new List<Propietario>();
                string query = @"
                    SELECT p.id_propietario, u.nombre, u.apellido, u.dni
                    FROM propietario p 
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
                    WHERE p.estado = true AND u.estado = 'activo'
                    ORDER BY u.apellido, u.nombre";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    propietarios.Add(new Propietario
                    {
                        IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                        Usuario = new Usuario
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                            Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                            Dni = reader.GetString(reader.GetOrdinal("dni"))
                        }
                    });
                }

                return propietarios;
            }
            catch
            {
                return new List<Propietario>();
            }
        }

        public async Task<IList<TipoInmueble>> ObtenerTiposInmuebleActivosAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var tiposInmuebleDictionary = new Dictionary<string, TipoInmueble>();

                string query = @"
                SELECT 
                id_tipo_inmueble, 
                nombre, 
                descripcion
                FROM tipo_inmueble 
                WHERE estado = 1
                ORDER BY nombre";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var nombre = reader.GetString(reader.GetOrdinal("nombre"));

                    if (!tiposInmuebleDictionary.ContainsKey(nombre))
                    {
                        var tipoInmueble = new TipoInmueble
                        {
                            IdTipoInmueble = reader.GetInt32(reader.GetOrdinal("id_tipo_inmueble")),
                            Nombre = nombre,
                            Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("descripcion"))
                        };

                        tiposInmuebleDictionary.Add(nombre, tipoInmueble);
                    }
                }

                return tiposInmuebleDictionary.Values.ToList();
            }
            catch
            {
                return new List<TipoInmueble>();
            }
        }
    }
}
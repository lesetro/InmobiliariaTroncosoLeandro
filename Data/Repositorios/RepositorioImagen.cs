using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioImagen : IRepositorioImagen
    {
        private readonly string _connectionString;

        public RepositorioImagen(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // ===== MÉTODOS CRUD BÁSICOS PARA GALERÍA =====

        public async Task<bool> CrearImagenAsync(ImagenInmueble imagen)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO imagen_inmueble 
                    (id_inmueble, url, descripcion, orden, fecha_creacion)
                    VALUES (@id_inmueble, @url, @descripcion, @orden, @fecha_creacion)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", imagen.IdInmueble);
                command.Parameters.AddWithValue("@url", imagen.Url);
                command.Parameters.AddWithValue("@descripcion", imagen.Descripcion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@orden", imagen.Orden);
                command.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarImagenAsync(ImagenInmueble imagen)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    UPDATE imagen_inmueble 
                    SET url = @url, descripcion = @descripcion, orden = @orden
                    WHERE id_imagen = @id_imagen";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@url", imagen.Url);
                command.Parameters.AddWithValue("@descripcion", imagen.Descripcion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@orden", imagen.Orden);
                command.Parameters.AddWithValue("@id_imagen", imagen.IdImagen);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarImagenAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM imagen_inmueble WHERE id_imagen = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ImagenInmueble?> ObtenerImagenPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT ii.id_imagen, ii.id_inmueble, ii.url, ii.descripcion, 
                           ii.orden, ii.fecha_creacion,
                           i.direccion as inmueble_direccion
                    FROM imagen_inmueble ii
                    INNER JOIN inmueble i ON ii.id_inmueble = i.id_inmueble
                    WHERE ii.id_imagen = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new ImagenInmueble
                    {
                        IdImagen = reader.GetInt32(reader.GetOrdinal("id_imagen")),
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Url = reader.GetString(reader.GetOrdinal("url")),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                        Orden = reader.GetInt32(reader.GetOrdinal("orden")),
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                        Inmueble = new Inmueble
                        {
                            Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
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

        // ===== MÉTODOS ESPECÍFICOS PARA GALERÍA DE INMUEBLE =====

        public async Task<IList<ImagenInmueble>> ObtenerImagenesPorInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var imagenes = new List<ImagenInmueble>();
                string query = @"
                    SELECT id_imagen, id_inmueble, url, descripcion, orden, fecha_creacion
                    FROM imagen_inmueble
                    WHERE id_inmueble = @id_inmueble
                    ORDER BY orden ASC, fecha_creacion ASC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);

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

                return imagenes;
            }
            catch
            {
                return new List<ImagenInmueble>();
            }
        }

        public async Task<bool> CrearImagenConArchivoAsync(ImagenInmueble imagen, IFormFile archivo, IWebHostEnvironment environment)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Guardar archivo físico
                    string urlArchivo = await GuardarArchivoGaleriaAsync(archivo, imagen.IdInmueble, environment);
                    imagen.Url = urlArchivo;

                    // 2. Obtener siguiente orden si no se especificó
                    if (imagen.Orden <= 0)
                    {
                        imagen.Orden = await ObtenerSiguienteOrdenInternoAsync(imagen.IdInmueble, connection, transaction);
                    }

                    // 3. Insertar en BD
                    string query = @"
                        INSERT INTO imagen_inmueble 
                        (id_inmueble, url, descripcion, orden, fecha_creacion)
                        VALUES (@id_inmueble, @url, @descripcion, @orden, @fecha_creacion)";

                    using var command = new MySqlCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@id_inmueble", imagen.IdInmueble);
                    command.Parameters.AddWithValue("@url", imagen.Url);
                    command.Parameters.AddWithValue("@descripcion", imagen.Descripcion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@orden", imagen.Orden);
                    command.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);

                    var result = await command.ExecuteNonQueryAsync() > 0;
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    // Si falló la BD, eliminar archivo guardado
                    await EliminarArchivoGaleriaAsync(imagen.Url, environment);
                    throw;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarImagenConArchivoAsync(ImagenInmueble imagen, IFormFile? archivo, IWebHostEnvironment environment)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    string? urlAnterior = null;
                    string nuevaUrl = imagen.Url;

                    // 1. Si hay nuevo archivo, procesarlo
                    if (archivo != null && archivo.Length > 0)
                    {
                        // Obtener URL anterior para eliminar archivo
                        string queryObtenerUrl = "SELECT url FROM imagen_inmueble WHERE id_imagen = @id";
                        using (var commandObtener = new MySqlCommand(queryObtenerUrl, connection, transaction))
                        {
                            commandObtener.Parameters.AddWithValue("@id", imagen.IdImagen);
                            urlAnterior = await commandObtener.ExecuteScalarAsync() as string;
                        }

                        // Guardar nuevo archivo
                        nuevaUrl = await GuardarArchivoGaleriaAsync(archivo, imagen.IdInmueble, environment);
                    }

                    // 2. Actualizar en BD
                    string queryActualizar = @"
                        UPDATE imagen_inmueble 
                        SET url = @url, descripcion = @descripcion, orden = @orden
                        WHERE id_imagen = @id_imagen";

                    using var commandActualizar = new MySqlCommand(queryActualizar, connection, transaction);
                    commandActualizar.Parameters.AddWithValue("@url", nuevaUrl);
                    commandActualizar.Parameters.AddWithValue("@descripcion", imagen.Descripcion ?? (object)DBNull.Value);
                    commandActualizar.Parameters.AddWithValue("@orden", imagen.Orden);
                    commandActualizar.Parameters.AddWithValue("@id_imagen", imagen.IdImagen);

                    var result = await commandActualizar.ExecuteNonQueryAsync() > 0;

                    // 3. Eliminar archivo anterior si se cambió
                    if (!string.IsNullOrEmpty(urlAnterior) && urlAnterior != nuevaUrl)
                    {
                        await EliminarArchivoGaleriaAsync(urlAnterior, environment);
                    }

                    await transaction.CommitAsync();
                    return result;
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

        // ===== MÉTODOS DE ORDEN Y CONTEO =====

        public async Task<int> ContarImagenesPorInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM imagen_inmueble WHERE id_inmueble = @id_inmueble";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);

                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> ObtenerSiguienteOrdenAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                return await ObtenerSiguienteOrdenInternoAsync(idInmueble, connection);
            }
            catch
            {
                return 1;
            }
        }

        private async Task<int> ObtenerSiguienteOrdenInternoAsync(int idInmueble, MySqlConnection connection, MySqlTransaction? transaction = null)
        {
            try
            {
                string query = "SELECT COALESCE(MAX(orden), 0) + 1 FROM imagen_inmueble WHERE id_inmueble = @id_inmueble";
                using var command = new MySqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);

                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            catch
            {
                return 1;
            }
        }
        public async Task<bool> ActualizarOrdenImagenesAsync(int idInmueble, Dictionary<int, int> nuevosOrdenes)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    foreach (var item in nuevosOrdenes)
                    {
                        string query = @"
                            UPDATE imagen_inmueble 
                            SET orden = @orden 
                            WHERE id_imagen = @id_imagen AND id_inmueble = @id_inmueble";

                        using var command = new MySqlCommand(query, connection, transaction);
                        command.Parameters.AddWithValue("@orden", item.Value);
                        command.Parameters.AddWithValue("@id_imagen", item.Key);
                        command.Parameters.AddWithValue("@id_inmueble", idInmueble);

                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
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

        public async Task<bool> ReorganizarOrdenDespuesDeEliminarAsync(int idInmueble, int ordenEliminado)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Reordenar imágenes que tenían orden mayor al eliminado
                string query = @"
                    UPDATE imagen_inmueble 
                    SET orden = orden - 1 
                    WHERE id_inmueble = @id_inmueble AND orden > @orden_eliminado";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);
                command.Parameters.AddWithValue("@orden_eliminado", ordenEliminado);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ===== VALIDACIONES DE NEGOCIO =====

        public async Task<bool> ExisteImagenAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM imagen_inmueble WHERE id_imagen = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExisteInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM inmueble WHERE id_inmueble = @id AND estado != 'inactivo'";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", idInmueble);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PuedeEliminarImagenAsync(int id)
        {
            try
            {
                // Siempre se puede eliminar una imagen de galería
                return await ExisteImagenAsync(id);
            }
            catch
            {
                return false;
            }
        }

        // ===== GESTIÓN DE ARCHIVOS FÍSICOS PARA GALERÍA =====

        public async Task<string> GuardarArchivoGaleriaAsync(IFormFile archivo, int idInmueble, IWebHostEnvironment environment)
        {
            try
            {
                // Crear estructura de directorios: wwwroot/images/inmuebles/{idInmueble}/galeria/
                var uploadsDir = Path.Combine(environment.WebRootPath, "images", "inmuebles", idInmueble.ToString(), "galeria");
                
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generar nombre único con timestamp
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"galeria_{timestamp}_{Guid.NewGuid().ToString("N")[..8]}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Guardar archivo
                using var stream = new FileStream(filePath, FileMode.Create);
                await archivo.CopyToAsync(stream);

                // Retornar URL relativa
                return $"/images/inmuebles/{idInmueble}/galeria/{fileName}";
            }
            catch
            {
                throw new Exception("Error al guardar el archivo de galería");
            }
        }

        public async Task<bool> EliminarArchivoGaleriaAsync(string rutaArchivo, IWebHostEnvironment environment)
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

        public async Task<bool> EliminarTodasLasImagenesInmuebleAsync(int idInmueble, IWebHostEnvironment environment)
        {
            try
            {
                // 1. Obtener todas las URLs de imágenes del inmueble
                var imagenes = await ObtenerImagenesPorInmuebleAsync(idInmueble);
                
                // 2. Eliminar archivos físicos
                foreach (var imagen in imagenes)
                {
                    await EliminarArchivoGaleriaAsync(imagen.Url, environment);
                }

                // 3. Eliminar carpeta de galería del inmueble
                var carpetaGaleria = Path.Combine(environment.WebRootPath, "images", "inmuebles", idInmueble.ToString(), "galeria");
                if (Directory.Exists(carpetaGaleria))
                {
                    await Task.Run(() => Directory.Delete(carpetaGaleria, true));
                }

                // 4. Eliminar registros de BD
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM imagen_inmueble WHERE id_inmueble = @id_inmueble";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidarArchivoImagenAsync(IFormFile archivo)
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

        // ===== MÉTODOS DE UTILIDAD =====

        public async Task<bool> LimpiarImagenesHuerfanasAsync(int idInmueble, IWebHostEnvironment environment)
        {
            try
            {
                // Obtener imágenes en BD
                var imagenesBD = await ObtenerImagenesPorInmuebleAsync(idInmueble);
                var urlsEnBD = imagenesBD.Select(i => i.Url).ToHashSet();

                // Obtener archivos físicos en galería
                var carpetaGaleria = Path.Combine(environment.WebRootPath, "images", "inmuebles", idInmueble.ToString(), "galeria");
                
                if (!Directory.Exists(carpetaGaleria))
                    return true;

                var archivos = Directory.GetFiles(carpetaGaleria);

                foreach (var archivo in archivos)
                {
                    var urlRelativa = $"/images/inmuebles/{idInmueble}/galeria/{Path.GetFileName(archivo)}";
                    
                    if (!urlsEnBD.Contains(urlRelativa))
                    {
                        try
                        {
                            File.Delete(archivo);
                        }
                        catch
                        {
                            // Continuar con el siguiente archivo
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> ObtenerEstadisticasGaleriaAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var estadisticas = new Dictionary<string, object>();

                // Total de imágenes en galería
                string queryTotal = "SELECT COUNT(*) FROM imagen_inmueble WHERE id_inmueble = @id_inmueble";
                using (var command = new MySqlCommand(queryTotal, connection))
                {
                    command.Parameters.AddWithValue("@id_inmueble", idInmueble);
                    estadisticas["TotalImagenesGaleria"] = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // Última imagen agregada
                string queryUltima = @"
                    SELECT fecha_creacion FROM imagen_inmueble 
                    WHERE id_inmueble = @id_inmueble 
                    ORDER BY fecha_creacion DESC LIMIT 1";
                using (var command = new MySqlCommand(queryUltima, connection))
                {
                    command.Parameters.AddWithValue("@id_inmueble", idInmueble);
                    var resultado = await command.ExecuteScalarAsync();
                    estadisticas["UltimaImagenAgregada"] = resultado != null ? Convert.ToDateTime(resultado) : DateTime.MinValue;
                }

                // Primer imagen (orden más bajo)
                string queryPrimera = @"
                    SELECT orden FROM imagen_inmueble 
                    WHERE id_inmueble = @id_inmueble 
                    ORDER BY orden ASC LIMIT 1";
                using (var command = new MySqlCommand(queryPrimera, connection))
                {
                    command.Parameters.AddWithValue("@id_inmueble", idInmueble);
                    var resultado = await command.ExecuteScalarAsync();
                    estadisticas["PrimerOrden"] = resultado != null ? Convert.ToInt32(resultado) : 0;
                }

                return estadisticas;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public async Task<bool> ExistenImagenesParaInmuebleAsync(int idInmueble)
        {
            try
            {
                var count = await ContarImagenesPorInmuebleAsync(idInmueble);
                return count > 0;
            }
            catch
            {
                return false;
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

            // TIFF
            if (buffer.Length >= 4 && 
                ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00) ||
                 (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A)))
                return true;

            return false;
        }

        private static string GenerarNombreArchivoUnico(string extension, int idInmueble)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            return $"galeria_{idInmueble}_{timestamp}_{guid}{extension}";
        }

        private static string ObtenerRutaCompleta(string rutaRelativa, IWebHostEnvironment environment)
        {
            return Path.Combine(environment.WebRootPath, rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool EsExtensionPermitida(string extension)
        {
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" };
            return extensionesPermitidas.Contains(extension.ToLowerInvariant());
        }

        private static string ObtenerTipoMimeDeExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };
        }

        // Método para validar integridad de archivo usando hash
        private static async Task<string> CalcularHashArchivoAsync(IFormFile archivo)
        {
            using var stream = archivo.OpenReadStream();
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToBase64String(hash);
        }

        // Método para optimizar tamaño de imagen (opcional)
        private static async Task<bool> OptimizarImagenAsync(string rutaArchivo)
        {
            try
            {
                // Aquí podrías integrar librerías como ImageSharp para optimizar
                // Por ahora solo retornamos true indicando que está "optimizada"
                await Task.Delay(1); // Placeholder para operación async
                return File.Exists(rutaArchivo);
            }
            catch
            {
                return false;
            }
        }

        // Método para crear thumbnail (opcional)
        private static async Task<string?> CrearThumbnailAsync(string rutaOriginal, IWebHostEnvironment environment)
        {
            try
            {
                // Placeholder para generar thumbnail
                // En una implementación real usarías ImageSharp o similar
                await Task.Delay(1);
                
                var nombreOriginal = Path.GetFileNameWithoutExtension(rutaOriginal);
                var extension = Path.GetExtension(rutaOriginal);
                var directorioOriginal = Path.GetDirectoryName(rutaOriginal) ?? "";
                
                var rutaThumbnail = Path.Combine(directorioOriginal, $"{nombreOriginal}_thumb{extension}");
                
                // Aquí crearías el thumbnail real
                // File.Copy(rutaOriginal, rutaThumbnail); // Temporal
                
                return rutaThumbnail;
            }
            catch
            {
                return null;
            }
        }

        // Método auxiliar para logging de errores (opcional)
        private static void LogError(string mensaje, Exception? ex = null)
        {
            // Implementar logging según las necesidades del proyecto
            Console.WriteLine($"[RepositorioImagen] ERROR: {mensaje}");
            if (ex != null)
            {
                Console.WriteLine($"[RepositorioImagen] Exception: {ex.Message}");
            }
        }

        // Método para limpiar recursos temporales
        private static async Task LimpiarArchivosTemporalesAsync(string directorio)
        {
            try
            {
                if (!Directory.Exists(directorio)) return;

                var archivos = Directory.GetFiles(directorio);
                var fechaLimite = DateTime.Now.AddDays(-7); // Archivos más antiguos a 7 días

                foreach (var archivo in archivos)
                {
                    var fechaCreacion = File.GetCreationTime(archivo);
                    if (fechaCreacion < fechaLimite)
                    {
                        await Task.Run(() => File.Delete(archivo));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error al limpiar archivos temporales", ex);
            }
        }
    }
}
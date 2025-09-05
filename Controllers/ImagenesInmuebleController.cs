using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class ImagenesInmuebleController : Controller
    {
        private readonly string _connectionString;

        public ImagenesInmuebleController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        // GET: ImagenesInmueble/Index 
        public IActionResult Index(int idInmueble)
        {
            if (idInmueble <= 0)
            {
                return NotFound("ID de inmueble inválido");
            }

            var imagenes = new List<ImagenInmueble>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT id_imagen, id_inmueble, url, descripcion, orden, fecha_creacion
                    FROM imagen_inmueble
                    WHERE id_inmueble = @idInmueble
                    ORDER BY orden";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idInmueble", idInmueble);
                
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    imagenes.Add(new ImagenInmueble
                    {
                        IdImagen = reader.GetInt32("id_imagen"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        Url = reader.GetString("url"),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString("descripcion"),
                        Orden = reader.GetInt32("orden"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion")
                    });
                }

                ViewData["IdInmueble"] = idInmueble;
                return View(imagenes);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar imágenes: {ex.Message}";
                return RedirectToAction("Details", "Inmuebles", new { id = idInmueble });
            }
        }

        // GET: ImagenesInmueble/Create/5
        public IActionResult Create(int idInmueble)
        {
            if (idInmueble <= 0)
            {
                return NotFound("ID de inmueble inválido");
            }

            // Obtener el siguiente orden disponible
            int siguienteOrden = 1;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = "SELECT COALESCE(MAX(orden), 0) + 1 FROM imagen_inmueble WHERE id_inmueble = @idInmueble";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idInmueble", idInmueble);
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    siguienteOrden = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al obtener orden: {ex.Message}";
            }

            var imagen = new ImagenInmueble 
            { 
                IdInmueble = idInmueble,
                Orden = siguienteOrden,
                Url = "" // Valor por defecto para satisfacer el required
            };
            return View(imagen);
        }

        // POST: ImagenesInmueble/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ImagenInmueble imagen, IFormFile? ImagenFile)
        {
            if (ImagenFile == null || ImagenFile.Length == 0)
            {
                ModelState.AddModelError("ImagenFile", "Debe seleccionar una imagen válida");
                return View(imagen);
            }

            try
            {
                // Validar formato y tamaño
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var extension = Path.GetExtension(ImagenFile.FileName).ToLowerInvariant();
                
                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError("ImagenFile", "Solo se permiten imágenes JPG, JPEG, PNG, GIF o BMP");
                    return View(imagen);
                }

                if (ImagenFile.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("ImagenFile", "La imagen no puede superar 5MB");
                    return View(imagen);
                }

                // Crear directorio si no existe
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "inmuebles");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generar nombre único
                var fileName = $"{imagen.IdInmueble}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Guardar imagen
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImagenFile.CopyToAsync(stream);
                }

                // Insertar en la base de datos
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    INSERT INTO imagen_inmueble (id_inmueble, url, descripcion, orden, fecha_creacion)
                    VALUES (@idInmueble, @url, @descripcion, @orden, @fechaCreacion)";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idInmueble", imagen.IdInmueble);
                command.Parameters.AddWithValue("@url", $"/images/inmuebles/{fileName}");
                command.Parameters.AddWithValue("@descripcion", imagen.Descripcion != null ? (object)imagen.Descripcion : DBNull.Value);
                command.Parameters.AddWithValue("@orden", imagen.Orden);
                command.Parameters.AddWithValue("@fechaCreacion", DateTime.Now);
                command.ExecuteNonQuery();

                TempData["SuccessMessage"] = "Imagen agregada exitosamente";
                return RedirectToAction(nameof(Index), new { idInmueble = imagen.IdInmueble });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear imagen: {ex.Message}");
                return View(imagen);
            }
        }

        // GET: ImagenesInmueble/Edit
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT id_imagen, id_inmueble, url, descripcion, orden, fecha_creacion
                    FROM imagen_inmueble
                    WHERE id_imagen = @id";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var imagen = new ImagenInmueble
                    {
                        IdImagen = reader.GetInt32("id_imagen"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        Url = reader.GetString("url"),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString("descripcion"),
                        Orden = reader.GetInt32("orden"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion")
                    };
                    return View(imagen);
                }
                
                TempData["ErrorMessage"] = "Imagen no encontrada";
                return RedirectToAction("Index", "Inmuebles");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar imagen: {ex.Message}";
                return RedirectToAction("Index", "Inmuebles");
            }
        }

        // POST: ImagenesInmueble/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ImagenInmueble imagen, IFormFile? ImagenFile)
        {
            if (id != imagen.IdImagen)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(imagen);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string newUrl = imagen.Url;

                // Si hay nueva imagen
                if (ImagenFile != null && ImagenFile.Length > 0)
                {
                    // Validar formato y tamaño
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var extension = Path.GetExtension(ImagenFile.FileName).ToLowerInvariant();
                    
                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError("ImagenFile", "Solo se permiten imágenes JPG, JPEG, PNG, GIF o BMP");
                        return View(imagen);
                    }

                    if (ImagenFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("ImagenFile", "La imagen no puede superar 5MB");
                        return View(imagen);
                    }

                    // Eliminar imagen antigua si existe
                    if (!string.IsNullOrEmpty(imagen.Url))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagen.Url.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                // Log pero no fallar por esto
                                Console.WriteLine($"No se pudo eliminar archivo anterior: {ex.Message}");
                            }
                        }
                    }

                    // Crear directorio si no existe
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "inmuebles");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    // Generar nombre único
                    var fileName = $"{imagen.IdInmueble}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    // Guardar nueva imagen
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImagenFile.CopyToAsync(stream);
                    }
                    newUrl = $"/images/inmuebles/{fileName}";
                }

                // Actualizar en la base de datos
                string query = @"
                    UPDATE imagen_inmueble
                    SET url = @url, descripcion = @descripcion, orden = @orden
                    WHERE id_imagen = @idImagen";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@url", newUrl);
                command.Parameters.AddWithValue("@descripcion", imagen.Descripcion != null ? (object)imagen.Descripcion : DBNull.Value);
                command.Parameters.AddWithValue("@orden", imagen.Orden);
                command.Parameters.AddWithValue("@idImagen", imagen.IdImagen);
                
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "Imagen actualizada exitosamente";
                return RedirectToAction(nameof(Index), new { idInmueble = imagen.IdInmueble });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar imagen: {ex.Message}");
                return View(imagen);
            }
        }

        // GET: ImagenesInmueble/Delete
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT id_imagen, id_inmueble, url, descripcion, orden, fecha_creacion
                    FROM imagen_inmueble
                    WHERE id_imagen = @id";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var imagen = new ImagenInmueble
                    {
                        IdImagen = reader.GetInt32("id_imagen"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        Url = reader.GetString("url"),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString("descripcion"),
                        Orden = reader.GetInt32("orden"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion")
                    };
                    return View(imagen);
                }
                
                TempData["ErrorMessage"] = "Imagen no encontrada";
                return RedirectToAction("Index", "Inmuebles");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar imagen: {ex.Message}";
                return RedirectToAction("Index", "Inmuebles");
            }
        }

        // POST: ImagenesInmueble/Delete
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public IActionResult DeleteConfirmed(int id, int idInmueble)
{
    try
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        // Obtener información de la imagen antes de eliminar
        string querySelect = @"
            SELECT ii.url, ii.descripcion, i.direccion 
            FROM imagen_inmueble ii
            LEFT JOIN inmueble i ON ii.id_inmueble = i.id_inmueble 
            WHERE ii.id_imagen = @id";
        
        string url = string.Empty;
        string descripcion = string.Empty;
        string direccionInmueble = string.Empty;
        
        using (var commandSelect = new MySqlCommand(querySelect, connection))
        {
            commandSelect.Parameters.AddWithValue("@id", id);
            using var reader = commandSelect.ExecuteReader();
            
            if (reader.Read())
            {
                url = reader.IsDBNull(reader.GetOrdinal("url")) ? string.Empty : reader.GetString("url");
                descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "Sin descripción" : reader.GetString("descripcion");
                direccionInmueble = reader.IsDBNull(reader.GetOrdinal("direccion")) ? "Desconocida" : reader.GetString("direccion");
            }
            else
            {
                TempData["ErrorMessage"] = "Imagen no encontrada";
                return RedirectToAction(nameof(Index), new { idInmueble });
            }
        }

        // Eliminar registro de la base de datos
        string queryDelete = "DELETE FROM imagen_inmueble WHERE id_imagen = @id";
        using (var commandDelete = new MySqlCommand(queryDelete, connection))
        {
            commandDelete.Parameters.AddWithValue("@id", id);
            int rowsAffected = commandDelete.ExecuteNonQuery();
            
            if (rowsAffected == 0)
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la imagen de la base de datos";
                return RedirectToAction(nameof(Index), new { idInmueble });
            }
        }

        // Eliminar archivo físico
        bool archivoEliminado = false;
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                string rutaCompleta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.TrimStart('/'));
                
                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                    
                    // Verificar que se eliminó
                    if (!System.IO.File.Exists(rutaCompleta))
                    {
                        archivoEliminado = true;
                    }
                }
                else
                {
                    // Intentar ruta alternativa
                    string rutaAlternativa = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));
                    
                    if (System.IO.File.Exists(rutaAlternativa))
                    {
                        System.IO.File.Delete(rutaAlternativa);
                        archivoEliminado = true;
                    }
                }
            }
            catch (Exception fileEx)
            {
                // No fallar por esto, pero avisar
                TempData["WarningMessage"] = $"Imagen eliminada de la base de datos, pero no se pudo eliminar el archivo físico: {fileEx.Message}";
            }
        }

        // Mensaje de éxito
        string mensajeExito = archivoEliminado 
            ? $"Imagen '{descripcion}' eliminada completamente"
            : $"Imagen '{descripcion}' eliminada de la base de datos";
            
        if (!string.IsNullOrEmpty(direccionInmueble))
        {
            mensajeExito += $" del inmueble en {direccionInmueble}";
        }
        
        TempData["SuccessMessage"] = mensajeExito;
        return RedirectToAction(nameof(Index), new { idInmueble });
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"Error al eliminar imagen: {ex.Message}";
        return RedirectToAction(nameof(Index), new { idInmueble });
    }
}
}   
}
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using System.Data;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class InquilinosController : Controller
    {
        private readonly string _connectionString;

        public InquilinosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        // GET: Inquilinos
        public IActionResult Index()
        {
            var listaInquilinos = new List<Inquilino>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM inquilino WHERE estado = 1 ORDER id_inquilino";
                    
                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaInquilinos.Add(new Inquilino
                            {
                                Id = reader.GetInt32("id_inquilino"),
                                Dni = reader.GetString("dni"),
                                Apellido = reader.GetString("apellido"),
                                Nombre = reader.GetString("nombre"),
                                Direccion = reader.IsDBNull(reader.GetOrdinal("direccion")) ? null : reader.GetString("direccion"),
                                Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString("telefono"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                FechaCreacion = reader.GetDateTime("fecha_alta"),
                                Estado = reader.GetBoolean("estado")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los inquilinos: {ex.Message}";
            }
            
            return View(listaInquilinos);
        }

        // GET: Inquilinos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inquilinos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Inquilino inquilino)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar si ya existe el DNI
                    if (ExisteDni(inquilino.Dni))
                    {
                        ModelState.AddModelError("Dni", "Ya existe un inquilino con este DNI");
                        return View(inquilino);
                    }

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"INSERT INTO inquilino 
                                        (dni, apellido, nombre, direccion, telefono, email, estado, fecha_alta) 
                                        VALUES (@dni, @apellido, @nombre, @direccion, @telefono, @email, @estado, @fecha_alta)";
                        
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@dni", inquilino.Dni);
                            command.Parameters.AddWithValue("@apellido", inquilino.Apellido);
                            command.Parameters.AddWithValue("@nombre", inquilino.Nombre);
                            command.Parameters.AddWithValue("@direccion", inquilino.Direccion != null ? (object)inquilino.Direccion : DBNull.Value);
                            command.Parameters.AddWithValue("@telefono", inquilino.Telefono != null ? (object)inquilino.Telefono : DBNull.Value);
                            command.Parameters.AddWithValue("@email", inquilino.Email != null ? (object)inquilino.Email : DBNull.Value);
                            command.Parameters.AddWithValue("@estado", inquilino.Estado);
                            command.Parameters.AddWithValue("@fecha_alta", DateTime.Now);
                            
                            command.ExecuteNonQuery();
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Inquilino creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al crear el inquilino: {ex.Message}");
                }
            }
            return View(inquilino);
        }

        // GET: Inquilinos/Edit/5
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Inquilino? inquilino = null;
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM inquilino WHERE id_inquilino = @id";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                inquilino = new Inquilino
                                {
                                    Id = reader.GetInt32("id_inquilino"),
                                    Dni = reader.GetString("dni"),
                                    Apellido = reader.GetString("apellido"),
                                    Nombre = reader.GetString("nombre"),
                                    Direccion = reader.IsDBNull(reader.GetOrdinal("direccion")) ? null : reader.GetString("direccion"),
                                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString("telefono"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    FechaCreacion = reader.GetDateTime("fecha_alta"),
                                    Estado = reader.GetBoolean("estado")
                                };
                            }
                        }
                    }
                }
                
                if (inquilino == null)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inquilino: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            
            return View(inquilino);
        }

        // POST: Inquilinos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Inquilino inquilino)
        {
            if (id != inquilino.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar si ya existe otro inquilino con el mismo DNI
                    if (ExisteDni(inquilino.Dni, id))
                    {
                        ModelState.AddModelError("Dni", "Ya existe un inquilino con este DNI");
                        return View(inquilino);
                    }

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"UPDATE inquilino 
                                        SET dni = @dni, apellido = @apellido, nombre = @nombre, 
                                            direccion = @direccion, telefono = @telefono, email = @email, estado = @estado 
                                        WHERE id_inquilino = @id";
                        
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@dni", inquilino.Dni);
                            command.Parameters.AddWithValue("@apellido", inquilino.Apellido);
                            command.Parameters.AddWithValue("@nombre", inquilino.Nombre);
                            command.Parameters.AddWithValue("@direccion", inquilino.Direccion != null ? (object)inquilino.Direccion : DBNull.Value);
                            command.Parameters.AddWithValue("@telefono", inquilino.Telefono != null ? (object)inquilino.Telefono : DBNull.Value);
                            command.Parameters.AddWithValue("@email", inquilino.Email != null ? (object)inquilino.Email : DBNull.Value);
                            command.Parameters.AddWithValue("@estado", inquilino.Estado);
                            command.Parameters.AddWithValue("@id", id);
                            
                            int rowsAffected = command.ExecuteNonQuery();
                            
                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Inquilino actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el inquilino: {ex.Message}");
                }
            }
            return View(inquilino);
        }

        // GET: Inquilinos/Delete/5
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Inquilino? inquilino = null;
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM inquilino WHERE id_inquilino = @id";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                inquilino = new Inquilino
                                {
                                    Id = reader.GetInt32("id_inquilino"),
                                    Dni = reader.GetString("dni"),
                                    Apellido = reader.GetString("apellido"),
                                    Nombre = reader.GetString("nombre"),
                                    Direccion = reader.IsDBNull(reader.GetOrdinal("direccion")) ? null : reader.GetString("direccion"),
                                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString("telefono"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    FechaCreacion = reader.GetDateTime("fecha_alta"),
                                    Estado = reader.GetBoolean("estado")
                                };
                            }
                        }
                    }
                }
                
                if (inquilino == null)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inquilino: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            
            return View(inquilino);
        }

        // POST: Inquilinos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Soft delete (cambiar estado a false)
                    string query = "UPDATE inquilino SET estado = 0 WHERE id_inquilino = @id";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            return NotFound();
                        }
                    }
                }
                
                TempData["SuccessMessage"] = "Inquilino eliminado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el inquilino: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // Métodos auxiliares
        private bool ExisteDni(string dni, int idExcluir = 0)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = idExcluir == 0 
                    ? "SELECT COUNT(*) FROM inquilino WHERE dni = @dni AND estado = 1" 
                    : "SELECT COUNT(*) FROM inquilino WHERE dni = @dni AND id_inquilino != @id AND estado = 1";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@dni", dni);
                    if (idExcluir != 0)
                    {
                        command.Parameters.AddWithValue("@id", idExcluir);
                    }
                    
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }
    }
}
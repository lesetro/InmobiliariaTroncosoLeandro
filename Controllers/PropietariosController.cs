using Microsoft.AspNetCore.Mvc;           // ← Para usar Controller, IActionResult, etc.
using MySql.Data.MySqlClient;            // ← Para conectarse a MySQL/MariaDB
using Inmobiliaria_troncoso_leandro.Models; // ← Para usar el modelo Propietario
using System.Data;                       // ← Para tipos de datos como DbNull

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class PropietariosController : Controller
    {
        private readonly string _connectionString;

        public PropietariosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        // GET: Propietarios
        public IActionResult Index()
        {
            var listaPropietarios = new List<Propietario>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM propietario WHERE estado = 1 ORDER BY id_propietario";
                    
                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaPropietarios.Add(new Propietario
                            {
                                Id = reader.GetInt32("id_propietario"),
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
                TempData["ErrorMessage"] = $"Error al cargar los propietarios: {ex.Message}";
            }
            
            return View(listaPropietarios);
        }

        // GET: Propietarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Propietarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Propietario propietario)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar si ya existe el DNI
                    if (ExisteDni(propietario.Dni))
                    {
                        ModelState.AddModelError("Dni", "Ya existe un propietario con este DNI");
                        return View(propietario);
                    }

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"INSERT INTO propietario 
                                        (dni, apellido, nombre, direccion, telefono, email, estado, fecha_alta) 
                                        VALUES (@dni, @apellido, @nombre, @direccion, @telefono, @email, @estado, @fecha_alta)";
                        
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@dni", propietario.Dni);
                            command.Parameters.AddWithValue("@apellido", propietario.Apellido);
                            command.Parameters.AddWithValue("@nombre", propietario.Nombre);
                            command.Parameters.AddWithValue("@direccion", propietario.Direccion != null ? (object)propietario.Direccion : DBNull.Value);
                            command.Parameters.AddWithValue("@telefono", propietario.Telefono != null ? (object)propietario.Telefono : DBNull.Value);
                            command.Parameters.AddWithValue("@email", propietario.Email != null ? (object)propietario.Email : DBNull.Value);
                            command.Parameters.AddWithValue("@estado", propietario.Estado);
                            command.Parameters.AddWithValue("@fecha_alta", DateTime.Now);
                            
                            command.ExecuteNonQuery();
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Propietario creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al crear el propietario: {ex.Message}");
                }
            }
            return View(propietario);
        }

        // GET: Propietarios/Edit/5
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Propietario? propietario = null;
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM propietario WHERE id_propietario = @id";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                propietario = new Propietario
                                {
                                    Id = reader.GetInt32("id_propietario"),
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
                
                if (propietario == null)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el propietario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            
            return View(propietario);
        }

        // POST: Propietarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Propietario propietario)
        {
            if (id != propietario.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar si ya existe otro propietario con el mismo DNI
                    if (ExisteDni(propietario.Dni, id))
                    {
                        ModelState.AddModelError("Dni", "Ya existe un propietario con este DNI");
                        return View(propietario);
                    }

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"UPDATE propietario 
                                        SET dni = @dni, apellido = @apellido, nombre = @nombre, 
                                            direccion = @direccion, telefono = @telefono, email = @email, 
                                            estado = @estado 
                                        WHERE id_propietario = @id";
                        
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@dni", propietario.Dni);
                            command.Parameters.AddWithValue("@apellido", propietario.Apellido);
                            command.Parameters.AddWithValue("@nombre", propietario.Nombre);
                            command.Parameters.AddWithValue("@direccion", propietario.Direccion != null ? (object)propietario.Direccion : DBNull.Value);
                            command.Parameters.AddWithValue("@telefono", propietario.Telefono != null ? (object)propietario.Telefono : DBNull.Value);
                            command.Parameters.AddWithValue("@email", propietario.Email != null ? (object)propietario.Email : DBNull.Value);
                            command.Parameters.AddWithValue("@estado", propietario.Estado);
                            command.Parameters.AddWithValue("@id", id);
                            
                            int rowsAffected = command.ExecuteNonQuery();
                            
                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Propietario actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el propietario: {ex.Message}");
                }
            }
            return View(propietario);
        }

        // GET: Propietarios/Delete/5
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Propietario? propietario = null;
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM propietario WHERE id_propietario = @id";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                propietario = new Propietario
                                {
                                    Id = reader.GetInt32("id_propietario"),
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
                
                if (propietario == null)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el propietario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            
            return View(propietario);
        }

        // POST: Propietarios/Delete/5
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
                    string query = "UPDATE propietario SET estado = 0 WHERE id_propietario = @id";
                    
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
                
                TempData["SuccessMessage"] = "Propietario eliminado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el propietario: {ex.Message}";
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
                    ? "SELECT COUNT(*) FROM propietario WHERE dni = @dni AND estado = 1" 
                    : "SELECT COUNT(*) FROM propietario WHERE dni = @dni AND id_propietario != @id AND estado = 1";
                
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
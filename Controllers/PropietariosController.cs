using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using System.Collections.Generic;
using Inmobiliaria_troncoso_leandro.Services;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class PropietariosController : Controller
    {
        private readonly string _connectionString;
        private readonly ISearchService _searchService;

        public PropietariosController(IConfiguration configuration, ISearchService searchService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
            _searchService = searchService;
        }

        // GET: Propietarios
        public IActionResult Index(int pagina = 1, string buscar = "")
        {
            var listaPropietarios = new List<Propietario>();
            const int ITEMS_POR_PAGINA = 10;
            int totalRegistros = 0;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // Construir WHERE dinámico
                    string whereClause = "WHERE p.estado = 1";
                    var parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(buscar))
                    {
                        whereClause += @" AND (u.nombre LIKE @buscar 
                                      OR u.apellido LIKE @buscar 
                                      OR u.dni LIKE @buscar
                                      OR u.email LIKE @buscar
                                      OR u.telefono LIKE @buscar
                                      OR CONCAT(u.nombre, ' ', u.apellido) LIKE @buscar)";
                        parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
                    }

                    // Contar total de registros
                    string countQuery = $@"
                SELECT COUNT(*) 
                FROM propietario p
                INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                {whereClause}";

                    using (var countCommand = new MySqlCommand(countQuery, connection))
                    {
                        foreach (var param in parameters)
                        {
                            countCommand.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                        }
                        totalRegistros = Convert.ToInt32(countCommand.ExecuteScalar());
                    }

                    // Consulta principal con paginación
                    string query = $@"
                SELECT p.id_propietario, p.id_usuario, p.fecha_creacion, p.estado,
                       u.dni, u.apellido, u.nombre, u.direccion, u.telefono, u.email
                FROM propietario p
                INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                {whereClause}
                ORDER BY u.apellido, u.nombre
                LIMIT @offset, @limit";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                        }
                        command.Parameters.AddWithValue("@offset", (pagina - 1) * ITEMS_POR_PAGINA);
                        command.Parameters.AddWithValue("@limit", ITEMS_POR_PAGINA);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaPropietarios.Add(new Propietario
                                {
                                    IdPropietario = reader.GetInt32("id_propietario"),
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    FechaAlta = reader.IsDBNull(reader.GetOrdinal("fecha_creacion")) ?
                                               DateTime.Now : reader.GetDateTime("fecha_creacion"),
                                    Estado = reader.GetBoolean("estado"),
                                    Usuario = new Usuario
                                    {
                                        IdUsuario = reader.GetInt32("id_usuario"),
                                        Dni = reader.GetString("dni"),
                                        Apellido = reader.GetString("apellido"),
                                        Nombre = reader.GetString("nombre"),
                                        Direccion = reader.IsDBNull(reader.GetOrdinal("direccion")) ? null : reader.GetString("direccion"),
                                        Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString("telefono"),
                                        Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email")
                                    }
                                });
                            }
                        }
                    }
                }

                // Preparar datos de paginación para la vista
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / ITEMS_POR_PAGINA);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.ITEMS_POR_PAGINA = ITEMS_POR_PAGINA;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los propietarios: {ex.Message}";
            }

            return View(listaPropietarios);
        }

        // 5. AGREGAR método nuevo para API de búsqueda:
        [HttpGet]
        public async Task<IActionResult> BuscarPropietarios(string termino, int limite = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarPropietariosAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        // GET: Propietarios/Create
        public IActionResult Create()
        {
            return View(new Propietario { Usuario = new Usuario() });
        }

        // POST: Propietarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Propietario propietario)
        {
            if (!ModelState.IsValid)
            {
                return View(propietario);
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Verificar DNI único
                if (!string.IsNullOrEmpty(propietario.Usuario.Dni) && ExisteDni(propietario.Usuario.Dni, connection, transaction))
                {
                    ModelState.AddModelError("Usuario.Dni", "Ya existe un usuario con este DNI");
                    return View(propietario);
                }

                // Verificar Email único
                if (!string.IsNullOrEmpty(propietario.Usuario.Email) && ExisteEmail(propietario.Usuario.Email, connection, transaction))
                {
                    ModelState.AddModelError("Usuario.Email", "Ya existe un usuario con este email");
                    return View(propietario);
                }

                // Crear usuario
                string queryUsuario = @"
                    INSERT INTO usuario 
                    (dni, nombre, apellido, telefono, email, direccion, password, rol, estado, fecha_creacion) 
                    VALUES (@dni, @nombre, @apellido, @telefono, @email, @direccion, @password, @rol, @estado, @fecha_creacion);
                    SELECT LAST_INSERT_ID();";

                int idUsuario;
                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@dni", propietario.Usuario.Dni);
                    commandUsuario.Parameters.AddWithValue("@nombre", propietario.Usuario.Nombre);
                    commandUsuario.Parameters.AddWithValue("@apellido", propietario.Usuario.Apellido);
                    commandUsuario.Parameters.AddWithValue("@telefono", propietario.Usuario.Telefono ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@email", propietario.Usuario.Email ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@direccion", propietario.Usuario.Direccion ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword("passwordtemporal"));
                    commandUsuario.Parameters.AddWithValue("@rol", "propietario");
                    commandUsuario.Parameters.AddWithValue("@estado", true);
                    commandUsuario.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);

                    idUsuario = Convert.ToInt32(commandUsuario.ExecuteScalar());
                }

                // Crear propietario
                string queryPropietario = @"
                    INSERT INTO propietario 
                    (id_usuario, fecha_alta, estado) 
                    VALUES (@id_usuario, @fecha_alta, @estado)";

                using (var commandPropietario = new MySqlCommand(queryPropietario, connection, transaction))
                {
                    commandPropietario.Parameters.AddWithValue("@id_usuario", idUsuario);
                    commandPropietario.Parameters.AddWithValue("@fecha_alta", DateTime.Now);
                    commandPropietario.Parameters.AddWithValue("@estado", true);

                    commandPropietario.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Propietario creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError("", $"Error al crear propietario: {ex.Message}");
                return View(propietario);
            }
        }

        private bool ExisteDni(string dni, MySqlConnection connection, MySqlTransaction transaction)
        {
            string query = "SELECT COUNT(*) FROM usuario WHERE dni = @dni";
            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@dni", dni);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ExisteEmail(string email, MySqlConnection connection, MySqlTransaction transaction)
        {
            if (string.IsNullOrEmpty(email)) return false;
            string query = "SELECT COUNT(*) FROM usuario WHERE email = @email";
            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@email", email);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        // GET: Propietarios/Edit
        public IActionResult Edit(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
                           u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion
                    FROM propietario p 
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
                    WHERE p.id_propietario = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var propietario = new Propietario
                    {
                        IdPropietario = reader.GetInt32("id_propietario"),
                        IdUsuario = reader.GetInt32("id_usuario"),
                        FechaAlta = reader.GetDateTime("fecha_alta"),
                        Estado = reader.GetBoolean("estado"),
                        Usuario = new Usuario
                        {
                            IdUsuario = reader.GetInt32("id_usuario"),
                            Dni = reader.GetString("dni"),
                            Nombre = reader.GetString("nombre"),
                            Apellido = reader.GetString("apellido"),
                            Telefono = reader.GetString("telefono"),
                            Email = reader.GetString("email"),
                            Direccion = reader.GetString("direccion")
                        }
                    };
                    return View(propietario);
                }
                TempData["ErrorMessage"] = "Propietario no encontrado";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar propietario: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
        // POST: Propietarios/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Propietario propietario)
        {
            if (id != propietario.IdPropietario)
            {
                TempData["ErrorMessage"] = "ID de propietario no coincide";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(propietario);
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Verificar DNI único 
                string queryDni = "SELECT COUNT(*) FROM usuario WHERE dni = @dni AND id_usuario != @id_usuario";
                using (var commandDni = new MySqlCommand(queryDni, connection, transaction))
                {
                    commandDni.Parameters.AddWithValue("@dni", propietario.Usuario.Dni);
                    commandDni.Parameters.AddWithValue("@id_usuario", propietario.IdUsuario);
                    if (Convert.ToInt32(commandDni.ExecuteScalar()) > 0)
                    {
                        ModelState.AddModelError("Usuario.Dni", "Ya existe otro usuario con este DNI");
                        return View(propietario);
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
                        if (Convert.ToInt32(commandEmail.ExecuteScalar()) > 0)
                        {
                            ModelState.AddModelError("Usuario.Email", "Ya existe otro usuario con este email");
                            return View(propietario);
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
                    commandUsuario.Parameters.AddWithValue("@dni", propietario.Usuario.Dni);
                    commandUsuario.Parameters.AddWithValue("@nombre", propietario.Usuario.Nombre);
                    commandUsuario.Parameters.AddWithValue("@apellido", propietario.Usuario.Apellido);
                    commandUsuario.Parameters.AddWithValue("@telefono", propietario.Usuario.Telefono ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@email", propietario.Usuario.Email ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@direccion", propietario.Usuario.Direccion ?? (object)DBNull.Value);
                    commandUsuario.Parameters.AddWithValue("@id_usuario", propietario.IdUsuario);

                    commandUsuario.ExecuteNonQuery();
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

                    commandPropietario.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Propietario actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError("", $"Error al actualizar propietario: {ex.Message}");
                return View(propietario);
            }
        }
        // GET: Propietarios/Delete
        public IActionResult Delete(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT p.id_propietario, p.id_usuario, p.fecha_alta, p.estado,
                           u.dni, u.nombre, u.apellido
                    FROM propietario p 
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
                    WHERE p.id_propietario = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var propietario = new Propietario
                    {
                        IdPropietario = reader.GetInt32("id_propietario"),
                        IdUsuario = reader.GetInt32("id_usuario"),
                        FechaAlta = reader.GetDateTime("fecha_alta"),
                        Estado = reader.GetBoolean("estado"),
                        Usuario = new Usuario
                        {
                            Dni = reader.GetString("dni"),
                            Nombre = reader.GetString("nombre"),
                            Apellido = reader.GetString("apellido")
                        }
                    };
                    return View(propietario);
                }
                TempData["ErrorMessage"] = "Propietario no encontrado";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar propietario: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Propietarios/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Obtener id_usuario
                int idUsuario;
                string queryGetUsuario = "SELECT id_usuario FROM propietario WHERE id_propietario = @id_propietario";
                using (var commandGet = new MySqlCommand(queryGetUsuario, connection, transaction))
                {
                    commandGet.Parameters.AddWithValue("@id_propietario", id);
                    idUsuario = Convert.ToInt32(commandGet.ExecuteScalar());
                }

                // Actualizar estado en propietario
                string queryPropietario = "UPDATE propietario SET estado = @estado WHERE id_propietario = @id_propietario";
                using (var commandPropietario = new MySqlCommand(queryPropietario, connection, transaction))
                {
                    commandPropietario.Parameters.AddWithValue("@estado", false);
                    commandPropietario.Parameters.AddWithValue("@id_propietario", id);
                    commandPropietario.ExecuteNonQuery();
                }

                // Actualizar estado en usuario
                string queryUsuario = "UPDATE usuario SET estado = @estado WHERE id_usuario = @id_usuario";
                using (var commandUsuario = new MySqlCommand(queryUsuario, connection, transaction))
                {
                    commandUsuario.Parameters.AddWithValue("@estado", false);
                    commandUsuario.Parameters.AddWithValue("@id_usuario", idUsuario);
                    commandUsuario.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Propietario eliminado exitosamente";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = $"Error al eliminar propietario: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
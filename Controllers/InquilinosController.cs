
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Services; 
using System.Data;

namespace Inmobiliaria_troncoso_leandro.Controllers
    {
        public class InquilinosController : Controller
        {
            private readonly string _connectionString;
            private readonly ISearchService _searchService; 

            // CONSTRUCTOR ACTUALIZADO
            public InquilinosController(IConfiguration configuration, ISearchService searchService)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
                _searchService = searchService; 
            }

            // GET: Inquilinos
            public IActionResult Index(int pagina = 1, string buscar = "")
            {
                var listaInquilinos = new List<Inquilino>();
                const int ITEMS_POR_PAGINA = 10;
                int totalRegistros = 0;

                try
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Construir WHERE dinámico
                        string whereClause = "WHERE inq.estado = 1";
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
                        FROM inquilino inq
                        INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
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
                        SELECT inq.id_inquilino, inq.id_usuario, inq.fecha_alta, inq.estado,
                               u.dni, u.apellido, u.nombre, u.direccion, u.telefono, u.email
                        FROM inquilino inq
                        INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
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
                                    listaInquilinos.Add(new Inquilino
                                    {
                                        IdInquilino = reader.GetInt32("id_inquilino"),
                                        IdUsuario = reader.GetInt32("id_usuario"),
                                        FechaAlta = reader.GetDateTime("fecha_alta"),
                                        Estado = reader.GetBoolean("estado"),
                                        Usuario = new Usuario
                                        {
                                            IdUsuario = reader.GetInt32("id_usuario"), // ← CORREGIDO: usar GetInt32
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
                    TempData["ErrorMessage"] = $"Error al cargar los inquilinos: {ex.Message}";
                }

                return View(listaInquilinos);
            }

            // MÉTODO NUEVO para API de búsqueda
            [HttpGet]
            public async Task<IActionResult> BuscarInquilinos(string termino, int limite = 10)
            {
                try
                {
                    var resultados = await _searchService.BuscarInquilinosAsync(termino, limite);
                    return Json(resultados);
                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message });
                }
            }

            // POST: Inquilinos/Create
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult Create(Inquilino inquilino)
            {
                if (!ModelState.IsValid)
                {
                    return View(inquilino);
                }

                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Verificar DNI único
                    if (!string.IsNullOrEmpty(inquilino.Usuario.Dni) && ExisteDni(inquilino.Usuario.Dni, connection, transaction))
                    {
                        ModelState.AddModelError("Usuario.Dni", "Ya existe un usuario con este DNI");
                        return View(inquilino);
                    }

                    // Verificar Email único
                    if (!string.IsNullOrEmpty(inquilino.Usuario.Email) && ExisteEmail(inquilino.Usuario.Email, connection, transaction))
                    {
                        ModelState.AddModelError("Usuario.Email", "Ya existe un usuario con este email");
                        return View(inquilino);
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
                        commandUsuario.Parameters.AddWithValue("@dni", inquilino.Usuario.Dni);
                        commandUsuario.Parameters.AddWithValue("@nombre", inquilino.Usuario.Nombre);
                        commandUsuario.Parameters.AddWithValue("@apellido", inquilino.Usuario.Apellido);
                        commandUsuario.Parameters.AddWithValue("@telefono", inquilino.Usuario.Telefono ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@email", inquilino.Usuario.Email ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@direccion", inquilino.Usuario.Direccion ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword("passwordtemporal"));
                        commandUsuario.Parameters.AddWithValue("@rol", "inquilino");
                        commandUsuario.Parameters.AddWithValue("@estado", true);
                        commandUsuario.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);

                        idUsuario = Convert.ToInt32(commandUsuario.ExecuteScalar());
                    }

                    // Crear inquilino
                    string queryInquilino = @"
                    INSERT INTO inquilino 
                    (id_usuario, fecha_alta, estado) 
                    VALUES (@id_usuario, @fecha_alta, @estado)";

                    using (var commandInquilino = new MySqlCommand(queryInquilino, connection, transaction))
                    {
                        commandInquilino.Parameters.AddWithValue("@id_usuario", idUsuario);
                        commandInquilino.Parameters.AddWithValue("@fecha_alta", DateTime.Now);
                        commandInquilino.Parameters.AddWithValue("@estado", true);

                        commandInquilino.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    TempData["SuccessMessage"] = "Inquilino creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", $"Error al crear inquilino: {ex.Message}");
                    return View(inquilino);
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
            // GET: Inquilinos/Edit/5
            public IActionResult Edit(int id)
            {
                try
                {
                    using var connection = new MySqlConnection(_connectionString);
                    connection.Open();
                    string query = @"
                    SELECT i.id_inquilino, i.id_usuario, i.fecha_alta, i.estado,
                           u.id_usuario, u.dni, u.nombre, u.apellido, u.telefono, u.email, u.direccion
                    FROM inquilino i 
                    INNER JOIN usuario u ON i.id_usuario = u.id_usuario 
                    WHERE i.id_inquilino = @id";

                    using var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", id);

                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        var inquilino = new Inquilino
                        {
                            IdInquilino = reader.GetInt32("id_inquilino"),
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
                                Direccion = reader.GetString("direccion"),

                            }
                        };
                        return View(inquilino);
                    }
                    TempData["ErrorMessage"] = "Inquilino no encontrado";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error al cargar inquilino: {ex.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
            // POST: Inquilinos/Edit/5
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult Edit(int id, Inquilino inquilino)
            {
                if (id != inquilino.IdInquilino)
                {
                    TempData["ErrorMessage"] = "ID de inquilino no coincide";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    return View(inquilino);
                }

                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Verificar DNI único (excluyendo el usuario actual)
                    string queryDni = "SELECT COUNT(*) FROM usuario WHERE dni = @dni AND id_usuario != @id_usuario";
                    using (var commandDni = new MySqlCommand(queryDni, connection, transaction))
                    {
                        commandDni.Parameters.AddWithValue("@dni", inquilino.Usuario.Dni);
                        commandDni.Parameters.AddWithValue("@id_usuario", inquilino.IdUsuario);
                        if (Convert.ToInt32(commandDni.ExecuteScalar()) > 0)
                        {
                            ModelState.AddModelError("Usuario.Dni", "Ya existe otro usuario con este DNI");
                            return View(inquilino);
                        }
                    }

                    // Verificar Email único (excluyendo el usuario actual)
                    if (!string.IsNullOrEmpty(inquilino.Usuario.Email))
                    {
                        string queryEmail = "SELECT COUNT(*) FROM usuario WHERE email = @email AND id_usuario != @id_usuario";
                        using (var commandEmail = new MySqlCommand(queryEmail, connection, transaction))
                        {
                            commandEmail.Parameters.AddWithValue("@email", inquilino.Usuario.Email);
                            commandEmail.Parameters.AddWithValue("@id_usuario", inquilino.IdUsuario);
                            if (Convert.ToInt32(commandEmail.ExecuteScalar()) > 0)
                            {
                                ModelState.AddModelError("Usuario.Email", "Ya existe otro usuario con este email");
                                return View(inquilino);
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
                        commandUsuario.Parameters.AddWithValue("@dni", inquilino.Usuario.Dni);
                        commandUsuario.Parameters.AddWithValue("@nombre", inquilino.Usuario.Nombre);
                        commandUsuario.Parameters.AddWithValue("@apellido", inquilino.Usuario.Apellido);
                        commandUsuario.Parameters.AddWithValue("@telefono", inquilino.Usuario.Telefono ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@email", inquilino.Usuario.Email ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@direccion", inquilino.Usuario.Direccion ?? (object)DBNull.Value);
                        commandUsuario.Parameters.AddWithValue("@id_usuario", inquilino.IdUsuario);

                        commandUsuario.ExecuteNonQuery();
                    }

                    // Actualizar inquilino
                    string queryInquilino = @"
                    UPDATE inquilino 
                    SET estado = @estado 
                    WHERE id_inquilino = @id_inquilino";

                    using (var commandInquilino = new MySqlCommand(queryInquilino, connection, transaction))
                    {
                        commandInquilino.Parameters.AddWithValue("@estado", inquilino.Estado);
                        commandInquilino.Parameters.AddWithValue("@id_inquilino", inquilino.IdInquilino);

                        commandInquilino.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    TempData["SuccessMessage"] = "Inquilino actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", $"Error al actualizar inquilino: {ex.Message}");
                    return View(inquilino);
                }
            }
            // GET: Inquilinos/Delete/5
            public IActionResult Delete(int id)
            {
                try
                {
                    using var connection = new MySqlConnection(_connectionString);
                    connection.Open();
                    string query = @"
                    SELECT i.id_inquilino, i.id_usuario, i.fecha_alta, i.estado,
                           u.dni, u.nombre, u.apellido
                    FROM inquilino i 
                    INNER JOIN usuario u ON i.id_usuario = u.id_usuario 
                    WHERE i.id_inquilino = @id";

                    using var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", id);

                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        var inquilino = new Inquilino
                        {
                            IdInquilino = reader.GetInt32("id_inquilino"),
                            IdUsuario = reader.GetInt32("id_usuario"),
                            FechaAlta = reader.GetDateTime("fecha_alta"),
                            Estado = reader.GetBoolean("estado"),
                            Usuario = new Usuario
                            {
                                Dni = reader.GetString("dni"),
                                Nombre = reader.GetString("nombre"),
                                Apellido = reader.GetString("apellido"),
                                Direccion = reader.GetString("direccion"),
                                Email = reader.GetString("email"),
                                Telefono = reader.GetString("telefono"),


                            }
                        };
                        return View(inquilino);
                    }
                    TempData["ErrorMessage"] = "Inquilino no encontrado";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error al cargar inquilino: {ex.Message}";
                }

                return RedirectToAction(nameof(Index));
            }

            // POST: Inquilinos/Delete/5
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
                    string queryGetUsuario = "SELECT id_usuario FROM inquilino WHERE id_inquilino = @id_inquilino";
                    using (var commandGet = new MySqlCommand(queryGetUsuario, connection, transaction))
                    {
                        commandGet.Parameters.AddWithValue("@id_inquilino", id);
                        idUsuario = Convert.ToInt32(commandGet.ExecuteScalar());
                    }

                    // Actualizar estado en inquilino
                    string queryInquilino = "UPDATE inquilino SET estado = @estado WHERE id_inquilino = @id_inquilino";
                    using (var commandInquilino = new MySqlCommand(queryInquilino, connection, transaction))
                    {
                        commandInquilino.Parameters.AddWithValue("@estado", false);
                        commandInquilino.Parameters.AddWithValue("@id_inquilino", id);
                        commandInquilino.ExecuteNonQuery();
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
                    TempData["SuccessMessage"] = "Inquilino eliminado exitosamente";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = $"Error al eliminar inquilino: {ex.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
        }

    }
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Services; // ← AGREGAR ESTA LÍNEA
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class InmueblesController : Controller
    {
        private readonly string _connectionString;
        private readonly ISearchService _searchService; // ← AGREGAR ESTA LÍNEA
        private const int ITEMS_POR_PAGINA = 10;

        // CONSTRUCTOR CORREGIDO
        public InmueblesController(IConfiguration configuration, ISearchService searchService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
            _searchService = searchService; // ← AGREGAR ESTA LÍNEA
        }

        // MÉTODO INDEX CORREGIDO CON BÚSQUEDA Y PAGINACIÓN
        public IActionResult Index(int pagina = 1, string buscar = "", string estado = "")
        {
            var listaInmuebles = new List<Inmueble>();
            int totalRegistros = 0;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

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

                    // Contar total de registros
                    string countQuery = $@"
                        SELECT COUNT(*) 
                        FROM inmueble i
                        INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                        INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                        INNER JOIN usuario up ON p.id_usuario = up.id_usuario
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
                        SELECT i.id_inmueble, i.id_propietario, i.id_tipo_inmueble, i.direccion, 
                               i.uso, i.ambientes, i.precio, i.coordenadas, i.estado, i.fecha_alta,
                               t.nombre as tipo_nombre,
                               up.nombre as propietario_nombre, up.apellido as propietario_apellido
                        FROM inmueble i
                        INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                        INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                        INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                        {whereClause}
                        ORDER BY i.direccion
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
                                listaInmuebles.Add(new Inmueble
                                {
                                    IdInmueble = reader.GetInt32("id_inmueble"),
                                    IdPropietario = reader.GetInt32("id_propietario"),
                                    IdTipoInmueble = reader.GetInt32("id_tipo_inmueble"),
                                    Direccion = reader.GetString("direccion"),
                                    Uso = reader.GetString("uso"),
                                    Ambientes = reader.GetInt32("ambientes"),
                                    Precio = reader.GetDecimal("precio"),
                                    Coordenadas = reader.IsDBNull(reader.GetOrdinal("coordenadas")) ? null : reader.GetString("coordenadas"),
                                    Estado = reader.GetString("estado"),
                                    FechaAlta = reader.GetDateTime("fecha_alta"),
                                    TipoInmueble = new TipoInmueble
                                    {
                                        Nombre = reader.GetString("tipo_nombre")
                                    },
                                    Propietario = new Propietario
                                    {
                                        Usuario = new Usuario
                                        {
                                            Nombre = reader.GetString("propietario_nombre"),
                                            Apellido = reader.GetString("propietario_apellido")
                                        }
                                    }
                                });
                            }
                        }
                    }
                }

                // Preparar datos de paginación
                ViewBag.PaginaActual = pagina;
                ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / ITEMS_POR_PAGINA);
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.Buscar = buscar;
                ViewBag.Estado = estado;
                ViewBag.ITEMS_POR_PAGINA = ITEMS_POR_PAGINA;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los inmuebles: {ex.Message}";
            }

            return View(listaInmuebles);
        }

        // MÉTODO POPULATEVIEWDATA CORREGIDO
        private void PopulateViewData()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // Cargar propietarios activos con sus usuarios
                var propietarios = new List<Propietario>();
                string queryPropietarios = @"
                    SELECT p.id_propietario, u.nombre, u.apellido, u.dni
                    FROM propietario p 
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
                    WHERE p.estado = true AND u.estado = 'activo'
                    ORDER BY u.apellido, u.nombre";

                using (var command = new MySqlCommand(queryPropietarios, connection))
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        propietarios.Add(new Propietario
                        {
                            IdPropietario = reader.GetInt32("id_propietario"),
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString("nombre"),
                                Apellido = reader.GetString("apellido"),
                                Dni = reader.GetString("dni")
                            }
                        });
                    }
                }

                ViewData["Propietarios"] = propietarios;

                // Cargar tipos de inmueble activos
                var tiposInmueble = new List<TipoInmueble>();
                string queryTipos = @"
                    SELECT id_tipo_inmueble, nombre, descripcion
                    FROM tipo_inmueble 
                    WHERE estado = true
                    ORDER BY nombre";

                using (var command = new MySqlCommand(queryTipos, connection))
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        tiposInmueble.Add(new TipoInmueble
                        {
                            IdTipoInmueble = reader.GetInt32("id_tipo_inmueble"),
                            Nombre = reader.GetString("nombre"),
                            Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString("descripcion")
                        });
                    }
                }

                ViewData["TiposInmuebles"] = tiposInmueble;
            }
            catch (Exception ex)
            {
                // Crear listas vacías para evitar errores
                ViewData["Propietarios"] = new List<Propietario>();
                ViewData["TiposInmuebles"] = new List<TipoInmueble>();
                Console.WriteLine($"Error en PopulateViewData: {ex.Message}");
            }
        }

        // GET: Inmuebles/Create
        public IActionResult Create()
        {
            PopulateViewData();
            return View(new Inmueble());
        }

        // POST: Inmuebles/Create - SIMPLIFICADO SIN IMÁGENES POR AHORA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Inmueble inmueble)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validar formato de coordenadas
                    if (!string.IsNullOrEmpty(inmueble.Coordenadas) && !IsValidCoordinates(inmueble.Coordenadas))
                    {
                        ModelState.AddModelError("Coordenadas", "Formato de coordenadas inválido (ej. -34.6037,-58.3816)");
                        PopulateViewData();
                        return View(inmueble);
                    }

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Verificar dirección única
                        string queryCheckDireccion = "SELECT COUNT(*) FROM inmueble WHERE direccion = @direccion";
                        using (var commandCheck = new MySqlCommand(queryCheckDireccion, connection))
                        {
                            commandCheck.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                            if (Convert.ToInt32(commandCheck.ExecuteScalar()) > 0)
                            {
                                ModelState.AddModelError("Direccion", "Ya existe un inmueble con esta dirección");
                                PopulateViewData();
                                return View(inmueble);
                            }
                        }

                        // Crear inmueble
                        string query = @"INSERT INTO inmueble 
                                        (id_propietario, id_tipo_inmueble, direccion, uso, ambientes, 
                                         precio, coordenadas, estado, fecha_alta) 
                                        VALUES (@id_propietario, @id_tipo_inmueble, @direccion, @uso, @ambientes, 
                                                @precio, @coordenadas, @estado, @fecha_alta)";

                        using (var command = new MySqlCommand(query, connection))
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

                            command.ExecuteNonQuery();
                        }
                    }

                    TempData["SuccessMessage"] = "Inmueble creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al crear el inmueble: {ex.Message}");
                }
            }

            PopulateViewData();
            return View(inmueble);
        }

        // Método auxiliar para validar coordenadas
        private bool IsValidCoordinates(string coordinates)
        {
            if (string.IsNullOrEmpty(coordinates)) return true;
            var pattern = @"^-?\d{1,2}(\.\d{1,6})?,-?\d{1,3}(\.\d{1,6})?$";
            return System.Text.RegularExpressions.Regex.IsMatch(coordinates, pattern);
        }
        // GET: Inmuebles/Edit/5
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Inmueble? inmueble = null;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM inmueble WHERE id_inmueble = @id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                inmueble = new Inmueble
                                {
                                    IdInmueble = reader.GetInt32("id_inmueble"),
                                    IdPropietario = reader.GetInt32("id_propietario"),
                                    IdTipoInmueble = reader.GetInt32("id_tipo_inmueble"),
                                    Direccion = reader.GetString("direccion"),
                                    Uso = reader.GetString("uso"),
                                    Ambientes = reader.GetInt32("ambientes"),
                                    Precio = reader.GetDecimal("precio"),
                                    Coordenadas = reader.IsDBNull(reader.GetOrdinal("coordenadas")) ? null : reader.GetString("coordenadas"),
                                    Estado = reader.GetString("estado"),
                                    FechaAlta = reader.GetDateTime("fecha_alta")
                                };
                            }
                        }
                    }
                }

                if (inmueble == null)
                {
                    return NotFound();
                }

                PopulateViewData();
                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inmuebles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Inmueble inmueble)
        {
            if (id != inmueble.IdInmueble)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validar formato de coordenadas
                    if (!string.IsNullOrEmpty(inmueble.Coordenadas) && !IsValidCoordinates(inmueble.Coordenadas))
                    {
                        ModelState.AddModelError("Coordenadas", "Formato de coordenadas inválido (ej. -34.6037,-58.3816)");
                        PopulateViewData();
                        return View(inmueble);
                    }

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Verificar dirección única (excluyendo el inmueble actual)
                        string queryCheckDireccion = "SELECT COUNT(*) FROM inmueble WHERE direccion = @direccion AND id_inmueble != @id_inmueble";
                        using (var commandCheck = new MySqlCommand(queryCheckDireccion, connection))
                        {
                            commandCheck.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                            commandCheck.Parameters.AddWithValue("@id_inmueble", inmueble.IdInmueble);
                            if (Convert.ToInt32(commandCheck.ExecuteScalar()) > 0)
                            {
                                ModelState.AddModelError("Direccion", "Ya existe otro inmueble con esta dirección");
                                PopulateViewData();
                                return View(inmueble);
                            }
                        }

                        // Actualizar inmueble
                        string query = @"UPDATE inmueble 
                                        SET id_propietario = @id_propietario, id_tipo_inmueble = @id_tipo_inmueble, 
                                            direccion = @direccion, uso = @uso, ambientes = @ambientes, 
                                            precio = @precio, coordenadas = @coordenadas, estado = @estado 
                                        WHERE id_inmueble = @id";

                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id_propietario", inmueble.IdPropietario);
                            command.Parameters.AddWithValue("@id_tipo_inmueble", inmueble.IdTipoInmueble);
                            command.Parameters.AddWithValue("@direccion", inmueble.Direccion);
                            command.Parameters.AddWithValue("@uso", inmueble.Uso);
                            command.Parameters.AddWithValue("@ambientes", inmueble.Ambientes);
                            command.Parameters.AddWithValue("@precio", inmueble.Precio);
                            command.Parameters.AddWithValue("@coordenadas", inmueble.Coordenadas ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@estado", inmueble.Estado);
                            command.Parameters.AddWithValue("@id", id);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }
                    }

                    TempData["SuccessMessage"] = "Inmueble actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar el inmueble: {ex.Message}");
                }
            }

            PopulateViewData();
            return View(inmueble);
        }

        // GET: Inmuebles/Delete/5
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

                // Cargar datos del inmueble con información relacionada
                string query = @"
                    SELECT i.id_inmueble, i.id_propietario, i.id_tipo_inmueble, i.direccion, i.uso, 
                           i.ambientes, i.precio, i.coordenadas, i.estado, i.fecha_alta,
                           p.id_usuario, u.nombre AS propietario_nombre, u.apellido AS propietario_apellido,
                           u.telefono AS propietario_telefono,
                           t.nombre AS tipo_inmueble_nombre,
                           (SELECT COUNT(*) FROM contrato WHERE id_inmueble = i.id_inmueble AND estado = 'vigente') as contratos_vigentes
                    FROM inmueble i
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    WHERE i.id_inmueble = @id";

                Inmueble? inmueble = null;
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        inmueble = new Inmueble
                        {
                            IdInmueble = reader.GetInt32("id_inmueble"),
                            IdPropietario = reader.GetInt32("id_propietario"),
                            IdTipoInmueble = reader.GetInt32("id_tipo_inmueble"),
                            Direccion = reader.GetString("direccion"),
                            Uso = reader.GetString("uso"),
                            Ambientes = reader.GetInt32("ambientes"),
                            Precio = reader.GetDecimal("precio"),
                            Coordenadas = reader.IsDBNull(reader.GetOrdinal("coordenadas")) ? null : reader.GetString("coordenadas"),
                            Estado = reader.GetString("estado"),
                            FechaAlta = reader.GetDateTime("fecha_alta"),
                            Propietario = new Propietario
                            {
                                IdUsuario = reader.GetInt32("id_usuario"),
                                Usuario = new Usuario
                                {
                                    Nombre = reader.GetString("propietario_nombre"),
                                    Apellido = reader.GetString("propietario_apellido"),
                                    Telefono = reader.IsDBNull(reader.GetOrdinal("propietario_telefono")) ? null : reader.GetString("propietario_telefono")
                                }
                            },
                            TipoInmueble = new TipoInmueble
                            {
                                Nombre = reader.GetString("tipo_inmueble_nombre")
                            }
                        };

                        // Pasar información adicional a la vista
                        ViewBag.ContratosVigentes = reader.GetInt32("contratos_vigentes");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Inmueble no encontrado";
                        return RedirectToAction(nameof(Index));
                    }
                }

                return View(inmueble);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el inmueble: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inmuebles/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // Verificar contratos vigentes
                string contractQuery = "SELECT COUNT(*) FROM contrato WHERE id_inmueble = @id AND estado = 'vigente'";
                using (var contractCommand = new MySqlCommand(contractQuery, connection))
                {
                    contractCommand.Parameters.AddWithValue("@id", id);
                    var contratosVigentes = Convert.ToInt32(contractCommand.ExecuteScalar());

                    if (contratosVigentes > 0)
                    {
                        TempData["ErrorMessage"] = "No se puede eliminar el inmueble porque tiene contratos vigentes";
                        return RedirectToAction(nameof(Delete), new { id });
                    }
                }

                // Cambiar estado a 'inactivo' (soft delete)
                string deleteQuery = "UPDATE inmueble SET estado = 'inactivo' WHERE id_inmueble = @id";
                using (var deleteCommand = new MySqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@id", id);
                    int rowsAffected = deleteCommand.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        TempData["ErrorMessage"] = "No se pudo eliminar el inmueble";
                        return RedirectToAction(nameof(Delete), new { id });
                    }
                }

                TempData["SuccessMessage"] = "Inmueble eliminado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el inmueble: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // APIs para búsqueda y autocompletado
        [HttpGet]
        public async Task<IActionResult> BuscarInmuebles(string termino, int limite = 10)
        {
            try
            {
                var resultados = await _searchService.BuscarInmueblesAsync(termino, limite);
                return Json(resultados);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPropietariosParaAutocompletar(string termino, int limite = 10)
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
    }
}
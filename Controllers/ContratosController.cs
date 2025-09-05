using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using System.Collections.Generic;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class ContratosController : Controller
    {
        private readonly string _connectionString;
        private const int ITEMS_POR_PAGINA = 10;

        public ContratosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        // GET: Contratos
        public IActionResult Index(int pagina = 1, string buscar = "", string estado = "")
        {
            var listaContratos = new List<Contrato>();
            int totalRegistros = 0;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // Construir WHERE dinámico
                var whereConditions = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(buscar))
                {
                    whereConditions.Add("(i.direccion LIKE @buscar OR u1.nombre LIKE @buscar OR u1.apellido LIKE @buscar OR u1.dni LIKE @buscar)");
                    parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    whereConditions.Add("c.estado = @estado");
                    parameters.Add(new MySqlParameter("@estado", estado));
                }

                string whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

                // Contar total de registros
                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u1 ON inq.id_usuario = u1.id_usuario
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
                    SELECT c.id_contrato, c.id_inmueble, c.id_inquilino, c.fecha_inicio, c.fecha_fin, 
                           c.fecha_fin_anticipada, c.monto_mensual, c.estado, c.multa_aplicada, 
                           c.id_usuario_creador, c.id_usuario_terminador, c.fecha_creacion, c.fecha_modificacion,
                           i.direccion AS inmueble_direccion, 
                           u1.dni AS inquilino_dni, u1.nombre AS inquilino_nombre, u1.apellido AS inquilino_apellido,
                           u2.nombre AS creador_nombre, u2.apellido AS creador_apellido,
                           u3.nombre AS terminador_nombre, u3.apellido AS terminador_apellido
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u1 ON inq.id_usuario = u1.id_usuario
                    INNER JOIN usuario u2 ON c.id_usuario_creador = u2.id_usuario
                    LEFT JOIN usuario u3 ON c.id_usuario_terminador = u3.id_usuario
                    {whereClause}
                    ORDER BY c.id_contrato DESC
                    LIMIT @offset, @limit";

                using (var command = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                    }
                    command.Parameters.AddWithValue("@offset", (pagina - 1) * ITEMS_POR_PAGINA);
                    command.Parameters.AddWithValue("@limit", ITEMS_POR_PAGINA);

                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        listaContratos.Add(new Contrato
                        {
                            IdContrato = reader.GetInt32("id_contrato"),
                            IdInmueble = reader.GetInt32("id_inmueble"),
                            IdInquilino = reader.GetInt32("id_inquilino"),
                            FechaInicio = reader.GetDateTime("fecha_inicio"),
                            FechaFin = reader.GetDateTime("fecha_fin"),
                            FechaFinAnticipada = reader.IsDBNull(reader.GetOrdinal("fecha_fin_anticipada")) ?
                                                null : reader.GetDateTime("fecha_fin_anticipada"),
                            MontoMensual = reader.GetDecimal("monto_mensual"),
                            Estado = reader.GetString("estado"),
                            MultaAplicada = reader.GetDecimal("multa_aplicada"),
                            IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                            IdUsuarioTerminador = reader.IsDBNull(reader.GetOrdinal("id_usuario_terminador")) ?
                                                 null : reader.GetInt32("id_usuario_terminador"),
                            FechaCreacion = reader.GetDateTime("fecha_creacion"),
                            FechaModificacion = reader.GetDateTime("fecha_modificacion"),
                            Inmueble = new Inmueble
                            {
                                Direccion = reader.GetString("inmueble_direccion")
                            },
                            Inquilino = new Inquilino
                            {
                                Usuario = new Usuario
                                {
                                    Dni = reader.GetString("inquilino_dni"),
                                    Nombre = reader.GetString("inquilino_nombre"),
                                    Apellido = reader.GetString("inquilino_apellido")
                                }
                            },
                            UsuarioCreador = new Usuario
                            {
                                Nombre = reader.GetString("creador_nombre"),
                                Apellido = reader.GetString("creador_apellido")
                            },
                            UsuarioTerminador = new Usuario
                            {
                                Nombre = reader.IsDBNull(reader.GetOrdinal("terminador_nombre")) ? string.Empty : reader.GetString("terminador_nombre") ?? string.Empty,
                                Apellido = reader.IsDBNull(reader.GetOrdinal("terminador_apellido")) ? string.Empty : reader.GetString("terminador_apellido") ?? string.Empty
                            }
                        });
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
                TempData["ErrorMessage"] = $"Error al cargar los contratos: {ex.Message}";
            }

            return View(listaContratos);
        }

        // GET: Contratos/Create
        public IActionResult Create()
        {
            var contrato = new Contrato();
            PopulateViewData();
            return View(contrato);
        }

        // POST: Contratos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Contrato contrato)
        {
            // Validaciones adicionales antes de ModelState
            if (contrato.FechaFin <= contrato.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");
            }
            
            if (contrato.MontoMensual <= 0)
            {
                ModelState.AddModelError("MontoMensual", "El monto debe ser mayor a 0");
            }
            
            // Verificar existencia de registros relacionados
            if (contrato.IdInmueble > 0 && !ExisteInmueble(contrato.IdInmueble))
            {
                ModelState.AddModelError("IdInmueble", "El inmueble seleccionado no existe o no está disponible");
            }
            
            if (contrato.IdInquilino > 0 && !ExisteInquilino(contrato.IdInquilino))
            {
                ModelState.AddModelError("IdInquilino", "El inquilino seleccionado no existe o no está activo");
            }
            
            if (contrato.IdUsuarioCreador > 0 && !ExisteUsuario(contrato.IdUsuarioCreador))
            {
                ModelState.AddModelError("IdUsuarioCreador", "El usuario seleccionado no existe o no está activo");
            }

            if (!ModelState.IsValid)
            {
                PopulateViewData();
                return View(contrato);
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Validar que el inmueble no esté ocupado en el rango de fechas
                string queryCheckInmueble = @"
                    SELECT COUNT(*) 
                    FROM contrato 
                    WHERE id_inmueble = @id_inmueble 
                    AND estado = 'vigente'
                    AND ((@fecha_inicio BETWEEN fecha_inicio AND fecha_fin)
                         OR (@fecha_fin BETWEEN fecha_inicio AND fecha_fin)
                         OR (fecha_inicio BETWEEN @fecha_inicio AND @fecha_fin))";
                using (var commandCheck = new MySqlCommand(queryCheckInmueble, connection, transaction))
                {
                    commandCheck.Parameters.AddWithValue("@id_inmueble", contrato.IdInmueble);
                    commandCheck.Parameters.AddWithValue("@fecha_inicio", contrato.FechaInicio);
                    commandCheck.Parameters.AddWithValue("@fecha_fin", contrato.FechaFin);
                    if (Convert.ToInt32(commandCheck.ExecuteScalar()) > 0)
                    {
                        ModelState.AddModelError("IdInmueble", "El inmueble ya está ocupado en ese rango de fechas");
                        PopulateViewData();
                        return View(contrato);
                    }
                }

                // Crear contrato
                string queryContrato = @"
                    INSERT INTO contrato 
                    (id_inmueble, id_inquilino, fecha_inicio, fecha_fin, monto_mensual, 
                     estado, multa_aplicada, id_usuario_creador, fecha_creacion, fecha_modificacion) 
                    VALUES (@id_inmueble, @id_inquilino, @fecha_inicio, @fecha_fin, @monto_mensual, 
                            @estado, @multa_aplicada, @id_usuario_creador, @fecha_creacion, @fecha_modificacion)";

                using (var commandContrato = new MySqlCommand(queryContrato, connection, transaction))
                {
                    commandContrato.Parameters.AddWithValue("@id_inmueble", contrato.IdInmueble);
                    commandContrato.Parameters.AddWithValue("@id_inquilino", contrato.IdInquilino);
                    commandContrato.Parameters.AddWithValue("@fecha_inicio", contrato.FechaInicio);
                    commandContrato.Parameters.AddWithValue("@fecha_fin", contrato.FechaFin);
                    commandContrato.Parameters.AddWithValue("@monto_mensual", contrato.MontoMensual);
                    commandContrato.Parameters.AddWithValue("@estado", "vigente");
                    commandContrato.Parameters.AddWithValue("@multa_aplicada", 0);
                    commandContrato.Parameters.AddWithValue("@id_usuario_creador", contrato.IdUsuarioCreador);
                    commandContrato.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);
                    commandContrato.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);

                    commandContrato.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Contrato creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError("", $"Error al crear contrato: {ex.Message}");
                PopulateViewData();
                return View(contrato);
            }
        }

        // GET: Contratos/Edit
        public IActionResult Edit(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT c.id_contrato, c.id_inmueble, c.id_inquilino, c.fecha_inicio, c.fecha_fin, 
                           c.fecha_fin_anticipada, c.monto_mensual, c.estado, c.multa_aplicada, 
                           c.id_usuario_creador, c.id_usuario_terminador, c.fecha_creacion, c.fecha_modificacion,
                           i.direccion AS inmueble_direccion,
                           u1.dni AS inquilino_dni, u1.nombre AS inquilino_nombre, u1.apellido AS inquilino_apellido
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u1 ON inq.id_usuario = u1.id_usuario
                    WHERE c.id_contrato = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var contrato = new Contrato
                    {
                        IdContrato = reader.GetInt32("id_contrato"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        IdInquilino = reader.GetInt32("id_inquilino"),
                        FechaInicio = reader.GetDateTime("fecha_inicio"),
                        FechaFin = reader.GetDateTime("fecha_fin"),
                        FechaFinAnticipada = reader.IsDBNull(reader.GetOrdinal("fecha_fin_anticipada")) ?
                                            null : reader.GetDateTime("fecha_fin_anticipada"),
                        MontoMensual = reader.GetDecimal("monto_mensual"),
                        Estado = reader.GetString("estado"),
                        MultaAplicada = reader.GetDecimal("multa_aplicada"),
                        IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                        IdUsuarioTerminador = reader.IsDBNull(reader.GetOrdinal("id_usuario_terminador")) ?
                                             null : reader.GetInt32("id_usuario_terminador"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion"),
                        FechaModificacion = reader.GetDateTime("fecha_modificacion"),
                        Inmueble = new Inmueble { Direccion = reader.GetString("inmueble_direccion") },
                        Inquilino = new Inquilino
                        {
                            Usuario = new Usuario
                            {
                                Dni = reader.GetString("inquilino_dni"),
                                Nombre = reader.GetString("inquilino_nombre"),
                                Apellido = reader.GetString("inquilino_apellido")
                            }
                        }
                    };
                    PopulateViewData();
                    return View(contrato);
                }
                TempData["ErrorMessage"] = "Contrato no encontrado";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Contratos/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Contrato contrato)
        {
            if (id != contrato.IdContrato)
            {
                TempData["ErrorMessage"] = "ID de contrato no coincide";
                return RedirectToAction(nameof(Index));
            }

            // Validaciones adicionales
            if (contrato.FechaFin <= contrato.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");
            }
            
            if (contrato.MontoMensual <= 0)
            {
                ModelState.AddModelError("MontoMensual", "El monto debe ser mayor a 0");
            }

            if (!ModelState.IsValid)
            {
                PopulateViewData();
                return View(contrato);
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Actualizar contrato
                string queryContrato = @"
                    UPDATE contrato 
                    SET id_inmueble = @id_inmueble, id_inquilino = @id_inquilino, 
                        fecha_inicio = @fecha_inicio, fecha_fin = @fecha_fin, 
                        fecha_fin_anticipada = @fecha_fin_anticipada, monto_mensual = @monto_mensual, 
                        estado = @estado, multa_aplicada = @multa_aplicada, 
                        id_usuario_terminador = @id_usuario_terminador, fecha_modificacion = @fecha_modificacion
                    WHERE id_contrato = @id_contrato";

                using (var commandContrato = new MySqlCommand(queryContrato, connection, transaction))
                {
                    commandContrato.Parameters.AddWithValue("@id_inmueble", contrato.IdInmueble);
                    commandContrato.Parameters.AddWithValue("@id_inquilino", contrato.IdInquilino);
                    commandContrato.Parameters.AddWithValue("@fecha_inicio", contrato.FechaInicio);
                    commandContrato.Parameters.AddWithValue("@fecha_fin", contrato.FechaFin);
                    commandContrato.Parameters.AddWithValue("@fecha_fin_anticipada", contrato.FechaFinAnticipada ?? (object)DBNull.Value);
                    commandContrato.Parameters.AddWithValue("@monto_mensual", contrato.MontoMensual);
                    commandContrato.Parameters.AddWithValue("@estado", contrato.Estado);
                    commandContrato.Parameters.AddWithValue("@multa_aplicada", contrato.MultaAplicada);
                    commandContrato.Parameters.AddWithValue("@id_usuario_terminador", contrato.IdUsuarioTerminador ?? (object)DBNull.Value);
                    commandContrato.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);
                    commandContrato.Parameters.AddWithValue("@id_contrato", contrato.IdContrato);

                    commandContrato.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Contrato actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError("", $"Error al actualizar contrato: {ex.Message}");
                PopulateViewData();
                return View(contrato);
            }
        }

        // GET: Contratos/Delete
        public IActionResult Delete(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT c.id_contrato, c.id_inmueble, c.id_inquilino, c.fecha_inicio, c.fecha_fin, 
                           c.monto_mensual, c.estado, c.multa_aplicada,
                           i.direccion AS inmueble_direccion,
                           u1.dni AS inquilino_dni, u1.nombre AS inquilino_nombre, u1.apellido AS inquilino_apellido
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u1 ON inq.id_usuario = u1.id_usuario
                    WHERE c.id_contrato = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var contrato = new Contrato
                    {
                        IdContrato = reader.GetInt32("id_contrato"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        IdInquilino = reader.GetInt32("id_inquilino"),
                        FechaInicio = reader.GetDateTime("fecha_inicio"),
                        FechaFin = reader.GetDateTime("fecha_fin"),
                        MontoMensual = reader.GetDecimal("monto_mensual"),
                        Estado = reader.GetString("estado"),
                        MultaAplicada = reader.GetDecimal("multa_aplicada"),
                        Inmueble = new Inmueble { Direccion = reader.GetString("inmueble_direccion") },
                        Inquilino = new Inquilino
                        {
                            Usuario = new Usuario
                            {
                                Dni = reader.GetString("inquilino_dni"),
                                Nombre = reader.GetString("inquilino_nombre"),
                                Apellido = reader.GetString("inquilino_apellido")
                            }
                        }
                    };
                    return View(contrato);
                }
                TempData["ErrorMessage"] = "Contrato no encontrado";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar contrato: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Contratos/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Actualizar estado del contrato a "finalizado"
                string queryContrato = @"
                    UPDATE contrato 
                    SET estado = @estado, fecha_modificacion = @fecha_modificacion
                    WHERE id_contrato = @id_contrato";
                using (var commandContrato = new MySqlCommand(queryContrato, connection, transaction))
                {
                    commandContrato.Parameters.AddWithValue("@estado", "finalizado");
                    commandContrato.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);
                    commandContrato.Parameters.AddWithValue("@id_contrato", id);
                    commandContrato.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Contrato finalizado exitosamente";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = $"Error al finalizar contrato: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // MÉTODOS AUXILIARES CORREGIDOS

        private void PopulateViewData()
        {
            ViewData["Inmuebles"] = GetInmuebles();
            ViewData["Inquilinos"] = GetInquilinos(); 
            ViewData["Usuarios"] = GetUsuarios();
        }

        private List<Inmueble> GetInmuebles()
        {
            var inmuebles = new List<Inmueble>();
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = "SELECT id_inmueble, direccion FROM inmueble WHERE estado = 'disponible'";
            
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                inmuebles.Add(new Inmueble
                {
                    IdInmueble = reader.GetInt32("id_inmueble"),
                    Direccion = reader.GetString("direccion")
                });
            }
            return inmuebles;
        }

        // CORREGIDO: Inquilinos con relación a usuario
        private List<Inquilino> GetInquilinos()
        {
            var inquilinos = new List<Inquilino>();
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            
            string query = @"SELECT inq.id_inquilino, u.nombre, u.apellido, u.dni 
                             FROM inquilino inq
                             INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                             WHERE inq.estado = 1 
                             ORDER BY u.apellido, u.nombre";
            
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                inquilinos.Add(new Inquilino
                {
                    IdInquilino = reader.GetInt32("id_inquilino"),
                    Usuario = new Usuario
                    {
                        Nombre = reader.GetString("nombre"),
                        Apellido = reader.GetString("apellido"),
                        Dni = reader.GetString("dni")
                    }
                });
            }
            return inquilinos;
        }

        private List<Usuario> GetUsuarios()
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = "SELECT id_usuario, nombre, apellido FROM usuario WHERE estado = 'activo'";
            
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                usuarios.Add(new Usuario
                {
                    IdUsuario = reader.GetInt32("id_usuario"),
                    Nombre = reader.GetString("nombre"),
                    Apellido = reader.GetString("apellido")
                });
            }
            return usuarios;
        }

        // VALIDACIONES DE EXISTENCIA CORREGIDAS

        private bool ExisteInmueble(int idInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = "SELECT COUNT(*) FROM inmueble WHERE id_inmueble = @id AND estado = 'disponible'";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", idInmueble);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ExisteInquilino(int idInquilino)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = @"SELECT COUNT(*) FROM inquilino inq 
                             INNER JOIN usuario u ON inq.id_usuario = u.id_usuario 
                             WHERE inq.id_inquilino = @id AND inq.estado = 1";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", idInquilino);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ExisteUsuario(int idUsuario)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = "SELECT COUNT(*) FROM usuario WHERE id_usuario = @id AND estado = 'activo'";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", idUsuario);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }
}
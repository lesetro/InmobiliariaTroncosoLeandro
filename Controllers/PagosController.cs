using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using System.Collections.Generic;

namespace Inmobiliaria_troncoso_leandro.Controllers
{
    public class PagosController : Controller
    {
        private readonly string _connectionString;

        public PagosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration), "La cadena de conexión está nula");
        }

        public IActionResult Index()
        {
            var listaPagos = new List<Pago>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
            SELECT p.id_pago, p.id_contrato, p.numero_pago, p.fecha_pago, p.concepto, 
                   p.monto, p.estado, p.id_usuario_creador, p.id_usuario_anulador, 
                   p.fecha_creacion, p.fecha_anulacion,
                   c.id_inmueble, i.direccion AS inmueble_direccion,
                   u1.nombre AS creador_nombre, u1.apellido AS creador_apellido,
                   u2.nombre AS anulador_nombre, u2.apellido AS anulador_apellido
            FROM pago p
            INNER JOIN contrato c ON p.id_contrato = c.id_contrato
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN usuario u1 ON p.id_usuario_creador = u1.id_usuario
            LEFT JOIN usuario u2 ON p.id_usuario_anulador = u2.id_usuario
            WHERE p.estado = 'activo'
            ORDER BY p.id_pago";

                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    listaPagos.Add(new Pago
                    {
                        IdPago = reader.GetInt32("id_pago"),
                        IdContrato = reader.GetInt32("id_contrato"),
                        NumeroPago = reader.GetInt32("numero_pago"),
                        FechaPago = reader.GetDateTime("fecha_pago"),
                        Concepto = reader.GetString("concepto"),
                        Monto = reader.GetDecimal("monto"),
                        Estado = reader.GetString("estado"),
                        IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                        IdUsuarioAnulador = reader.GetInt32("id_usuario_anulador"), // ← SIN NULL CHECK
                        FechaCreacion = reader.GetDateTime("fecha_creacion"),
                        FechaAnulacion = reader.GetDateTime("fecha_anulacion"), // ← SIN NULL CHECK
                        Contrato = new Contrato
                        {
                            IdInmueble = reader.GetInt32("id_inmueble"),
                            Inmueble = new Inmueble
                            {
                                Direccion = reader.GetString("inmueble_direccion")
                            }
                        },
                        UsuarioCreador = new Usuario
                        {
                            Nombre = reader.GetString("creador_nombre"),
                            Apellido = reader.GetString("creador_apellido")
                        },
                        UsuarioAnulador = new Usuario // ← SIN NULL CHECK
                        {
                            Nombre = reader.GetString("anulador_nombre"),
                            Apellido = reader.GetString("anulador_apellido")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar pagos: {ex.Message}";
            }

            return View(listaPagos);
        }
        // GET: Pagos/Create
        public IActionResult Create()
        {
            var pago = new Pago();
            PopulateViewData();
            return View(pago);
        }

        // POST: Pagos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pago pago)
        {
            if (!ModelState.IsValid)
            {
                PopulateViewData();
                return View(pago);
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Verificar que el contrato esté vigente
                string queryCheckContrato = "SELECT estado FROM contrato WHERE id_contrato = @id_contrato";
                using (var commandCheck = new MySqlCommand(queryCheckContrato, connection, transaction))
                {
                    commandCheck.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    var estadoContrato = commandCheck.ExecuteScalar()?.ToString();
                    if (estadoContrato != "vigente")
                    {
                        ModelState.AddModelError("IdContrato", "El contrato no está vigente");
                        PopulateViewData();
                        return View(pago);
                    }
                }

                // Verificar número de pago único por contrato
                string queryCheckNumeroPago = "SELECT COUNT(*) FROM pago WHERE id_contrato = @id_contrato AND numero_pago = @numero_pago";
                using (var commandCheck = new MySqlCommand(queryCheckNumeroPago, connection, transaction))
                {
                    commandCheck.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    commandCheck.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                    if (Convert.ToInt32(commandCheck.ExecuteScalar()) > 0)
                    {
                        ModelState.AddModelError("NumeroPago", "Ya existe un pago con este número para el contrato");
                        PopulateViewData();
                        return View(pago);
                    }
                }

                // Crear pago
                string queryPago = @"
                    INSERT INTO pago 
                    (id_contrato, numero_pago, fecha_pago, concepto, monto, estado, id_usuario_creador, fecha_creacion) 
                    VALUES (@id_contrato, @numero_pago, @fecha_pago, @concepto, @monto, @estado, @id_usuario_creador, @fecha_creacion)";

                using (var commandPago = new MySqlCommand(queryPago, connection, transaction))
                {
                    commandPago.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    commandPago.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                    commandPago.Parameters.AddWithValue("@fecha_pago", pago.FechaPago);
                    commandPago.Parameters.AddWithValue("@concepto", pago.Concepto);
                    commandPago.Parameters.AddWithValue("@monto", pago.Monto);
                    commandPago.Parameters.AddWithValue("@estado", "activo");
                    commandPago.Parameters.AddWithValue("@id_usuario_creador", pago.IdUsuarioCreador);
                    commandPago.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);

                    commandPago.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Pago creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError("", $"Error al crear pago: {ex.Message}");
                PopulateViewData();
                return View(pago);
            }
        }

        private void PopulateViewData()
        {
            ViewData["Contratos"] = GetContratos();
            ViewData["Usuarios"] = GetUsuarios();
        }

        private List<Contrato> GetContratos()
        {
            var contratos = new List<Contrato>();
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = @"
                SELECT c.id_contrato, i.direccion AS inmueble_direccion
                FROM contrato c
                INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                WHERE c.estado = 'vigente'";
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contratos.Add(new Contrato
                {
                    IdContrato = reader.GetInt32("id_contrato"),
                    Inmueble = new Inmueble { Direccion = reader.GetString("inmueble_direccion") }
                });
            }
            return contratos;
        }

        private List<Usuario> GetUsuarios()
        {
            var usuarios = new List<Usuario>();
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = "SELECT id_usuario, nombre, apellido FROM usuario WHERE estado = 1";
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

        // GET: Pagos/Edit
        public IActionResult Edit(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT p.id_pago, p.id_contrato, p.numero_pago, p.fecha_pago, p.concepto, 
                           p.monto, p.estado, p.id_usuario_creador, p.id_usuario_anulador, 
                           p.fecha_creacion, p.fecha_anulacion,
                           c.id_inmueble, i.direccion AS inmueble_direccion
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    WHERE p.id_pago = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var pago = new Pago
                    {
                        IdPago = reader.GetInt32("id_pago"),
                        IdContrato = reader.GetInt32("id_contrato"),
                        NumeroPago = reader.GetInt32("numero_pago"),
                        FechaPago = reader.GetDateTime("fecha_pago"),
                        Concepto = reader.GetString("concepto"),
                        Monto = reader.GetDecimal("monto"),
                        Estado = reader.GetString("estado"),
                        IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                        IdUsuarioAnulador = reader.IsDBNull(reader.GetOrdinal("id_usuario_anulador")) ? null : reader.GetInt32("id_usuario_anulador"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion"),
                        FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("fecha_anulacion")) ? null : reader.GetDateTime("fecha_anulacion"),
                        Contrato = new Contrato
                        {
                            IdInmueble = reader.GetInt32("id_inmueble"),
                            Inmueble = new Inmueble { Direccion = reader.GetString("inmueble_direccion") }
                        }
                    };
                    PopulateViewData();
                    return View(pago);
                }
                TempData["ErrorMessage"] = "Pago no encontrado";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar pago: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Pagos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Pago pago)
        {
            if (id != pago.IdPago)
            {
                TempData["ErrorMessage"] = "ID de pago no coincide";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                PopulateViewData();
                return View(pago);
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Verificar que el contrato esté vigente
                string queryCheckContrato = "SELECT estado FROM contrato WHERE id_contrato = @id_contrato";
                using (var commandCheck = new MySqlCommand(queryCheckContrato, connection, transaction))
                {
                    commandCheck.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    var estadoContrato = commandCheck.ExecuteScalar()?.ToString();
                    if (estadoContrato != "vigente")
                    {
                        ModelState.AddModelError("IdContrato", "El contrato no está vigente");
                        PopulateViewData();
                        return View(pago);
                    }
                }

                // Verificar número de pago único por contrato (excluyendo el pago actual)
                string queryCheckNumeroPago = "SELECT COUNT(*) FROM pago WHERE id_contrato = @id_contrato AND numero_pago = @numero_pago AND id_pago != @id_pago";
                using (var commandCheck = new MySqlCommand(queryCheckNumeroPago, connection, transaction))
                {
                    commandCheck.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    commandCheck.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                    commandCheck.Parameters.AddWithValue("@id_pago", pago.IdPago);
                    if (Convert.ToInt32(commandCheck.ExecuteScalar()) > 0)
                    {
                        ModelState.AddModelError("NumeroPago", "Ya existe otro pago con este número para el contrato");
                        PopulateViewData();
                        return View(pago);
                    }
                }

                // Actualizar pago
                string queryPago = @"
                    UPDATE pago 
                    SET id_contrato = @id_contrato, numero_pago = @numero_pago, fecha_pago = @fecha_pago, 
                        concepto = @concepto, monto = @monto, estado = @estado, 
                        id_usuario_anulador = @id_usuario_anulador, fecha_anulacion = @fecha_anulacion
                    WHERE id_pago = @id_pago";

                using (var commandPago = new MySqlCommand(queryPago, connection, transaction))
                {
                    commandPago.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    commandPago.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                    commandPago.Parameters.AddWithValue("@fecha_pago", pago.FechaPago);
                    commandPago.Parameters.AddWithValue("@concepto", pago.Concepto);
                    commandPago.Parameters.AddWithValue("@monto", pago.Monto);
                    commandPago.Parameters.AddWithValue("@estado", pago.Estado);
                    commandPago.Parameters.AddWithValue("@id_usuario_anulador", pago.IdUsuarioAnulador ?? (object)DBNull.Value);
                    commandPago.Parameters.AddWithValue("@fecha_anulacion", pago.FechaAnulacion ?? (object)DBNull.Value);
                    commandPago.Parameters.AddWithValue("@id_pago", pago.IdPago);

                    commandPago.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Pago actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError("", $"Error al actualizar pago: {ex.Message}");
                PopulateViewData();
                return View(pago);
            }
        }

        // GET: Pagos/Delete
        public IActionResult Delete(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                string query = @"
                    SELECT p.id_pago, p.id_contrato, p.numero_pago, p.fecha_pago, p.concepto, 
                           p.monto, p.estado, c.id_inmueble, i.direccion AS inmueble_direccion
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    WHERE p.id_pago = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var pago = new Pago
                    {
                        IdPago = reader.GetInt32("id_pago"),
                        IdContrato = reader.GetInt32("id_contrato"),
                        NumeroPago = reader.GetInt32("numero_pago"),
                        FechaPago = reader.GetDateTime("fecha_pago"),
                        Concepto = reader.GetString("concepto"),
                        Monto = reader.GetDecimal("monto"),
                        Estado = reader.GetString("estado"),
                        Contrato = new Contrato
                        {
                            IdInmueble = reader.GetInt32("id_inmueble"),
                            Inmueble = new Inmueble { Direccion = reader.GetString("inmueble_direccion") }
                        }
                    };
                    return View(pago);
                }
                TempData["ErrorMessage"] = "Pago no encontrado";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar pago: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Pagos/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id, int idUsuarioAnulador)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Verificar que el pago esté activo
                string queryCheckEstado = "SELECT estado FROM pago WHERE id_pago = @id_pago";
                using (var commandCheck = new MySqlCommand(queryCheckEstado, connection, transaction))
                {
                    commandCheck.Parameters.AddWithValue("@id_pago", id);
                    var estadoPago = commandCheck.ExecuteScalar()?.ToString();
                    if (estadoPago != "activo")
                    {
                        TempData["ErrorMessage"] = "El pago ya está anulado";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Actualizar estado del pago a "anulado" y registrar anulador
                string queryPago = @"
                    UPDATE pago 
                    SET estado = @estado, id_usuario_anulador = @id_usuario_anulador, fecha_anulacion = @fecha_anulacion
                    WHERE id_pago = @id_pago";
                using (var commandPago = new MySqlCommand(queryPago, connection, transaction))
                {
                    commandPago.Parameters.AddWithValue("@estado", "anulado");
                    commandPago.Parameters.AddWithValue("@id_usuario_anulador", idUsuarioAnulador);
                    commandPago.Parameters.AddWithValue("@fecha_anulacion", DateTime.Now);
                    commandPago.Parameters.AddWithValue("@id_pago", id);
                    commandPago.ExecuteNonQuery();
                }

                transaction.Commit();
                TempData["SuccessMessage"] = "Pago anulado exitosamente";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = $"Error al anular pago: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
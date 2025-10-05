using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Models.DTOs;
using MySql.Data.MySqlClient;



namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioAlquiler : IRepositorioAlquiler
    {
        private readonly string _connectionString;
        private const decimal MONTO_DIARIO_MORA_DEFAULT = 50m; // $50 por día por defecto

        public RepositorioAlquiler(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // ========================
        // MÉTODOS CRUD BÁSICOS
        // ========================

        public async Task<bool> CrearPagoAlquilerAsync(Pago pago)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Verificar que el contrato esté vigente
                if (!await ContratoVigenteInternoAsync(pago.IdContrato!.Value, connection, transaction))
                {
                    return false;
                }

                // Verificar que no exista otro pago con el mismo número para el contrato
                if (await ExistePagoMesContratoInternoAsync(pago.IdContrato.Value, pago.NumeroPago, connection, transaction))
                {
                    return false;
                }

                // Obtener datos del contrato para llenar campos automáticamente
                var contrato = await ObtenerDatosContratoInternoAsync(pago.IdContrato.Value, connection, transaction);
                if (contrato == null) return false;

                // Asignar valores automáticos
                pago.IdInmueble = contrato.IdInmueble;
                pago.TipoPago = "alquiler";

                // Calcular fecha de vencimiento si no se especificó
                if (!pago.FechaVencimiento.HasValue)
                {
                    pago.FechaVencimiento = await CalcularFechaVencimientoInternoAsync(pago.IdContrato.Value, pago.NumeroPago, connection, transaction);
                }

                // Obtener monto diario de mora
                if (!pago.MontoDiarioMora.HasValue)
                {
                    pago.MontoDiarioMora = await ObtenerMontoDiarioMoraInternoAsync(connection, transaction);
                }

                // Calcular mora automáticamente si la fecha de pago es posterior al vencimiento
                if (pago.FechaVencimiento.HasValue && pago.FechaPago.Date > pago.FechaVencimiento.Value.Date)
                {
                    pago.DiasMora = (pago.FechaPago.Date - pago.FechaVencimiento.Value.Date).Days;
                    pago.RecargoMora = pago.DiasMora.Value * pago.MontoDiarioMora.Value;
                }
                else
                {
                    pago.DiasMora = 0;
                    pago.RecargoMora = 0;
                }

                pago.MontoTotal = pago.MontoBase + pago.RecargoMora;
                

                // Insertar pago
                string queryPago = @"
                    INSERT INTO pago 
                    (id_contrato, id_inmueble, tipo_pago, numero_pago, fecha_pago, fecha_vencimiento,
                     concepto, monto_base, recargo_mora, monto_total, dias_mora, monto_diario_mora,
                     estado, id_usuario_creador, fecha_creacion, observaciones) 
                    VALUES (@id_contrato, @id_inmueble, @tipo_pago, @numero_pago, @fecha_pago, @fecha_vencimiento,
                            @concepto, @monto_base, @recargo_mora, @monto_total, @dias_mora, @monto_diario_mora,
                            @estado, @id_usuario_creador, @fecha_creacion, @observaciones)";

                using (var commandPago = new MySqlCommand(queryPago, connection, transaction))
                {
                    commandPago.Parameters.AddWithValue("@id_contrato", pago.IdContrato);
                    commandPago.Parameters.AddWithValue("@id_inmueble", pago.IdInmueble);
                    commandPago.Parameters.AddWithValue("@tipo_pago", "alquiler");
                    commandPago.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                    commandPago.Parameters.AddWithValue("@fecha_pago", pago.FechaPago);
                    commandPago.Parameters.AddWithValue("@fecha_vencimiento", pago.FechaVencimiento);
                    commandPago.Parameters.AddWithValue("@concepto", pago.Concepto);
                    commandPago.Parameters.AddWithValue("@monto_base", pago.MontoBase);
                    commandPago.Parameters.AddWithValue("@recargo_mora", pago.RecargoMora);
                    commandPago.Parameters.AddWithValue("@monto_total", pago.MontoTotal);
                    commandPago.Parameters.AddWithValue("@dias_mora", pago.DiasMora);
                    commandPago.Parameters.AddWithValue("@monto_diario_mora", pago.MontoDiarioMora);
                    commandPago.Parameters.AddWithValue("@estado", "pagado");
                    commandPago.Parameters.AddWithValue("@id_usuario_creador", pago.IdUsuarioCreador);
                    commandPago.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);
                    commandPago.Parameters.AddWithValue("@observaciones", pago.Observaciones ?? (object)DBNull.Value);

                    await commandPago.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> ActualizarPagoAlquilerAsync(Pago pago)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Recalcular mora antes de actualizar
                if (pago.FechaVencimiento.HasValue && pago.MontoDiarioMora.HasValue)
                {
                    if (pago.FechaPago.Date > pago.FechaVencimiento.Value.Date)
                    {
                        pago.DiasMora = (pago.FechaPago.Date - pago.FechaVencimiento.Value.Date).Days;
                        pago.RecargoMora = pago.DiasMora.Value * pago.MontoDiarioMora.Value;
                    }
                    else
                    {
                        pago.DiasMora = 0;
                        pago.RecargoMora = 0;
                    }
                    pago.MontoTotal = pago.MontoBase + pago.RecargoMora;
                }

                string query = @"
                    UPDATE pago 
                    SET numero_pago = @numero_pago, fecha_pago = @fecha_pago, fecha_vencimiento = @fecha_vencimiento,
                        concepto = @concepto, monto_base = @monto_base, recargo_mora = @recargo_mora, 
                        monto_total = @monto_total, dias_mora = @dias_mora, monto_diario_mora = @monto_diario_mora,
                        observaciones = @observaciones
                    WHERE id_pago = @id_pago AND tipo_pago = 'alquiler'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                command.Parameters.AddWithValue("@fecha_pago", pago.FechaPago);
                command.Parameters.AddWithValue("@fecha_vencimiento", pago.FechaVencimiento);
                command.Parameters.AddWithValue("@concepto", pago.Concepto);
                command.Parameters.AddWithValue("@monto_base", pago.MontoBase);
                command.Parameters.AddWithValue("@recargo_mora", pago.RecargoMora);
                command.Parameters.AddWithValue("@monto_total", pago.MontoTotal);
                command.Parameters.AddWithValue("@dias_mora", pago.DiasMora);
                command.Parameters.AddWithValue("@monto_diario_mora", pago.MontoDiarioMora);
                command.Parameters.AddWithValue("@observaciones", pago.Observaciones ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@id_pago", pago.IdPago);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AnularPagoAlquilerAsync(int idPago, int idUsuarioAnulador)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    UPDATE pago 
                    SET estado = 'anulado', id_usuario_anulador = @id_usuario_anulador, fecha_anulacion = @fecha_anulacion
                    WHERE id_pago = @id_pago AND tipo_pago = 'alquiler'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_usuario_anulador", idUsuarioAnulador);
                command.Parameters.AddWithValue("@fecha_anulacion", DateTime.Now);
                command.Parameters.AddWithValue("@id_pago", idPago);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Pago?> ObtenerPagoAlquilerPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id_pago, id_contrato, id_inmueble, tipo_pago, numero_pago, fecha_pago, fecha_vencimiento,
                           concepto, monto_base, recargo_mora, monto_total, dias_mora, monto_diario_mora,
                           estado, id_usuario_creador, id_usuario_anulador, fecha_creacion, fecha_anulacion, observaciones
                    FROM pago 
                    WHERE id_pago = @id AND tipo_pago = 'alquiler'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearPagoAlquilerBasico(reader);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Pago?> ObtenerPagoAlquilerConDetallesAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT p.id_pago, p.id_contrato, p.id_inmueble, p.tipo_pago, p.numero_pago, p.fecha_pago, p.fecha_vencimiento,
                           p.concepto, p.monto_base, p.recargo_mora, p.monto_total, p.dias_mora, p.monto_diario_mora,
                           p.estado, p.id_usuario_creador, p.id_usuario_anulador, p.fecha_creacion, p.fecha_anulacion, p.observaciones,
                           i.direccion AS inmueble_direccion,
                           uinq.nombre AS inquilino_nombre, uinq.apellido AS inquilino_apellido,
                           u1.nombre AS creador_nombre, u1.apellido AS creador_apellido,
                           u2.nombre AS anulador_nombre, u2.apellido AS anulador_apellido
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario uinq ON inq.id_usuario = uinq.id_usuario
                    INNER JOIN usuario u1 ON p.id_usuario_creador = u1.id_usuario
                    LEFT JOIN usuario u2 ON p.id_usuario_anulador = u2.id_usuario
                    WHERE p.id_pago = @id AND p.tipo_pago = 'alquiler'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearPagoAlquilerCompleto(reader);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(IList<Pago> pagos, int totalRegistros)> ObtenerPagosAlquilerConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Construir WHERE dinámico
                var whereConditions = new List<string> { "p.tipo_pago = 'alquiler'" };
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(buscar))
                {
                    whereConditions.Add(@"(p.concepto LIKE @buscar 
                                          OR i.direccion LIKE @buscar 
                                          OR uinq.nombre LIKE @buscar
                                          OR uinq.apellido LIKE @buscar
                                          OR CONCAT(uinq.nombre, ' ', uinq.apellido) LIKE @buscar)");
                    parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    whereConditions.Add("p.estado = @estado");
                    parameters.Add(new MySqlParameter("@estado", estado));
                }

                string whereClause = "WHERE " + string.Join(" AND ", whereConditions);

                // 1. Contar total
                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario uinq ON inq.id_usuario = uinq.id_usuario
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

                // 2. Obtener datos paginados
                int offset = (pagina - 1) * itemsPorPagina;
                string dataQuery = $@"
                    SELECT p.id_pago, p.id_contrato, p.numero_pago, p.fecha_pago, p.fecha_vencimiento, p.concepto,
                           p.monto_base, p.recargo_mora, p.monto_total, p.dias_mora, p.estado, p.fecha_creacion,
                           i.direccion AS inmueble_direccion,
                           uinq.nombre AS inquilino_nombre, uinq.apellido AS inquilino_apellido
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario uinq ON inq.id_usuario = uinq.id_usuario
                    {whereClause}
                    ORDER BY p.fecha_pago DESC, p.id_pago DESC
                    LIMIT @limit OFFSET @offset";

                var pagos = new List<Pago>();
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
                        pagos.Add(MapearPagoAlquilerListado(reader));
                    }
                }

                return (pagos, totalRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener pagos de alquiler con paginación: {ex.Message}", ex);
            }
        }

        // ========================
        // MÉTODOS ESPECÍFICOS DE MORA
        // ========================

        public async Task<int> CalcularDiasMoraAsync(int idPago)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT fecha_pago, fecha_vencimiento 
                    FROM pago 
                    WHERE id_pago = @id_pago AND tipo_pago = 'alquiler'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_pago", idPago);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var fechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago"));
                    var fechaVencimiento = reader.IsDBNull(reader.GetOrdinal("fecha_vencimiento")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha_vencimiento"));

                    if (fechaVencimiento.HasValue && fechaPago.Date > fechaVencimiento.Value.Date)
                    {
                        return (fechaPago.Date - fechaVencimiento.Value.Date).Days;
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<decimal> CalcularRecargoMoraAsync(int diasMora, decimal montoDiario)
        {
            return await Task.FromResult(diasMora * montoDiario);
        }

        public async Task<bool> ActualizarMoraAsync(int idPago)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Obtener datos del pago
                string querySelect = @"
                    SELECT fecha_pago, fecha_vencimiento, monto_base, monto_diario_mora 
                    FROM pago 
                    WHERE id_pago = @id_pago AND tipo_pago = 'alquiler'";

                decimal montoBase = 0;
                DateTime? fechaVencimiento = null;
                DateTime fechaPago = DateTime.Now;
                decimal montoDiario = 0;

                using (var commandSelect = new MySqlCommand(querySelect, connection))
                {
                    commandSelect.Parameters.AddWithValue("@id_pago", idPago);
                    using var reader = await commandSelect.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        fechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago"));
                        fechaVencimiento = reader.IsDBNull(reader.GetOrdinal("fecha_vencimiento")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_vencimiento"));
                        montoBase = reader.GetDecimal(reader.GetOrdinal("monto_base"));
                        montoDiario = reader.IsDBNull(reader.GetOrdinal("monto_diario_mora")) ? MONTO_DIARIO_MORA_DEFAULT : reader.GetDecimal(reader.GetOrdinal("monto_diario_mora"));
                    }
                    else
                    {
                        return false;
                    }
                }

                // Calcular mora
                int diasMora = 0;
                decimal recargoMora = 0;

                if (fechaVencimiento.HasValue && fechaPago.Date > fechaVencimiento.Value.Date)
                {
                    diasMora = (fechaPago.Date - fechaVencimiento.Value.Date).Days;
                    recargoMora = diasMora * montoDiario;
                }

                decimal montoTotal = montoBase + recargoMora;

                // Actualizar
                string queryUpdate = @"
                    UPDATE pago 
                    SET dias_mora = @dias_mora, recargo_mora = @recargo_mora, monto_total = @monto_total
                    WHERE id_pago = @id_pago";

                using var commandUpdate = new MySqlCommand(queryUpdate, connection);
                commandUpdate.Parameters.AddWithValue("@dias_mora", diasMora);
                commandUpdate.Parameters.AddWithValue("@recargo_mora", recargoMora);
                commandUpdate.Parameters.AddWithValue("@monto_total", montoTotal);
                commandUpdate.Parameters.AddWithValue("@id_pago", idPago);

                return await commandUpdate.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IList<Pago>> ObtenerPagosConMoraAsync(int diasMinimos = 1)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT p.id_pago, p.id_contrato, p.numero_pago, p.concepto, p.monto_base, p.recargo_mora, 
                           p.monto_total, p.dias_mora, p.fecha_pago, p.fecha_vencimiento,
                           i.direccion AS inmueble_direccion,
                           uinq.nombre AS inquilino_nombre, uinq.apellido AS inquilino_apellido
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario uinq ON inq.id_usuario = uinq.id_usuario
                    WHERE p.tipo_pago = 'alquiler' AND p.estado = 'pagado' 
                    AND p.dias_mora >= @dias_minimos
                    ORDER BY p.dias_mora DESC, p.fecha_vencimiento";

                var pagos = new List<Pago>();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@dias_minimos", diasMinimos);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    pagos.Add(MapearPagoAlquilerListado(reader));
                }

                return pagos;
            }
            catch
            {
                return new List<Pago>();
            }
        }

        // ========================
        // MÉTODOS DE VALIDACIÓN ESPECÍFICOS
        // ========================

        public async Task<bool> ContratoVigenteAsync(int idContrato)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await ContratoVigenteInternoAsync(idContrato, connection, null);
        }

        public async Task<bool> ExistePagoMesContratoAsync(int idContrato, int numeroPago, int idPagoExcluir = 0)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await ExistePagoMesContratoInternoAsync(idContrato, numeroPago, connection, null, idPagoExcluir);
        }

        public async Task<int> ObtenerProximoNumeroPagoAsync(int idContrato)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT COALESCE(MAX(numero_pago), 0) + 1 
                    FROM pago 
                    WHERE id_contrato = @id_contrato AND tipo_pago = 'alquiler'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_contrato", idContrato);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch
            {
                return 1;
            }
        }

        public async Task<bool> ContratoPermiteMasPagosAsync(int idContrato)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.fecha_fin,
                           COUNT(p.id_pago) as pagos_realizados,
                           TIMESTAMPDIFF(MONTH, c.fecha_inicio, c.fecha_fin) + 1 as meses_total
                    FROM contrato c
                    LEFT JOIN pago p ON c.id_contrato = p.id_contrato AND p.tipo_pago = 'alquiler' AND p.estado = 'pagado'
                    WHERE c.id_contrato = @id_contrato AND c.estado = 'vigente'
                    GROUP BY c.id_contrato, c.fecha_inicio, c.fecha_fin";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_contrato", idContrato);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var fechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin"));
                    var pagosRealizados = reader.GetInt32(reader.GetOrdinal("pagos_realizados"));
                    var mesesTotal = reader.GetInt32(reader.GetOrdinal("meses_total"));

                    // Verificar si el contrato no ha vencido y aún faltan pagos
                    return fechaFin >= DateTime.Now.Date && pagosRealizados < mesesTotal;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // ========================
        // MÉTODOS DE NEGOCIO ESPECÍFICOS
        // ========================

        public async Task<DateTime> CalcularFechaVencimientoAsync(int idContrato, int numeroPago)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await CalcularFechaVencimientoInternoAsync(idContrato, numeroPago, connection, null);
        }

        public async Task<decimal> ObtenerMontoDiarioMoraAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await ObtenerMontoDiarioMoraInternoAsync(connection, null);
        }

        public async Task<bool> ActualizarEstadoContratoAsync(int idContrato)
        {
            // Implementación futura - podría actualizar estado del contrato basado en pagos
            return await Task.FromResult(true);
        }

        // ========================
        // MÉTODOS AUXILIARES PARA DROPDOWNS
        // ========================

        public async Task<List<Contrato>> ObtenerContratosVigentesAsync(int limite = 20)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var contratos = new List<Contrato>();

                string query = @"
                    SELECT c.id_contrato, i.direccion, u.nombre, u.apellido, c.monto_mensual
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                    WHERE c.estado = 'vigente' AND c.fecha_fin >= CURDATE()
                    ORDER BY i.direccion
                    LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    contratos.Add(new Contrato
                    {
                        IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                        MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                        Inmueble = new Inmueble
                        {
                            Direccion = reader.GetString(reader.GetOrdinal("direccion"))
                        },
                        Inquilino = new Inquilino
                        {
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                                Apellido = reader.GetString(reader.GetOrdinal("apellido"))
                            }
                        }
                    });
                }

                return contratos;
            }
            catch
            {
                return new List<Contrato>();
            }
        }

        public async Task<List<Usuario>> ObtenerUsuariosActivosAsync(int limite = 20)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var usuarios = new List<Usuario>();

                string query = @"
                    SELECT id_usuario, nombre, apellido, dni
                    FROM usuario 
                    WHERE estado = 'activo' 
                    ORDER BY apellido, nombre
                    LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    usuarios.Add(new Usuario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                        Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        Dni = reader.GetString(reader.GetOrdinal("dni"))
                    });
                }

                return usuarios;
            }
            catch
            {
                return new List<Usuario>();
            }
        }

        public async Task<Contrato?> ObtenerDatosContratoAsync(int idContrato)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await ObtenerDatosContratoInternoAsync(idContrato, connection, null);
        }

        // ========================
        // REPORTES ESPECÍFICOS DE ALQUILER
        // ========================

        public async Task<object> ObtenerResumenPagosPorContratoAsync(int idContrato)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        COUNT(*) as total_pagos,
                        SUM(monto_base) as total_base,
                        SUM(recargo_mora) as total_mora,
                        SUM(monto_total) as total_general,
                        AVG(COALESCE(dias_mora, 0)) as promedio_dias_mora,
                        COUNT(CASE WHEN dias_mora > 0 THEN 1 END) as pagos_con_mora
                    FROM pago 
                    WHERE id_contrato = @id_contrato AND tipo_pago = 'alquiler' AND estado = 'pagado'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_contrato", idContrato);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new
                    {
                        TotalPagos = reader.GetInt32(reader.GetOrdinal("total_pagos")),
                        TotalBase = reader.GetDecimal(reader.GetOrdinal("total_base")),
                        TotalMora = reader.GetDecimal(reader.GetOrdinal("total_mora")),
                        TotalGeneral = reader.GetDecimal(reader.GetOrdinal("total_general")),
                        PromedioDiasMora = reader.GetDouble(reader.GetOrdinal("promedio_dias_mora")),
                        PagosConMora = reader.GetInt32(reader.GetOrdinal("pagos_con_mora"))
                    };
                }

                return new { };
            }
            catch
            {
                return new { };
            }
        }

        public async Task<IList<Contrato>> ObtenerContratosProximosVencerAsync(int diasAnticipacion = 30)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.id_contrato, c.fecha_fin, c.monto_mensual,
                           i.direccion AS inmueble_direccion,
                           u.nombre AS inquilino_nombre, u.apellido AS inquilino_apellido,
                           DATEDIFF(c.fecha_fin, CURDATE()) as dias_restantes
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                    WHERE c.estado = 'vigente' 
                    AND c.fecha_fin BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL @dias DAY)
                    ORDER BY c.fecha_fin";

                var contratos = new List<Contrato>();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@dias", diasAnticipacion);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    contratos.Add(new Contrato
                    {
                        IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                        FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                        MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                        Inmueble = new Inmueble
                        {
                            Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
                        },
                        Inquilino = new Inquilino
                        {
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("inquilino_nombre")),
                                Apellido = reader.GetString(reader.GetOrdinal("inquilino_apellido"))
                            }
                        }
                    });
                }

                return contratos;
            }
            catch
            {
                return new List<Contrato>();
            }
        }

        // ========================
        // MÉTODOS PRIVADOS DE APOYO
        // ========================

        private async Task<bool> ContratoVigenteInternoAsync(int idContrato, MySqlConnection connection, MySqlTransaction? transaction)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM contrato 
                WHERE id_contrato = @id_contrato AND estado = 'vigente' AND fecha_fin >= CURDATE()";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_contrato", idContrato);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExistePagoMesContratoInternoAsync(int idContrato, int numeroPago, MySqlConnection connection, MySqlTransaction? transaction, int idPagoExcluir = 0)
        {
            string query = idPagoExcluir == 0
                ? "SELECT COUNT(*) FROM pago WHERE id_contrato = @id_contrato AND numero_pago = @numero_pago AND tipo_pago = 'alquiler'"
                : "SELECT COUNT(*) FROM pago WHERE id_contrato = @id_contrato AND numero_pago = @numero_pago AND tipo_pago = 'alquiler' AND id_pago != @id_excluir";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_contrato", idContrato);
            command.Parameters.AddWithValue("@numero_pago", numeroPago);
            if (idPagoExcluir != 0)
            {
                command.Parameters.AddWithValue("@id_excluir", idPagoExcluir);
            }

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<DateTime> CalcularFechaVencimientoInternoAsync(int idContrato, int numeroPago, MySqlConnection connection, MySqlTransaction? transaction)
        {
            // Por defecto: fecha de inicio + (numero_pago - 1) meses + 5 días de gracia
            string query = @"
                SELECT DATE_ADD(DATE_ADD(fecha_inicio, INTERVAL (@numero_pago - 1) MONTH), INTERVAL 5 DAY) as fecha_vencimiento
                FROM contrato 
                WHERE id_contrato = @id_contrato";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_contrato", idContrato);
            command.Parameters.AddWithValue("@numero_pago", numeroPago);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToDateTime(result) : DateTime.Now.AddDays(30);
        }

        private async Task<decimal> ObtenerMontoDiarioMoraInternoAsync(MySqlConnection connection, MySqlTransaction? transaction)
        {
            // Por ahora retorna valor por defecto, podrías implementar tabla de configuración
            return await Task.FromResult(MONTO_DIARIO_MORA_DEFAULT);
        }

        private async Task<Contrato?> ObtenerDatosContratoInternoAsync(int idContrato, MySqlConnection connection, MySqlTransaction? transaction)
        {
            string query = @"
                SELECT id_contrato, id_inmueble, id_inquilino, monto_mensual, fecha_inicio, fecha_fin
                FROM contrato 
                WHERE id_contrato = @id_contrato AND estado = 'vigente'";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_contrato", idContrato);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Contrato
                {
                    IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    IdInquilino = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                    MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                    FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                    FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin"))
                };
            }

            return null;
        }

        // ========================
        // MÉTODOS DE MAPEO
        // ========================

        private static Pago MapearPagoAlquilerBasico(System.Data.Common.DbDataReader reader)
        {
            return new Pago
            {
                IdPago = reader.GetInt32(reader.GetOrdinal("id_pago")),
                IdContrato = reader.IsDBNull(reader.GetOrdinal("id_contrato")) ? null : reader.GetInt32(reader.GetOrdinal("id_contrato")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                TipoPago = reader.GetString(reader.GetOrdinal("tipo_pago")),
                NumeroPago = reader.GetInt32(reader.GetOrdinal("numero_pago")),
                FechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago")),
                FechaVencimiento = reader.IsDBNull(reader.GetOrdinal("fecha_vencimiento")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_vencimiento")),
                Concepto = reader.GetString(reader.GetOrdinal("concepto")),
                MontoBase = reader.GetDecimal(reader.GetOrdinal("monto_base")),
                RecargoMora = reader.GetDecimal(reader.GetOrdinal("recargo_mora")),
                MontoTotal = reader.GetDecimal(reader.GetOrdinal("monto_total")),
                DiasMora = reader.IsDBNull(reader.GetOrdinal("dias_mora")) ? null : reader.GetInt32(reader.GetOrdinal("dias_mora")),
                MontoDiarioMora = reader.IsDBNull(reader.GetOrdinal("monto_diario_mora")) ? null : reader.GetDecimal(reader.GetOrdinal("monto_diario_mora")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                IdUsuarioCreador = reader.GetInt32(reader.GetOrdinal("id_usuario_creador")),
                IdUsuarioAnulador = reader.IsDBNull(reader.GetOrdinal("id_usuario_anulador")) ? null : reader.GetInt32(reader.GetOrdinal("id_usuario_anulador")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("fecha_anulacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_anulacion")),
                Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones"))
            };
        }

        private static Pago MapearPagoAlquilerCompleto(System.Data.Common.DbDataReader reader)
        {
            var pago = MapearPagoAlquilerBasico(reader);

            // Mapear relaciones
            pago.Inmueble = new Inmueble
            {
                Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
            };

            pago.Contrato = new Contrato
            {
                Inquilino = new Inquilino
                {
                    Usuario = new Usuario
                    {
                        Nombre = reader.GetString(reader.GetOrdinal("inquilino_nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("inquilino_apellido"))
                    }
                }
            };

            pago.UsuarioCreador = new Usuario
            {
                Nombre = reader.GetString(reader.GetOrdinal("creador_nombre")),
                Apellido = reader.GetString(reader.GetOrdinal("creador_apellido"))
            };

            if (!reader.IsDBNull(reader.GetOrdinal("anulador_nombre")))
            {
                pago.UsuarioAnulador = new Usuario
                {
                    Nombre = reader.GetString(reader.GetOrdinal("anulador_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("anulador_apellido"))
                };
            }

            return pago;
        }

        private static Pago MapearPagoAlquilerListado(System.Data.Common.DbDataReader reader)
        {
            return new Pago
            {
                IdPago = reader.GetInt32(reader.GetOrdinal("id_pago")),
                IdContrato = reader.IsDBNull(reader.GetOrdinal("id_contrato")) ? null : reader.GetInt32(reader.GetOrdinal("id_contrato")),
                NumeroPago = reader.GetInt32(reader.GetOrdinal("numero_pago")),
                FechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago")),
                FechaVencimiento = reader.IsDBNull(reader.GetOrdinal("fecha_vencimiento")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_vencimiento")),
                Concepto = reader.GetString(reader.GetOrdinal("concepto")),
                MontoBase = reader.GetDecimal(reader.GetOrdinal("monto_base")),
                RecargoMora = reader.GetDecimal(reader.GetOrdinal("recargo_mora")),
                MontoTotal = reader.GetDecimal(reader.GetOrdinal("monto_total")),
                DiasMora = reader.IsDBNull(reader.GetOrdinal("dias_mora")) ? null : reader.GetInt32(reader.GetOrdinal("dias_mora")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),

                // Mapear relaciones para listado
                Inmueble = new Inmueble
                {
                    Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
                },
                Contrato = new Contrato
                {
                    Inquilino = new Inquilino
                    {
                        Usuario = new Usuario
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("inquilino_nombre")),
                            Apellido = reader.GetString(reader.GetOrdinal("inquilino_apellido"))
                        }
                    }
                }
            };
        }

        //Obtener historial de pagos para un contrato específico
        public async Task<IList<Pago>> ObtenerHistorialPagosContratoAsync(int idContrato)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            SELECT p.id_pago, p.id_contrato, p.numero_pago, p.fecha_pago, p.fecha_vencimiento, 
                   p.concepto, p.monto_base, p.recargo_mora, p.monto_total, p.dias_mora, 
                   p.estado, p.fecha_creacion,
                   i.direccion AS inmueble_direccion,
                   uinq.nombre AS inquilino_nombre, uinq.apellido AS inquilino_apellido,
                   c.fecha_inicio, c.fecha_fin, c.monto_mensual
            FROM pago p
            INNER JOIN contrato c ON p.id_contrato = c.id_contrato
            INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario uinq ON inq.id_usuario = uinq.id_usuario
            WHERE p.id_contrato = @idContrato
            ORDER BY p.numero_pago ASC";

                var pagos = new List<Pago>();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idContrato", idContrato);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    pagos.Add(MapearPagoAlquilerListado(reader));
                }

                return pagos;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener historial de pagos del contrato {idContrato}: {ex.Message}", ex);
            }
        }
        

        //Metodos para llenar el formulario de alquiler 

        public async Task<List<ContratoAlquilerBusqueda>> BuscarContratosParaPagoAsync(string termino, int limite = 10)
{
    try
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var contratos = new List<ContratoAlquilerBusqueda>();

        string query = @"
            SELECT 
                c.id_contrato,
                c.id_inmueble,
                i.direccion as inmueble_direccion,
                u_inq.nombre as inquilino_nombre,
                u_inq.apellido as inquilino_apellido,
                u_prop.nombre as propietario_nombre,
                u_prop.apellido as propietario_apellido,
                c.monto_mensual,
                c.fecha_inicio,
                c.fecha_fin,
                TIMESTAMPDIFF(MONTH, c.fecha_inicio, c.fecha_fin) + 1 as total_meses,
                COALESCE((SELECT COUNT(*) FROM pago p 
                          WHERE p.id_contrato = c.id_contrato 
                          AND p.tipo_pago = 'alquiler' 
                          AND p.estado = 'pagado'), 0) as pagos_realizados,
                COALESCE((SELECT MAX(numero_pago) FROM pago p 
                          WHERE p.id_contrato = c.id_contrato 
                          AND p.tipo_pago = 'alquiler' 
                          AND p.estado = 'pagado'), 0) as ultimo_pago
            FROM contrato c
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario u_inq ON inq.id_usuario = u_inq.id_usuario
            INNER JOIN propietario prop ON c.id_propietario = prop.id_propietario
            INNER JOIN usuario u_prop ON prop.id_usuario = u_prop.id_usuario
            WHERE c.estado = 'vigente' 
              AND c.fecha_fin >= CURDATE()
              AND (
                  i.direccion LIKE CONCAT('%', @termino, '%')
                  OR u_inq.nombre LIKE CONCAT('%', @termino, '%')
                  OR u_inq.apellido LIKE CONCAT('%', @termino, '%')
                  OR CONCAT(u_inq.nombre, ' ', u_inq.apellido) LIKE CONCAT('%', @termino, '%')
                  OR CAST(c.id_contrato AS CHAR) LIKE CONCAT('%', @termino, '%')
              )
            ORDER BY i.direccion
            LIMIT @limite";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@termino", termino);
        command.Parameters.AddWithValue("@limite", limite);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var totalMeses = reader.GetInt32(reader.GetOrdinal("total_meses"));
            var pagosRealizados = reader.GetInt32(reader.GetOrdinal("pagos_realizados"));
            var proximoNumeroPago = pagosRealizados + 1;
            
            var contratoBusqueda = new ContratoAlquilerBusqueda
            {
                IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                InmuebleDireccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                InquilinoNombre = reader.GetString(reader.GetOrdinal("inquilino_nombre")),
                InquilinoApellido = reader.GetString(reader.GetOrdinal("inquilino_apellido")),
                PropietarioNombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                PropietarioApellido = reader.GetString(reader.GetOrdinal("propietario_apellido")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                MontoDiarioMora = MONTO_DIARIO_MORA_DEFAULT,
                FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                TotalMeses = totalMeses,
                ProximoNumeroPago = proximoNumeroPago
            };
            
            contratos.Add(contratoBusqueda);
        }

        return contratos;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en BuscarContratosParaPagoAsync: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
        return new List<ContratoAlquilerBusqueda>();
    }
}

        //validacion de la fehcas del contrato para asignarle un dia mes año a cada pago 
        
        public async Task<DatosContratoParaPago?> ObtenerDatosContratoParaPagoAsync(int idContrato)
{
    try
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Query principal para obtener datos del contrato
        string query = @"
            SELECT 
                c.id_contrato,
                c.id_inmueble,
                c.fecha_inicio,
                c.fecha_fin,
                c.monto_mensual,
                i.direccion as inmueble_direccion,
                CONCAT(u_inq.apellido, ', ', u_inq.nombre) as inquilino_completo,
                CONCAT(u_prop.apellido, ', ', u_prop.nombre) as propietario_completo,
                TIMESTAMPDIFF(MONTH, c.fecha_inicio, c.fecha_fin) + 1 as total_meses,
                (SELECT COUNT(*) FROM pago p 
                 WHERE p.id_contrato = c.id_contrato 
                 AND p.tipo_pago = 'alquiler' 
                 AND p.estado = 'pagado') as pagos_realizados
            FROM contrato c
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario u_inq ON inq.id_usuario = u_inq.id_usuario
            INNER JOIN propietario prop ON c.id_propietario = prop.id_propietario
            INNER JOIN usuario u_prop ON prop.id_usuario = u_prop.id_usuario
            WHERE c.id_contrato = @idContrato 
              AND c.estado = 'vigente'";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@idContrato", idContrato);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var fechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio"));
            var fechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin"));
            var totalMeses = reader.GetInt32(reader.GetOrdinal("total_meses"));
            var pagosRealizados = reader.GetInt32(reader.GetOrdinal("pagos_realizados"));
            var proximoNumeroPago = pagosRealizados + 1;

            // Calcular fecha de vencimiento para el próximo pago
            DateTime proximaFechaVencimiento;
            if (proximoNumeroPago <= totalMeses)
            {
                // Calcular el mes correspondiente al pago
                var fechaPeriodo = fechaInicio.AddMonths(proximoNumeroPago - 1);
                // Vencimiento: día 10 del mes siguiente al período
                if (fechaPeriodo.Month == 12)
                {
                    proximaFechaVencimiento = new DateTime(fechaPeriodo.Year + 1, 1, 10);
                }
                else
                {
                    proximaFechaVencimiento = new DateTime(fechaPeriodo.Year, fechaPeriodo.Month + 1, 10);
                }
            }
            else
            {
                // Si ya se completaron todos los pagos, usar fecha actual + 30 días
                proximaFechaVencimiento = DateTime.Now.AddDays(30);
            }

            return new DatosContratoParaPago
            {
                IdContrato = idContrato,
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                InmuebleDireccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                InquilinoNombreCompleto = reader.GetString(reader.GetOrdinal("inquilino_completo")),
                PropietarioNombreCompleto = reader.GetString(reader.GetOrdinal("propietario_completo")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                MontoDiarioMora = MONTO_DIARIO_MORA_DEFAULT,
                ProximoNumeroPago = proximoNumeroPago,
                ProximaFechaVencimiento = proximaFechaVencimiento,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalMeses = totalMeses,
                PagosRealizados = pagosRealizados
            };
        }

        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en ObtenerDatosContratoParaPagoAsync: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
        return null;
    }
}
    }
}
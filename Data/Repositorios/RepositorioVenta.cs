using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioVenta : IRepositorioVenta
    {
        private readonly string _connectionString;

        public RepositorioVenta(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // ========================
        // MÉTODOS CRUD BÁSICOS
        // ========================

        public async Task<bool> CrearPagoVentaAsync(Pago pago)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Verificar que el inmueble esté disponible
                if (!await InmuebleDisponibleParaVentaInternoAsync(pago.IdInmueble, connection, transaction))
                {
                    return false;
                }

                // 1. Crear el pago de venta
                string queryPago = @"
                    INSERT INTO pago 
                    (id_inmueble, tipo_pago, numero_pago, fecha_pago, concepto, 
                     monto_base, monto_total, estado, id_usuario_creador, fecha_creacion,
                     comprobante_ruta, comprobante_nombre, observaciones) 
                    VALUES (@id_inmueble, @tipo_pago, @numero_pago, @fecha_pago, @concepto, 
                            @monto_base, @monto_total, @estado, @id_usuario_creador, @fecha_creacion,
                            @comprobante_ruta, @comprobante_nombre, @observaciones)";

                using (var commandPago = new MySqlCommand(queryPago, connection, transaction))
                {
                    commandPago.Parameters.AddWithValue("@id_inmueble", pago.IdInmueble);
                    commandPago.Parameters.AddWithValue("@tipo_pago", "venta");
                    commandPago.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                    commandPago.Parameters.AddWithValue("@fecha_pago", pago.FechaPago);
                    commandPago.Parameters.AddWithValue("@concepto", pago.Concepto);
                    commandPago.Parameters.AddWithValue("@monto_base", pago.MontoBase);
                    commandPago.Parameters.AddWithValue("@monto_total", pago.MontoBase); // Sin mora en ventas
                    commandPago.Parameters.AddWithValue("@estado", "pagado");
                    commandPago.Parameters.AddWithValue("@id_usuario_creador", pago.IdUsuarioCreador);
                    commandPago.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);
                    commandPago.Parameters.AddWithValue("@comprobante_ruta", pago.ComprobanteRuta ?? (object)DBNull.Value);
                    commandPago.Parameters.AddWithValue("@comprobante_nombre", pago.ComprobanteNombre ?? (object)DBNull.Value);
                    commandPago.Parameters.AddWithValue("@observaciones", pago.Observaciones ?? (object)DBNull.Value);

                    await commandPago.ExecuteNonQueryAsync();
                }

                // 2. Cambiar estado del inmueble a "vendido"
                await MarcarInmuebleComoVendidoInternoAsync(pago.IdInmueble, connection, transaction);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> ActualizarPagoVentaAsync(Pago pago)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    UPDATE pago 
                    SET numero_pago = @numero_pago, fecha_pago = @fecha_pago, 
                        concepto = @concepto, monto_base = @monto_base, monto_total = @monto_total,
                        comprobante_ruta = @comprobante_ruta, comprobante_nombre = @comprobante_nombre,
                        observaciones = @observaciones
                    WHERE id_pago = @id_pago AND tipo_pago = 'venta'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@numero_pago", pago.NumeroPago);
                command.Parameters.AddWithValue("@fecha_pago", pago.FechaPago);
                command.Parameters.AddWithValue("@concepto", pago.Concepto);
                command.Parameters.AddWithValue("@monto_base", pago.MontoBase);
                command.Parameters.AddWithValue("@monto_total", pago.MontoBase); // Sin mora
                command.Parameters.AddWithValue("@comprobante_ruta", pago.ComprobanteRuta ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@comprobante_nombre", pago.ComprobanteNombre ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@observaciones", pago.Observaciones ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@id_pago", pago.IdPago);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AnularPagoVentaAsync(int idPago, int idUsuarioAnulador)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 1. Obtener ID del inmueble antes de anular
                int idInmueble;
                string queryGetInmueble = "SELECT id_inmueble FROM pago WHERE id_pago = @id_pago AND tipo_pago = 'venta'";
                using (var commandGet = new MySqlCommand(queryGetInmueble, connection, transaction))
                {
                    commandGet.Parameters.AddWithValue("@id_pago", idPago);
                    var result = await commandGet.ExecuteScalarAsync();
                    if (result == null) return false;
                    idInmueble = Convert.ToInt32(result);
                }

                // 2. Anular el pago
                string queryAnular = @"
                    UPDATE pago 
                    SET estado = 'anulado', id_usuario_anulador = @id_usuario_anulador, fecha_anulacion = @fecha_anulacion
                    WHERE id_pago = @id_pago AND tipo_pago = 'venta'";

                using (var commandAnular = new MySqlCommand(queryAnular, connection, transaction))
                {
                    commandAnular.Parameters.AddWithValue("@id_usuario_anulador", idUsuarioAnulador);
                    commandAnular.Parameters.AddWithValue("@fecha_anulacion", DateTime.Now);
                    commandAnular.Parameters.AddWithValue("@id_pago", idPago);
                    await commandAnular.ExecuteNonQueryAsync();
                }

                // 3. Restaurar estado del inmueble
                await RestaurarEstadoInmuebleInternoAsync(idInmueble, connection, transaction);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<Pago?> ObtenerPagoVentaPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id_pago, id_inmueble, tipo_pago, numero_pago, fecha_pago, concepto,
                           monto_base, recargo_mora, monto_total, estado, id_usuario_creador,
                           id_usuario_anulador, fecha_creacion, fecha_anulacion,
                           comprobante_ruta, comprobante_nombre, observaciones
                    FROM pago 
                    WHERE id_pago = @id AND tipo_pago = 'venta'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearPagoVentaBasico(reader);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Pago?> ObtenerPagoVentaConDetallesAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT p.id_pago, p.id_inmueble, p.tipo_pago, p.numero_pago, p.fecha_pago, p.concepto,
                           p.monto_base, p.recargo_mora, p.monto_total, p.estado, p.id_usuario_creador,
                           p.id_usuario_anulador, p.fecha_creacion, p.fecha_anulacion,
                           p.comprobante_ruta, p.comprobante_nombre, p.observaciones,
                           i.direccion AS inmueble_direccion,
                           u1.nombre AS creador_nombre, u1.apellido AS creador_apellido,
                           u2.nombre AS anulador_nombre, u2.apellido AS anulador_apellido
                    FROM pago p
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN usuario u1 ON p.id_usuario_creador = u1.id_usuario
                    LEFT JOIN usuario u2 ON p.id_usuario_anulador = u2.id_usuario
                    WHERE p.id_pago = @id AND p.tipo_pago = 'venta'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearPagoVentaCompleto(reader);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ========================
        // MÉTODOS DE LISTADO Y PAGINACIÓN
        // ========================

        public async Task<(IList<Pago> pagos, int totalRegistros)> ObtenerPagosVentaConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Construir WHERE dinámico
                var whereConditions = new List<string> { "p.tipo_pago = 'venta'" };
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(buscar))
                {
                    whereConditions.Add(@"(p.concepto LIKE @buscar 
                                          OR i.direccion LIKE @buscar 
                                          OR u1.nombre LIKE @buscar
                                          OR u1.apellido LIKE @buscar)");
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
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN usuario u1 ON p.id_usuario_creador = u1.id_usuario
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
                    SELECT p.id_pago, p.id_inmueble, p.numero_pago, p.fecha_pago, p.concepto,
                           p.monto_base, p.monto_total, p.estado, p.fecha_creacion,
                           p.comprobante_ruta, p.comprobante_nombre,
                           i.direccion AS inmueble_direccion,
                           u1.nombre AS creador_nombre, u1.apellido AS creador_apellido
                    FROM pago p
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN usuario u1 ON p.id_usuario_creador = u1.id_usuario
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
                        pagos.Add(MapearPagoVentaListado(reader));
                    }
                }

                return (pagos, totalRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener pagos de venta con paginación: {ex.Message}", ex);
            }
        }

        // ========================
        // MÉTODOS DE VALIDACIÓN
        // ========================

        public async Task<bool> InmuebleDisponibleParaVentaAsync(int idInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await InmuebleDisponibleParaVentaInternoAsync(idInmueble, connection, null);
        }

        public async Task<bool> ExisteVentaParaInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT COUNT(*) 
                    FROM pago 
                    WHERE id_inmueble = @id_inmueble AND tipo_pago = 'venta' AND estado = 'pagado'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExisteNumeroPagoVentaAsync(int idInmueble, int numeroPago, int idPagoExcluir = 0)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = idPagoExcluir == 0
                    ? "SELECT COUNT(*) FROM pago WHERE id_inmueble = @id_inmueble AND numero_pago = @numero_pago AND tipo_pago = 'venta'"
                    : "SELECT COUNT(*) FROM pago WHERE id_inmueble = @id_inmueble AND numero_pago = @numero_pago AND tipo_pago = 'venta' AND id_pago != @id_excluir";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", idInmueble);
                command.Parameters.AddWithValue("@numero_pago", numeroPago);
                if (idPagoExcluir != 0)
                {
                    command.Parameters.AddWithValue("@id_excluir", idPagoExcluir);
                }

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch
            {
                return false;
            }
        }

        // ========================
        // MÉTODOS DE NEGOCIO
        // ========================

        public async Task<bool> MarcarInmuebleComoVendidoAsync(int idInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await MarcarInmuebleComoVendidoInternoAsync(idInmueble, connection, null);
        }

        public async Task<bool> RestaurarEstadoInmuebleAsync(int idInmueble)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await RestaurarEstadoInmuebleInternoAsync(idInmueble, connection, null);
        }

        // ========================
        // MÉTODOS AUXILIARES
        // ========================

       public async Task<List<Inmueble>> ObtenerInmueblesDisponiblesVentaAsync(string termino, int limite = 20)
{
    try
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var inmuebles = new List<Inmueble>();
        
        // ✅ Ahora sí filtra por el término de búsqueda
        string query = @"
            SELECT i.id_inmueble, i.direccion, i.precio, i.id_tipo_inmueble,
                   t.nombre as tipo_nombre
            FROM inmueble i
            LEFT JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
            WHERE i.estado IN ('disponible', 'disponible_venta') 
            AND NOT EXISTS (
                SELECT 1 FROM pago p 
                WHERE p.id_inmueble = i.id_inmueble 
                AND p.tipo_pago = 'venta' 
                AND p.estado = 'pagado'
            )
            AND (i.direccion LIKE @termino 
                 OR t.nombre LIKE @termino)
            ORDER BY i.direccion
            LIMIT @limite";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@termino", $"%{termino}%");
        command.Parameters.AddWithValue("@limite", limite);
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            inmuebles.Add(new Inmueble
            {
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                TipoInmueble = new TipoInmueble 
                { 
                    Nombre = reader.IsDBNull(reader.GetOrdinal("tipo_nombre")) 
                        ? "Sin tipo" 
                        : reader.GetString(reader.GetOrdinal("tipo_nombre"))
                }
            });
        }

        return inmuebles;
    }
    catch (Exception ex)
    {
        // Mejor loggear el error para debug
        Console.WriteLine($"Error en ObtenerInmueblesDisponiblesVentaAsync: {ex.Message}");
        return new List<Inmueble>();
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

        // ========================
        // MÉTODOS PRIVADOS DE APOYO
        // ========================

        private async Task<bool> InmuebleDisponibleParaVentaInternoAsync(int idInmueble, MySqlConnection connection, MySqlTransaction? transaction)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM inmueble i
                WHERE i.id_inmueble = @id_inmueble 
                AND i.estado IN ('disponible', 'disponible_venta')
                AND NOT EXISTS (
                    SELECT 1 FROM pago p 
                    WHERE p.id_inmueble = i.id_inmueble 
                    AND p.tipo_pago = 'venta' 
                    AND p.estado = 'pagado'
                )";

            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_inmueble", idInmueble);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> MarcarInmuebleComoVendidoInternoAsync(int idInmueble, MySqlConnection connection, MySqlTransaction? transaction)
        {
            string query = "UPDATE inmueble SET estado = 'vendido' WHERE id_inmueble = @id_inmueble";
            
            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_inmueble", idInmueble);
            
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private async Task<bool> RestaurarEstadoInmuebleInternoAsync(int idInmueble, MySqlConnection connection, MySqlTransaction? transaction)
        {
            string query = "UPDATE inmueble SET estado = 'disponible' WHERE id_inmueble = @id_inmueble";
            
            using var command = new MySqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_inmueble", idInmueble);
            
            return await command.ExecuteNonQueryAsync() > 0;
        }

        // ========================
        // MÉTODOS DE MAPEO
        // ========================

        private static Pago MapearPagoVentaBasico(System.Data.Common.DbDataReader reader)
        {
            return new Pago
            {
                IdPago = reader.GetInt32(reader.GetOrdinal("id_pago")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                TipoPago = reader.GetString(reader.GetOrdinal("tipo_pago")),
                NumeroPago = reader.GetInt32(reader.GetOrdinal("numero_pago")),
                FechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago")),
                Concepto = reader.GetString(reader.GetOrdinal("concepto")),
                MontoBase = reader.GetDecimal(reader.GetOrdinal("monto_base")),
                RecargoMora = reader.GetDecimal(reader.GetOrdinal("recargo_mora")),
                MontoTotal = reader.GetDecimal(reader.GetOrdinal("monto_total")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                IdUsuarioCreador = reader.GetInt32(reader.GetOrdinal("id_usuario_creador")),
                IdUsuarioAnulador = reader.IsDBNull(reader.GetOrdinal("id_usuario_anulador")) ? null : reader.GetInt32(reader.GetOrdinal("id_usuario_anulador")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("fecha_anulacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_anulacion")),
                ComprobanteRuta = reader.IsDBNull(reader.GetOrdinal("comprobante_ruta")) ? null : reader.GetString(reader.GetOrdinal("comprobante_ruta")),
                ComprobanteNombre = reader.IsDBNull(reader.GetOrdinal("comprobante_nombre")) ? null : reader.GetString(reader.GetOrdinal("comprobante_nombre")),
                Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones"))
            };
        }

        private static Pago MapearPagoVentaCompleto(System.Data.Common.DbDataReader reader)
        {
            var pago = MapearPagoVentaBasico(reader);
            
            // Mapear relaciones
            pago.Inmueble = new Inmueble 
            { 
                Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
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

        private static Pago MapearPagoVentaListado(System.Data.Common.DbDataReader reader)
        {
            return new Pago
            {
                IdPago = reader.GetInt32(reader.GetOrdinal("id_pago")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                NumeroPago = reader.GetInt32(reader.GetOrdinal("numero_pago")),
                FechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago")),
                Concepto = reader.GetString(reader.GetOrdinal("concepto")),
                MontoBase = reader.GetDecimal(reader.GetOrdinal("monto_base")),
                MontoTotal = reader.GetDecimal(reader.GetOrdinal("monto_total")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                ComprobanteRuta = reader.IsDBNull(reader.GetOrdinal("comprobante_ruta")) ? null : reader.GetString(reader.GetOrdinal("comprobante_ruta")),
                ComprobanteNombre = reader.IsDBNull(reader.GetOrdinal("comprobante_nombre")) ? null : reader.GetString(reader.GetOrdinal("comprobante_nombre")),
                
                // Mapear relaciones para listado
                Inmueble = new Inmueble 
                { 
                    Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
                },
                UsuarioCreador = new Usuario
                {
                    Nombre = reader.GetString(reader.GetOrdinal("creador_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("creador_apellido"))
                }
            };
        }
    }
}
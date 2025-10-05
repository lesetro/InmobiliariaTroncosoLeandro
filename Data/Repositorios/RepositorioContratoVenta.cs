using System.Data;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Repositorios;
using MySql.Data.MySqlClient;
using System.Data;

namespace Inmobiliaria_troncoso_leandro.Data
{
    public class RepositorioContratoVenta : IRepositorioContratoVenta
    {
        private readonly string _connectionString;

        public RepositorioContratoVenta(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<ContratoVenta> ObtenerPorIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT 
                    cv.id_contrato_venta, cv.id_inmueble, cv.id_comprador, cv.id_vendedor,
                    cv.fecha_inicio, cv.fecha_escrituracion, cv.fecha_cancelacion,
                    cv.precio_total, cv.monto_seña, cv.monto_anticipos, cv.monto_pagado,
                    cv.estado, cv.porcentaje_pagado, cv.id_usuario_creador, cv.id_usuario_cancelador,
                    cv.fecha_creacion, cv.fecha_modificacion, cv.observaciones, cv.motivo_cancelacion,
                    i.direccion AS inmueble_direccion,
                    uc.nombre AS comprador_nombre, uc.apellido AS comprador_apellido, uc.dni AS comprador_dni,
                    p.id_propietario, up.nombre AS vendedor_nombre, up.apellido AS vendedor_apellido, up.dni AS vendedor_dni
                FROM contrato_venta cv
                INNER JOIN inmueble i ON cv.id_inmueble = i.id_inmueble
                INNER JOIN usuario uc ON cv.id_comprador = uc.id_usuario
                INNER JOIN propietario p ON cv.id_vendedor = p.id_propietario
                INNER JOIN usuario up ON p.id_usuario = up.id_usuario
                WHERE cv.id_contrato_venta = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapearContratoVenta(reader);
            }

            return null;
        }

        public async Task<ContratoVenta> ObtenerCompletoPorIdAsync(int id)
        {
            var contrato = await ObtenerPorIdAsync(id);
            if (contrato == null) return null;

            // Cargar pagos relacionados
            contrato.Pagos = await ObtenerPagosPorContratoVentaAsync(id);
            contrato.ActualizarMontoPagado();

            return contrato;
        }

        public async Task CrearAsync(ContratoVenta contratoVenta)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO contrato_venta (
                    id_inmueble, id_comprador, id_vendedor, fecha_inicio, fecha_escrituracion,
                    precio_total, monto_seña, monto_anticipos, monto_pagado, estado, porcentaje_pagado,
                    id_usuario_creador, fecha_creacion, fecha_modificacion, observaciones
                ) VALUES (
                    @id_inmueble, @id_comprador, @id_vendedor, @fecha_inicio, @fecha_escrituracion,
                    @precio_total, @monto_seña, @monto_anticipos, @monto_pagado, @estado, @porcentaje_pagado,
                    @id_usuario_creador, @fecha_creacion, @fecha_modificacion, @observaciones
                );
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@id_inmueble", contratoVenta.IdInmueble);
            command.Parameters.AddWithValue("@id_comprador", contratoVenta.IdComprador);
            command.Parameters.AddWithValue("@id_vendedor", contratoVenta.IdVendedor);
            command.Parameters.AddWithValue("@fecha_inicio", contratoVenta.FechaInicio);
            command.Parameters.AddWithValue("@fecha_escrituracion", contratoVenta.FechaEscrituracion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@precio_total", contratoVenta.PrecioTotal);
            command.Parameters.AddWithValue("@monto_seña", contratoVenta.MontoSeña);
            command.Parameters.AddWithValue("@monto_anticipos", contratoVenta.MontoAnticipos);
            command.Parameters.AddWithValue("@monto_pagado", contratoVenta.MontoPagado);
            command.Parameters.AddWithValue("@estado", contratoVenta.Estado);
            command.Parameters.AddWithValue("@porcentaje_pagado", contratoVenta.PorcentajePagado);
            command.Parameters.AddWithValue("@id_usuario_creador", contratoVenta.IdUsuarioCreador);
            command.Parameters.AddWithValue("@fecha_creacion", contratoVenta.FechaCreacion);
            command.Parameters.AddWithValue("@fecha_modificacion", contratoVenta.FechaModificacion);
            command.Parameters.AddWithValue("@observaciones", contratoVenta.Observaciones ?? (object)DBNull.Value);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            contratoVenta.IdContratoVenta = id;
        }

        public async Task ActualizarAsync(ContratoVenta contratoVenta)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE contrato_venta SET
                    id_inmueble = @id_inmueble,
                    id_comprador = @id_comprador,
                    id_vendedor = @id_vendedor,
                    fecha_inicio = @fecha_inicio,
                    fecha_escrituracion = @fecha_escrituracion,
                    fecha_cancelacion = @fecha_cancelacion,
                    precio_total = @precio_total,
                    monto_seña = @monto_seña,
                    monto_anticipos = @monto_anticipos,
                    monto_pagado = @monto_pagado,
                    estado = @estado,
                    porcentaje_pagado = @porcentaje_pagado,
                    id_usuario_cancelador = @id_usuario_cancelador,
                    fecha_modificacion = @fecha_modificacion,
                    observaciones = @observaciones,
                    motivo_cancelacion = @motivo_cancelacion
                WHERE id_contrato_venta = @id_contrato_venta";

            using var command = new MySqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@id_inmueble", contratoVenta.IdInmueble);
            command.Parameters.AddWithValue("@id_comprador", contratoVenta.IdComprador);
            command.Parameters.AddWithValue("@id_vendedor", contratoVenta.IdVendedor);
            command.Parameters.AddWithValue("@fecha_inicio", contratoVenta.FechaInicio);
            command.Parameters.AddWithValue("@fecha_escrituracion", contratoVenta.FechaEscrituracion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@fecha_cancelacion", contratoVenta.FechaCancelacion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@precio_total", contratoVenta.PrecioTotal);
            command.Parameters.AddWithValue("@monto_seña", contratoVenta.MontoSeña);
            command.Parameters.AddWithValue("@monto_anticipos", contratoVenta.MontoAnticipos);
            command.Parameters.AddWithValue("@monto_pagado", contratoVenta.MontoPagado);
            command.Parameters.AddWithValue("@estado", contratoVenta.Estado);
            command.Parameters.AddWithValue("@porcentaje_pagado", contratoVenta.PorcentajePagado);
            command.Parameters.AddWithValue("@id_usuario_cancelador", contratoVenta.IdUsuarioCancelador ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@fecha_modificacion", contratoVenta.FechaModificacion);
            command.Parameters.AddWithValue("@observaciones", contratoVenta.Observaciones ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@motivo_cancelacion", contratoVenta.MotivoCancelacion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@id_contrato_venta", contratoVenta.IdContratoVenta);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> EliminarAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM contrato_venta WHERE id_contrato_venta = @id";
            
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        private ContratoVenta MapearContratoVenta(MySqlDataReader reader)
        {
            return new ContratoVenta
            {
                IdContratoVenta = reader.GetInt32(reader.GetOrdinal("id_contrato_venta")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                IdComprador = reader.GetInt32(reader.GetOrdinal("id_comprador")),
                IdVendedor = reader.GetInt32(reader.GetOrdinal("id_vendedor")),
                FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                FechaEscrituracion = reader.IsDBNull(reader.GetOrdinal("fecha_escrituracion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_escrituracion")),
                FechaCancelacion = reader.IsDBNull(reader.GetOrdinal("fecha_cancelacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_cancelacion")),
                PrecioTotal = reader.GetDecimal(reader.GetOrdinal("precio_total")),
                MontoSeña = reader.GetDecimal(reader.GetOrdinal("monto_seña")),
                MontoAnticipos = reader.GetDecimal(reader.GetOrdinal("monto_anticipos")),
                MontoPagado = reader.GetDecimal(reader.GetOrdinal("monto_pagado")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                PorcentajePagado = reader.GetDecimal(reader.GetOrdinal("porcentaje_pagado")),
                IdUsuarioCreador = reader.GetInt32(reader.GetOrdinal("id_usuario_creador")),
                IdUsuarioCancelador = reader.IsDBNull(reader.GetOrdinal("id_usuario_cancelador")) ? null : reader.GetInt32(reader.GetOrdinal("id_usuario_cancelador")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                FechaModificacion = reader.GetDateTime(reader.GetOrdinal("fecha_modificacion")),
                Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString("observaciones"),
                MotivoCancelacion = reader.IsDBNull(reader.GetOrdinal("motivo_cancelacion")) ? null : reader.GetString(reader.GetOrdinal("motivo_cancelacion")),
                
                Inmueble = new Inmueble 
                { 
                    IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                    Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
                },
                Comprador = new Usuario
                {
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("id_comprador")),
                    Nombre = reader.GetString(reader.GetOrdinal("comprador_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("comprador_apellido")),
                    Dni = reader.GetString(reader.GetOrdinal("comprador_dni"))
                },
                Vendedor = new Propietario
                {
                    IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                    Usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("id_vendedor")),
                        Nombre = reader.GetString(reader.GetOrdinal("vendedor_nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("vendedor_apellido")),
                        Dni = reader.GetString(reader.GetOrdinal("vendedor_dni"))
                    }
                }
            };
        }

        private async Task<ICollection<Pago>> ObtenerPagosPorContratoVentaAsync(int idContratoVenta)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT id_pago, monto_base, fecha_pago, estado, tipo_pago, observaciones
                FROM pago 
                WHERE id_contrato_venta = @id_contrato_venta 
                AND tipo_pago = 'venta'
                ORDER BY fecha_pago";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id_contrato_venta", idContratoVenta);

            var pagos = new List<Pago>();
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                pagos.Add(new Pago
                {
                    IdPago = reader.GetInt32(reader.GetOrdinal("id_pago")),
                    MontoBase = reader.GetDecimal(reader.GetOrdinal("monto_base")),
                    FechaPago = reader.GetDateTime(reader.GetOrdinal("fecha_pago")),
                    Estado = reader.GetString(reader.GetOrdinal("estado")),
                    TipoPago = reader.GetString(reader.GetOrdinal("tipo_pago")),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones"))
                });
            }

            return pagos;
        }
    }
}
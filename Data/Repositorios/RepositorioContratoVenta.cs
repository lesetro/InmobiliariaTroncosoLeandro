using System.Data;
using MySql.Data.MySqlClient;
using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Data.Repositorios;

namespace Inmobiliaria_troncoso_leandro.Data
{
    public class RepositorioContratoVenta : IRepositorioContratoVenta
    {
        private readonly string _connectionString;

        public RepositorioContratoVenta(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<ContratoVenta> ObtenerPorIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();


            string query = @"
        SELECT 
            id_contrato_venta,
            id_inmueble,
            id_comprador,
            id_vendedor,
            fecha_inicio,
            fecha_escrituracion,
            fecha_cancelacion,
            precio_total,
            monto_seña,
            monto_anticipos,
            monto_pagado,
            estado,
            porcentaje_pagado,
            id_usuario_creador,
            id_usuario_cancelador,
            fecha_creacion,
            fecha_modificacion,
            observaciones,
            motivo_cancelacion
        FROM contrato_venta 
        WHERE id_contrato_venta = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ContratoVenta
                {
                    IdContratoVenta = reader.GetInt32("id_contrato_venta"),
                    IdInmueble = reader.GetInt32("id_inmueble"),
                    IdComprador = reader.GetInt32("id_comprador"),
                    IdVendedor = reader.GetInt32("id_vendedor"),
                    FechaInicio = reader.GetDateTime("fecha_inicio"),
                    FechaEscrituracion = reader.IsDBNull("fecha_escrituracion") ? null : reader.GetDateTime("fecha_escrituracion"),
                    FechaCancelacion = reader.IsDBNull("fecha_cancelacion") ? null : reader.GetDateTime("fecha_cancelacion"),
                    PrecioTotal = reader.GetDecimal("precio_total"),
                    MontoSeña = reader.GetDecimal("monto_seña"),
                    MontoAnticipos = reader.GetDecimal("monto_anticipos"),
                    MontoPagado = reader.GetDecimal("monto_pagado"),
                    Estado = reader.GetString("estado"),
                    PorcentajePagado = reader.GetDecimal("porcentaje_pagado"),
                    IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                    IdUsuarioCancelador = reader.IsDBNull("id_usuario_cancelador") ? null : reader.GetInt32("id_usuario_cancelador"),
                    FechaCreacion = reader.GetDateTime("fecha_creacion"),
                    FechaModificacion = reader.GetDateTime("fecha_modificacion"),
                    Observaciones = reader.IsDBNull("observaciones") ? null : reader.GetString("observaciones"),
                    MotivoCancelacion = reader.IsDBNull("motivo_cancelacion") ? null : reader.GetString("motivo_cancelacion")
                };
            }

            return null;
        }
        private ContratoVenta MapearContratoVentaCompleto(MySqlDataReader reader)
        {
            var contrato = new ContratoVenta
            {
                IdContratoVenta = reader.GetInt32("id_contrato_venta"),
                IdInmueble = reader.GetInt32("id_inmueble"),
                IdComprador = reader.GetInt32("id_comprador"),
                IdVendedor = reader.GetInt32("id_vendedor"),
                FechaInicio = reader.GetDateTime("fecha_inicio"),
                FechaEscrituracion = reader.IsDBNull("fecha_escrituracion") ? null : reader.GetDateTime("fecha_escrituracion"),
                FechaCancelacion = reader.IsDBNull("fecha_cancelacion") ? null : reader.GetDateTime("fecha_cancelacion"),
                PrecioTotal = reader.GetDecimal("precio_total"),
                MontoSeña = reader.GetDecimal("monto_seña"),
                MontoAnticipos = reader.GetDecimal("monto_anticipos"),
                MontoPagado = reader.GetDecimal("monto_pagado"),
                Estado = reader.GetString("estado"),
                PorcentajePagado = reader.GetDecimal("porcentaje_pagado"),
                IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                IdUsuarioCancelador = reader.IsDBNull("id_usuario_cancelador") ? null : reader.GetInt32("id_usuario_cancelador"),
                FechaCreacion = reader.GetDateTime("fecha_creacion"),
                FechaModificacion = reader.GetDateTime("fecha_modificacion"),
                Observaciones = reader.IsDBNull("observaciones") ? null : reader.GetString("observaciones"),
                MotivoCancelacion = reader.IsDBNull("motivo_cancelacion") ? null : reader.GetString("motivo_cancelacion"),

                // RELACIONES
                Inmueble = new Inmueble
                {
                    IdInmueble = reader.GetInt32("id_inmueble"),
                    Direccion = reader.GetString("inmueble_direccion"),
                    Precio = reader.GetDecimal("inmueble_precio")
                },
                Comprador = new Usuario
                {
                    IdUsuario = reader.GetInt32("id_comprador"),
                    Nombre = reader.GetString("comprador_nombre"),
                    Apellido = reader.GetString("comprador_apellido"),
                    Dni = reader.GetString("comprador_dni")
                },
                Vendedor = new Propietario
                {
                    IdPropietario = reader.GetInt32("id_propietario"),
                    Usuario = new Usuario
                    {
                        IdUsuario = reader.GetInt32("vendedor_id_usuario"),
                        Nombre = reader.GetString("vendedor_nombre"),
                        Apellido = reader.GetString("vendedor_apellido"),
                        Dni = reader.GetString("vendedor_dni")
                    }
                },
                UsuarioCreador = new Usuario
                {
                    IdUsuario = reader.GetInt32("id_usuario_creador"),
                    Nombre = reader.GetString("creador_nombre"),
                    Apellido = reader.GetString("creador_apellido")
                }
            };

            return contrato;
        }
        public async Task<ContratoVenta> ObtenerCompletoPorIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // CONSULTA COMPLETA pero SIN ALIAS PROBLEMÁTICOS
            string query = @"
        SELECT 
            cv.id_contrato_venta,
            cv.id_inmueble,
            cv.id_comprador,
            cv.id_vendedor,
            cv.fecha_inicio,
            cv.fecha_escrituracion,
            cv.fecha_cancelacion,
            cv.precio_total,
            cv.monto_seña,
            cv.monto_anticipos,
            cv.monto_pagado,
            cv.estado,
            cv.porcentaje_pagado,
            cv.id_usuario_creador,
            cv.id_usuario_cancelador,
            cv.fecha_creacion,
            cv.fecha_modificacion,
            cv.observaciones,
            cv.motivo_cancelacion,
            i.direccion AS inmueble_direccion,
            i.precio AS inmueble_precio,
            uc.nombre AS comprador_nombre,
            uc.apellido AS comprador_apellido,
            uc.dni AS comprador_dni,
            p.id_propietario,
            uv.nombre AS vendedor_nombre,
            uv.apellido AS vendedor_apellido,
            uv.dni AS vendedor_dni,
            ucreador.nombre AS creador_nombre,
            ucreador.apellido AS creador_apellido
        FROM contrato_venta cv
        LEFT JOIN inmueble i ON cv.id_inmueble = i.id_inmueble
        LEFT JOIN usuario uc ON cv.id_comprador = uc.id_usuario
        LEFT JOIN propietario p ON cv.id_vendedor = p.id_propietario
        LEFT JOIN usuario uv ON p.id_usuario = uv.id_usuario
        LEFT JOIN usuario ucreador ON cv.id_usuario_creador = ucreador.id_usuario
        WHERE cv.id_contrato_venta = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var contrato = new ContratoVenta
                {
                    IdContratoVenta = reader.GetInt32("id_contrato_venta"),
                    IdInmueble = reader.GetInt32("id_inmueble"),
                    IdComprador = reader.GetInt32("id_comprador"),
                    IdVendedor = reader.GetInt32("id_vendedor"),
                    FechaInicio = reader.GetDateTime("fecha_inicio"),
                    FechaEscrituracion = reader.IsDBNull("fecha_escrituracion") ? null : reader.GetDateTime("fecha_escrituracion"),
                    FechaCancelacion = reader.IsDBNull("fecha_cancelacion") ? null : reader.GetDateTime("fecha_cancelacion"),
                    PrecioTotal = reader.GetDecimal("precio_total"),
                    MontoSeña = reader.GetDecimal("monto_seña"),
                    MontoAnticipos = reader.GetDecimal("monto_anticipos"),
                    MontoPagado = reader.GetDecimal("monto_pagado"),
                    Estado = reader.GetString("estado"),
                    PorcentajePagado = reader.GetDecimal("porcentaje_pagado"),
                    IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                    IdUsuarioCancelador = reader.IsDBNull("id_usuario_cancelador") ? null : reader.GetInt32("id_usuario_cancelador"),
                    FechaCreacion = reader.GetDateTime("fecha_creacion"),
                    FechaModificacion = reader.GetDateTime("fecha_modificacion"),
                    Observaciones = reader.IsDBNull("observaciones") ? null : reader.GetString("observaciones"),
                    MotivoCancelacion = reader.IsDBNull("motivo_cancelacion") ? null : reader.GetString("motivo_cancelacion"),

                    Inmueble = new Inmueble
                    {
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        Direccion = reader.GetString("inmueble_direccion"),
                        Precio = reader.GetDecimal("inmueble_precio")
                    },
                    Comprador = new Usuario
                    {
                        IdUsuario = reader.GetInt32("id_comprador"),
                        Nombre = reader.GetString("comprador_nombre"),
                        Apellido = reader.GetString("comprador_apellido"),
                        Dni = reader.GetString("comprador_dni")
                    },
                    Vendedor = new Propietario
                    {
                        IdPropietario = reader.GetInt32("id_propietario"),
                        Usuario = new Usuario
                        {
                            IdUsuario = reader.GetInt32("id_vendedor"),
                            Nombre = reader.GetString("vendedor_nombre"),
                            Apellido = reader.GetString("vendedor_apellido"),
                            Dni = reader.GetString("vendedor_dni")
                        }
                    },
                    UsuarioCreador = new Usuario
                    {
                        IdUsuario = reader.GetInt32("id_usuario_creador"),
                        Nombre = reader.GetString("creador_nombre"),
                        Apellido = reader.GetString("creador_apellido")
                    }
                };

                // Cargar pagos
                contrato.Pagos = await ObtenerPagosPorContratoVentaAsync(id);
                contrato.ActualizarMontoPagado();

                return contrato;
            }

            return null;
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

            // BAJA LÓGICA - Cambia estado a cancelada
            string query = @"
                UPDATE contrato_venta SET 
                    estado = 'cancelada',
                    fecha_cancelacion = @fecha_cancelacion,
                    fecha_modificacion = @fecha_modificacion
                WHERE id_contrato_venta = @id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@fecha_cancelacion", DateTime.Now);
            command.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);

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

            // CONSULTA CORREGIDA - usa la NUEVA columna
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
                    IdPago = reader.GetInt32("id_pago"),
                    MontoBase = reader.GetDecimal("monto_base"),
                    FechaPago = reader.GetDateTime("fecha_pago"),
                    Estado = reader.GetString("estado"),
                    TipoPago = reader.GetString("tipo_pago"),
                    Observaciones = reader.IsDBNull("observaciones") ? null : reader.GetString("observaciones")
                });
            }

            return pagos;
        }
        //buscador de contratos 
        public async Task<IEnumerable<ContratoVenta>> ObtenerTodosAsync()
        {
            var contratos = new List<ContratoVenta>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT 
                    cv.id_contrato_venta,
                    cv.id_inmueble,
                    cv.id_comprador,
                    cv.id_vendedor,
                    cv.fecha_inicio,
                    cv.fecha_escrituracion,
                    cv.fecha_cancelacion,
                    cv.precio_total,
                    cv.monto_seña,
                    cv.monto_anticipos,
                    cv.monto_pagado,
                    cv.estado,
                    cv.porcentaje_pagado,
                    cv.fecha_creacion,
                    cv.fecha_modificacion,
                    i.direccion AS inmueble_direccion,
                    i.precio AS inmueble_precio,
                    uc.nombre AS comprador_nombre,
                    uc.apellido AS comprador_apellido,
                    uc.dni AS comprador_dni,
                    uv.nombre AS vendedor_nombre,
                    uv.apellido AS vendedor_apellido,
                    uv.dni AS vendedor_dni,
                    p.id_propietario,
                    p.id_usuario AS vendedor_id_usuario
                FROM contrato_venta cv
                LEFT JOIN inmueble i ON cv.id_inmueble = i.id_inmueble
                LEFT JOIN usuario uc ON cv.id_comprador = uc.id_usuario
                LEFT JOIN propietario p ON cv.id_vendedor = p.id_propietario
                LEFT JOIN usuario uv ON p.id_usuario = uv.id_usuario
                ORDER BY cv.fecha_creacion DESC";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                try
                {
                    var contrato = new ContratoVenta
                    {
                        // DATOS BÁSICOS DEL CONTRATO
                        IdContratoVenta = reader.GetInt32("id_contrato_venta"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        IdComprador = reader.GetInt32("id_comprador"),
                        IdVendedor = reader.GetInt32("id_vendedor"),
                        FechaInicio = reader.GetDateTime("fecha_inicio"),
                        PrecioTotal = reader.GetDecimal("precio_total"),
                        MontoSeña = reader.GetDecimal("monto_seña"),
                        MontoAnticipos = reader.GetDecimal("monto_anticipos"),
                        MontoPagado = reader.GetDecimal("monto_pagado"),
                        Estado = reader.GetString("estado"),
                        PorcentajePagado = reader.GetDecimal("porcentaje_pagado"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion"),
                        FechaModificacion = reader.GetDateTime("fecha_modificacion"),

                        // OBJETOS BÁSICOS PARA LA VISTA
                        Inmueble = new Inmueble
                        {
                            IdInmueble = reader.GetInt32("id_inmueble"),
                            Direccion = reader.GetString("inmueble_direccion"),
                            Precio = reader.GetDecimal("inmueble_precio")
                        },

                        Comprador = new Usuario
                        {
                            IdUsuario = reader.GetInt32("id_comprador"),
                            Nombre = reader.GetString("comprador_nombre"),
                            Apellido = reader.GetString("comprador_apellido"),
                            Dni = reader.GetString("comprador_dni")
                        },

                        Vendedor = new Propietario
                        {
                            IdPropietario = reader.GetInt32("id_propietario"),
                            Usuario = new Usuario
                            {
                                IdUsuario = reader.GetInt32("vendedor_id_usuario"),
                                Nombre = reader.GetString("vendedor_nombre"),
                                Apellido = reader.GetString("vendedor_apellido"),
                                Dni = reader.GetString("vendedor_dni")
                            }
                        }
                    };

                    // FECHAS OPCIONALES
                    if (!reader.IsDBNull("fecha_escrituracion"))
                        contrato.FechaEscrituracion = reader.GetDateTime("fecha_escrituracion");

                    if (!reader.IsDBNull("fecha_cancelacion"))
                        contrato.FechaCancelacion = reader.GetDateTime("fecha_cancelacion");

                    contratos.Add(contrato);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" ERROR mapeando contrato: {ex.Message}");
                    // Continuar con el siguiente contrato
                }
            }

            return contratos;
        }


        public async Task<ContratoVenta?> ObtenerContratoPorInmuebleAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
            SELECT 
                cv.id_contrato_venta,
                cv.id_inmueble,
                cv.id_comprador,
                cv.id_vendedor,
                cv.fecha_inicio,
                cv.fecha_escrituracion,
                cv.fecha_cancelacion,
                cv.precio_total,
                cv.monto_seña,
                cv.monto_anticipos,
                cv.monto_pagado,
                cv.estado,
                cv.porcentaje_pagado,
                cv.id_usuario_creador,
                cv.id_usuario_cancelador,
                cv.fecha_creacion,
                cv.fecha_modificacion,
                cv.observaciones,
                cv.motivo_cancelacion,
                -- Datos del inmueble (CORREGIDO)
                i.direccion,
                i.id_tipo_inmueble,  -- CAMBIADO: usar id_tipo_inmueble en lugar de tipo_inmueble
                ti.nombre as tipo_inmueble_nombre,  -- AGREGADO: obtener nombre del tipo
                i.precio,
                -- Datos del comprador (usuario)
                uc.nombre as comprador_nombre,
                uc.apellido as comprador_apellido,
                uc.dni as comprador_dni,
                uc.email as comprador_email,
                uc.telefono as comprador_telefono,
                -- Datos del vendedor (propietario -> usuario)
                p.id_propietario,
                uv.nombre as vendedor_nombre,
                uv.apellido as vendedor_apellido,
                uv.dni as vendedor_dni,
                uv.email as vendedor_email,
                uv.telefono as vendedor_telefono
            FROM contrato_venta cv
            LEFT JOIN inmueble i ON cv.id_inmueble = i.id_inmueble
            LEFT JOIN tipo_inmueble ti ON i.id_tipo_inmueble = ti.id_tipo_inmueble  -- AGREGADO: join con tipo_inmueble
            LEFT JOIN usuario uc ON cv.id_comprador = uc.id_usuario
            LEFT JOIN propietario p ON cv.id_vendedor = p.id_propietario
            LEFT JOIN usuario uv ON p.id_usuario = uv.id_usuario
            WHERE cv.id_inmueble = @idInmueble
            AND cv.estado NOT IN ('cancelada', 'escriturada')
            ORDER BY cv.fecha_creacion DESC
            LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idInmueble", idInmueble);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var contrato = new ContratoVenta
                    {
                        IdContratoVenta = reader.GetInt32("id_contrato_venta"),
                        IdInmueble = reader.GetInt32("id_inmueble"),
                        IdComprador = reader.GetInt32("id_comprador"),
                        IdVendedor = reader.GetInt32("id_vendedor"),
                        FechaInicio = reader.GetDateTime("fecha_inicio"),
                        PrecioTotal = reader.GetDecimal("precio_total"),
                        MontoSeña = reader.GetDecimal("monto_seña"),
                        MontoAnticipos = reader.GetDecimal("monto_anticipos"),
                        MontoPagado = reader.GetDecimal("monto_pagado"),
                        Estado = reader.GetString("estado"),
                        PorcentajePagado = reader.GetDecimal("porcentaje_pagado"),
                        IdUsuarioCreador = reader.GetInt32("id_usuario_creador"),
                        FechaCreacion = reader.GetDateTime("fecha_creacion"),
                        FechaModificacion = reader.GetDateTime("fecha_modificacion"),
                        Observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString("observaciones"),
                        MotivoCancelacion = reader.IsDBNull(reader.GetOrdinal("motivo_cancelacion")) ? null : reader.GetString("motivo_cancelacion")
                    };

                    // Fechas nullable
                    if (!reader.IsDBNull(reader.GetOrdinal("fecha_escrituracion")))
                        contrato.FechaEscrituracion = reader.GetDateTime("fecha_escrituracion");

                    if (!reader.IsDBNull(reader.GetOrdinal("fecha_cancelacion")))
                        contrato.FechaCancelacion = reader.GetDateTime("fecha_cancelacion");

                    if (!reader.IsDBNull(reader.GetOrdinal("id_usuario_cancelador")))
                        contrato.IdUsuarioCancelador = reader.GetInt32("id_usuario_cancelador");

                    // Crear objetos de navegación básicos (CORREGIDO)
                    contrato.Inmueble = new Inmueble
                    {
                        IdInmueble = contrato.IdInmueble,
                        Direccion = reader.GetString("direccion"),
                        TipoInmueble = new TipoInmueble
                        {
                            IdTipoInmueble = reader.GetInt32("id_tipo_inmueble"),
                            Nombre = reader.GetString("tipo_inmueble_nombre")  // CAMBIADO: usar tipo_inmueble_nombre
                        },
                        Precio = reader.GetDecimal("precio")
                    };

                    // Comprador (usuario normal)
                    contrato.Comprador = new Usuario
                    {
                        IdUsuario = contrato.IdComprador,
                        Nombre = reader.GetString("comprador_nombre"),
                        Apellido = reader.GetString("comprador_apellido"),
                        Dni = reader.GetString("comprador_dni"),
                        Email = reader.GetString("comprador_email"),
                        Telefono = reader.GetString("comprador_telefono"),
                        Rol = "comprador"
                    };

                    // Vendedor (propietario con su usuario relacionado)
                    contrato.Vendedor = new Propietario
                    {
                        IdPropietario = reader.GetInt32("id_propietario"),
                        IdUsuario = await GetUsuarioIdFromPropietario(reader.GetInt32("id_propietario")),
                        Usuario = new Usuario
                        {
                            IdUsuario = await GetUsuarioIdFromPropietario(reader.GetInt32("id_propietario")),
                            Nombre = reader.GetString("vendedor_nombre"),
                            Apellido = reader.GetString("vendedor_apellido"),
                            Dni = reader.GetString("vendedor_dni"),
                            Email = reader.GetString("vendedor_email"),
                            Telefono = reader.GetString("vendedor_telefono"),
                            Rol = "propietario"
                        }
                    };

                    // Cargar pagos relacionados
                    contrato.Pagos = await ObtenerPagosPorContratoVentaAsync(contrato.IdContratoVenta);

                    return contrato;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener contrato por inmueble {idInmueble}: {ex.Message}");
                throw;
            }
        }

        private async Task<int> GetUsuarioIdFromPropietario(int idPropietario)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT id_usuario FROM propietario WHERE id_propietario = @idPropietario";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idPropietario", idPropietario);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo usuario de propietario {idPropietario}: {ex.Message}");
                return 0;
            }
        }
    }
}
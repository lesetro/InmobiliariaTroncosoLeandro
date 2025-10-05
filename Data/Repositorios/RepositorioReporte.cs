using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioReporte : IRepositorioReporte
    {
        private readonly string _connectionString;

        public RepositorioReporte(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // ========================
        // ESTADÍSTICAS BÁSICAS
        // ========================

        public async Task<object> ObtenerResumenGeneralAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        COUNT(*) as total_pagos,
                        SUM(monto_total) as ingresos_total,
                        AVG(monto_total) as promedio_pago,
                        COUNT(CASE WHEN tipo_pago = 'alquiler' THEN 1 END) as pagos_alquiler,
                        COUNT(CASE WHEN tipo_pago = 'venta' THEN 1 END) as pagos_venta
                    FROM pago 
                    WHERE estado = 'pagado' 
                    AND fecha_pago >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new
                    {
                        TotalPagos = reader.GetInt32(reader.GetOrdinal("total_pagos")),
                        IngresosTotal = reader.GetDecimal(reader.GetOrdinal("ingresos_total")),
                        PromedioPago = reader.IsDBNull(reader.GetOrdinal("promedio_pago")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_pago")),
                        PagosAlquiler = reader.GetInt32(reader.GetOrdinal("pagos_alquiler")),
                        PagosVenta = reader.GetInt32(reader.GetOrdinal("pagos_venta"))
                    };
                }

                return new { };
            }
            catch
            {
                return new { };
            }
        }

        public async Task<IList<object>> ObtenerIngresosPorMesAsync(int meses = 6)
{
    try
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            SELECT 
                DATE_FORMAT(fecha_pago, '%Y-%m') as mes,
                CASE MONTH(fecha_pago)
                    WHEN 1 THEN 'Enero'
                    WHEN 2 THEN 'Febrero'
                    WHEN 3 THEN 'Marzo'
                    WHEN 4 THEN 'Abril'
                    WHEN 5 THEN 'Mayo'
                    WHEN 6 THEN 'Junio'
                    WHEN 7 THEN 'Julio'
                    WHEN 8 THEN 'Agosto'
                    WHEN 9 THEN 'Septiembre'
                    WHEN 10 THEN 'Octubre'
                    WHEN 11 THEN 'Noviembre'
                    WHEN 12 THEN 'Diciembre'
                END as nombre_mes,
                YEAR(fecha_pago) as año,
                COUNT(*) as cantidad_pagos,
                SUM(monto_total) as total_ingresos
            FROM pago 
            WHERE fecha_pago >= DATE_SUB(CURDATE(), INTERVAL @meses MONTH)
            AND estado = 'pagado'
            GROUP BY DATE_FORMAT(fecha_pago, '%Y-%m'), YEAR(fecha_pago), MONTH(fecha_pago)
            ORDER BY mes DESC";

        var ingresos = new List<object>();
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@meses", meses);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ingresos.Add(new
            {
                Mes = reader.GetString(reader.GetOrdinal("mes")),
                NombreMes = reader.GetString(reader.GetOrdinal("nombre_mes")),
                Año = reader.GetInt32(reader.GetOrdinal("año")),
                CantidadPagos = reader.GetInt32(reader.GetOrdinal("cantidad_pagos")),
                TotalIngresos = reader.GetDecimal(reader.GetOrdinal("total_ingresos"))
            });
        }

        return ingresos;
    }
    catch
    {
        return new List<object>();
    }
}

        public async Task<IList<object>> ObtenerTopInmueblesAsync(int limite = 5)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        i.direccion,
                        COUNT(p.id_pago) as total_pagos,
                        SUM(p.monto_total) as ingresos_total
                    FROM inmueble i
                    INNER JOIN pago p ON i.id_inmueble = p.id_inmueble
                    WHERE p.estado = 'pagado'
                    GROUP BY i.id_inmueble, i.direccion
                    ORDER BY ingresos_total DESC
                    LIMIT @limite";

                var topInmuebles = new List<object>();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    topInmuebles.Add(new
                    {
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        TotalPagos = reader.GetInt32(reader.GetOrdinal("total_pagos")),
                        IngresosTotal = reader.GetDecimal(reader.GetOrdinal("ingresos_total"))
                    });
                }

                return topInmuebles;
            }
            catch
            {
                return new List<object>();
            }
        }

        // ========================
        // ALERTAS BÁSICAS
        // ========================

        public async Task<IList<object>> ObtenerPagosConMoraAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        p.concepto,
                        p.dias_mora,
                        p.recargo_mora,
                        i.direccion as inmueble_direccion,
                        CONCAT(u.apellido, ', ', u.nombre) as inquilino
                    FROM pago p
                    INNER JOIN contrato c ON p.id_contrato = c.id_contrato
                    INNER JOIN inmueble i ON p.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                    WHERE p.dias_mora > 0 
                    AND p.estado = 'pagado'
                    ORDER BY p.dias_mora DESC
                    LIMIT 10";

                var pagosMora = new List<object>();
                using var command = new MySqlCommand(query, connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    pagosMora.Add(new
                    {
                        Concepto = reader.GetString(reader.GetOrdinal("concepto")),
                        DiasMora = reader.IsDBNull(reader.GetOrdinal("dias_mora")) ? 0 : reader.GetInt32(reader.GetOrdinal("dias_mora")),
                        RecargoMora = reader.GetDecimal(reader.GetOrdinal("recargo_mora")),
                        InmuebleDireccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                        Inquilino = reader.GetString(reader.GetOrdinal("inquilino"))
                    });
                }

                return pagosMora;
            }
            catch
            {
                return new List<object>();
            }
        }

        public async Task<IList<object>> ObtenerContratosProximosVencerAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        i.direccion as inmueble_direccion,
                        CONCAT(u.apellido, ', ', u.nombre) as inquilino,
                        c.fecha_fin,
                        DATEDIFF(c.fecha_fin, CURDATE()) as dias_restantes
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                    WHERE c.estado = 'vigente' 
                    AND c.fecha_fin BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)
                    ORDER BY c.fecha_fin";

                var contratos = new List<object>();
                using var command = new MySqlCommand(query, connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    contratos.Add(new
                    {
                        InmuebleDireccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                        Inquilino = reader.GetString(reader.GetOrdinal("inquilino")),
                        FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                        DiasRestantes = reader.GetInt32(reader.GetOrdinal("dias_restantes"))
                    });
                }

                return contratos;
            }
            catch
            {
                return new List<object>();
            }
        }

        // ========================
        // ESTADO DE INMUEBLES
        // ========================

        public async Task<object> ObtenerEstadoInmueblesAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        estado,
                        COUNT(*) as cantidad
                    FROM inmueble 
                    WHERE estado != 'inactivo'
                    GROUP BY estado
                    ORDER BY cantidad DESC";

                var estados = new List<object>();
                using var command = new MySqlCommand(query, connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    estados.Add(new
                    {
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        Cantidad = reader.GetInt32(reader.GetOrdinal("cantidad"))
                    });
                }

                return estados;
            }
            catch
            {
                return new List<object>();
            }
        }
    }
}
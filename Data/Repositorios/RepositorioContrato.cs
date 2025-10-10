using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;
using Dapper;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioContrato : IRepositorioContrato
    {
        private readonly string _connectionString;

        public RepositorioContrato(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
        }

        // ========================================
        // MÉTODOS CRUD BÁSICOS
        // ========================================

        public async Task<bool> CrearContratoAsync(Contrato contrato)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Verificar que el inmueble esté disponible
                    if (!await ExisteInmuebleDisponibleAsync(contrato.IdInmueble, contrato.FechaInicio, contrato.FechaFin))
                    {
                        return false;
                    }

                    string query = @"
                        INSERT INTO contrato 
                        (id_inmueble, id_inquilino, id_propietario, fecha_inicio, fecha_fin, 
                         monto_mensual, estado, multa_aplicada, id_usuario_creador, 
                         fecha_creacion, fecha_modificacion) 
                        VALUES (@id_inmueble, @id_inquilino, @id_propietario, @fecha_inicio, @fecha_fin, 
                                @monto_mensual, @estado, @multa_aplicada, @id_usuario_creador, 
                                @fecha_creacion, @fecha_modificacion)";

                    using var command = new MySqlCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@id_inmueble", contrato.IdInmueble);
                    command.Parameters.AddWithValue("@id_inquilino", contrato.IdInquilino);
                    command.Parameters.AddWithValue("@id_propietario", contrato.IdPropietario);
                    command.Parameters.AddWithValue("@fecha_inicio", contrato.FechaInicio);
                    command.Parameters.AddWithValue("@fecha_fin", contrato.FechaFin);
                    command.Parameters.AddWithValue("@monto_mensual", contrato.MontoMensual);
                    command.Parameters.AddWithValue("@estado", "vigente");
                    command.Parameters.AddWithValue("@multa_aplicada", 0);
                    command.Parameters.AddWithValue("@id_usuario_creador", contrato.IdUsuarioCreador);
                    command.Parameters.AddWithValue("@fecha_creacion", DateTime.Now);
                    command.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);

                    await command.ExecuteNonQueryAsync();

                    // Actualizar estado del inmueble a "alquilado"
                    string updateInmuebleQuery = "UPDATE inmueble SET estado = 'alquilado' WHERE id_inmueble = @id_inmueble";
                    using var updateCommand = new MySqlCommand(updateInmuebleQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@id_inmueble", contrato.IdInmueble);
                    await updateCommand.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear contrato: {ex.Message}", ex);
            }
        }

        public async Task<bool> ActualizarContratoAsync(Contrato contrato)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    UPDATE contrato 
                    SET id_inmueble = @id_inmueble, id_inquilino = @id_inquilino, 
                        id_propietario = @id_propietario, fecha_inicio = @fecha_inicio, 
                        fecha_fin = @fecha_fin, fecha_fin_anticipada = @fecha_fin_anticipada, 
                        monto_mensual = @monto_mensual, estado = @estado, 
                        multa_aplicada = @multa_aplicada, id_usuario_terminador = @id_usuario_terminador, 
                        fecha_modificacion = @fecha_modificacion
                    WHERE id_contrato = @id_contrato";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inmueble", contrato.IdInmueble);
                command.Parameters.AddWithValue("@id_inquilino", contrato.IdInquilino);
                command.Parameters.AddWithValue("@id_propietario", contrato.IdPropietario);
                command.Parameters.AddWithValue("@fecha_inicio", contrato.FechaInicio);
                command.Parameters.AddWithValue("@fecha_fin", contrato.FechaFin);
                command.Parameters.AddWithValue("@fecha_fin_anticipada", contrato.FechaFinAnticipada ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@monto_mensual", contrato.MontoMensual);
                command.Parameters.AddWithValue("@estado", contrato.Estado);
                command.Parameters.AddWithValue("@multa_aplicada", contrato.MultaAplicada);
                command.Parameters.AddWithValue("@id_usuario_terminador", contrato.IdUsuarioTerminador ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);
                command.Parameters.AddWithValue("@id_contrato", contrato.IdContrato);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar contrato: {ex.Message}", ex);
            }
        }

        public async Task<bool> EliminarContratoAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Obtener el ID del inmueble antes de finalizar el contrato
                    int idInmueble = 0;
                    string getInmuebleQuery = "SELECT id_inmueble FROM contrato WHERE id_contrato = @id";
                    using (var getCommand = new MySqlCommand(getInmuebleQuery, connection, transaction))
                    {
                        getCommand.Parameters.AddWithValue("@id", id);
                        var inmuebleResult = await getCommand.ExecuteScalarAsync();
                        if (inmuebleResult != null && inmuebleResult != DBNull.Value)
                        {
                            idInmueble = Convert.ToInt32(inmuebleResult);
                        }

                    }

                    // Finalizar contrato (soft delete)
                    string query = @"
                        UPDATE contrato 
                        SET estado = 'finalizado', fecha_modificacion = @fecha_modificacion,
                            fecha_fin_anticipada = @fecha_fin_anticipada
                        WHERE id_contrato = @id";

                    using var command = new MySqlCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@fecha_modificacion", DateTime.Now);
                    command.Parameters.AddWithValue("@fecha_fin_anticipada", DateTime.Now);
                    command.Parameters.AddWithValue("@id", id);

                    var result = await command.ExecuteNonQueryAsync() > 0;

                    // Actualizar estado del inmueble a "disponible" si se finalizó el contrato
                    if (result && idInmueble > 0)
                    {
                        string updateInmuebleQuery = "UPDATE inmueble SET estado = 'disponible' WHERE id_inmueble = @id_inmueble";
                        using var updateCommand = new MySqlCommand(updateInmuebleQuery, connection, transaction);
                        updateCommand.Parameters.AddWithValue("@id_inmueble", idInmueble);
                        await updateCommand.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar contrato: {ex.Message}", ex);
            }
        }

        public async Task<Contrato?> ObtenerContratoPorIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id_contrato, id_inmueble, id_inquilino, id_propietario, fecha_inicio, fecha_fin, 
                           fecha_fin_anticipada, monto_mensual, estado, multa_aplicada, 
                           id_usuario_creador, id_usuario_terminador, fecha_creacion, fecha_modificacion
                    FROM contrato 
                    WHERE id_contrato = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearContratoBasico(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener contrato por ID: {ex.Message}", ex);
            }
        }
        // ========================================
        // CONTINUACIÓN - MÉTODOS CRUD BÁSICOS
        // ========================================

        public async Task<Contrato?> ObtenerContratoConDetallesAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.id_contrato, c.id_inmueble, c.id_inquilino, c.id_propietario, 
                           c.fecha_inicio, c.fecha_fin, c.fecha_fin_anticipada, c.monto_mensual, 
                           c.estado, c.multa_aplicada, c.id_usuario_creador, c.id_usuario_terminador, 
                           c.fecha_creacion, c.fecha_modificacion,
                           -- Datos del inmueble
                           i.direccion AS inmueble_direccion, i.ambientes AS inmueble_ambientes,
                           i.precio AS inmueble_precio, i.uso AS inmueble_uso,
                           -- Datos del tipo de inmueble
                           ti.nombre AS tipo_inmueble_nombre,
                           -- Datos del inquilino
                           ui.dni AS inquilino_dni, ui.nombre AS inquilino_nombre, 
                           ui.apellido AS inquilino_apellido, ui.telefono AS inquilino_telefono,
                           ui.email AS inquilino_email,
                           -- Datos del propietario
                           up.dni AS propietario_dni, up.nombre AS propietario_nombre, 
                           up.apellido AS propietario_apellido, up.telefono AS propietario_telefono,
                           up.email AS propietario_email,
                           -- Datos del usuario creador
                           uc.nombre AS creador_nombre, uc.apellido AS creador_apellido,
                           -- Datos del usuario terminador
                           ut.nombre AS terminador_nombre, ut.apellido AS terminador_apellido
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    INNER JOIN tipo_inmueble ti ON i.id_tipo_inmueble = ti.id_tipo_inmueble
                    INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                    INNER JOIN usuario ui ON inq.id_usuario = ui.id_usuario
                    INNER JOIN propietario prop ON c.id_propietario = prop.id_propietario
                    INNER JOIN usuario up ON prop.id_usuario = up.id_usuario
                    INNER JOIN usuario uc ON c.id_usuario_creador = uc.id_usuario
                    LEFT JOIN usuario ut ON c.id_usuario_terminador = ut.id_usuario
                    WHERE c.id_contrato = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapearContratoCompleto(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener contrato con detalles: {ex.Message}", ex);
            }
        }

        // ========================================
        // PAGINACIÓN Y BÚSQUEDA PARA INDEX
        // ========================================

        public async Task<(IList<Contrato> contratos, int totalRegistros)> ObtenerConPaginacionYBusquedaAsync(
    int pagina, string buscar, string estado, string tipoContrato, int itemsPorPagina) // Agregar tipoContrato
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Construir WHERE dinámico
                var whereConditions = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(buscar))
                {
                    whereConditions.Add(@"(i.direccion LIKE @buscar 
                                  OR ui.nombre LIKE @buscar 
                                  OR ui.apellido LIKE @buscar 
                                  OR ui.dni LIKE @buscar
                                  OR up.nombre LIKE @buscar 
                                  OR up.apellido LIKE @buscar
                                  OR CONCAT(ui.apellido, ', ', ui.nombre) LIKE @buscar
                                  OR CONCAT(up.apellido, ', ', up.nombre) LIKE @buscar)");
                    parameters.Add(new MySqlParameter("@buscar", $"%{buscar}%"));
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    whereConditions.Add("c.estado = @estado");
                    parameters.Add(new MySqlParameter("@estado", estado));
                }

                // AGREGAR FILTRO POR TIPO DE CONTRATO
                if (!string.IsNullOrEmpty(tipoContrato))
                {
                    whereConditions.Add("c.tipo_contrato = @tipoContrato");
                    parameters.Add(new MySqlParameter("@tipoContrato", tipoContrato));
                }

                string whereClause = whereConditions.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereConditions)
                    : "";

                // 1. Contar total de registros
                string countQuery = $@"
            SELECT COUNT(*) 
            FROM contrato c
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario ui ON inq.id_usuario = ui.id_usuario
            INNER JOIN propietario prop ON c.id_propietario = prop.id_propietario
            INNER JOIN usuario up ON prop.id_usuario = up.id_usuario
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

                // 2. Obtener registros con paginación
                int offset = (pagina - 1) * itemsPorPagina;
                string dataQuery = $@"
            SELECT c.id_contrato, c.fecha_inicio, c.fecha_fin, c.monto_mensual, c.estado, 
                   c.tipo_contrato,  -- AGREGAR ESTA COLUMNA
                   i.direccion AS inmueble_direccion,
                   ui.nombre AS inquilino_nombre, ui.apellido AS inquilino_apellido,
                   up.nombre AS propietario_nombre, up.apellido AS propietario_apellido
            FROM contrato c
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario ui ON inq.id_usuario = ui.id_usuario
            INNER JOIN propietario prop ON c.id_propietario = prop.id_propietario
            INNER JOIN usuario up ON prop.id_usuario = up.id_usuario
            {whereClause}
            ORDER BY c.id_contrato DESC
            LIMIT @limit OFFSET @offset";

                var contratos = new List<Contrato>();
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
                        contratos.Add(MapearContratoParaIndex(reader));
                    }
                }

                return (contratos, totalRegistros);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener contratos con paginación: {ex.Message}", ex);
            }
        }

        // ========================================
        // MÉTODOS PRIVADOS DE MAPEO
        // ========================================

        private static Contrato MapearContratoBasico(System.Data.Common.DbDataReader reader)
        {
            return new Contrato
            {
                IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                IdInquilino = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                FechaFinAnticipada = reader.IsDBNull(reader.GetOrdinal("fecha_fin_anticipada"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("fecha_fin_anticipada")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                MultaAplicada = reader.GetDecimal(reader.GetOrdinal("multa_aplicada")),
                IdUsuarioCreador = reader.GetInt32(reader.GetOrdinal("id_usuario_creador")),
                IdUsuarioTerminador = reader.IsDBNull(reader.GetOrdinal("id_usuario_terminador"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("id_usuario_terminador")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                FechaModificacion = reader.GetDateTime(reader.GetOrdinal("fecha_modificacion"))
            };
        }

        private static Contrato MapearContratoCompleto(System.Data.Common.DbDataReader reader)
        {
            var contrato = MapearContratoBasico(reader);

            // Mapear inmueble
            contrato.Inmueble = new Inmueble
            {
                Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                Ambientes = reader.GetInt32(reader.GetOrdinal("inmueble_ambientes")),
                Precio = reader.GetDecimal(reader.GetOrdinal("inmueble_precio")),
                Uso = reader.GetString(reader.GetOrdinal("inmueble_uso")),
                TipoInmueble = new TipoInmueble
                {
                    Nombre = reader.GetString(reader.GetOrdinal("tipo_inmueble_nombre"))
                }
            };

            // Mapear inquilino
            contrato.Inquilino = new Inquilino
            {
                Usuario = new Usuario
                {
                    Dni = reader.GetString(reader.GetOrdinal("inquilino_dni")),
                    Nombre = reader.GetString(reader.GetOrdinal("inquilino_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("inquilino_apellido")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("inquilino_telefono"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("inquilino_telefono")),
                    Email = reader.IsDBNull(reader.GetOrdinal("inquilino_email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("inquilino_email"))
                }
            };

            // Mapear propietario
            contrato.Propietario = new Propietario
            {
                Usuario = new Usuario
                {
                    Dni = reader.GetString(reader.GetOrdinal("propietario_dni")),
                    Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("propietario_telefono"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("propietario_telefono")),
                    Email = reader.IsDBNull(reader.GetOrdinal("propietario_email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("propietario_email"))
                }
            };

            // Mapear usuario creador
            contrato.UsuarioCreador = new Usuario
            {
                Nombre = reader.GetString(reader.GetOrdinal("creador_nombre")),
                Apellido = reader.GetString(reader.GetOrdinal("creador_apellido"))
            };

            // Mapear usuario terminador (puede ser null)
            if (!reader.IsDBNull(reader.GetOrdinal("terminador_nombre")))
            {
                contrato.UsuarioTerminador = new Usuario
                {
                    Nombre = reader.GetString(reader.GetOrdinal("terminador_nombre")),
                    Apellido = reader.GetString(reader.GetOrdinal("terminador_apellido"))
                };
            }

            return contrato;
        }

        private static Contrato MapearContratoParaIndex(System.Data.Common.DbDataReader reader)
        {
            return new Contrato
            {
                IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                TipoContrato = reader.GetString(reader.GetOrdinal("tipo_contrato")),
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
                },
                Propietario = new Propietario
                {
                    Usuario = new Usuario
                    {
                        Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido"))
                    }
                }
            };
        }
        // ========================================
        // MÉTODOS DE VALIDACIÓN ESPECÍFICOS DE NEGOCIO
        // ========================================

        public async Task<bool> ExisteInmuebleDisponibleAsync(int idInmueble, DateTime fechaInicio, DateTime fechaFin, int idContratoExcluir = 0)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Verificar que el inmueble existe y está disponible o alquilado (para edición)
                string queryInmueble = "SELECT estado FROM inmueble WHERE id_inmueble = @id_inmueble";
                using var commandInmueble = new MySqlCommand(queryInmueble, connection);
                commandInmueble.Parameters.AddWithValue("@id_inmueble", idInmueble);

                var estadoInmueble = (await commandInmueble.ExecuteScalarAsync())?.ToString();
                if (estadoInmueble == null || estadoInmueble == "inactivo")
                {
                    return false; // Inmueble no existe o está inactivo
                }

                // Verificar conflictos de fechas con otros contratos vigentes
                string queryConflictos = @"
                    SELECT COUNT(*) 
                    FROM contrato 
                    WHERE id_inmueble = @id_inmueble 
                    AND estado = 'vigente'
                    AND id_contrato != @id_contrato_excluir
                    AND ((@fecha_inicio BETWEEN fecha_inicio AND fecha_fin)
                         OR (@fecha_fin BETWEEN fecha_inicio AND fecha_fin)
                         OR (fecha_inicio BETWEEN @fecha_inicio AND @fecha_fin))";

                using var commandConflictos = new MySqlCommand(queryConflictos, connection);
                commandConflictos.Parameters.AddWithValue("@id_inmueble", idInmueble);
                commandConflictos.Parameters.AddWithValue("@fecha_inicio", fechaInicio);
                commandConflictos.Parameters.AddWithValue("@fecha_fin", fechaFin);
                commandConflictos.Parameters.AddWithValue("@id_contrato_excluir", idContratoExcluir);

                var conflictos = Convert.ToInt32(await commandConflictos.ExecuteScalarAsync());
                return conflictos == 0; // Retorna true si no hay conflictos
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al verificar disponibilidad del inmueble: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExisteInquilinoActivoAsync(int idInquilino)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT COUNT(*) FROM inquilino inq 
                    INNER JOIN usuario u ON inq.id_usuario = u.id_usuario 
                    WHERE inq.id_inquilino = @id AND inq.estado = 1 AND u.estado = 'activo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", idInquilino);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al verificar inquilino activo: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistePropietarioActivoAsync(int idPropietario)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT COUNT(*) FROM propietario p 
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
                    WHERE p.id_propietario = @id AND p.estado = 1 AND u.estado = 'activo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", idPropietario);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al verificar propietario activo: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExisteUsuarioActivoAsync(int idUsuario)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM usuario WHERE id_usuario = @id AND estado = 'activo'";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", idUsuario);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al verificar usuario activo: {ex.Message}", ex);
            }
        }

        // ========================================
        // MÉTODOS AUXILIARES PARA DROPDOWNS
        // ========================================

        public async Task<List<Inmueble>> ObtenerInmueblesDisponiblesAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inmuebles = new List<Inmueble>();

                string query = @"
                    SELECT i.id_inmueble, i.direccion, i.precio, i.ambientes, i.uso,
                           t.nombre AS tipo_nombre,
                           u.nombre AS propietario_nombre, u.apellido AS propietario_apellido
                    FROM inmueble i
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    INNER JOIN propietario p ON i.id_propietario = p.id_propietario
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                    WHERE i.estado IN ('disponible', 'alquilado') 
                    AND p.estado = 1 AND u.estado = 'activo'
                    ORDER BY i.direccion
                    LIMIT 100";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    inmuebles.Add(new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        Ambientes = reader.GetInt32(reader.GetOrdinal("ambientes")),
                        Uso = reader.GetString(reader.GetOrdinal("uso")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_nombre"))
                        },
                        Propietario = new Propietario
                        {
                            Usuario = new Usuario
                            {
                                Nombre = reader.GetString(reader.GetOrdinal("propietario_nombre")),
                                Apellido = reader.GetString(reader.GetOrdinal("propietario_apellido"))
                            }
                        }
                    });
                }

                return inmuebles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener inmuebles disponibles: {ex.Message}", ex);
            }
        }

        public async Task<List<Inquilino>> ObtenerInquilinosActivosAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inquilinos = new List<Inquilino>();

                string query = @"
                    SELECT inq.id_inquilino, u.nombre, u.apellido, u.dni, u.telefono, u.email
                    FROM inquilino inq
                    INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                    WHERE inq.estado = 1 AND u.estado = 'activo'
                    ORDER BY u.apellido, u.nombre
                    LIMIT 100";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    inquilinos.Add(new Inquilino
                    {
                        IdInquilino = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                        Usuario = new Usuario
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                            Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                            Dni = reader.GetString(reader.GetOrdinal("dni")),
                            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("telefono")),
                            Email = reader.IsDBNull(reader.GetOrdinal("email"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("email"))
                        }
                    });
                }

                return inquilinos;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener inquilinos activos: {ex.Message}", ex);
            }
        }

        public async Task<List<Propietario>> ObtenerPropietariosActivosAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var propietarios = new List<Propietario>();

                string query = @"
                    SELECT p.id_propietario, u.nombre, u.apellido, u.dni, u.telefono, u.email
                    FROM propietario p
                    INNER JOIN usuario u ON p.id_usuario = u.id_usuario
                    WHERE p.estado = 1 AND u.estado = 'activo'
                    ORDER BY u.apellido, u.nombre
                    LIMIT 100";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    propietarios.Add(new Propietario
                    {
                        IdPropietario = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                        Usuario = new Usuario
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                            Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                            Dni = reader.GetString(reader.GetOrdinal("dni")),
                            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("telefono")),
                            Email = reader.IsDBNull(reader.GetOrdinal("email"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("email"))
                        }
                    });
                }

                return propietarios;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener propietarios activos: {ex.Message}", ex);
            }
        }

        public async Task<List<Usuario>> ObtenerUsuariosActivosAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var usuarios = new List<Usuario>();

                string query = @"
                    SELECT id_usuario, nombre, apellido, dni, rol
                    FROM usuario 
                    WHERE estado = 'activo' 
                    ORDER BY apellido, nombre
                    LIMIT 100";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    usuarios.Add(new Usuario
                    {
                        IdUsuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                        Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        Apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        Dni = reader.GetString(reader.GetOrdinal("dni")),
                        Rol = reader.GetString(reader.GetOrdinal("rol"))
                    });
                }

                return usuarios;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener usuarios activos: {ex.Message}", ex);
            }
        }

        // MÉTODOS DE CONSULTA ESPECÍFICOS
        // ========================================

        public async Task<List<Inmueble>> ObtenerInmueblesAsync(bool soloDisponibles = false)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inmuebles = new List<Inmueble>();

                string whereClause = soloDisponibles
                    ? "WHERE i.estado = 'disponible'"
                    : "WHERE i.estado != 'inactivo'";

                string query = $@"
                    SELECT i.id_inmueble, i.direccion, i.precio, i.ambientes, i.uso, i.estado,
                           t.nombre AS tipo_nombre
                    FROM inmueble i
                    INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
                    {whereClause}
                    ORDER BY i.direccion";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    inmuebles.Add(new Inmueble
                    {
                        IdInmueble = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        Direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        Ambientes = reader.GetInt32(reader.GetOrdinal("ambientes")),
                        Uso = reader.GetString(reader.GetOrdinal("uso")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        TipoInmueble = new TipoInmueble
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("tipo_nombre"))
                        }
                    });
                }

                return inmuebles;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener inmuebles: {ex.Message}", ex);
            }
        }

        public async Task<bool> InmuebleTieneContratosVigentesAsync(int idInmueble)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM contrato WHERE id_inmueble = @id AND estado = 'vigente'";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", idInmueble);

                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al verificar contratos vigentes del inmueble: {ex.Message}", ex);
            }
        }

        public async Task<List<Contrato>> ObtenerContratosVigentesPorInquilinoAsync(int idInquilino)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var contratos = new List<Contrato>();

                string query = @"
                    SELECT c.id_contrato, c.fecha_inicio, c.fecha_fin, c.monto_mensual, c.estado,
                           i.direccion AS inmueble_direccion
                    FROM contrato c
                    INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                    WHERE c.id_inquilino = @id_inquilino AND c.estado = 'vigente'
                    ORDER BY c.fecha_inicio DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_inquilino", idInquilino);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    contratos.Add(new Contrato
                    {
                        IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                        FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                        FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                        MontoMensual = reader.GetDecimal(reader.GetOrdinal("monto_mensual")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        Inmueble = new Inmueble
                        {
                            Direccion = reader.GetString(reader.GetOrdinal("inmueble_direccion"))
                        }
                    });
                }

                return contratos;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener contratos vigentes del inquilino: {ex.Message}", ex);
            }
        }

        public async Task<List<Contrato>> ObtenerContratosPorPropietarioAsync(int idPropietario)
        {
            try
            {
                Console.WriteLine($"=== REPOSITORIO: Buscando contratos para propietario {idPropietario} ===");

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var contratos = new List<Contrato>();

                string query = @"
            SELECT c.id_contrato, c.fecha_inicio, c.fecha_fin, c.monto_mensual, c.estado,
                   i.id_inmueble, i.direccion AS inmueble_direccion, i.id_propietario,
                   u.id_usuario, u.nombre AS inquilino_nombre, u.apellido AS inquilino_apellido
            FROM contrato c
            INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
            INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
            INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
            WHERE i.id_propietario = @id_propietario
            ORDER BY c.fecha_inicio DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_propietario", idPropietario);

                Console.WriteLine($"=== REPOSITORIO: Ejecutando query ===");

                using var reader = await command.ExecuteReaderAsync();
                int count = 0;

                while (await reader.ReadAsync())
                {
                    count++;
                    // ... resto del código
                }

                Console.WriteLine($"=== REPOSITORIO: Se encontraron {count} contratos ===");
                return contratos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN REPOSITORIO: {ex.Message} ===");
                throw new Exception($"Error al obtener contratos del propietario: {ex.Message}", ex);
            }
        }


        // MÉTODOS ESPECÍFICOS PARA AUTOCOMPLETADO EN CONTRATOS

        /// <summary>
        /// Busca inmuebles DISPONIBLES para autocompletado (3+ caracteres)
        /// </summary>
        public async Task<List<dynamic>> BuscarInmueblesParaContratoAsync(string termino, int limite = 10)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inmuebles = new List<dynamic>();

                string query = @"
            SELECT i.id_inmueble, i.direccion, i.precio, i.id_propietario,
                   t.nombre as tipo_nombre,
                   u.nombre as propietario_nombre, u.apellido as propietario_apellido, u.dni as propietario_dni
            FROM inmueble i
            INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
            INNER JOIN propietario p ON i.id_propietario = p.id_propietario
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario
            WHERE i.estado = 'disponible' 
            AND (i.direccion LIKE @termino 
                 OR t.nombre LIKE @termino 
                 OR u.nombre LIKE @termino
                 OR u.apellido LIKE @termino
                 OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino)
            ORDER BY i.direccion
            LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                Console.WriteLine(command.CommandText); // Debug: Ver el comando SQL

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    inmuebles.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        texto = $"{reader.GetString(reader.GetOrdinal("direccion"))} - {reader.GetString(reader.GetOrdinal("tipo_nombre"))} ({reader.GetDecimal(reader.GetOrdinal("precio")):C})",
                        direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        propietarioId = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                        propietarioNombre = $"{reader.GetString(reader.GetOrdinal("propietario_apellido"))}, {reader.GetString(reader.GetOrdinal("propietario_nombre"))}",
                        propietarioDni = reader.GetString(reader.GetOrdinal("propietario_dni"))
                    });
                    Console.WriteLine($"Found inmueble: {inmuebles[^1].texto}"); // Debug: Ver cada inmueble encontrado
                }

                return inmuebles;
            }
            catch
            {
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Busca propietarios para autocompletado (3+ caracteres)
        /// </summary>
        public async Task<List<dynamic>> BuscarPropietariosParaContratoAsync(string termino, int limite = 10)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var propietarios = new List<dynamic>();

                string query = @"
            SELECT p.id_propietario, u.nombre, u.apellido, u.dni
            FROM propietario p
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario
            WHERE p.estado = true AND u.estado = 'activo'
            AND (u.nombre LIKE @termino 
                 OR u.apellido LIKE @termino 
                 OR u.dni LIKE @termino
                 OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino)
            ORDER BY u.apellido, u.nombre
            LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    propietarios.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                        texto = $"{reader.GetString(reader.GetOrdinal("apellido"))}, {reader.GetString(reader.GetOrdinal("nombre"))} - DNI: {reader.GetString(reader.GetOrdinal("dni"))}",
                        nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        dni = reader.GetString(reader.GetOrdinal("dni"))
                    });
                }

                return propietarios;
            }
            catch
            {
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Busca inquilinos para autocompletado (3+ caracteres)
        /// </summary>
        public async Task<List<dynamic>> BuscarInquilinosParaContratoAsync(string termino, int limite = 10)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inquilinos = new List<dynamic>();

                string query = @"
            SELECT inq.id_inquilino, u.nombre, u.apellido, u.dni
            FROM inquilino inq
            INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
            WHERE inq.estado = true AND u.estado = 'activo'
            AND (u.nombre LIKE @termino 
                 OR u.apellido LIKE @termino 
                 OR u.dni LIKE @termino
                 OR CONCAT(u.nombre, ' ', u.apellido) LIKE @termino)
            ORDER BY u.apellido, u.nombre
            LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@termino", $"%{termino}%");
                command.Parameters.AddWithValue("@limite", limite);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    inquilinos.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id_inquilino")),
                        texto = $"{reader.GetString(reader.GetOrdinal("apellido"))}, {reader.GetString(reader.GetOrdinal("nombre"))} - DNI: {reader.GetString(reader.GetOrdinal("dni"))}",
                        nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        dni = reader.GetString(reader.GetOrdinal("dni"))
                    });
                }

                return inquilinos;
            }
            catch
            {
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Obtiene SOLO inmuebles DISPONIBLES de un propietario específico
        /// </summary>
        public async Task<List<dynamic>> ObtenerInmueblesPorPropietarioAsync(int propietarioId, int limite = 15)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var inmuebles = new List<dynamic>();

                // CONSULTA CON DEBUG INFORMACIÓN
                string query = @"
            SELECT i.id_inmueble, i.direccion, i.precio, i.estado,
                   t.nombre as tipo_nombre,
                   p.id_propietario,
                   u.nombre as propietario_nombre, u.apellido as propietario_apellido
            FROM inmueble i
            INNER JOIN tipo_inmueble t ON i.id_tipo_inmueble = t.id_tipo_inmueble
            INNER JOIN propietario p ON i.id_propietario = p.id_propietario
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario
            WHERE i.id_propietario = @propietarioId 
            ORDER BY i.direccion
            LIMIT @limite";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@propietarioId", propietarioId);
                command.Parameters.AddWithValue("@limite", limite);

                Console.WriteLine($"=== DEBUG INMUEBLES POR PROPIETARIO ===");
                Console.WriteLine($"Propietario ID: {propietarioId}");
                Console.WriteLine($"Límite: {limite}");
                Console.WriteLine($"Query: {query}");

                using var reader = await command.ExecuteReaderAsync();
                int contador = 0;

                while (await reader.ReadAsync())
                {
                    contador++;
                    var inmueble = new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id_inmueble")),
                        texto = $"{reader.GetString(reader.GetOrdinal("direccion"))} - {reader.GetString(reader.GetOrdinal("tipo_nombre"))} ({reader.GetDecimal(reader.GetOrdinal("precio")):C})",
                        direccion = reader.GetString(reader.GetOrdinal("direccion")),
                        precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                        estado = reader.GetString(reader.GetOrdinal("estado")) // AGREGAMOS ESTADO PARA DEBUG
                    };

                    Console.WriteLine($"Inmueble {contador}: ID={inmueble.id}, Dir={inmueble.direccion}, Estado={inmueble.estado}");

                    // FILTRAR SOLO DISPONIBLES AQUÍ (para debug)
                    if (reader.GetString(reader.GetOrdinal("estado")) == "disponible")
                    {
                        inmuebles.Add(inmueble);
                        Console.WriteLine($"  -> AGREGADO (disponible)");
                    }
                    else
                    {
                        Console.WriteLine($"  -> OMITIDO (estado: {reader.GetString(reader.GetOrdinal("estado"))})");
                    }
                }

                Console.WriteLine($"Total encontrados: {contador}");
                Console.WriteLine($"Total disponibles: {inmuebles.Count}");
                Console.WriteLine($"=== FIN DEBUG ===");

                return inmuebles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en ObtenerInmueblesPorPropietarioAsync: {ex.Message}");
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Obtiene el propietario de un inmueble específico
        /// </summary>
        public async Task<dynamic?> ObtenerPropietarioDeInmuebleAsync(int inmuebleId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            SELECT p.id_propietario, u.nombre, u.apellido, u.dni
            FROM inmueble i
            INNER JOIN propietario p ON i.id_propietario = p.id_propietario
            INNER JOIN usuario u ON p.id_usuario = u.id_usuario
            WHERE i.id_inmueble = @inmuebleId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@inmuebleId", inmuebleId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id_propietario")),
                        texto = $"{reader.GetString(reader.GetOrdinal("apellido"))}, {reader.GetString(reader.GetOrdinal("nombre"))} - DNI: {reader.GetString(reader.GetOrdinal("dni"))}",
                        nombre = reader.GetString(reader.GetOrdinal("nombre")),
                        apellido = reader.GetString(reader.GetOrdinal("apellido")),
                        dni = reader.GetString(reader.GetOrdinal("dni"))
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
        //para eliminar inmueble de propietario
        public async Task<bool> TieneContratosActivosAsync(int idInmueble)
{
    using var connection = new MySqlConnection(_connectionString);
    await connection.OpenAsync();
    
    var sql = @"
        SELECT COUNT(*) 
        FROM contrato 
        WHERE id_inmueble = @IdInmueble 
        AND estado = 'activo' 
        AND fecha_inicio <= CURDATE() 
        AND fecha_fin >= CURDATE()";
    
    using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@IdInmueble", idInmueble);
    
    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
    
    return count > 0;
}

    }
}



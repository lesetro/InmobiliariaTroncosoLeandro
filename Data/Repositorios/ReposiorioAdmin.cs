using Inmobiliaria_troncoso_leandro.Data.Interfaces;
using Inmobiliaria_troncoso_leandro.Models;
using MySql.Data.MySqlClient;

namespace Inmobiliaria_troncoso_leandro.Data.Repositorios
{
    public class RepositorioAdmin : IRepositorioAdmin
    {
        private readonly string _connectionString;
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly IRepositorioPropietario _repositorioPropietario;
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioInteresInmueble _repositorioInteres;

        public RepositorioAdmin(IConfiguration configuration,
                               IRepositorioUsuario repositorioUsuario,
                               IRepositorioPropietario repositorioPropietario,
                               IRepositorioInmueble repositorioInmueble,
                               IRepositorioInteresInmueble repositorioInteres)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new ArgumentNullException(nameof(configuration));
            _repositorioUsuario = repositorioUsuario;
            _repositorioPropietario = repositorioPropietario;
            _repositorioInmueble = repositorioInmueble;
            _repositorioInteres = repositorioInteres;
        }

        // Dashboard principal
        public async Task<AdminDashboardDto> GetDashboardDataAsync()
        {
            var dashboard = new AdminDashboardDto
            {
                Usuarios = new UsuarioStatsDto
                {
                    Total = await GetTotalUsuariosAsync(),
                    PorRol = await GetUsuariosPorRolAsync()
                },
                Propietarios = new PropietarioStatsDto
                {
                    Total = await GetTotalPropietariosActivosAsync()
                },
                Inquilinos = new InquilinoStatsDto
                {
                    Total = await GetTotalInquilinosActivosAsync()
                },
                Inmuebles = new InmuebleStatsDto
                {
                    Total = await GetTotalInmueblesAsync(),
                    Disponibles = await GetInmueblesDisponiblesAsync()
                },
                Contratos = new ContratoStatsDto
                {
                    Total = await GetTotalContratosAsync(),
                    Vigentes = await GetContratosVigentesAsync(),
                    Recientes = await GetContratosRecientesAsync(5)
                },
                Intereses = new InteresStatsDto
                {
                    Total = await GetTotalInteresesAsync(),
                    Pendientes = await GetInteresesPendientesAsync(),
                    UltimosIntereses = await GetInteresesRecientesAsync(5)
                }
            };

            return dashboard;
        }

        // Estadísticas de usuarios
        public async Task<int> GetTotalUsuariosAsync()
        {
            return await _repositorioUsuario.GetTotalUsuariosAsync();
        }

        public async Task<Dictionary<string, int>> GetUsuariosPorRolAsync()
        {
            return await _repositorioUsuario.GetEstadisticasPorRolAsync();
        }

        public async Task<int> GetTotalPropietariosActivosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT COUNT(*) 
                FROM propietario p 
                INNER JOIN usuario u ON p.id_usuario = u.id_usuario 
                WHERE u.estado = 'activo'";

            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetTotalInquilinosActivosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT COUNT(*) 
                FROM inquilino i 
                INNER JOIN usuario u ON i.id_usuario = u.id_usuario 
                WHERE u.estado = 'activo'";

            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Estadísticas de inmuebles
        public async Task<int> GetTotalInmueblesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM inmueble WHERE estado != 'inactivo'";
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetInmueblesDisponiblesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM inmueble WHERE estado = 'disponible'";
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetInmueblesOcupadosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM inmueble WHERE estado IN ('alquilado', 'vendido')";
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Estadísticas de contratos
        public async Task<int> GetTotalContratosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM contrato";
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetContratosVigentesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT COUNT(*) 
                FROM contrato 
                WHERE fecha_fin >= CURDATE() AND estado = 'vigente'";

            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetContratosFinalizadosAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT COUNT(*) 
                FROM contrato 
                WHERE fecha_fin < CURDATE() OR estado = 'finalizado'";

            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Estadísticas de intereses
        public async Task<int> GetTotalInteresesAsync()
        {
            return await _repositorioInteres.GetTotalInteresesAsync();
        }

        public async Task<int> GetInteresesPendientesAsync()
        {
            return await _repositorioInteres.GetInteresesPendientesCountAsync();
        }

        public async Task<int> GetInteresesContactadosAsync()
        {
            return await _repositorioInteres.ContarInteresesContactadosAsync();
        }

        public async Task<int> GetInteresesRecientesAsync()
        {
            return await _repositorioInteres.GetInteresesSemanaAsync();
        }

        // Datos recientes para dashboard
        public async Task<IEnumerable<ContratoRecenteDto>> GetContratosRecientesAsync(int limite = 5)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT c.id_contrato, c.fecha_inicio, c.fecha_fin, c.estado,
                       i.direccion as inmueble_direccion,
                       CONCAT(u.nombre, ' ', u.apellido) as inquilino_nombre
                FROM contrato c
                INNER JOIN inmueble i ON c.id_inmueble = i.id_inmueble
                INNER JOIN inquilino inq ON c.id_inquilino = inq.id_inquilino
                INNER JOIN usuario u ON inq.id_usuario = u.id_usuario
                WHERE c.fecha_fin >= CURDATE()
                ORDER BY c.fecha_inicio DESC
                LIMIT @limite";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@limite", limite);

            using var reader = await command.ExecuteReaderAsync();
            var contratos = new List<ContratoRecenteDto>();

            while (await reader.ReadAsync())
            {
                contratos.Add(new ContratoRecenteDto
                {
                    IdContrato = reader.GetInt32(reader.GetOrdinal("id_contrato")),
                    FechaInicio = reader.GetDateTime(reader.GetOrdinal("fecha_inicio")),
                    FechaFin = reader.GetDateTime(reader.GetOrdinal("fecha_fin")),
                    Estado = reader.GetString(reader.GetOrdinal("estado")),
                    InmuebleDireccion = reader.GetString(reader.GetOrdinal("inmueble_direccion")),
                    InquilinoNombre = reader.GetString(reader.GetOrdinal("inquilino_nombre"))
                });
            }

            return contratos;
        }

        public async Task<IEnumerable<InteresRecenteDto>> GetInteresesRecientesAsync(int limite = 5)
        {
            var interesesRecientes = await _repositorioInteres.ObtenerInteresesRecientesAsync(limite);

            return interesesRecientes.Select(i => new InteresRecenteDto
            {
                IdInteres = i.IdInteres,
                Nombre = i.Nombre,
                Email = i.Email,
                Telefono = i.Telefono ?? "",
                FechaInteres = i.Fecha, 
                Contactado = i.Contactado,
                InmuebleDireccion = i.Inmueble?.Direccion ?? "Sin dirección",
                DiasDesdeInteres = $"{(DateTime.Now - i.Fecha).Days} días"
            });
        }

        public async Task<IEnumerable<UsuarioRecenteDto>> GetUsuariosRecientesAsync(int limite = 5)
        {
            var usuariosRecientes = await _repositorioUsuario.GetUsuariosRecientesAsync(limite);

            return usuariosRecientes.Select(u => new UsuarioRecenteDto
            {
                IdUsuario = u.IdUsuario,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email,
                Rol = u.Rol,
                Estado = u.Estado
            });
        }

        // Métodos no implementados (retornan datos vacíos)
        public async Task<Dictionary<string, int>> GetInteresesPorMesAsync(int año)
        {
            await Task.Delay(1);
            return new Dictionary<string, int>();
        }

        public async Task<Dictionary<string, int>> GetContratosPorMesAsync(int año)
        {
            await Task.Delay(1);
            return new Dictionary<string, int>();
        }

        public async Task<Dictionary<string, decimal>> GetIngresosPorMesAsync(int año)
        {
            await Task.Delay(1);
            return new Dictionary<string, decimal>();
        }

        public async Task<int> GetContratosProximosVencerAsync(int dias = 30)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT COUNT(*) 
                FROM contrato 
                WHERE fecha_fin BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL @dias DAY)
                AND estado = 'vigente'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@dias", dias);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetPagosVencidosAsync()
        {
            await Task.Delay(1);
            return 0; // Implementar cuando tengas tabla de pagos
        }

        public async Task<IEnumerable<AlertaDto>> GetAlertasDelSistemaAsync()
        {
            await Task.Delay(1);
            return new List<AlertaDto>();
        }

        public async Task<ReporteGeneralDto> GetReporteGeneralAsync()
        {
            await Task.Delay(1);
            return new ReporteGeneralDto();
        }

        public async Task<Dictionary<string, object>> GetEstadisticasComparativasAsync()
        {
            await Task.Delay(1);
            return new Dictionary<string, object>();
        }
    }
}
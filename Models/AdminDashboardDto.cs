using System.Collections.Generic;

namespace Inmobiliaria_troncoso_leandro.Models
{
    public class AdminDashboardDto
    {
        public UsuarioStatsDto Usuarios { get; set; } = new();
        public PropietarioStatsDto Propietarios { get; set; } = new();
        public InquilinoStatsDto Inquilinos { get; set; } = new();
        public InmuebleStatsDto Inmuebles { get; set; } = new();
        public ContratoStatsDto Contratos { get; set; } = new();
        public InteresStatsDto Intereses { get; set; } = new();
    }

    public class UsuarioStatsDto
    {
        public int Total { get; set; }
        public Dictionary<string, int> PorRol { get; set; } = new();
    }

    public class PropietarioStatsDto
    {
        public int Total { get; set; }
        public int Activos { get; set; }
    }

    public class InquilinoStatsDto
    {
        public int Total { get; set; }
        public int Activos { get; set; }
    }

    public class InmuebleStatsDto
    {
        public int Total { get; set; }
        public int Disponibles { get; set; }
        public int Ocupados { get; set; }
    }

    public class ContratoStatsDto
    {
        public int Total { get; set; }
        public int Vigentes { get; set; }
        public int Finalizados { get; set; }
        public IEnumerable<ContratoRecenteDto> Recientes { get; set; } = new List<ContratoRecenteDto>();
    }

    public class InteresStatsDto
    {
        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int Contactados { get; set; }
        public IEnumerable<InteresRecenteDto> UltimosIntereses { get; set; } = new List<InteresRecenteDto>();
    }

    public class ContratoRecenteDto
    {
        public int IdContrato { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string InmuebleDireccion { get; set; } = string.Empty;
        public string InquilinoNombre { get; set; } = string.Empty;
    }

    public class InteresRecenteDto
    {
        public int IdInteres { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public DateTime FechaInteres { get; set; }
        public bool Contactado { get; set; }
        public string InmuebleDireccion { get; set; } = string.Empty;
        public string DiasDesdeInteres { get; set; } = string.Empty;
    }

    public class UsuarioRecenteDto
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class AlertaDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Prioridad { get; set; } = string.Empty;
    }

    public class ReporteGeneralDto
    {
        public Dictionary<string, object> Datos { get; set; } = new();
    }
}
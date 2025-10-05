namespace Inmobiliaria_troncoso_leandro.Models.DTOs
{
    public class ContratoAlquilerBusqueda
    {
        public int IdContrato { get; set; }
        public int IdInmueble { get; set; }
        public string? InmuebleDireccion { get; set; } = string.Empty;
        public string? InquilinoNombre { get; set; } = string.Empty;
        public string? InquilinoApellido { get; set; } = string.Empty;
        public string? PropietarioNombre { get; set; } = string.Empty;
        public string? PropietarioApellido { get; set; } = string.Empty;
        public decimal MontoMensual { get; set; }
        public decimal MontoDiarioMora { get; set; } = 50m;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalMeses { get; set; }
        public int ProximoNumeroPago { get; set; }


        // PROPIEDADES CALCULADAS
        public string Texto => $"Contrato #{IdContrato} - {InmuebleDireccion}";
        public string MontoFormateado => $"${MontoMensual:N2}/mes";
        public string InquilinoCompleto => $"{InquilinoApellido}, {InquilinoNombre}";
        public string PropietarioCompleto => $"{PropietarioApellido}, {PropietarioNombre}";
        public string TextoConPagos => $"Contrato #{IdContrato} - {InmuebleDireccion} (Pagos: {ProximoNumeroPago - 1}/{TotalMeses})"; 
        public string InfoPagos => $"{ProximoNumeroPago - 1}/{TotalMeses}";
    }
}
namespace Inmobiliaria_troncoso_leandro.Models.DTOs
{
    /// <summary>
    /// DTO con todos los datos necesarios para crear un pago de alquiler
    /// </summary>
    public class DatosContratoParaPago
    {
        public int IdContrato { get; set; }
        public int IdInmueble { get; set; }
        public string InmuebleDireccion { get; set; } = string.Empty;
        public string InquilinoNombreCompleto { get; set; } = string.Empty;
        public string PropietarioNombreCompleto { get; set; } = string.Empty;
        public decimal MontoMensual { get; set; }
        public decimal MontoDiarioMora { get; set; }
        public int ProximoNumeroPago { get; set; }
        public DateTime ProximaFechaVencimiento { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalMeses { get; set; }
        public int PagosRealizados { get; set; }
        
        // Propiedades calculadas
        public string InfoPagos => $"{PagosRealizados}/{TotalMeses}";
        public bool PermiteMasPagos => ProximoNumeroPago <= TotalMeses;
        
        // Para el concepto del pago
        public string PeriodoPago 
        { 
            get 
            {
                if (ProximoNumeroPago > 0)
                {
                    var fechaPeriodo = FechaInicio.AddMonths(ProximoNumeroPago - 1);
                    return fechaPeriodo.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
                }
                return "";
            }
        }
        
        public int PeriodoAño 
        { 
            get 
            {
                if (ProximoNumeroPago > 0)
                {
                    var fechaPeriodo = FechaInicio.AddMonths(ProximoNumeroPago - 1);
                    return fechaPeriodo.Year;
                }
                return DateTime.Now.Year;
            }
        }
        
        public int PeriodoMes 
        { 
            get 
            {
                if (ProximoNumeroPago > 0)
                {
                    var fechaPeriodo = FechaInicio.AddMonths(ProximoNumeroPago - 1);
                    return fechaPeriodo.Month;
                }
                return DateTime.Now.Month;
            }
        }
        
        public string Concepto => $"Alquiler {PeriodoPago} - {InmuebleDireccion}";
    }
}
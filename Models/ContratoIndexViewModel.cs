using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria_troncoso_leandro.Models
{
    public class ContratoIndexViewModel
    {
        // IDENTIFICACIÓN
        public int Id { get; set; }
        public string TipoContrato { get; set; } = ""; // "alquiler" o "venta"
        
        // INMUEBLE
        public string? DireccionInmueble { get; set; }
        
        // PERSONAS
        public string? NombreCliente { get; set; }
        public string? ApellidoCliente { get; set; }
        public string? DniCliente { get; set; }
        public string? NombrePropietario { get; set; }
        public string? ApellidoPropietario { get; set; }
        public string? DniPropietario { get; set; }
        
        // FECHAS
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public DateTime? FechaEscrituracion { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        
        // MONETARIO
        public decimal MontoPrincipal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoSeña { get; set; }
        public decimal MultaAplicada { get; set; }
        
        // ESTADO
        public string Estado { get; set; } = "";
        public string? EstadoDescripcion { get; set; }
        
        // PROPIEDADES CALCULADAS
        public decimal PorcentajePagado => MontoPrincipal > 0 ? (MontoPagado / MontoPrincipal) * 100 : 0;
        public decimal SaldoPendiente => MontoPrincipal - MontoPagado;
        
        public string EstadoBadgeClass 
        { 
            get 
            {
                return Estado switch
                {
                    "vigente" => "bg-success",
                    "finalizado" => "bg-secondary",
                    "finalizado_anticipado" => "bg-warning text-dark",
                    "seña_pendiente" => "bg-warning text-dark",
                    "seña_pagada" => "bg-info text-white", 
                    "en_proceso" => "bg-primary text-white",
                    "pendiente_escritura" => "bg-success text-white",
                    "escriturada" => "bg-dark text-white",
                    "cancelada" => "bg-danger text-white",
                    _ => "bg-secondary text-white"
                };
            }
        }
        
        public string EstadoTexto
        {
            get
            {
                return Estado switch
                {
                    "vigente" => "VIGENTE",
                    "finalizado" => "FINALIZADO", 
                    "finalizado_anticipado" => "FINALIZADO ANTICIPADO",
                    "seña_pendiente" => "SEÑA PENDIENTE",
                    "seña_pagada" => "SEÑA PAGADA",
                    "en_proceso" => "EN PROCESO",
                    "pendiente_escritura" => "PENDIENTE ESCRITURA", 
                    "escriturada" => "ESCRITURADA",
                    "cancelada" => "CANCELADA",
                    _ => Estado.ToUpper()
                };
            }
        }
    }
}
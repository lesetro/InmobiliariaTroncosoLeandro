using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria_troncoso_leandro.Models
{
    /// <summary>
    /// Clase para representar resultados de búsqueda de contratos de venta
    /// </summary>
    public class SearchResultVenta
    {
        /// <summary>
        /// ID del contrato de venta
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// ID del inmueble
        /// </summary>
        public string IdInmueble { get; set; } = string.Empty;

        /// <summary>
        /// Texto principal del resultado (dirección + precio)
        /// </summary>
        [Required]
        public string Texto { get; set; } = string.Empty;

        /// <summary>
        /// Dirección completa del inmueble
        /// </summary>
        public string Direccion { get; set; } = string.Empty;

        /// <summary>
        /// Precio total del contrato de venta
        /// </summary>
        public decimal PrecioTotal { get; set; }

        /// <summary>
        /// Monto de la seña requerida
        /// </summary>
        public decimal MontoSeña { get; set; }

        /// <summary>
        /// Monto total pagado hasta el momento
        /// </summary>
        public decimal MontoPagado { get; set; }

        /// <summary>
        /// Saldo pendiente por pagar
        /// </summary>
        public decimal SaldoPendiente { get; set; }

        /// <summary>
        /// Estado del contrato: "activo", "cancelado", "finalizado", etc.
        /// </summary>
        public string EstadoContrato { get; set; } = string.Empty;

        /// <summary>
        /// Estado de la seña: "pagada", "pendiente", "sin seña"
        /// </summary>
        public string EstadoSeña { get; set; } = string.Empty;

        /// <summary>
        /// Porcentaje del total que ha sido pagado
        /// </summary>
        public decimal PorcentajePagado { get; set; }

        /// <summary>
        /// Nombre completo del comprador
        /// </summary>
        public string NombreComprador { get; set; } = string.Empty;

        /// <summary>
        /// Email del comprador
        /// </summary>
        public string? EmailComprador { get; set; }

        /// <summary>
        /// Teléfono del comprador
        /// </summary>
        public string? TelefonoComprador { get; set; }

        /// <summary>
        /// Tipo de inmueble: "Casa", "Departamento", "Terreno", etc.
        /// </summary>
        [Required]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Información adicional (saldo + estado seña)
        /// </summary>
        public string? InfoAdicional { get; set; }

        /// <summary>
        /// Información del comprador para mostrar
        /// </summary>
        public string InfoComprador => $"{NombreComprador} | Tel: {TelefonoComprador ?? "N/A"}";

        /// <summary>
        /// Información financiera resumida
        /// </summary>
        public string InfoFinanciera => 
            $"Pagado: ${MontoPagado:N0} | Saldo: ${SaldoPendiente:N0} | {PorcentajePagado:F1}%";

        /// <summary>
        /// Indica si la seña está pagada
        /// </summary>
        public bool SeñaPagada => EstadoSeña?.ToLower() == "pagada";

        /// <summary>
        /// Indica si el contrato está activo
        /// </summary>
        public bool ContratoActivo => 
            EstadoContrato?.ToLower() is "activo" or "seña_pagada" or "en_proceso";
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    /// <summary>
    /// Representa un proceso de venta de inmueble.
    /// A diferencia de ContratoAlquiler, este modelo gestiona el proceso completo de venta:
    /// seña, anticipos y pago final hasta la escrituración.
    /// </summary>
    [Table("contrato_venta")]
    public class ContratoVenta
    {
        [Key]
        [Column("id_contrato_venta")]
        public int IdContratoVenta { get; set; }

        // ========================
        // RELACIONES PRINCIPALES
        // ========================

        [Required(ErrorMessage = "Debe seleccionar un inmueble")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inmueble válido")]
        [Display(Name = "Inmueble")]
        [Column("id_inmueble")]
        public int IdInmueble { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un comprador")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un comprador válido")]
        [Display(Name = "Comprador")]
        [Column("id_comprador")]
        public int IdComprador { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un vendedor/propietario")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un vendedor válido")]
        [Display(Name = "Vendedor")]
        [Column("id_vendedor")]
        public int IdVendedor { get; set; }

        // ========================
        // INFORMACIÓN DE LA VENTA
        // ========================

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [Display(Name = "Fecha de Inicio")]
        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Display(Name = "Fecha de Escrituración")]
        [Column("fecha_escrituracion")]
        public DateTime? FechaEscrituracion { get; set; }

        [Display(Name = "Fecha de Cancelación")]
        [Column("fecha_cancelacion")]
        public DateTime? FechaCancelacion { get; set; }

        [Required(ErrorMessage = "El precio total es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [Display(Name = "Precio Total")]
        [Column("precio_total", TypeName = "decimal(15,2)")]
        public decimal PrecioTotal { get; set; }

        [Display(Name = "Monto Seña")]
        [Column("monto_seña", TypeName = "decimal(15,2)")]
        public decimal MontoSeña { get; set; } = 0;

        [Display(Name = "Monto Anticipos")]
        [Column("monto_anticipos", TypeName = "decimal(15,2)")]
        public decimal MontoAnticipos { get; set; } = 0;

        [Display(Name = "Monto Pagado")]
        [Column("monto_pagado", TypeName = "decimal(15,2)")]
        public decimal MontoPagado { get; set; } = 0;

        // ========================
        // ESTADO Y SEGUIMIENTO
        // ========================

        /// <summary>
        /// Estado de la venta:
        /// - "seña_pendiente": Aún no se pagó la seña
        /// - "seña_pagada": Seña pagada, en proceso de venta
        /// - "en_proceso": Pagando anticipos
        /// - "pendiente_escritura": Todo pagado, falta escriturar
        /// - "escriturada": Venta completada
        /// - "cancelada": Venta cancelada
        /// </summary>
        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(30, ErrorMessage = "El estado no puede exceder 30 caracteres")]
        [Display(Name = "Estado")]
        [Column("estado")]
        public string Estado { get; set; } = "seña_pendiente";

        /// <summary>
        /// Porcentaje completado del pago (0-100)
        /// </summary>
        [Column("porcentaje_pagado", TypeName = "decimal(5,2)")]
        public decimal PorcentajePagado { get; set; } = 0;

        // ========================
        // AUDITORÍA
        // ========================

        [Required(ErrorMessage = "Debe seleccionar un usuario creador")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un usuario válido")]
        [Display(Name = "Usuario Creador")]
        [Column("id_usuario_creador")]
        public int IdUsuarioCreador { get; set; }

        [Display(Name = "Usuario Cancelador")]
        [Column("id_usuario_cancelador")]
        public int? IdUsuarioCancelador { get; set; }

        [Display(Name = "Fecha de Creación")]
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Modificación")]
        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        [Display(Name = "Observaciones")]
        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [StringLength(500, ErrorMessage = "El motivo de cancelación no puede exceder 500 caracteres")]
        [Display(Name = "Motivo de Cancelación")]
        [Column("motivo_cancelacion")]
        public string? MotivoCancelacion { get; set; }

        // ========================
        // PROPIEDADES DE NAVEGACIÓN
        // ========================

        [ForeignKey(nameof(IdInmueble))]
        public virtual Inmueble? Inmueble { get; set; }

        [ForeignKey(nameof(IdComprador))]
        public virtual Usuario? Comprador { get; set; }

        [ForeignKey(nameof(IdVendedor))]
        public virtual Propietario? Vendedor { get; set; }

        [ForeignKey(nameof(IdUsuarioCreador))]
        public virtual Usuario? UsuarioCreador { get; set; }

        [ForeignKey(nameof(IdUsuarioCancelador))]
        public virtual Usuario? UsuarioCancelador { get; set; }

        // Relación con pagos
        public virtual ICollection<Pago>? Pagos { get; set; }

        // ========================
        // PROPIEDADES CALCULADAS
        // ========================

        [NotMapped]
        public decimal SaldoPendiente => PrecioTotal - MontoPagado;

        [NotMapped]
        public bool SeñaPagada => MontoSeña > 0 && Estado != "seña_pendiente";

        [NotMapped]
        public bool EstaCompleta => MontoPagado >= PrecioTotal;

        [NotMapped]
        public bool EstaEscritura => Estado == "escriturada";

        [NotMapped]
        public bool EstaCancelada => Estado == "cancelada";

        [NotMapped]
        public string EstadoDescripcion => Estado switch
        {
            "seña_pendiente" => "Seña Pendiente",
            "seña_pagada" => "Seña Pagada",
            "en_proceso" => "En Proceso",
            "pendiente_escritura" => "Pendiente Escritura",
            "escriturada" => "Escriturada",
            "cancelada" => "Cancelada",
            _ => Estado.ToUpper()
        };

        [NotMapped]
        public string EstadoBadgeClass => Estado switch
        {
            "seña_pendiente" => "bg-warning text-dark",
            "seña_pagada" => "bg-info text-white",
            "en_proceso" => "bg-primary text-white",
            "pendiente_escritura" => "bg-success text-white",
            "escriturada" => "bg-dark text-white",
            "cancelada" => "bg-danger text-white",
            _ => "bg-secondary text-white"
        };

        // ========================
        // MÉTODOS DE NEGOCIO
        // ========================

        /// <summary>
        /// Actualiza el monto pagado y el porcentaje según los pagos registrados
        /// </summary>
        public void ActualizarMontoPagado()
        {
            if (Pagos == null || !Pagos.Any()) return;

            MontoPagado = Pagos
                .Where(p => p.Estado == "pagado" && p.TipoPago == "venta")
                .Sum(p => p.MontoBase);

            PorcentajePagado = PrecioTotal > 0 
                ? (MontoPagado / PrecioTotal) * 100 
                : 0;

            ActualizarEstadoAutomatico();
        }

        /// <summary>
        /// Actualiza el estado automáticamente según el avance de pagos
        /// </summary>
        private void ActualizarEstadoAutomatico()
        {
            if (Estado == "cancelada" || Estado == "escriturada") 
                return;

            if (MontoSeña > 0 && Estado == "seña_pendiente")
            {
                Estado = "seña_pagada";
            }
            else if (MontoPagado > MontoSeña && MontoPagado < PrecioTotal)
            {
                Estado = "en_proceso";
            }
            else if (MontoPagado >= PrecioTotal && !FechaEscrituracion.HasValue)
            {
                Estado = "pendiente_escritura";
            }
            else if (FechaEscrituracion.HasValue)
            {
                Estado = "escriturada";
            }
        }

        /// <summary>
        /// Cancela la venta
        /// </summary>
        public void Cancelar(int idUsuario, string motivo)
        {
            Estado = "cancelada";
            IdUsuarioCancelador = idUsuario;
            FechaCancelacion = DateTime.Now;
            MotivoCancelacion = motivo;
        }

        /// <summary>
        /// Marca como escriturada
        /// </summary>
        public void MarcarEscriturada(DateTime fechaEscritura)
        {
            if (MontoPagado < PrecioTotal)
            {
                throw new InvalidOperationException("No se puede escriturar sin completar el pago");
            }

            Estado = "escriturada";
            FechaEscrituracion = fechaEscritura;
        }

        /// <summary>
        /// Validaciones del contrato de venta
        /// </summary>
        public List<string> ValidarContrato()
        {
            var errores = new List<string>();

            if (PrecioTotal <= 0)
                errores.Add("El precio total debe ser mayor a 0");

            if (IdInmueble <= 0)
                errores.Add("Debe seleccionar un inmueble válido");

            if (IdComprador <= 0)
                errores.Add("Debe seleccionar un comprador válido");

            if (IdVendedor <= 0)
                errores.Add("Debe seleccionar un vendedor válido");

            if (MontoSeña > PrecioTotal)
                errores.Add("La seña no puede ser mayor al precio total");

            if (MontoPagado > PrecioTotal)
                errores.Add("El monto pagado no puede exceder el precio total");

            return errores;
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("pago")]
    public class Pago
    {
        [Key]
        [Column("id_pago")]
        public int IdPago { get; set; }

        // ========================
        // RELACIONES PRINCIPALES
        // ========================

        /// <summary>
        /// ID del contrato - SOLO para alquileres (nullable porque ventas no tienen contrato)
        /// </summary>
        [Column("id_contrato")]
        public int? IdContrato { get; set; }

        /// <summary>
        /// ID del inmueble - OBLIGATORIO para todos los pagos (alquileres y ventas)
        /// </summary>
        [Required(ErrorMessage = "El inmueble es requerido")]
        [Column("id_inmueble")]
        public int IdInmueble { get; set; }

        // ========================
        // INFORMACIÓN DEL PAGO
        // ========================

        /// <summary>
        /// Tipo de pago: "alquiler" o "venta"
        /// </summary>
        [Required(ErrorMessage = "El tipo de pago es obligatorio")]
        [StringLength(20, ErrorMessage = "El tipo de pago no puede exceder 20 caracteres")]
        [Column("tipo_pago")]
        public string TipoPago { get; set; } = "alquiler";

        /// <summary>
        /// Número de pago: 
        /// - Para alquileres: 1, 2, 3... (secuencial por contrato)
        /// - Para ventas: siempre 1 (o puede ser: 1=seña, 2=anticipo, 3=final)
        /// </summary>
        [Required(ErrorMessage = "El número de pago es requerido")]
        [Column("numero_pago")]
        public int NumeroPago { get; set; }

        [Required(ErrorMessage = "La fecha de pago es obligatoria")]
        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        /// <summary>
        /// Fecha de vencimiento - SOLO para alquileres (nullable porque ventas no tienen vencimiento)
        /// </summary>
        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Required(ErrorMessage = "El concepto es obligatorio")]
        [StringLength(200, ErrorMessage = "El concepto no puede exceder 200 caracteres")]
        [Column("concepto")]
        public string Concepto { get; set; } = string.Empty;

        // ========================
        // SISTEMA DE MONTOS
        // ========================

        /// <summary>
        /// Monto base del pago (sin mora)
        /// </summary>
        [Required(ErrorMessage = "El monto base es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto base debe ser mayor a 0")]
        [Column("monto_base", TypeName = "decimal(12,2)")]
        public decimal MontoBase { get; set; }

        /// <summary>
        /// Recargo por mora - SOLO para alquileres (siempre 0 para ventas)
        /// </summary>
        [Column("recargo_mora", TypeName = "decimal(12,2)")]
        public decimal RecargoMora { get; set; } = 0;

        /// <summary>
        /// Monto total final = MontoBase + RecargoMora
        /// </summary>
        [Required(ErrorMessage = "El monto total es obligatorio")]
        [Column("monto_total", TypeName = "decimal(12,2)")]
        public decimal MontoTotal { get; set; }

        // ========================
        // GESTIÓN DE MORA - SOLO ALQUILERES
        // ========================

        /// <summary>
        /// Días de mora - SOLO para alquileres (nullable porque ventas no tienen mora)
        /// </summary>
        [Column("dias_mora")]
        public int? DiasMora { get; set; }

        /// <summary>
        /// Monto fijo por día de mora - SOLO para alquileres
        /// </summary>
        [Column("monto_diario_mora", TypeName = "decimal(10,2)")]
        public decimal? MontoDiarioMora { get; set; }

        // ========================
        // GESTIÓN DE COMPROBANTES
        // ========================

        [StringLength(500, ErrorMessage = "La ruta del comprobante no puede exceder 500 caracteres")]
        [Column("comprobante_ruta")]
        public string? ComprobanteRuta { get; set; }

        [StringLength(255, ErrorMessage = "El nombre del comprobante no puede exceder 255 caracteres")]
        [Column("comprobante_nombre")]
        public string? ComprobanteNombre { get; set; }

        /// <summary>
        /// Mes/año al que corresponde este pago (YYYY-MM)
        /// Ejemplo: "2025-03" para marzo 2025
        /// </summary>
        [StringLength(7)]
        [Column("periodo_pago")]
        public string? PeriodoPago { get; set; } // Formato: "2025-03"

        /// <summary>
        /// Año del período
        /// </summary>
        [Column("periodo_año")]
        public int? PeriodoAño { get; set; }

        /// <summary>
        /// Mes del período (1-12)
        /// </summary>
        [Column("periodo_mes")]
        public int? PeriodoMes { get; set; }

        // ========================
        // CAMPOS DE ESTADO
        // ========================

        /// <summary>
        /// Estado: "pagado", "pendiente", "anulado"
        /// </summary>
        [Required(ErrorMessage = "El estado es obligatorio")]
        [Column("estado")]
        public string Estado { get; set; } = "pagado";

        [Required(ErrorMessage = "Usuario creador es requerido")]
        [Column("id_usuario_creador")]
        public int IdUsuarioCreador { get; set; }
        [Column("id_contrato_venta")]
        public int? IdContratoVenta { get; set; }

        [ForeignKey(nameof(IdContratoVenta))]
        public virtual ContratoVenta? ContratoVenta { get; set; }

        [Column("id_usuario_anulador")]
        public int? IdUsuarioAnulador { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_anulacion")]
        public DateTime? FechaAnulacion { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        [Column("observaciones")]
        public string? Observaciones { get; set; }

        // ========================
        // PROPIEDADES DE NAVEGACIÓN (TODAS NULLABLE)
        // ========================

        [ForeignKey(nameof(IdContrato))]
        public virtual Contrato? Contrato { get; set; }

        [ForeignKey(nameof(IdInmueble))]
        public virtual Inmueble? Inmueble { get; set; }

        [ForeignKey(nameof(IdUsuarioCreador))]
        public virtual Usuario? UsuarioCreador { get; set; }

        [ForeignKey(nameof(IdUsuarioAnulador))]
        public virtual Usuario? UsuarioAnulador { get; set; }

        // ========================
        // PROPIEDADES CALCULADAS
        // ========================

        [NotMapped]
        public bool TieneMora => DiasMora.HasValue && DiasMora.Value > 0;

        [NotMapped]
        public bool EsAlquiler => TipoPago?.ToLower() == "alquiler";

        [NotMapped]
        public bool EsVenta => TipoPago?.ToLower() == "venta";

        [NotMapped]
        public string TipoPagoDescripcion => TipoPago?.ToLower() switch
        {
            "alquiler" => "Alquiler",
            "venta" => "Venta",
            "seña" => "Seña",
            _ => TipoPago?.ToUpper() ?? "DESCONOCIDO"
        };

        [NotMapped]
        public string EstadoBadgeClass => Estado?.ToLower() switch
        {
            "pagado" => "bg-success text-white",
            "pendiente" => "bg-warning text-dark",
            "anulado" => "bg-danger text-white",
            _ => "bg-secondary text-white"
        };

        [NotMapped]
        public bool TieneComprobante => !string.IsNullOrEmpty(ComprobanteRuta);

        // ========================
        // MÉTODOS DE CÁLCULO
        // ========================

        /// <summary>
        /// Calcula días de mora - SOLO para alquileres
        /// </summary>
        public void CalcularDiasMora()
        {
            if (!EsAlquiler || !FechaVencimiento.HasValue)
            {
                DiasMora = 0;
                return;
            }

            if (FechaPago.Date > FechaVencimiento.Value.Date)
            {
                DiasMora = (FechaPago.Date - FechaVencimiento.Value.Date).Days;
            }
            else
            {
                DiasMora = 0;
            }
        }

        /// <summary>
        /// Aplica recargo por mora - SOLO para alquileres
        /// </summary>
        public void AplicarRecargoMora()
        {
            if (!EsAlquiler)
            {
                RecargoMora = 0;
                MontoTotal = MontoBase;
                return;
            }

            CalcularDiasMora();

            if (DiasMora.HasValue && DiasMora.Value > 0 && MontoDiarioMora.HasValue)
            {
                RecargoMora = DiasMora.Value * MontoDiarioMora.Value;
            }
            else
            {
                RecargoMora = 0;
            }

            MontoTotal = MontoBase + RecargoMora;
        }

        /// <summary>
        /// Validaciones según tipo de pago
        /// </summary>
        public List<string> ValidarPago()
        {
            var errores = new List<string>();

            if (MontoBase <= 0)
                errores.Add("El monto base debe ser mayor a 0");

            if (EsAlquiler && !IdContrato.HasValue)
                errores.Add("Los pagos de alquiler requieren un contrato");

            if (EsVenta && IdContrato.HasValue)
                errores.Add("Los pagos de venta no deben tener contrato asociado");

            if (RecargoMora < 0)
                errores.Add("El recargo de mora no puede ser negativo");

            if (IdInmueble <= 0)
                errores.Add("Debe seleccionar un inmueble válido");

            return errores;
        }
    }
}
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

        [Required(ErrorMessage = "El contrato es requerido")]
        [Column("id_contrato")]
        public int IdContrato { get; set; }

        [Required(ErrorMessage = "El número de pago es requerido")]
        [Column("numero_pago")]
        public int NumeroPago { get; set; }

        [Required(ErrorMessage = "La fecha de pago es obligatoria")]
        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [Required(ErrorMessage = "El concepto es obligatorio")]
        [StringLength(100, ErrorMessage = "El concepto no puede exceder 100 caracteres")]
        [Column("concepto")]
        public string Concepto { get; set; } = null!;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(1, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        [Column("monto", TypeName = "decimal(12,2)")]
        public decimal Monto { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "activo"; // activo, anulado

        [Required(ErrorMessage = "Usuario creador es requerido")]
        [Column("id_usuario_creador")]
        public int IdUsuarioCreador { get; set; }

        [Column("id_usuario_anulador")]
        public int? IdUsuarioAnulador { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_anulacion")]
        public DateTime? FechaAnulacion { get; set; }

        
        [ForeignKey(nameof(IdContrato))]
        public virtual Contrato Contrato { get; set; } = null!;

        [ForeignKey(nameof(IdUsuarioCreador))]
        public virtual Usuario UsuarioCreador { get; set; } = null!;

        [ForeignKey(nameof(IdUsuarioAnulador))]
        public virtual Usuario UsuarioAnulador { get; set; } = null!;
        
    }
}
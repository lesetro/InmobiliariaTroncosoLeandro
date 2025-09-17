using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("contrato")]
    public class Contrato
    {
        [Key]
        [Column("id_contrato")]
        public int IdContrato { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un inmueble")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inmueble válido")]
        [Display(Name = "Inmueble")]
        [Column("id_inmueble")]
        public int IdInmueble { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un inquilino")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inquilino válido")]
        [Display(Name = "Inquilino")]
        [Column("id_inquilino")]
        public int IdInquilino { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un propietario")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un propietario válido")]
        [Display(Name = "Propietario")]
        [Column("id_propietario")]
        public int IdPropietario { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [Display(Name = "Fecha de Inicio")]
        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        [Display(Name = "Fecha de Fin")]
        [Column("fecha_fin")]
        public DateTime FechaFin { get; set; }

        [Display(Name = "Fecha de Fin Anticipada")]
        [Column("fecha_fin_anticipada")]
        public DateTime? FechaFinAnticipada { get; set; }

        [Required(ErrorMessage = "El monto mensual es obligatorio")]
        [Display(Name = "Monto Mensual")]
        [Column("monto_mensual", TypeName = "decimal(12,2)")]
        public decimal MontoMensual { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(20, ErrorMessage = "El estado no puede exceder 20 caracteres")]
        [Display(Name = "Estado")]
        [Column("estado")]
        public string Estado { get; set; } = "vigente";

        [Display(Name = "Multa Aplicada")]
        [Column("multa_aplicada", TypeName = "decimal(12,2)")]
        public decimal MultaAplicada { get; set; } = 0;

        [Required(ErrorMessage = "Debe seleccionar un usuario creador")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un usuario válido")]
        [Display(Name = "Usuario Creador")]
        [Column("id_usuario_creador")]
        public int IdUsuarioCreador { get; set; }

        [Display(Name = "Usuario Terminador")]
        [Column("id_usuario_terminador")]
        public int? IdUsuarioTerminador { get; set; }

        [Display(Name = "Fecha de Creación")]
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Modificación")]
        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        // Propiedades de navegación (para mostrar en vistas)
        [NotMapped]
        public virtual Inmueble? Inmueble { get; set; }

        [NotMapped]
        public virtual Inquilino? Inquilino { get; set; }
        [NotMapped]
        public virtual Propietario? Propietario { get; set; }

        [NotMapped]
        public virtual Usuario? UsuarioCreador { get; set; }

        [NotMapped]
        public virtual Usuario? UsuarioTerminador { get; set; }
    }
}
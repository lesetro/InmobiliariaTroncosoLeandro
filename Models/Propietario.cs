using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("propietario")]
    public class Propietario
    {
        [Key]
        [Column("id_propietario")]
        public int IdPropietario { get; set; }

        // RELACIÓN CON USUARIO (para DNI, email, teléfono, dirección)
        [Required(ErrorMessage = "El usuario es requerido")]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La fecha de alta es obligatoria")]
        [Column("fecha_alta")]
        public DateTime? FechaAlta { get; set; } = DateTime.Now;

        [Column("estado")]
        public bool Estado { get; set; } = true;

        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuario Usuario { get; set; } = null!;

        public virtual ICollection<Inmueble> Inmuebles { get; set; } = new List<Inmueble>();
      
    }
}
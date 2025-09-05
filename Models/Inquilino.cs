using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("inquilino")]
    public class Inquilino
    {
        [Key]
        [Column("id_inquilino")]
        public int IdInquilino { get; set; }

        // RELACIÓN CON USUARIO (para DNI, email, teléfono, dirección)
        [Required(ErrorMessage = "El usuario es requerido")]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("fecha_alta")]
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        [Column("estado")]
        public bool Estado { get; set; } = true;

        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuario Usuario { get; set; } = null!;
        
    }
}
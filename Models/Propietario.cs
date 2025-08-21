using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("propietarios")] 
    public class Propietario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [StringLength(20, ErrorMessage = "El DNI no puede exceder 20 caracteres")]
        [Column("dni")]
        public required string Dni { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        [Column("apellido")]
        public required string Apellido { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public required string Nombre { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Debe ingresar un email válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [Column("email")]
        public string? Email { get; set; }

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        // Relación 1 a N con Inmuebles (un propietario tiene muchos inmuebles)
        //public virtual ICollection<Inmueble> Inmuebles { get; set; } = new List<Inmueble>();
    }
}

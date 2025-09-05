using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("tipo_inmueble")]
    public class TipoInmueble
    {
        [Key]
        [Column("id_tipo_inmueble")]
        public int IdTipoInmueble { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres")]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("estado")]
        public Boolean Estado { get; set; } = true; 

       
        public virtual ICollection<Inmueble> Inmuebles { get; set; } = new List<Inmueble>();
        
    }
}
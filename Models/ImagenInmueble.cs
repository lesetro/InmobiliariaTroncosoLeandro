using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("imagen_inmueble")]
    public class ImagenInmueble
    {
        [Key]
        [Column("id_imagen")]
        public int IdImagen { get; set; }

        [Required(ErrorMessage = "El inmueble es obligatorio")]
        [Column("id_inmueble")]
        public int IdInmueble { get; set; }

        
        [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres")]
        [Column("url")]
        public required string Url { get; set; } = " ";

        [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("orden")]
        public int Orden { get; set; } = 1;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Navegación hacia Inmueble
        [NotMapped]
        public virtual Inmueble? Inmueble { get; set; }
    }
}
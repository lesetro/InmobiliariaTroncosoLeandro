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

        [Required(ErrorMessage = "La URL es obligatoria")]
        [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres")]
        [Column("url")]
        public string Url { get; set; } = "";

        [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("orden")]
        public int Orden { get; set; } = 1;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

    
        [NotMapped]
        public virtual Inmueble? Inmueble { get; set; }

        
        [NotMapped]
        public string NombreArchivo
        {
            get
            {
                if (string.IsNullOrEmpty(Url)) return "Sin archivo";
                return Path.GetFileName(Url);
            }
        }

        [NotMapped]
        public bool EsImagenValida => !string.IsNullOrWhiteSpace(Url);

        [NotMapped]
        public string TamanioFormateado { get; set; } = "";

        [NotMapped]
        public string IconoTipo => "bi-images";

        [NotMapped]
        public string CssClassOrden => $"orden-{Orden}";
    }
}
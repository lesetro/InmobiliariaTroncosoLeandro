using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("interes_inmueble")]
    public class InteresInmueble
    {
        [Key]
        [Column("id_interes")]
        public int IdInteres { get; set; }

        [Required]
        [Column("id_inmueble")]
        public int IdInmueble { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [EmailAddress(ErrorMessage = "El email no es válido")]
        [Column("email")]
        public string Email { get; set; } = null!;

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        // Nuevos campos para gestión de contactos
        [Column("contactado")]
        public bool Contactado { get; set; } = false;

        [Column("fecha_contacto")]
        public DateTime? FechaContacto { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        [Column("observaciones")]
        public string? Observaciones { get; set; }

        // Relación con Inmueble
        [ForeignKey(nameof(IdInmueble))]
        public virtual Inmueble Inmueble { get; set; } = null!;

        // Propiedades calculadas para la vista
        [NotMapped]
        public string EstadoTexto => Contactado ? "Contactado" : "Pendiente";

        [NotMapped]
        public string DiasDesdeInteres => (DateTime.Now - Fecha).Days switch
        {
            0 => "Hoy",
            1 => "Hace 1 día",
            var dias when dias <= 7 => $"Hace {dias} días",
            var dias when dias <= 30 => $"Hace {dias / 7} semana(s)",
            var dias => $"Hace {dias / 30} mes(es)"
        };

        [NotMapped]
        public string TelefonoFormateado => !string.IsNullOrEmpty(Telefono) ? 
            $"tel:{Telefono.Replace(" ", "").Replace("-", "")}" : string.Empty;

        [NotMapped]
        public string EmailFormateado => $"mailto:{Email}";
    }
}
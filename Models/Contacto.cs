using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria_troncoso_leandro.Models
{
    public class Contacto
    {
        public int IdContacto { get; set; }
      
        
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        public string Apellido { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [StringLength(150, ErrorMessage = "El email no puede exceder 150 caracteres")]
        public string Email { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Telefono { get; set; }
        
        [StringLength(200, ErrorMessage = "El asunto no puede exceder 200 caracteres")]
        public string? Asunto { get; set; }
        
        [Required(ErrorMessage = "El mensaje es obligatorio")]
        public string Mensaje { get; set; } = string.Empty;
        
        public DateTime FechaContacto { get; set; } = DateTime.Now;
        
        public string Estado { get; set; } = "pendiente";
        
        public int? IdInmueble { get; set; }
        
        public string? IpOrigen { get; set; }
        
        public string? UserAgent { get; set; }
        
        // Propiedades de navegación
        public Inmueble? Inmueble { get; set; }
        
        // Propiedades calculadas
        public string NombreCompleto => $"{Nombre} {Apellido}";
        public string EstadoBadgeClass => Estado switch
        {
            "pendiente" => "badge-warning",
            "respondido" => "badge-success",
            "cerrado" => "badge-secondary",
            _ => "badge-secondary"
        };
    }
}
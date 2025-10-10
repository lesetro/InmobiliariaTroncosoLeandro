using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("inmueble")]
    public class Inmueble
    {
        [Key]
        [Column("id_inmueble")]
        public int IdInmueble { get; set; }

        [Required]
        [Column("id_propietario")]
        public int IdPropietario { get; set; }

        [Required]
        [Column("id_tipo_inmueble")]
        public int IdTipoInmueble { get; set; }

        [Column("id_usuario_creador")]
        public int? IdUsuarioCreador { get; set; }

        [Required]
        [StringLength(255)]
        [Column("direccion")]
        public string Direccion { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Column("uso")]
        public string Uso { get; set; } = null!;

        [Required]
        [Column("ambientes")]
        public int Ambientes { get; set; }

        [Required]
        [Column("precio")]
        public decimal Precio { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^-?\d{1,2}(\.\d{1,6})?,\s*-?\d{1,3}(\.\d{1,6})?$", 
        ErrorMessage = "Formato: latitud,longitud (ej. 40.7128, -74.0060),sin espacios despues de la coma")]
        [Column("coordenadas")]
        public string? Coordenadas { get; set; }

       
        [StringLength(500, ErrorMessage = "La URL de portada no puede exceder 500 caracteres")]
        [Display(Name = "Imagen de Portada")]
        [Column("url_portada")]
        public string? UrlPortada { get; set; }

        [Required]
        [Column("estado")]
        public string Estado { get; set; } = "disponible"; // disponible, no_disponible, alquilado

        [Column("fecha_alta")]
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdPropietario))]
        public virtual Propietario? Propietario { get; set; } = null!;

        [ForeignKey(nameof(IdTipoInmueble))]
        public virtual TipoInmueble? TipoInmueble { get; set; } = null!;

        [NotMapped]
        public bool EstaDisponible => Estado == "disponible";
        
        [NotMapped]
        public bool EstaEliminado => Estado == "inactivo";
        
        [NotMapped]
        public string EstadoParaMostrar => Estado switch
        {
            "disponible" => "DISPONIBLE",
            "alquilado" => "ALQUILADO",
            "no_disponible" => "NO DISPONIBLE", 
            "inactivo" => "ELIMINADO",
            "venta"=> "PARA VENTA",
            "vendido" => "VENDIDO",
            "reservado_alquiler" => "RESERVADO ALQUILER",
            "reservado_venta" => "RESERVADO VENTA", 
            _ => Estado.ToUpper()
        };
        
        [NotMapped]
        public string EstadoBadgeClass => Estado switch
        {
            "disponible" => "bg-success text-white",      // Verde
            "alquilado" => "bg-warning text-dark",        // Amarillo
            "vendido" => "bg-danger text-white",          // Rojo
            "reservado_alquiler" => "bg-info text-white", // Azul
            "reservado_venta" => "bg-primary text-white", // Azul oscuro
            "venta" => "bg-purple text-white",       // Morado
            "no_disponible" => "bg-secondary text-white", // Gris
            "inactivo" => "bg-dark text-white",   
            _ => "bg-light text-dark"
        };


        // Relaciones 
        public virtual ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
        public virtual ICollection<ImagenInmueble> Imagenes { get; set; } = new List<ImagenInmueble>();
        public virtual ICollection<InteresInmueble> Intereses { get; set; } = new List<InteresInmueble>();
        
        [NotMapped]
        public bool EstaAlquilado { get; set; } = false;

        //  GESTIÓN DE PORTADA

        [NotMapped]
        [Display(Name = "Archivo de Portada")]
        public IFormFile? PortadaFile { get; set; }

        [NotMapped]
        public bool TienePortada => !string.IsNullOrWhiteSpace(UrlPortada);

        [NotMapped]
        public string UrlPortadaODefault => TienePortada ? UrlPortada! : "/images/default/no-image-inmueble.jpg";

        [NotMapped]
        public int TotalImagenesGaleria => Imagenes?.Count ?? 0;

        [NotMapped]
        public string ResumenImagenes
        {
            get
            {
                if (TienePortada && TotalImagenesGaleria > 0)
                    return $"Portada + {TotalImagenesGaleria} en galería";
                if (TienePortada)
                    return "Solo portada";
                if (TotalImagenesGaleria > 0)
                    return $"{TotalImagenesGaleria} imagen(es) en galería";
                return "Sin imágenes";
            }
        }

        [NotMapped]
        public string IconoEstadoImagenes => TienePortada ? "bi-image-fill text-success" : "bi-image text-muted";
    }
}
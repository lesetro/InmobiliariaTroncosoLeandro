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
        [RegularExpression(@"^-?\d{1,2}(\.\d{1,6})?,-?\d{1,3}(\.\d{1,6})?$", 
            ErrorMessage = "Formato: latitud,longitud (ej. 40.7128,-74.0060)")]
        [Column("coordenadas")]
        public string? Coordenadas { get; set; }

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
            _ => Estado.ToUpper()
        };
        
        [NotMapped]
        public string EstadoBadgeClass => Estado switch
        {
            "disponible" => "bg-success text-white",
            "alquilado" => "bg-warning text-dark",
            "no_disponible" => "bg-secondary text-white",
            "inactivo" => "bg-danger text-white",
            _ => "bg-light text-dark"
        };

        public virtual ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
        public virtual ICollection<ImagenInmueble> Imagenes { get; set; } = new List<ImagenInmueble>();
        public virtual ICollection<InteresInmueble> Intereses { get; set; } = new List<InteresInmueble>();
        [NotMapped]
        public bool EstaAlquilado { get; set; } = false;
    }
}
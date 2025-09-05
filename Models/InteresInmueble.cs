using System.ComponentModel.DataAnnotations;
 using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models { 

    [Table("interes_inmueble")] 
    public class InteresInmueble { 
        [Key] [Column("id_interes")] 
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

        [ForeignKey(nameof(IdInmueble))]
        public virtual Inmueble Inmueble { get; set; } = null!;
}

}
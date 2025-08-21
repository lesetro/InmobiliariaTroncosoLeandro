using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("contratos")] 
    public class Contrato
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        
        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; }
        
        [Column("fecha_fin")]
        public DateTime FechaFin { get; set; }
        
        [Column("monto_alquiler")]
        public decimal MontoAlquiler { get; set; }
        
        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}
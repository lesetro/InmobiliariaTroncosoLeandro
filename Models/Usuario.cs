using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("usuario")]
    public class Usuario
    {
        public Usuario()
        {
            // Valores por defecto para no obtener valores nulos
            Rol = "empleado";
            Estado = "activo";
            Nombre = "Sin nombre";
            Apellido = "Sin apellido";
            Dni = "00000000";
            Password = "PasswordTemporal123";
            Telefono = "Sin especificar";
            Direccion = "Sin dirección especificada";
            Email = "sin-email@dominio.com";
        }

        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Debe ingresar un email válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [Column("email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255, ErrorMessage = "La contraseña no puede exceder 255 caracteres")]
        [Column("password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [Column("rol")]
        public string Rol { get; set; } = "empleado";

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string? Nombre { get; set; } = "Sin nombre";

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        [Column("apellido")]
        public string? Apellido { get; set; } = "Sin apellido";

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [StringLength(20, ErrorMessage = "El DNI no puede exceder 20 caracteres")]
        [Column("dni")]
        public string? Dni { get; set; } = "00000000";

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        [Column("direccion")]
        public string? Direccion { get; set; } = "Sin dirección especificada";

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Column("estado")]
        public string Estado { get; set; } = "activo";

        public string NombreCompleto => $"{Nombre} {Apellido}";

        // Relaciones
        public ICollection<Contrato> ContratosCreados { get; set; } = new List<Contrato>();
        public ICollection<Contrato> ContratosTerminados { get; set; } = new List<Contrato>();
        public ICollection<Pago> PagosCreados { get; set; } = new List<Pago>();
        public ICollection<Pago> PagosAnulados { get; set; } = new List<Pago>();
    }
}
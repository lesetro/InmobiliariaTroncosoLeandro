using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria_troncoso_leandro.Models
{
    [Table("usuario")]
    public class Usuario
    {
        // Constructor SIN valores por defecto problemáticos
        public Usuario()
        {
            // Solo valores que realmente son seguros como default
            Estado = "activo";  // Este sí puede ser default
            // NO asignar Rol aquí - se debe especificar explícitamente
            // NO asignar datos personales por defecto
        }

        // Constructor específico para crear usuarios con datos completos
        public Usuario(string nombre, string apellido, string dni, string email, string rol, string avatar = null)
        {
            Nombre = nombre;
            Apellido = apellido;
            Dni = dni;
            Email = email;
            Rol = rol;  // ← Asignado explícitamente
            Estado = "activo";
            Password = GenerarPasswordTemporal();
            Telefono = "Sin especificar";
            Direccion = "Sin dirección especificada";
            Avatar = avatar; // ← Avatar opcional
        }

        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Debe ingresar un email válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255, ErrorMessage = "La contraseña no puede exceder 255 caracteres")]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        [Column("rol")]
        public string Rol { get; set; } = string.Empty; // ← SIN valor por defecto

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        [Column("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [StringLength(20, ErrorMessage = "El DNI no puede exceder 20 caracteres")]
        [Column("dni")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        [Column("direccion")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Column("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Column("estado")]
        public string Estado { get; set; } = "activo";

        [StringLength(500, ErrorMessage = "La URL del avatar no puede exceder 500 caracteres")]
        [Column("avatar")]
        public string? Avatar { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}";

        // Método helper para generar password temporal
        private string GenerarPasswordTemporal()
        {
            return BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123");
        }

        // Método helper para generar avatars automáticamente con iniciales
        private static string GenerarAvatarPorRol(string rol, string nombre, string apellido)
        {
            // Obtener iniciales (manejo seguro para nombres/apellidos cortos)
            string inicial1 = !string.IsNullOrEmpty(nombre) ? nombre.Substring(0, 1).ToUpper() : "U";
            string inicial2 = !string.IsNullOrEmpty(apellido) ? apellido.Substring(0, 1).ToUpper() : "S";
            string iniciales = inicial1 + inicial2;

            // Colores por rol
            string color = rol.ToLower() switch
            {
                "propietario" => "#007bff",    // Azul
                "inquilino" => "#28a745",      // Verde  
                "empleado" => "#ffc107",       // Amarillo
                "administrador" => "#dc3545",   // Rojo
                _ => "#6c757d"                 // Gris por defecto
            };

            string textColor = rol.ToLower() switch
            {
                "empleado" => "#000000",       // Texto negro para amarillo
                _ => "#ffffff"                 // Texto blanco para otros
            };

            // Generar SVG en Base64 - NO depende de servicios externos
            var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='150' height='150' viewBox='0 0 150 150'>
        <circle cx='75' cy='75' r='75' fill='{color}'/>
        <text x='50%' y='50%' font-family='Arial, sans-serif' font-size='60' font-weight='bold' 
              fill='{textColor}' text-anchor='middle' dominant-baseline='central'>{iniciales}</text>
        </svg>";

            var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
            return $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";
        }

        // Métodos estáticos para crear usuarios específicos - SIN avatar en parámetros
        public static Usuario CrearPropietario(string nombre, string apellido, string dni, string email, string telefono = null, string direccion = null)
        {
            return new Usuario
            {
                Nombre = nombre,
                Apellido = apellido,
                Dni = dni,
                Email = email,
                Rol = "propietario", // ← Explícitamente propietario
                Telefono = telefono ?? "Sin especificar",
                Direccion = direccion ?? "Sin dirección especificada",
                Password = BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123"),
                Estado = "activo",
                Avatar = GenerarAvatarPorRol("propietario", nombre, apellido) // ← Avatar automático con iniciales
            };
        }

        public static Usuario CrearInquilino(string nombre, string apellido, string dni, string email, string telefono = null, string direccion = null)
        {
            return new Usuario
            {
                Nombre = nombre,
                Apellido = apellido,
                Dni = dni,
                Email = email,
                Rol = "inquilino", // ← Explícitamente inquilino
                Telefono = telefono ?? "Sin especificar",
                Direccion = direccion ?? "Sin dirección especificada",
                Password = BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123"),
                Estado = "activo",
                Avatar = GenerarAvatarPorRol("inquilino", nombre, apellido) // ← Avatar automático con iniciales
            };
        }

        public static Usuario CrearEmpleado(string nombre, string apellido, string dni, string email, string telefono = null, string direccion = null)
        {
            return new Usuario
            {
                Nombre = nombre,
                Apellido = apellido,
                Dni = dni,
                Email = email,
                Rol = "empleado", // ← Explícitamente empleado
                Telefono = telefono ?? "Sin especificar",
                Direccion = direccion ?? "Sin dirección especificada",
                Password = BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123"),
                Estado = "activo",
                Avatar = GenerarAvatarPorRol("empleado", nombre, apellido) // ← Avatar automático con iniciales
            };
        }

        public static Usuario CrearAdministrador(string nombre, string apellido, string dni, string email, string telefono = null, string direccion = null)
        {
            return new Usuario
            {
                Nombre = nombre,
                Apellido = apellido,
                Dni = dni,
                Email = email,
                Rol = "administrador", // ← Explícitamente administrador
                Telefono = telefono ?? "Sin especificar",
                Direccion = direccion ?? "Sin dirección especificada",
                Password = BCrypt.Net.BCrypt.HashPassword("PasswordTemporal123"),
                Estado = "activo",
                Avatar = GenerarAvatarPorRol("administrador", nombre, apellido) // ← Avatar automático con iniciales
            };
        }

        // Método para actualizar avatar (para cuando el usuario quiera personalizarlo después)
        public void ActualizarAvatar(string nuevoAvatar)
        {
            Avatar = string.IsNullOrWhiteSpace(nuevoAvatar)
                ? GenerarAvatarPorRol(Rol, Nombre, Apellido)
                : nuevoAvatar;
        }

        // Relaciones
        public ICollection<Contrato> ContratosCreados { get; set; } = new List<Contrato>();
        public ICollection<Contrato> ContratosTerminados { get; set; } = new List<Contrato>();
        public ICollection<Pago> PagosCreados { get; set; } = new List<Pago>();
        public ICollection<Pago> PagosAnulados { get; set; } = new List<Pago>();
    }
}
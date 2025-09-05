
using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria_troncoso_leandro.Models
{
    /// <summary>
    /// Clase para representar resultados de búsqueda de usuarios (propietarios, inquilinos, etc.)
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// ID del usuario (como string para flexibilidad)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Texto principal del resultado (nombre completo)
        /// </summary>
        [Required]
        public string Texto { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono del usuario (opcional)
        /// </summary>
        public string? Telefono { get; set; }

        /// <summary>
        /// Email del usuario (opcional)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Información adicional como DNI, dirección, etc. (opcional)
        /// </summary>
        public string? InfoAdicional { get; set; }

        /// <summary>
        /// Tipo de usuario: "Propietario", "Inquilino", "Propietario e Inquilino", etc.
        /// </summary>
        [Required]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Texto formateado para mostrar en los resultados de búsqueda
        /// </summary>
        public string TextoCompleto => $"{Texto} ({Tipo})";

        /// <summary>
        /// Información de contacto combinada
        /// </summary>
        public string ContactoInfo
        {
            get
            {
                var contacto = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(Telefono))
                    contacto.Add($"Tel: {Telefono}");
                
                if (!string.IsNullOrWhiteSpace(Email))
                    contacto.Add($"Email: {Email}");
                
                return string.Join(" | ", contacto);
            }
        }
    }

    /// <summary>
    /// Clase para representar resultados paginados de búsqueda
    /// </summary>
    public class SearchPaginatedResult
    {
        /// <summary>
        /// Lista de elementos encontrados en la página actual
        /// </summary>
        public List<SearchResult> Items { get; set; } = new List<SearchResult>();

        /// <summary>
        /// Total de elementos encontrados (en todas las páginas)
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total de páginas disponibles
        /// </summary>
        public int TotalPaginas { get; set; }

        /// <summary>
        /// Página actual (1-based)
        /// </summary>
        public int PaginaActual { get; set; }

        /// <summary>
        /// Indica si existe una página anterior
        /// </summary>
        public bool TienePaginaAnterior => PaginaActual > 1;

        /// <summary>
        /// Indica si existe una página siguiente
        /// </summary>
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;

        /// <summary>
        /// Número de la página anterior (si existe)
        /// </summary>
        public int? PaginaAnterior => TienePaginaAnterior ? PaginaActual - 1 : null;

        /// <summary>
        /// Número de la página siguiente (si existe)
        /// </summary>
        public int? PaginaSiguiente => TienePaginaSiguiente ? PaginaActual + 1 : null;

        /// <summary>
        /// Información de paginación para mostrar al usuario
        /// </summary>
        public string InfoPaginacion => $"Página {PaginaActual} de {TotalPaginas} ({TotalItems} elementos encontrados)";
    }
}
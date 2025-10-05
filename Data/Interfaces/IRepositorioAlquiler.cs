using Inmobiliaria_troncoso_leandro.Models;
using Inmobiliaria_troncoso_leandro.Models.DTOs;

namespace Inmobiliaria_troncoso_leandro.Data.Interfaces
{
    /// <summary>
    /// Interface específica para gestión de pagos de alquiler con contratos
    /// </summary>
    public interface IRepositorioAlquiler
    {
        // ========================
        // MÉTODOS CRUD BÁSICOS
        // ========================

        /// <summary>
        /// Crea un nuevo pago de alquiler vinculado a contrato
        /// </summary>
        Task<bool> CrearPagoAlquilerAsync(Pago pago);

        /// <summary>
        /// Actualiza un pago de alquiler existente
        /// </summary>
        Task<bool> ActualizarPagoAlquilerAsync(Pago pago);

        /// <summary>
        /// Anula un pago de alquiler (soft delete)
        /// </summary>
        Task<bool> AnularPagoAlquilerAsync(int idPago, int idUsuarioAnulador);

        /// <summary>
        /// Obtiene un pago de alquiler por ID
        /// </summary>
        Task<Pago?> ObtenerPagoAlquilerPorIdAsync(int id);

        /// <summary>
        /// Obtiene un pago de alquiler con detalles completos (contrato, inmueble, inquilino)
        /// </summary>
        Task<Pago?> ObtenerPagoAlquilerConDetallesAsync(int id);

        // ========================
        // MÉTODOS DE LISTADO Y PAGINACIÓN
        // ========================

        /// <summary>
        /// Obtiene pagos de alquiler con paginación y búsqueda
        /// </summary>
        Task<(IList<Pago> pagos, int totalRegistros)> ObtenerPagosAlquilerConPaginacionAsync(
            int pagina, string buscar, string estado, int itemsPorPagina);

        // ========================
        // MÉTODOS ESPECÍFICOS DE MORA
        // ========================

        /// <summary>
        /// Calcula los días de mora para un pago
        /// </summary>
        Task<int> CalcularDiasMoraAsync(int idPago);

        /// <summary>
        /// Calcula el recargo de mora basado en días y monto diario
        /// </summary>
        Task<decimal> CalcularRecargoMoraAsync(int diasMora, decimal montoDiario);

        /// <summary>
        /// Actualiza la mora de un pago específico
        /// </summary>
        Task<bool> ActualizarMoraAsync(int idPago);

        /// <summary>
        /// Obtiene pagos con mora aplicada
        /// </summary>
        Task<IList<Pago>> ObtenerPagosConMoraAsync(int diasMinimos = 1);

        // ========================
        // MÉTODOS DE VALIDACIÓN ESPECÍFICOS
        // ========================

        /// <summary>
        /// Verifica si existe un contrato vigente
        /// </summary>
        Task<bool> ContratoVigenteAsync(int idContrato);

        /// <summary>
        /// Verifica si existe un número de pago duplicado para el contrato
        /// </summary>
        Task<bool> ExistePagoMesContratoAsync(int idContrato, int numeroPago, int idPagoExcluir = 0);

        /// <summary>
        /// Obtiene el próximo número de pago para un contrato
        /// </summary>
        Task<int> ObtenerProximoNumeroPagoAsync(int idContrato);

        /// <summary>
        /// Verifica si el contrato permite más pagos
        /// </summary>
        Task<bool> ContratoPermiteMasPagosAsync(int idContrato);

        // ========================
        // MÉTODOS DE NEGOCIO ESPECÍFICOS
        // ========================

        /// <summary>
        /// Calcula la fecha de vencimiento para un pago
        /// </summary>
        Task<DateTime> CalcularFechaVencimientoAsync(int idContrato, int numeroPago);

        /// <summary>
        /// Obtiene el monto diario de mora configurado
        /// </summary>
        Task<decimal> ObtenerMontoDiarioMoraAsync();

        /// <summary>
        /// Actualiza el estado del contrato basado en pagos
        /// </summary>
        Task<bool> ActualizarEstadoContratoAsync(int idContrato);

        // ========================
        // MÉTODOS AUXILIARES PARA DROPDOWNS
        // ========================

        /// <summary>
        /// Obtiene contratos vigentes para dropdown
        /// </summary>
        Task<List<Contrato>> ObtenerContratosVigentesAsync(int limite = 20);


        /// <summary>
        /// Obtiene usuarios activos para dropdown
        /// </summary>
        Task<List<Usuario>> ObtenerUsuariosActivosAsync(int limite = 20);

        /// <summary>
        /// Obtiene datos del contrato
        /// </summary>
        Task<Contrato?> ObtenerDatosContratoAsync(int idContrato);

        // ========================
        // REPORTES ESPECÍFICOS DE ALQUILER
        // ========================

        /// <summary>
        /// Obtiene resumen de pagos por contrato
        /// </summary>
        Task<object> ObtenerResumenPagosPorContratoAsync(int idContrato);

        /// <summary>
        /// Obtiene contratos próximos a vencer
        /// </summary>
        Task<IList<Contrato>> ObtenerContratosProximosVencerAsync(int diasAnticipacion = 30);

        /// <summary>
        /// Obtiene historial de pagos para un contrato específico
        /// </summary>
        Task<IList<Pago>> ObtenerHistorialPagosContratoAsync(int idContrato);

        /// <summary>
        /// Busca contratos vigentes por término para crear pagos (no confundir con ObtenerContratosVigentesAsync)
        /// Devuelve datos completos para el autocompletado del formulario
        /// </summary>
        Task<List<ContratoAlquilerBusqueda>> BuscarContratosParaPagoAsync(string termino, int limite = 10);

        /// <summary>
        /// Obtiene todos los datos necesarios de un contrato para crear un pago
        /// </summary>
        Task<DatosContratoParaPago?> ObtenerDatosContratoParaPagoAsync(int idContrato);
        
        
    }
    
}
// Archivo: Services/ISystemSetupService.cs

using Inmobiliaria_troncoso_leandro.Models;

namespace Inmobiliaria_troncoso_leandro.Services
{
    public interface ISystemSetupService
    {
        Task<bool> NeedsInitialSetupAsync();
        Task<SetupStatus> GetSystemStatusAsync();
        Task CreateInitialAdminAsync(string nombre, string apellido, string dni, string email, 
                                   string telefono, string direccion, string password);
        Task UpgradeExistingUserToAdminAsync(string email, string newPassword = null);
    }

    public class SetupStatus
    {
        public bool HasAdministrators { get; set; }
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int EmployeeCount { get; set; }
        public int PropietarioCount { get; set; }
        public int InquilinoCount { get; set; }
        public bool NeedsSetup { get; set; }
        public string? RecommendedAction { get; set; }
        public List<string> ExistingAdminEmails { get; set; } = new();
    }
}
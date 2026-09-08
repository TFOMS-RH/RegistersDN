using RegistrDN.Models.Entities;

namespace RegistrDN.Services.Interfaces;

public interface IDiagnosisFilterService
{
    Task<List<string>> GetActiveDiagnosisCodesAsync();
    Task<bool> IsDiagnosisAllowedAsync(string diagCode);
    Task<List<DsDnEntity>> GetAllDiagnosesAsync();
    Task<DsDnEntity?> GetDiagnosisByCodeAsync(string diagCode);
    Task<bool> AddDiagnosisAsync(DsDnEntity diagnosis);
    Task<bool> UpdateDiagnosisAsync(DsDnEntity diagnosis);
    Task<bool> ToggleDiagnosisStatusAsync(string diagCode);
    Task<bool> DeleteDiagnosisAsync(string diagCode);
}
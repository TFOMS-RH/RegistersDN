using Microsoft.EntityFrameworkCore;
using RegistrDN.Data;
using RegistrDN.Models.Entities;
using RegistrDN.Services.Interfaces;

namespace RegistrDN.Services;

public class DiagnosisFilterService : IDiagnosisFilterService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DiagnosisFilterService> _logger;

    public DiagnosisFilterService(ApplicationDbContext context, ILogger<DiagnosisFilterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<string>> GetActiveDiagnosisCodesAsync()
    {
        try
        {
            var codes = await _context.DsDn
                .Where(x => x.IsActive)
                .Select(x => x.DiagCode)
                .ToListAsync();
            return codes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения списка диагнозов");
            return new List<string>();
        }
    }

    public async Task<bool> IsDiagnosisAllowedAsync(string diagCode)
    {
        try
        {
            return await _context.DsDn
                .AnyAsync(x => x.DiagCode == diagCode && x.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка проверки диагноза {diagCode}");
            return false;
        }
    }

    public async Task<List<DsDnEntity>> GetAllDiagnosesAsync()
    {
        return await _context.DsDn
            .OrderBy(x => x.DiagCode)
            .ToListAsync();
    }

    public async Task<DsDnEntity?> GetDiagnosisByCodeAsync(string diagCode)
    {
        return await _context.DsDn
            .FirstOrDefaultAsync(x => x.DiagCode == diagCode);
    }

    public async Task<bool> AddDiagnosisAsync(DsDnEntity diagnosis)
    {
        try
        {
            var exists = await _context.DsDn
                .AnyAsync(x => x.DiagCode == diagnosis.DiagCode);
            if (exists)
                return false;

            await _context.DsDn.AddAsync(diagnosis);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка добавления диагноза {diagnosis.DiagCode}");
            return false;
        }
    }

    public async Task<bool> UpdateDiagnosisAsync(DsDnEntity diagnosis)
    {
        try
        {
            var existing = await _context.DsDn
                .FirstOrDefaultAsync(x => x.Id == diagnosis.Id);
            if (existing == null)
                return false;

            existing.DiagName = diagnosis.DiagName;
            existing.IsActive = diagnosis.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка обновления диагноза {diagnosis.DiagCode}");
            return false;
        }
    }

    public async Task<bool> ToggleDiagnosisStatusAsync(string diagCode)
    {
        try
        {
            var diagnosis = await _context.DsDn
                .FirstOrDefaultAsync(x => x.DiagCode == diagCode);
            if (diagnosis == null)
                return false;

            diagnosis.IsActive = !diagnosis.IsActive;
            diagnosis.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка изменения статуса диагноза {diagCode}");
            return false;
        }
    }

    public async Task<bool> DeleteDiagnosisAsync(string diagCode)
    {
        try
        {
            var diagnosis = await _context.DsDn
                .FirstOrDefaultAsync(x => x.DiagCode == diagCode);
            if (diagnosis == null)
                return false;

            _context.DsDn.Remove(diagnosis);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка удаления диагноза {diagCode}");
            return false;
        }
    }
}
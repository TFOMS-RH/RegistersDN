using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RegistrDN.Data;

namespace RegistrDN.Services.Validation;

/// <summary>
/// Сервис для логического контроля (ЛК) через хранимую процедуру
/// </summary>
public class LogicalValidator
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LogicalValidator> _logger;

    public LogicalValidator(ApplicationDbContext context, ILogger<LogicalValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Запуск логического контроля для документа
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateAsync(int documentId)
    {
        var errors = new List<string>();

        try
        {
            _logger.LogInformation($"Запуск логического контроля для документа {documentId}");

            var parameters = new[]
            {
                new SqlParameter("@DocumentId", documentId),
                new SqlParameter("@IsValid", System.Data.SqlDbType.Bit) { Direction = System.Data.ParameterDirection.Output },
                new SqlParameter("@Errors", System.Data.SqlDbType.NVarChar, -1) { Direction = System.Data.ParameterDirection.Output }
            };

            await _context.Database.ExecuteSqlRawAsync("EXEC sp_ValidateDocument @DocumentId, @IsValid OUTPUT, @Errors OUTPUT",parameters);

            var isValid = parameters[1].Value != DBNull.Value && (bool)parameters[1].Value;
            var errorsText = parameters[2].Value != DBNull.Value ? parameters[2].Value.ToString() : null;

            if (!string.IsNullOrEmpty(errorsText))
            {
                errors.AddRange(errorsText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e)));
            }

            _logger.LogInformation($"Логический контроль для документа {documentId}: {(isValid ? "Успешно" : "Ошибки")}");
            
            return (isValid, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка выполнения логического контроля для документа {documentId}");
            errors.Add($"Ошибка выполнения логического контроля: {ex.Message}");
            return (false, errors);
        }
    }
}
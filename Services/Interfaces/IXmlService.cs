namespace RegistrDN.Services.Interfaces;

public interface IXmlService<TImportDto, TExportDto, TEntity>
    where TImportDto : class
    where TExportDto : class
    where TEntity : class
{
    Task<TImportDto> ParseXmlAsync(string xmlContent);
    Task<string> SerializeToXmlAsync(TExportDto exportData);
    Task<bool> ValidateXmlAsync(string xmlContent);
    Task<(bool success, string message, int recordsCount, List<string> errors)> ImportAsync(
        string xmlContent,
        int documentId);
    Task<string> ExportAsync(int documentId);
}

/// <summary>
/// Расширенный интерфейс для сервисов с ответными файлами (DSPN, PROF)
/// </summary>
public interface IXmlServiceWithResponse<TImportDto, TExportDto, TEntity, TResponseDto>
    : IXmlService<TImportDto, TExportDto, TEntity>
    where TImportDto : class
    where TExportDto : class
    where TEntity : class
    where TResponseDto : class
{
    /// <summary>
    /// Генерация ответного файла после импорта
    /// </summary>
    Task<TResponseDto> GenerateResponseAsync(int documentId, List<string> errors);
    
    /// <summary>
    /// Сериализация ответного файла
    /// </summary>
    Task<string> SerializeResponseToXmlAsync(TResponseDto responseData);
}
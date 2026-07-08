namespace RegistrDN.Services.Interfaces;


/// <typeparam name="TImportDto">DTO для импорта</typeparam>
/// <typeparam name="TExportDto">DTO для экспорта</typeparam>
/// <typeparam name="TEntity">Сущность БД</typeparam>
public interface IXmlService<TImportDto, TExportDto, TEntity>
    where TImportDto : class
    where TExportDto : class
    where TEntity : class
{

    /// Парсинг XML строки в DTO

    Task<TImportDto> ParseXmlAsync(string xmlContent);


    /// Сериализация DTO в XML строку

    Task<string> SerializeToXmlAsync(TExportDto exportData);


    /// Валидация XML содержимого

    Task<bool> ValidateXmlAsync(string xmlContent);


    /// Импорт XML в БД

    Task<(bool success, string message, int recordsCount, List<string> errors)> ImportAsync(
        string xmlContent, 
        int documentId);


    /// Экспорт данных из БД в XML

    Task<string> ExportAsync(int documentId);
}
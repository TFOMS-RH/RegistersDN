using System.IO.Compression;
using System.Text.RegularExpressions;

namespace RegistrDN.Services.Zip;

public class ZipValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? XmlContent { get; set; }
    public string? XmlFileName { get; set; }
    public string? FileType { get; set; } // GST, GPT, GF, GSM, GPM
}

public class ZipValidationService
{
    private readonly ILogger<ZipValidationService> _logger;

    // Допустимые типы файлов
    private readonly HashSet<string> _allowedFileTypes = new()
    {
        "GST", "GPT", "GF", "GSM", "GPM"
    };

    public ZipValidationService(ILogger<ZipValidationService> logger)
    {
        _logger = logger;
    }


    /// Проверка и извлечение XML из ZIP архива

    public async Task<ZipValidationResult> ValidateAndExtractAsync(IFormFile zipFile)
    {
        var result = new ZipValidationResult();

        try
        {
            // 1. Проверка расширения файла
            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = "Файл должен иметь расширение .zip";
                return result;
            }

            // 2. Извлечение имени файла без расширения
            var zipFileName = Path.GetFileNameWithoutExtension(zipFile.FileName);
            
            // 3. Проверка формата имени (поддерживаем все типы)
            var fileNamePattern = new Regex(@"^(GST|GPT|GF|GSM|GPM)(\d+|[A-Z0-9]{6})_\d+_\d+$");
            if (!fileNamePattern.IsMatch(zipFileName))
            {
                result.ErrorMessage = $"Некорректный формат имени файла: {zipFileName}. Ожидается: [ТИП][номер/код]_[дата]_[номер]";
                return result;
            }

            // 4. Определяем тип файла из имени
            var fileTypeMatch = new Regex(@"^(GST|GPT|GF|GSM|GPM)").Match(zipFileName);
            if (!fileTypeMatch.Success)
            {
                result.ErrorMessage = "Не удалось определить тип файла (GST/GPT/GF/GSM/GPM)";
                return result;
            }
            result.FileType = fileTypeMatch.Groups[1].Value;

            // 5. Распаковка ZIP
            using var memoryStream = new MemoryStream();
            await zipFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            // 6. Поиск XML файла в архиве
            var xmlEntries = archive.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name) && 
                            e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!xmlEntries.Any())
            {
                result.ErrorMessage = "В архиве не найден XML файл";
                return result;
            }

            if (xmlEntries.Count > 1)
            {
                result.ErrorMessage = $"В архиве найдено несколько XML файлов: {string.Join(", ", xmlEntries.Select(e => e.Name))}";
                return result;
            }

            var xmlEntry = xmlEntries.First();
            var xmlFileName = Path.GetFileNameWithoutExtension(xmlEntry.Name);

            // 7. Проверка совпадения имен (ZIP и XML должны совпадать)
            if (!zipFileName.Equals(xmlFileName, StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"Имя ZIP файла ({zipFileName}) не совпадает с именем XML файла ({xmlFileName})";
                return result;
            }

            // 8. Чтение содержимого XML
            using var xmlStream = xmlEntry.Open();
            using var reader = new StreamReader(xmlStream);
            result.XmlContent = await reader.ReadToEndAsync();
            result.XmlFileName = xmlEntry.Name;

            // 9. Проверка, что XML не пустой
            if (string.IsNullOrWhiteSpace(result.XmlContent))
            {
                result.ErrorMessage = "XML файл пуст";
                return result;
            }

            // 10. Валидация XML (базовая)
            try
            {
                var xmlDoc = System.Xml.Linq.XDocument.Parse(result.XmlContent);
                result.IsValid = true;
            }
            catch (System.Xml.XmlException ex)
            {
                result.ErrorMessage = $"Ошибка парсинга XML: {ex.Message}";
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки ZIP файла");
            result.ErrorMessage = $"Ошибка обработки архива: {ex.Message}";
            return result;
        }
    }


    /// Определение типа файла по имени

    public string? DetectFileType(string fileName)
    {
        var match = new Regex(@"^(GST|GPT|GF|GSM|GPM)").Match(fileName);
        return match.Success ? match.Groups[1].Value : null;
    }
}
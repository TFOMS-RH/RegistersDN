using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace RegistrDN.Services.Zip;

public class ZipValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? XmlContent { get; set; }
    public string? XmlFileName { get; set; }
    public string? FileType { get; set; }
}

public class ZipValidationService
{
    private readonly ILogger<ZipValidationService> _logger;

    private readonly HashSet<string> _allowedFileTypes = new()
    {
        "GST", "GPT", "GF", "GSM", "GPM", "DSPN", "PROF", "DF"
    };

    public ZipValidationService(ILogger<ZipValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<ZipValidationResult> ValidateAndExtractAsync(IFormFile zipFile)
    {
        var result = new ZipValidationResult();

        try
        {
            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = "Файл должен иметь расширение .zip";
                return result;
            }

            var zipFileName = Path.GetFileNameWithoutExtension(zipFile.FileName);

            var fileTypeMatch = new Regex(@"^(GST|GPT|GF|GSM|GPM|DSPN|PROF|DF)").Match(zipFileName);
            if (!fileTypeMatch.Success)
            {
                result.ErrorMessage = "Не удалось определить тип файла";
                return result;
            }
            result.FileType = fileTypeMatch.Groups[1].Value;

            // Валидация формата имени
            bool isValidFormat = false;

            switch (result.FileType)
            {
                case "GST":
                case "GPT":
                    isValidFormat = Regex.IsMatch(zipFileName, @"^(GST|GPT)\d+_\d+_\d+$");
                    break;
                case "GF":
                    isValidFormat = Regex.IsMatch(zipFileName, @"^GF\d+_\d+(_\d+)?$");
                    break;
                case "GSM":
                case "GPM":
                    isValidFormat = Regex.IsMatch(zipFileName, @"^(GSM|GPM)\d{6}_\d+_\d+$");
                    break;
                case "DSPN":
                    isValidFormat = Regex.IsMatch(zipFileName, @"^DSPN_M\d{6}T\d{2}_\d+$");
                    break;
                case "PROF":
                    isValidFormat = Regex.IsMatch(zipFileName, @"^PROF_M\d{6}T\d{2}_\d+$");
                    break;
                case "DF":
                    isValidFormat = Regex.IsMatch(zipFileName, @"^DF\d+_\d+$");
                    break;
                default:
                    isValidFormat = false;
                    break;
            }

            if (!isValidFormat)
            {
                result.ErrorMessage = $"Некорректный формат имени файла: {zipFileName}";
                return result;
            }

            using var memoryStream = new MemoryStream();
            await zipFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

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
                result.ErrorMessage = $"В архиве найдено несколько XML файлов";
                return result;
            }

            var xmlEntry = xmlEntries.First();
            result.XmlFileName = xmlEntry.Name;


            using var xmlStream = xmlEntry.Open();
            using var reader = new StreamReader(xmlStream, Encoding.GetEncoding("windows-1251"));
            result.XmlContent = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(result.XmlContent))
            {
                result.ErrorMessage = "XML файл пуст";
                return result;
            }

            // Проверка XML на валидность
            try
            {
                var xmlDoc = System.Xml.Linq.XDocument.Parse(result.XmlContent);
                result.IsValid = true;
            }
            catch (System.Xml.XmlException ex)
            {
                _logger.LogError(ex, "Ошибка парсинга XML");
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

    public string? DetectFileType(string fileName)
    {
        var match = new Regex(@"^(GST|GPT|GF|GSM|GPM|DSPN|PROF|DF)").Match(fileName);
        return match.Success ? match.Groups[1].Value : null;
    }
}
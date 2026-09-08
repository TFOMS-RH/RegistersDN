using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Models.Entities;
using RegistrDN.Models.Enums;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Services.Interfaces;
using RegistrDN.Services.Zip;
using System.Text;
using System.Text.RegularExpressions;

namespace RegistrDN.Controllers;

[Authorize(Roles = "Admin, MO")]
public class ImportController : Controller
{
    private const bool SKIP_VALIDATION = false;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportController> _logger;
    private readonly ZipValidationService _zipService;

    public ImportController(
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider,
        ILogger<ImportController> logger,
        ZipValidationService zipService)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _zipService = zipService;
    }

    /// <summary>
    /// Страница импорта
    /// </summary>
    public IActionResult Index()
    {
        _logger.LogInformation("Открыта страница импорта");
        return View();
    }

    /// <summary>
    /// Загрузка файла
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, MO")]
    public async Task<IActionResult> Upload(IFormFile file, string period)
    {
        var requestId = Guid.NewGuid().ToString();
        _logger.LogInformation("=== НАЧАЛО ЗАГРУЗКИ [RequestId: {RequestId}] ===", requestId);

        try
        {
            // 1. Проверка периода
            _logger.LogInformation("[{RequestId}] Шаг 1: Проверка периода", requestId);

            if (string.IsNullOrEmpty(period) || !Regex.IsMatch(period, @"^\d{4}-\d{2}$"))
            {
                _logger.LogWarning("[{RequestId}] Неверный период: {Period}", requestId, period);
                TempData["Error"] = "Неверный формат периода. Используйте: ГГГГ-ММ (например: 2026-07)";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("[{RequestId}] Период валиден: {Period}", requestId, period);

            // 2. Проверка файла
            _logger.LogInformation("[{RequestId}] Шаг 2: Проверка файла", requestId);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("[{RequestId}] Файл не выбран или пустой", requestId);
                TempData["Error"] = "Пожалуйста, выберите файл";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("[{RequestId}] Файл: {FileName}, Размер: {Size} байт, Тип: {ContentType}",
                requestId, file.FileName, file.Length, file.ContentType);

            if (file.Length > 50 * 1024 * 1024)
            {
                _logger.LogWarning("[{RequestId}] Файл слишком большой: {Size} байт (лимит 50MB)", requestId, file.Length);
                TempData["Error"] = "Размер файла не должен превышать 50MB";
                return RedirectToAction(nameof(Index));
            }

            // 3. Обработка файла
            string xmlContent;
            string fileName;
            string fileType;
            string? originalFileName = null;
            string? hospitalCode = null;

            _logger.LogInformation("[{RequestId}] Шаг 3: Обработка файла", requestId);

            if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[{RequestId}] Обработка ZIP архива", requestId);

                var validationResult = await _zipService.ValidateAndExtractAsync(file);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("[{RequestId}] Ошибка валидации ZIP: {Error}", requestId, validationResult.ErrorMessage);
                    TempData["Error"] = validationResult.ErrorMessage;
                    return RedirectToAction(nameof(Index));
                }

                xmlContent = validationResult.XmlContent!;
                fileName = validationResult.XmlFileName!;
                fileType = validationResult.FileType!;
                originalFileName = Path.GetFileNameWithoutExtension(file.FileName);

                _logger.LogInformation("[{RequestId}] ZIP распакован: {FileName}, Тип: {FileType}, XML: {XmlFileName}",
                    requestId, file.FileName, fileType, fileName);

                if (fileType == "GSM" || fileType == "GPM" || fileType == "PROF")
                {
                    hospitalCode = ExtractHospitalCode(fileName);
                    if (string.IsNullOrEmpty(hospitalCode))
                    {
                        _logger.LogWarning("[{RequestId}] Не удалось извлечь код МО из имени файла: {FileName}", requestId, fileName);
                        TempData["Error"] = $"Не удалось извлечь код МО из имени файла: {fileName}";
                        return RedirectToAction(nameof(Index));
                    }
                    _logger.LogInformation("[{RequestId}] Код МО: {HospitalCode}", requestId, hospitalCode);
                }
            }
            else if (file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[{RequestId}] Обработка XML файла", requestId);

                // Определяем кодировку файла
                using var stream = file.OpenReadStream();
                var encoding = DetectEncoding(stream);
                stream.Position = 0;
                
                _logger.LogInformation("[{RequestId}] Определена кодировка файла: {Encoding}", 
                    requestId, encoding?.EncodingName ?? "unknown (будет использована windows-1251)");
                
                // Используем правильную кодировку
                using var reader = new StreamReader(stream, encoding ?? Encoding.GetEncoding("windows-1251"));
                xmlContent = await reader.ReadToEndAsync();

                _logger.LogInformation("[{RequestId}] XML прочитан, длина: {Length} символов", requestId, xmlContent.Length);
                
                // Логируем информацию о кодировке из XML
                var xmlEncoding = GetEncodingFromXml(xmlContent);
                _logger.LogInformation("[{RequestId}] Кодировка указанная в XML: {XmlEncoding}", 
                    requestId, xmlEncoding?.EncodingName ?? "не указана");
                
                _logger.LogInformation("[{RequestId}] Первые 300 символов XML: {XmlPreview}", 
                    requestId, xmlContent.Length > 300 ? xmlContent.Substring(0, 300) : xmlContent);

                fileName = Path.GetFileNameWithoutExtension(file.FileName);

                var fileTypeMatch = new Regex(@"^(GST|GPT|GF|GSM|GPM|DSPN|PROF|DF)").Match(fileName);
                if (!fileTypeMatch.Success)
                {
                    _logger.LogWarning("[{RequestId}] Не удалось определить тип файла: {FileName}", requestId, fileName);
                    TempData["Error"] = "Не удалось определить тип файла";
                    return RedirectToAction(nameof(Index));
                }
                fileType = fileTypeMatch.Groups[1].Value;
                _logger.LogInformation("[{RequestId}] Тип файла: {FileType}", requestId, fileType);

                if (fileType == "GSM" || fileType == "GPM" || fileType == "PROF")
                {
                    hospitalCode = ExtractHospitalCode(fileName);
                    if (string.IsNullOrEmpty(hospitalCode))
                    {
                        _logger.LogWarning("[{RequestId}] Не удалось извлечь код МО из имени файла: {FileName}", requestId, fileName);
                        TempData["Error"] = $"Не удалось извлечь код МО из имени файла: {fileName}";
                        return RedirectToAction(nameof(Index));
                    }
                    _logger.LogInformation("[{RequestId}] Код МО: {HospitalCode}", requestId, hospitalCode);
                }
            }
            else
            {
                _logger.LogWarning("[{RequestId}] Неподдерживаемый тип файла: {FileName}", requestId, file.FileName);
                TempData["Error"] = "Поддерживаются только файлы .zip и .xml";
                return RedirectToAction(nameof(Index));
            }

            // ⚡ ДОПОЛНИТЕЛЬНОЕ ЛОГИРОВАНИЕ ДЛЯ ПРОВЕРКИ КИРИЛЛИЦЫ
            _logger.LogInformation("[{RequestId}] Проверка кириллицы в XML:", requestId);
            
            // Проверяем наличие кириллицы в первых 500 символах
            var sample = xmlContent.Length > 500 ? xmlContent.Substring(0, 500) : xmlContent;
            var hasCyrillic = Regex.IsMatch(sample, @"[А-Яа-яЁё]");
            _logger.LogInformation("[{RequestId}] Наличие кириллицы в первых 500 символах: {HasCyrillic}", requestId, hasCyrillic);
            
            if (hasCyrillic)
            {
                // Находим несколько кириллических слов
                var cyrillicMatches = Regex.Matches(sample, @"[А-Яа-яЁё]{2,}");
                var words = cyrillicMatches.Take(3).Select(m => m.Value).ToList();
                _logger.LogInformation("[{RequestId}] Примеры кириллических слов: {Words}", requestId, string.Join(", ", words));
            }

            // 4. Проверка прав для MO
            if (User.IsInRole("MO") && fileType != "DSPN" && fileType != "PROF")
            {
                _logger.LogWarning("[{RequestId}] MO пытается загрузить неразрешенный тип: {FileType}", requestId, fileType);
                TempData["Error"] = "МО может загружать только файлы DSPN и PROF";
                return RedirectToAction(nameof(Index));
            }

            // 5. Проверка на дубликат
            _logger.LogInformation("[{RequestId}] Шаг 4: Проверка на дубликат", requestId);

            var exists = await _unitOfWork.Documents
                .AnyAsync(x => x.FileName == fileName && x.Period == period && x.HospitalCode == hospitalCode);
            if (exists)
            {
                _logger.LogWarning("[{RequestId}] Дубликат: {FileName} за период {Period}", requestId, fileName, period);
                TempData["Error"] = $"Документ '{fileName}' за период {period} уже загружен!";
                return RedirectToAction(nameof(Index));
            }

            // 6. Сохранение документа
            _logger.LogInformation("[{RequestId}] Шаг 5: Сохранение документа в БД", requestId);

            var document = new DnDocumentEntity
            {
                FileName = fileName,
                FileType = fileType,
                RegionCode = "19",
                HospitalCode = hospitalCode,
                Period = period,
                FileDate = DateTime.Now,
                UploadDate = DateTime.Now,
                IsValid = false,
                Status = DocumentStatus.Uploaded,
                UploadedBy = User.Identity?.Name ?? "System"
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[{RequestId}] Документ сохранен, ID: {DocumentId}, Статус: Загружен", requestId, document.Id);

            // 7. Получение сервиса для импорта
            _logger.LogInformation("[{RequestId}] Шаг 6: Получение сервиса для типа {FileType}", requestId, fileType);

            var service = GetXmlService(fileType);
            if (service == null)
            {
                _logger.LogError("[{RequestId}] Сервис для типа {FileType} не найден", requestId, fileType);
                TempData["Error"] = $"Сервис для типа {fileType} не найден";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("[{RequestId}] Сервис получен: {ServiceType}", requestId, service.GetType().Name);

            // 8. Импорт данных
            _logger.LogInformation("[{RequestId}] Шаг 7: Импорт данных", requestId);

            var importMethod = service.GetType().GetMethod("ImportAsync");
            if (importMethod == null)
            {
                _logger.LogError("[{RequestId}] Метод ImportAsync не найден в {ServiceType}", requestId, service.GetType().Name);
                TempData["Error"] = "Метод ImportAsync не найден";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("[{RequestId}] Метод ImportAsync найден, вызываем...", requestId);

            try
            {
                var importTask = importMethod.Invoke(service, new object[] { xmlContent, document.Id });
                var result = await (Task<(bool success, string message, int recordsCount, List<string> errors)>)importTask!;

                _logger.LogInformation("[{RequestId}] Импорт завершен: Success={Success}, Records={Records}, Errors={Errors}",
                    requestId, result.success, result.recordsCount, string.Join("; ", result.errors.Take(5)));

                if (result.success)
                {
                    document.RecordsCount = result.recordsCount;
                    document.IsValid = true;
                    document.Status = DocumentStatus.Ready;
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("[{RequestId}] Документ обновлен: Records={Records}, Status=Ready", 
                        requestId, result.recordsCount);

                    TempData["Success"] = $"✅ {fileType}: Импортировано {result.recordsCount} записей (Период: {period})";
                    if (result.errors.Any())
                    {
                        TempData["Warnings"] = string.Join("<br />", result.errors.Take(10));
                    }
                }
                else
                {
                    _logger.LogError("[{RequestId}] Ошибка импорта {FileType}: {Message}", requestId, fileType, result.message);
                    document.IsValid = false;
                    document.Status = DocumentStatus.Error;
                    document.ValidationErrors = string.Join("; ", result.errors.Take(10));
                    await _unitOfWork.SaveChangesAsync();

                    TempData["Error"] = $"❌ {fileType}: {result.message}";
                    if (result.errors.Any())
                    {
                        TempData["Errors"] = string.Join("<br />", result.errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] Ошибка при вызове ImportAsync", requestId);
                document.IsValid = false;
                document.Status = DocumentStatus.Error;
                document.ValidationErrors = $"Ошибка: {ex.Message}";
                await _unitOfWork.SaveChangesAsync();
                TempData["Error"] = $"Ошибка импорта: {ex.Message}";
            }

            _logger.LogInformation("[{RequestId}] === ЗАГРУЗКА ЗАВЕРШЕНА ===", requestId);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] КРИТИЧЕСКАЯ ОШИБКА загрузки", requestId);
            TempData["Error"] = $"Критическая ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Извлечение кода МО из имени файла GSM/GPM
    /// </summary>
    private string? ExtractHospitalCode(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var match = Regex.Match(nameWithoutExt, @"^(GSM|GPM)(\d{6})_\d+_\d+$");
        return match.Success ? match.Groups[2].Value : null;
    }

    /// <summary>
    /// Получение сервиса для работы с XML
    /// </summary>
    private object? GetXmlService(string fileType)
    {
        _logger.LogInformation("GetXmlService вызван для типа: {FileType}", fileType);

        object? result = fileType.ToUpper() switch
        {
            "GST" => _serviceProvider.GetService<IXmlService<GstImportDto, GstExportDto, GstEntity>>(),
            "GPT" => _serviceProvider.GetService<IXmlService<GptImportDto, GptExportDto, GptEntity>>(),
            "GF" => _serviceProvider.GetService<IXmlService<GfImportDto, GfExportDto, GfEntity>>(),
            "GSM" => _serviceProvider.GetService<IXmlService<GsmImportDto, GsmExportDto, GstEntity>>(),
            "GPM" => _serviceProvider.GetService<IXmlService<GpmImportDto, GpmExportDto, GptEntity>>(),
            "DSPN" => _serviceProvider.GetService<IXmlServiceWithResponse<DspnImportDto, DspnExportDto, DspnEntity, DspnResponseDto>>(),
            "PROF" => _serviceProvider.GetService<IXmlServiceWithResponse<ProfImportDto, ProfExportDto, ProfEntity, ProfResponseDto>>(),
            "DF" => _serviceProvider.GetService<IXmlService<DfImportDto, DfExportDto, DfEntity>>(),
            _ => null
        };

        _logger.LogInformation("GetXmlService вернул: {Result}", result == null ? "null" : result.GetType().Name);
        return result;
    }

    /// <summary>
    /// Определение кодировки из BOM или по содержанию
    /// </summary>
    private Encoding? DetectEncoding(Stream stream)
    {
        if (!stream.CanSeek)
            return null;

        var originalPosition = stream.Position;
        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            
            // Читаем первые 4 байта
            byte[] bom = reader.ReadBytes(4);
            stream.Position = originalPosition;

            // UTF-8 BOM: EF BB BF
            if (bom.Length >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return Encoding.UTF8;

            // UTF-16 LE BOM: FF FE
            if (bom.Length >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode;

            // UTF-16 BE BOM: FE FF
            if (bom.Length >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            // UTF-32 LE BOM: FF FE 00 00
            if (bom.Length >= 4 && bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
                return Encoding.UTF32;

            // По умолчанию windows-1251 (кириллица)
            return Encoding.GetEncoding("windows-1251");
        }
        catch
        {
            return null;
        }
        finally
        {
            // Восстанавливаем позицию
            try
            {
                stream.Position = originalPosition;
            }
            catch
            {
                // Если не удалось восстановить позицию, игнорируем
            }
        }
    }

    /// <summary>
    /// Извлечение кодировки из XML declaration
    /// </summary>
    private Encoding? GetEncodingFromXml(string xmlContent)
    {
        try
        {
            var match = Regex.Match(xmlContent, @"encoding\s*=\s*[""']([^""']+)[""']");
            if (match.Success)
            {
                var encodingName = match.Groups[1].Value;
                try
                {
                    return Encoding.GetEncoding(encodingName);
                }
                catch
                {
                    _logger.LogWarning("Неизвестная кодировка: {EncodingName}", encodingName);
                    return null;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при определении кодировки из XML");
            return null;
        }
    }

    /// <summary>
    /// Проверка содержимого на кириллицу (для отладки)
    /// </summary>
    private bool HasCyrillic(string text)
    {
        return Regex.IsMatch(text, @"[А-Яа-яЁё]");
    }

    /// <summary>
    /// Получение образца кириллического текста (для отладки)
    /// </summary>
    private List<string> GetCyrillicSample(string text, int count = 3)
    {
        var matches = Regex.Matches(text, @"[А-Яа-яЁё]{2,}");
        return matches.Take(count).Select(m => m.Value).ToList();
    }
}
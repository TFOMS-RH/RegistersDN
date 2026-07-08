using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Models.Entities;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Services.Interfaces;
using RegistrDN.Services.Xml;
using RegistrDN.Services.Zip;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace RegistrDN.Controllers;

public class ImportController : Controller
{
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

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string period)
    {

        if (string.IsNullOrEmpty(period))
        {
            TempData["Error"] = "Выбор периода обязателен! Пожалуйста, выберите период перед загрузкой файла.";
            return RedirectToAction(nameof(Index));
        }

        if (!Regex.IsMatch(period, @"^\d{4}-\d{2}$"))
        {
            TempData["Error"] = "Неверный формат периода. Используйте: ГГГГ-ММ (например: 2026-07)";
            return RedirectToAction(nameof(Index));
        }

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Пожалуйста, выберите файл";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > 50 * 1024 * 1024)
        {
            TempData["Error"] = "Размер файла не должен превышать 50MB";
            return RedirectToAction(nameof(Index));
        }

        string xmlContent;
        string fileName;
        string fileType;
        string? originalFileName = null;
        string? hospitalCode = null;

        try
        {

            if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var validationResult = await _zipService.ValidateAndExtractAsync(file);

                if (!validationResult.IsValid)
                {
                    TempData["Error"] = validationResult.ErrorMessage;
                    return RedirectToAction(nameof(Index));
                }

                xmlContent = validationResult.XmlContent!;
                fileName = validationResult.XmlFileName!;
                fileType = validationResult.FileType!;
                originalFileName = Path.GetFileNameWithoutExtension(file.FileName);

                if (fileType == "GSM" || fileType == "GPM")
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    hospitalCode = ExtractHospitalCode(nameWithoutExt);
                    
                    if (string.IsNullOrEmpty(hospitalCode))
                    {
                        TempData["Error"] = $"Не удалось извлечь код больницы из имени файла: {fileName}. Ожидается формат: GSM190006_2605_1 или GPM190006_2605_1";
                        return RedirectToAction(nameof(Index));
                    }
                }

                TempData["Info"] = $"Распакован архив: {file.FileName} → {fileName} (Период: {period})";
                if (!string.IsNullOrEmpty(hospitalCode))
                {
                    TempData["Info"] += $" (Код МО: {hospitalCode})";
                }
            }
            else if (file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                xmlContent = await reader.ReadToEndAsync();

                fileName = Path.GetFileNameWithoutExtension(file.FileName);
                
                var fileTypeMatch = new Regex(@"^(GST|GPT|GF|GSM|GPM)").Match(fileName);
                if (!fileTypeMatch.Success)
                {
                    TempData["Error"] = "Не удалось определить тип файла по имени. Используйте формат: GST*, GPT*, GF*, GSM*, GPM*";
                    return RedirectToAction(nameof(Index));
                }
                fileType = fileTypeMatch.Groups[1].Value;

                if (fileType == "GSM" || fileType == "GPM")
                {
                    hospitalCode = ExtractHospitalCode(fileName);
                    if (string.IsNullOrEmpty(hospitalCode))
                    {
                        TempData["Error"] = $"Не удалось извлечь код больницы из имени файла: {file.FileName}. Ожидается формат: GSM190006_2605_1.xml или GPM190006_2605_1.xml";
                        return RedirectToAction(nameof(Index));
                    }
                }

                TempData["Info"] = $"Выбран файл: {file.FileName} (Период: {period})";
                if (!string.IsNullOrEmpty(hospitalCode))
                {
                    TempData["Info"] += $" (Код МО: {hospitalCode})";
                }
            }
            else
            {
                TempData["Error"] = "Поддерживаются только файлы .zip и .xml";
                return RedirectToAction(nameof(Index));
            }

            var service = GetXmlService(fileType);
            if (service == null)
            {
                TempData["Error"] = $"Сервис для типа {fileType} не найден";
                return RedirectToAction(nameof(Index));
            }


            var validateMethod = service.GetType().GetMethod("ValidateXmlAsync");
            if (validateMethod == null)
            {
                TempData["Error"] = "Метод ValidateXmlAsync не найден";
                return RedirectToAction(nameof(Index));
            }

            var validateTask = (Task<bool>)validateMethod.Invoke(service, new object[] { xmlContent })!;
            var isValid = await validateTask;

            if (!isValid)
            {
                TempData["Error"] = $"Неверная структура XML файла {fileType}";
                return RedirectToAction(nameof(Index));
            }

            var document = new DnDocumentEntity
            {
                FileName = fileName,
                FileType = fileType,
                XmlContent = xmlContent,
                RegionCode = "77",
                HospitalCode = hospitalCode,
                Period = period,  
                FileDate = DateTime.Now,
                UploadDate = DateTime.Now,
                IsValid = true,
                UploadedBy = User.Identity?.Name ?? "System"
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            var importMethod = service.GetType().GetMethod("ImportAsync");
            if (importMethod == null)
            {
                TempData["Error"] = "Метод ImportAsync не найден";
                return RedirectToAction(nameof(Index));
            }

            var importTask = importMethod.Invoke(service, new object[] { xmlContent, document.Id });
            var result = await (Task<(bool success, string message, int recordsCount, List<string> errors)>)importTask!;

            if (result.success)
            {
                var sourceInfo = originalFileName != null ? $" (из {originalFileName}.zip)" : "";
                var hospitalInfo = !string.IsNullOrEmpty(hospitalCode) ? $" (МО: {hospitalCode})" : "";
                TempData["Success"] = $" {fileType}: Импортировано {result.recordsCount} записей{sourceInfo}{hospitalInfo} (Период: {period})";
                
                if (result.errors.Any())
                {
                    TempData["Warnings"] = string.Join("<br />", result.errors);
                }
            }
            else
            {
                TempData["Error"] = $" {fileType}: {result.message}";
                if (result.errors.Any())
                {
                    TempData["Errors"] = string.Join("<br />", result.errors);
                }
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки файла");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private string? ExtractHospitalCode(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        
        var match = Regex.Match(nameWithoutExt, @"^(GSM|GPM)(\d{6})_\d+_\d+$");
        if (match.Success)
        {
            return match.Groups[2].Value; 
        }
        return null;
    }

    private object? GetXmlService(string fileType)
    {
        return fileType.ToUpper() switch
        {
            "GST" => _serviceProvider.GetService<IXmlService<GstImportDto, GstExportDto, GstEntity>>(),
            "GPT" => _serviceProvider.GetService<IXmlService<GptImportDto, GptExportDto, GptEntity>>(),
            "GF" => _serviceProvider.GetService<IXmlService<GfImportDto, GfExportDto, GfEntity>>(),
            "GSM" => _serviceProvider.GetService<IXmlService<GsmImportDto, GsmExportDto, GstEntity>>(),
            "GPM" => _serviceProvider.GetService<IXmlService<GpmImportDto, GpmExportDto, GptEntity>>(),
            _ => null
        };
    }
}
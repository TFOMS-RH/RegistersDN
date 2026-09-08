using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Models.Entities;
using RegistrDN.Services.Interfaces;
using RegistrDN.Services.Zip;

namespace RegistrDN.Controllers;

[Authorize]
public class ProfController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProfController> _logger;
    private readonly ZipValidationService _zipService;

    public ProfController(
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider,
        ILogger<ProfController> logger,
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
    public async Task<IActionResult> Upload(IFormFile file)
    {
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
        string? originalFileName = null;
        string period;

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
                originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                period = DateTime.Now.ToString("yyyyMM");

                TempData["Info"] = $"Распакован архив: {file.FileName} → {fileName}";
            }
            else if (file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                xmlContent = await reader.ReadToEndAsync();

                fileName = Path.GetFileNameWithoutExtension(file.FileName);
                period = DateTime.Now.ToString("yyyyMM");
                TempData["Info"] = $"Выбран файл: {file.FileName}";
            }
            else
            {
                TempData["Error"] = "Поддерживаются только файлы .zip и .xml";
                return RedirectToAction(nameof(Index));
            }

            var service = _serviceProvider.GetService<IXmlServiceWithResponse<ProfImportDto, ProfExportDto, ProfEntity, ProfResponseDto>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис PROF не найден";
                return RedirectToAction(nameof(Index));
            }

            var isValid = await service.ValidateXmlAsync(xmlContent);
            if (!isValid)
            {
                TempData["Error"] = "Неверная структура XML файла PROF";
                return RedirectToAction(nameof(Index));
            }

            var document = new DnDocumentEntity
            {
                FileName = fileName,
                FileType = "PROF",
               // XmlContent = xmlContent,
                RegionCode = "19",
                Period = period,
                FileDate = DateTime.Now,
                UploadDate = DateTime.Now,
                IsValid = true,
                UploadedBy = User.Identity?.Name ?? "System"
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            var result = await service.ImportAsync(xmlContent, document.Id);

            if (result.success)
            {
                TempData["Success"] = $"Импортировано {result.recordsCount} записей PROF";
                
                var response = await service.GenerateResponseAsync(document.Id, result.errors);
                var responseXml = await service.SerializeResponseToXmlAsync(response);
                
                TempData["ResponseXml"] = responseXml;
                TempData["ResponseFileName"] = $"PROF_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (result.errors.Any())
                {
                    TempData["Warnings"] = string.Join("<br />", result.errors.Take(20));
                }
            }
            else
            {
                TempData["Error"] = $"Ошибка импорта: {result.message}";
                if (result.errors.Any())
                {
                    TempData["Errors"] = string.Join("<br />", result.errors);
                }
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки PROF файла");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult DownloadResponse()
    {
        try
        {
            var responseXml = TempData["ResponseXml"] as string;
            var fileName = TempData["ResponseFileName"] as string ?? $"PROF_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (string.IsNullOrEmpty(responseXml))
            {
                TempData["Error"] = "Нет доступного ответного файла";
                return RedirectToAction(nameof(Index));
            }

            var bytes = System.Text.Encoding.GetEncoding("windows-1251").GetBytes(responseXml);
            return File(bytes, "application/xml", $"{fileName}.xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка скачивания ответа PROF");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Export(int documentId)
    {
        try
        {
            var service = _serviceProvider.GetService<IXmlServiceWithResponse<ProfImportDto, ProfExportDto, ProfEntity, ProfResponseDto>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис PROF не найден";
                return RedirectToAction(nameof(Index));
            }

            var xmlContent = await service.ExportAsync(documentId);
            var fileName = $"PROF_{DateTime.Now:yyyyMMdd_HHmmss}";

            var bytes = System.Text.Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            return File(bytes, "application/xml", $"{fileName}.xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта PROF");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Documents()
    {
        var documents = await _unitOfWork.Documents
            .FindAsync(x => x.FileType == "PROF");

        return View(documents);
    }

    public async Task<IActionResult> Details(int id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        var records = await _unitOfWork.ProfRecords
            .FindAsync(x => x.DocumentId == id);

        ViewBag.Document = document;
        return View(records);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(id);
            if (document == null)
            {
                TempData["Error"] = "Документ не найден";
                return RedirectToAction(nameof(Documents));
            }

            var profRecords = await _unitOfWork.ProfRecords
                .FindAsync(x => x.DocumentId == id);
            if (profRecords.Any())
            {
                var profIds = profRecords.Select(x => x.Id).ToList();
                var merRecords = await _unitOfWork.ProfMerRecords
                    .FindAsync(x => profIds.Contains(x.ProfRecordId));
                if (merRecords.Any())
                {
                    await _unitOfWork.ProfMerRecords.DeleteRangeAsync(merRecords);
                }
                await _unitOfWork.ProfRecords.DeleteRangeAsync(profRecords);
            }

            await _unitOfWork.Documents.DeleteAsync(document);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Документ успешно удален";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления PROF документа");
            TempData["Error"] = $"Ошибка: {ex.Message}";
        }

        return RedirectToAction(nameof(Documents));
    }
}
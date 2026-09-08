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
public class DspnController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DspnController> _logger;
    private readonly ZipValidationService _zipService;

    public DspnController(
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider,
        ILogger<DspnController> logger,
        ZipValidationService zipService)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _zipService = zipService;
    }

    /// <summary>
    /// Страница импорта DSPN
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Загрузка DSPN файла
    /// </summary>
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

            // Получаем сервис DSPN
            var service = _serviceProvider.GetService<IXmlServiceWithResponse<DspnImportDto, DspnExportDto, DspnEntity, DspnResponseDto>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис DSPN не найден";
                return RedirectToAction(nameof(Index));
            }

            // Валидация XML
            var isValid = await service.ValidateXmlAsync(xmlContent);
            if (!isValid)
            {
                TempData["Error"] = "Неверная структура XML файла DSPN";
                return RedirectToAction(nameof(Index));
            }

            // Сохраняем документ
            var document = new DnDocumentEntity
            {
                FileName = fileName,
                FileType = "DSPN",
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

            // Импортируем данные
            var result = await service.ImportAsync(xmlContent, document.Id);

            if (result.success)
            {
                TempData["Success"] = $"Импортировано {result.recordsCount} записей DSPN";
                
                // Получаем ответный файл
                var response = await service.GenerateResponseAsync(document.Id, result.errors);
                var responseXml = await service.SerializeResponseToXmlAsync(response);
                
                // Сохраняем ответ в сессии или TempData
                TempData["ResponseXml"] = responseXml;
                TempData["ResponseFileName"] = $"DSPN_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}";

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
            _logger.LogError(ex, "Ошибка загрузки DSPN файла");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Скачать ответный файл на импорт DSPN
    /// </summary>
    [HttpGet]
    public IActionResult DownloadResponse()
    {
        try
        {
            var responseXml = TempData["ResponseXml"] as string;
            var fileName = TempData["ResponseFileName"] as string ?? $"DSPN_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}";

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
            _logger.LogError(ex, "Ошибка скачивания ответа DSPN");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Экспорт DSPN
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export(int documentId)
    {
        try
        {
            var service = _serviceProvider.GetService<IXmlServiceWithResponse<DspnImportDto, DspnExportDto, DspnEntity, DspnResponseDto>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис DSPN не найден";
                return RedirectToAction(nameof(Index));
            }

            var xmlContent = await service.ExportAsync(documentId);
            var fileName = $"DSPN_{DateTime.Now:yyyyMMdd_HHmmss}";

            var bytes = System.Text.Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            return File(bytes, "application/xml", $"{fileName}.xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта DSPN");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Список загруженных DSPN документов
    /// </summary>
    public async Task<IActionResult> Documents()
    {
        var documents = await _unitOfWork.Documents
            .FindAsync(x => x.FileType == "DSPN");

        return View(documents);
    }

    /// <summary>
    /// Детали документа DSPN
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        var records = await _unitOfWork.DspnRecords
            .FindAsync(x => x.DocumentId == id);

        ViewBag.Document = document;
        return View(records);
    }

    /// <summary>
    /// Удаление документа DSPN
    /// </summary>
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

            // Удаляем связанные записи DSPN
            var dspnRecords = await _unitOfWork.DspnRecords
                .FindAsync(x => x.DocumentId == id);
            if (dspnRecords.Any())
            {
                await _unitOfWork.DspnRecords.DeleteRangeAsync(dspnRecords);
            }

            await _unitOfWork.Documents.DeleteAsync(document);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Документ успешно удален";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления DSPN документа");
            TempData["Error"] = $"Ошибка: {ex.Message}";
        }

        return RedirectToAction(nameof(Documents));
    }
}
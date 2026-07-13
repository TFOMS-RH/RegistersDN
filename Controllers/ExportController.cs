using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Services.Interfaces;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Models.Entities;
using System.IO.Compression;
using System.Text;
using RegistrDN.Services.Xml;
using AutoMapper;

namespace RegistrDN.Controllers;

[Authorize]
public class ExportController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider,
        IMapper mapper,
        ILogger<ExportController> logger)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _logger = logger;
    }

    // ==========================================
    // ГЛАВНАЯ СТРАНИЦА ЭКСПОРТА
    // ==========================================
    public async Task<IActionResult> Index()
    {
        var documents = await _unitOfWork.Documents
            .FindAsync(x => x.FileType == "GST" || x.FileType == "GPT" || x.FileType == "GF" 
                         || x.FileType == "GSM" || x.FileType == "GPM");

        var periods = documents
            .Where(x => !string.IsNullOrEmpty(x.Period))
            .Select(x => x.Period)
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();

        ViewBag.Periods = periods;
        
        return View(documents);
    }

    // ==========================================
    // АГРЕГАЦИЯ GSM → GST
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportAggregatedGst(string? period)
    {
        try
        {
            // 1. Получаем документы GSM с фильтром по периоду
            var gsmDocuments = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GSM");

            if (!string.IsNullOrEmpty(period))
            {
                gsmDocuments = gsmDocuments.Where(x => x.Period == period).ToList();
            }

            if (!gsmDocuments.Any())
            {
                TempData["Error"] = $"Нет документов GSM для агрегации {(string.IsNullOrEmpty(period) ? "" : $"за период {period}")}";
                return RedirectToAction(nameof(Index));
            }

            // 2. Получаем все GST записи из этих документов
            var docIds = gsmDocuments.Select(x => x.Id).ToList();
            var gstRecords = await _unitOfWork.GstRecords
                .FindAsync(x => docIds.Contains(x.DocumentId));

            if (!gstRecords.Any())
            {
                TempData["Error"] = "Нет записей GST для агрегации";
                return RedirectToAction(nameof(Index));
            }

            // 3. Формируем заголовок
            var firstDoc = gsmDocuments.First();
            var regionCode = firstDoc.RegionCode ?? "77";
            
            var header = new GstExportHeader
            {
                FileName = BuildAggregatedFileName("GST", regionCode, period),
                RegionCode = regionCode,
                RecordsCount = gstRecords.Count(),
                FileNumber = 1,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // 4. Маппим записи в Export DTO
            var exportRecords = _mapper.Map<List<GstExportRecord>>(gstRecords);

            // 5. Формируем итоговый DTO
            var exportDto = new GstExportDto
            {
                Header = header,
                Records = exportRecords
            };

            // 6. Получаем сервис для сериализации
            var service = _serviceProvider.GetService<IXmlService<GstImportDto, GstExportDto, GstEntity>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис GST не найден";
                return RedirectToAction(nameof(Index));
            }

            // 7. Сериализуем в XML
            var xmlContent = await service.SerializeToXmlAsync(exportDto);

            // 8. Формируем имя файла для скачивания
            var fileName = BuildAggregatedFileName("GST", regionCode, period);
            var zipFileName = $"{fileName}.zip";
            var xmlFileName = $"{fileName}.xml";

            // 9. Создаем ZIP архив
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(xmlFileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(xmlContent);
            }

            var zipData = memoryStream.ToArray();
            return File(zipData, "application/zip", zipFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка агрегации GSM → GST");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // АГРЕГАЦИЯ GPM → GPT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportAggregatedGpt(string? period)
    {
        try
        {
            // 1. Получаем документы GPM с фильтром по периоду
            var gpmDocuments = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GPM");

            if (!string.IsNullOrEmpty(period))
            {
                gpmDocuments = gpmDocuments.Where(x => x.Period == period).ToList();
            }

            if (!gpmDocuments.Any())
            {
                TempData["Error"] = $"Нет документов GPM для агрегации {(string.IsNullOrEmpty(period) ? "" : $"за период {period}")}";
                return RedirectToAction(nameof(Index));
            }

            // 2. Получаем все GPT записи из этих документов
            var docIds = gpmDocuments.Select(x => x.Id).ToList();
            var gptRecords = await _unitOfWork.GptRecords
                .FindAsync(x => docIds.Contains(x.DocumentId));

            if (!gptRecords.Any())
            {
                TempData["Error"] = "Нет записей GPT для агрегации";
                return RedirectToAction(nameof(Index));
            }

            // 3. Формируем заголовок
            var firstDoc = gpmDocuments.First();
            var regionCode = firstDoc.RegionCode ?? "77";
            
            var header = new GptExportHeader
            {
                FileName = BuildAggregatedFileName("GPT", regionCode, period),
                RegionCode = regionCode,
                RecordsCount = gptRecords.Count(),
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // 4. Маппим записи в Export DTO
            var exportRecords = _mapper.Map<List<GptExportRecord>>(gptRecords);

            // 5. Формируем итоговый DTO
            var exportDto = new GptExportDto
            {
                Header = header,
                Records = exportRecords
            };

            // 6. Получаем сервис для сериализации
            var service = _serviceProvider.GetService<IXmlService<GptImportDto, GptExportDto, GptEntity>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис GPT не найден";
                return RedirectToAction(nameof(Index));
            }

            // 7. Сериализуем в XML
            var xmlContent = await service.SerializeToXmlAsync(exportDto);

            // 8. Формируем имя файла для скачивания
            var fileName = BuildAggregatedFileName("GPT", regionCode, period);
            var zipFileName = $"{fileName}.zip";
            var xmlFileName = $"{fileName}.xml";

            // 9. Создаем ZIP архив
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(xmlFileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(xmlContent);
            }

            var zipData = memoryStream.ToArray();
            return File(zipData, "application/zip", zipFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка агрегации GPM → GPT");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // ОДИНОЧНЫЙ ЭКСПОРТ (БЕЗ ИЗМЕНЕНИЙ)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportGst(int documentId)
    {
        return await ExportFile(documentId, "GST");
    }

    [HttpGet]
    public async Task<IActionResult> ExportGpt(int documentId)
    {
        return await ExportFile(documentId, "GPT");
    }

    [HttpGet]
    public async Task<IActionResult> ExportGf(int documentId)
    {
        return await ExportFile(documentId, "GF");
    }

    [HttpGet]
    public async Task<IActionResult> ExportGsm(int documentId)
    {
        return await ExportFile(documentId, "GSM");
    }

    [HttpGet]
    public async Task<IActionResult> ExportGpm(int documentId)
    {
        return await ExportFile(documentId, "GPM");
    }

    private async Task<IActionResult> ExportFile(int documentId, string fileType)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
            {
                TempData["Error"] = "Документ не найден";
                return RedirectToAction(nameof(Index));
            }

            var service = GetXmlService(fileType);
            if (service == null)
            {
                TempData["Error"] = $"Сервис для типа {fileType} не найден";
                return RedirectToAction(nameof(Index));
            }

            var exportMethod = service.GetType().GetMethod("ExportAsync");
            if (exportMethod == null)
            {
                TempData["Error"] = $"Метод ExportAsync не найден в {service.GetType().Name}";
                return RedirectToAction(nameof(Index));
            }

            var exportTask = exportMethod.Invoke(service, new object[] { documentId });
            var xmlContent = await (Task<string>)exportTask!;

            // Формируем имя файла для скачивания
            var fileName = document.FileName;
            var zipFileName = $"{fileName}.zip";
            var xmlFileName = $"{fileName}.xml";

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(xmlFileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(xmlContent);
            }

            var zipData = memoryStream.ToArray();
            return File(zipData, "application/zip", zipFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка экспорта {fileType}");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ==========================================

    /// <summary>
/// Формирование имени файла для агрегации
/// Формат: [ТИП][КОД_РЕГИОНА]_[2 ЦИФРЫ ГОДА][2 ЦИФРЫ МЕСЯЦА]_[НОМЕР]
/// Пример: GST19_2607_1
/// </summary>
private string BuildAggregatedFileName(string fileType, string regionCode, string? period)
{
    // Получаем год и месяц из периода
    string yearShort;
    string month;
    
    if (!string.IsNullOrEmpty(period) && period.Length >= 7)
    {
        // period = "2026-07"
        var parts = period.Split('-');
        yearShort = parts[0].Substring(2, 2); // "26"
        month = parts[1]; // "07"
    }
    else
    {
        // Если период не указан - используем текущие
        yearShort = DateTime.Now.ToString("yy");
        month = DateTime.Now.ToString("MM");
    }

    // Номер версии (пока всегда 1)
    var version = "1";

    // Собираем имя: GST19_2607_1
    return $"{fileType}{regionCode}_{yearShort}{month}_{version}";
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
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
using Microsoft.AspNetCore.Authorization;

namespace RegistrDN.Controllers;

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

    public async Task<IActionResult> Index()
    {
        var documents = await _unitOfWork.Documents
            .FindAsync(x => x.FileType == "GST" || x.FileType == "GPT" || x.FileType == "GF" 
                         || x.FileType == "GSM" || x.FileType == "GPM");

        ViewBag.Months = GetMonths();
        ViewBag.Years = GetYears();
        
        return View(documents);
    }

    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        var count = await _unitOfWork.Documents.CountAsync();
        return Json(new { count });
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
                return Json(new { success = false, message = "Документ не найден" });
            }

            if (document.FileType == "GST" || document.FileType == "GSM")
            {
                var gstRecords = await _unitOfWork.GstRecords
                    .FindAsync(x => x.DocumentId == id);
                if (gstRecords.Any())
                {
                    await _unitOfWork.GstRecords.DeleteRangeAsync(gstRecords);
                }
            }
            else if (document.FileType == "GPT" || document.FileType == "GPM")
            {
                var gptRecords = await _unitOfWork.GptRecords
                    .FindAsync(x => x.DocumentId == id);
                if (gptRecords.Any())
                {
                    await _unitOfWork.GptRecords.DeleteRangeAsync(gptRecords);
                }
            }
            else if (document.FileType == "GF")
            {
                var gfRecords = await _unitOfWork.GfRecords
                    .FindAsync(x => x.DocumentId == id);
                if (gfRecords.Any())
                {
                    await _unitOfWork.GfRecords.DeleteRangeAsync(gfRecords);
                }
            }

            await _unitOfWork.Documents.DeleteAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return Json(new { success = true, message = "Документ успешно удален" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления документа");
            return Json(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        ViewBag.Document = document;

        switch (document.FileType)
        {
            case "GST":
            case "GSM":
                var gstRecords = await _unitOfWork.GstRecords
                    .FindAsync(x => x.DocumentId == id);
                return View("DetailsGst", gstRecords);
                
            case "GPT":
            case "GPM":
                var gptRecords = await _unitOfWork.GptRecords
                    .FindAsync(x => x.DocumentId == id);
                return View("DetailsGpt", gptRecords);
                
            case "GF":
                var gfRecords = await _unitOfWork.GfRecords
                    .FindAsync(x => x.DocumentId == id);
                return View("DetailsGf", gfRecords);
                
            default:
                return NotFound();
        }
    }


    [HttpGet]
    public async Task<IActionResult> ExportAggregatedGst(int? year, int? month)
    {
        try
        {
            var gstRecords = await _unitOfWork.GstRecords
                .FindAsync(x => true);

            if (!gstRecords.Any())
            {
                TempData["Error"] = "Нет данных для агрегации GSM → GST";
                return RedirectToAction(nameof(Index));
            }

            var gsmDocuments = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GSM");

            if (!gsmDocuments.Any())
            {
                TempData["Error"] = "Нет документов GSM для агрегации";
                return RedirectToAction(nameof(Index));
            }

            var firstDoc = gsmDocuments.First();
            var header = new GstExportHeader
            {
                FileName = $"GST_AGGREGATED_{DateTime.Now:yyyyMMdd_HHmmss}",
                RegionCode = firstDoc.RegionCode ?? "77",
                RecordsCount = gstRecords.Count(),
                FileNumber = 1,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportRecords = _mapper.Map<List<GstExportRecord>>(gstRecords);

            var exportDto = new GstExportDto
            {
                Header = header,
                Records = exportRecords
            };

            var service = _serviceProvider.GetService<IXmlService<GstImportDto, GstExportDto, GstEntity>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис GST не найден";
                return RedirectToAction(nameof(Index));
            }

            var xmlContent = await service.SerializeToXmlAsync(exportDto);

            var fileName = BuildFileNameAggregated("GST", year, month);
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
            _logger.LogError(ex, "Ошибка агрегации GSM → GST");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportAggregatedGpt(int? year, int? month)
    {
        try
        {
            var gptRecords = await _unitOfWork.GptRecords
                .FindAsync(x => true);

            if (!gptRecords.Any())
            {
                TempData["Error"] = "Нет данных для агрегации GPM → GPT";
                return RedirectToAction(nameof(Index));
            }

            var gpmDocuments = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GPM");

            if (!gpmDocuments.Any())
            {
                TempData["Error"] = "Нет документов GPM для агрегации";
                return RedirectToAction(nameof(Index));
            }

            var firstDoc = gpmDocuments.First();
            var header = new GptExportHeader
            {
                FileName = $"GPT_AGGREGATED_{DateTime.Now:yyyyMMdd_HHmmss}",
                RegionCode = firstDoc.RegionCode ?? "77",
                RecordsCount = gptRecords.Count(),
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportRecords = _mapper.Map<List<GptExportRecord>>(gptRecords);

            var exportDto = new GptExportDto
            {
                Header = header,
                Records = exportRecords
            };

            var service = _serviceProvider.GetService<IXmlService<GptImportDto, GptExportDto, GptEntity>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис GPT не найден";
                return RedirectToAction(nameof(Index));
            }

            var xmlContent = await service.SerializeToXmlAsync(exportDto);

            var fileName = BuildFileNameAggregated("GPT", year, month);
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
            _logger.LogError(ex, "Ошибка агрегации GPM → GPT");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }


    [HttpGet]
    public async Task<IActionResult> ExportGst(int documentId, int? year, int? month)
    {
        return await ExportFile(documentId, "GST", year, month);
    }

    [HttpGet]
    public async Task<IActionResult> ExportGpt(int documentId, int? year, int? month)
    {
        return await ExportFile(documentId, "GPT", year, month);
    }

    [HttpGet]
    public async Task<IActionResult> ExportGf(int documentId, int? year, int? month)
    {
        return await ExportFile(documentId, "GF", year, month);
    }

    [HttpGet]
    public async Task<IActionResult> ExportGsm(int documentId, int? year, int? month)
    {
        return await ExportFile(documentId, "GSM", year, month);
    }

    [HttpGet]
    public async Task<IActionResult> ExportGpm(int documentId, int? year, int? month)
    {
        return await ExportFile(documentId, "GPM", year, month);
    }

    private async Task<IActionResult> ExportFile(int documentId, string fileType, int? year, int? month)
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

            var fileName = BuildFileName(fileType, document.FileName, year, month);
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

    private string BuildFileName(string fileType, string baseName, int? year, int? month)
    {
        var prefix = fileType.ToUpper();
        var numberMatch = System.Text.RegularExpressions.Regex.Match(baseName, @"^[A-Z]+(\d+|[A-Z0-9]{6})");
        var number = numberMatch.Success ? numberMatch.Groups[1].Value : "01";

        var currentYear = year ?? DateTime.Now.Year;
        var currentMonth = month ?? DateTime.Now.Month;

        var day = DateTime.Now.Day.ToString("D2");
        var monthStr = currentMonth.ToString("D2");
        var yearShort = currentYear.ToString("D4").Substring(2, 2);

        var version = "1";

        return $"{prefix}{number}_{day}{monthStr}_{version}";
    }

    private string BuildFileNameAggregated(string fileType, int? year, int? month)
    {
        var prefix = fileType.ToUpper();
        var currentYear = year ?? DateTime.Now.Year;
        var currentMonth = month ?? DateTime.Now.Month;

        var day = DateTime.Now.Day.ToString("D2");
        var monthStr = currentMonth.ToString("D2");
        var yearShort = currentYear.ToString("D4").Substring(2, 2);

        var version = "1";

        return $"{prefix}_AGGREGATED_{day}{monthStr}_{version}";
    }

    private List<SelectListItem> GetMonths()
    {
        var months = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Все месяцы" }
        };
        
        var monthNames = new[]
        {
            "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
            "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
        };

        for (int i = 1; i <= 12; i++)
        {
            months.Add(new SelectListItem 
            { 
                Value = i.ToString(), 
                Text = monthNames[i - 1] 
            });
        }
        
        return months;
    }

    private List<SelectListItem> GetYears()
    {
        var years = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Все годы" }
        };
        
        for (int i = DateTime.Now.Year; i >= 2020; i--)
        {
            years.Add(new SelectListItem { Value = i.ToString(), Text = i.ToString() });
        }
        
        return years;
    }
}

public class SelectListItem
{
    public string? Value { get; set; }
    public string? Text { get; set; }
}
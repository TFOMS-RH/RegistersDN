using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using RegistrDN.Data;
using RegistrDN.Models.Entities;
using RegistrDN.Models.Enums;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Services.Interfaces;
using RegistrDN.Services.Validation;
using System.Text;

namespace RegistrDN.Controllers;

[Authorize]
public class ExportController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDiagnosisFilterService _diagnosisFilterService;
    private readonly IMapper _mapper;
    private readonly LogicalValidator _logicalValidator;

    public ExportController(
        IUnitOfWork unitOfWork,
        ILogger<ExportController> logger,
        IServiceProvider serviceProvider,
        IDiagnosisFilterService diagnosisFilterService,
        IMapper mapper,
        LogicalValidator logicalValidator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _diagnosisFilterService = diagnosisFilterService;
        _mapper = mapper;
        _logicalValidator = logicalValidator;
    }

    // ==========================================
    // ГЛАВНАЯ СТРАНИЦА ЭКСПОРТА
    // ==========================================
    public async Task<IActionResult> Index(string? search, string? type, string? hospital, string? period, string? status, int page = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Export.Index: page={Page}, search={Search}, type={Type}, period={Period}", page, search, type, period);

            var allDocs = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GST" || x.FileType == "GPT" || x.FileType == "GF" 
                             || x.FileType == "GSM" || x.FileType == "GPM"
                             || x.FileType == "DSPN" || x.FileType == "PROF" || x.FileType == "DF");

            // MO видит только свои файлы
            if (User.IsInRole("MO"))
            {
                var userHospitalCode = User.FindFirst("HospitalCode")?.Value;
                if (!string.IsNullOrEmpty(userHospitalCode))
                {
                    allDocs = allDocs.Where(x => x.HospitalCode == userHospitalCode).ToList();
                }
            }

            var query = allDocs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) 
                                      || (x.HospitalCode != null && x.HospitalCode.Contains(search, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(x => x.FileType == type);

            if (!string.IsNullOrEmpty(hospital))
                query = query.Where(x => x.HospitalCode != null && x.HospitalCode.Contains(hospital, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(period))
                query = query.Where(x => x.Period == period);

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<DocumentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(x => x.Status == statusEnum);
                }
            }

            var sortedDocs = query.OrderByDescending(x => x.UploadDate).ToList();
            var totalCount = sortedDocs.Count();
            var paginatedDocs = sortedDocs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Статистика по статусам
            var uploadedCount = allDocs.Count(x => x.Status == DocumentStatus.Uploaded);
            var checkingCount = allDocs.Count(x => x.Status == DocumentStatus.Checking);
            var readyCount = allDocs.Count(x => x.Status == DocumentStatus.Ready);
            var errorCount = allDocs.Count(x => x.Status == DocumentStatus.Error);

            // Статистика по типам
            var gstCount = allDocs.Count(x => x.FileType == "GST" || x.FileType == "GSM");
            var gptCount = allDocs.Count(x => x.FileType == "GPT" || x.FileType == "GPM");
            var dspnCount = allDocs.Count(x => x.FileType == "DSPN");
            var profCount = allDocs.Count(x => x.FileType == "PROF");
            var gfCount = allDocs.Count(x => x.FileType == "GF");
            var dfCount = allDocs.Count(x => x.FileType == "DF");

            var periods = allDocs
                .Where(x => !string.IsNullOrEmpty(x.Period))
                .Select(x => x.Period!)
                .Distinct()
                .OrderByDescending(p => p)
                .ToList();

            var moList = allDocs
                .Where(x => !string.IsNullOrEmpty(x.HospitalCode))
                .Select(x => x.HospitalCode!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.UploadedCount = uploadedCount;
            ViewBag.CheckingCount = checkingCount;
            ViewBag.ReadyCount = readyCount;
            ViewBag.ErrorCount = errorCount;
            ViewBag.GstCount = gstCount;
            ViewBag.GptCount = gptCount;
            ViewBag.DspnCount = dspnCount;
            ViewBag.ProfCount = profCount;
            ViewBag.GfCount = gfCount;
            ViewBag.DfCount = dfCount;
            ViewBag.Periods = periods;
            ViewBag.MoList = moList;
            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.Hospital = hospital;
            ViewBag.Period = period;
            ViewBag.Status = status;

            return View(paginatedDocs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки страницы экспорта");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return View(new List<DnDocumentEntity>());
        }
    }

    // ==========================================
    // ДЕТАЛИ ДОКУМЕНТА
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id);
        if (document == null)
            return NotFound();

        ViewBag.Document = document;

        // Загружаем записи в зависимости от типа файла
        object? records = null;

        switch (document.FileType)
        {
            case "GST":
            case "GSM":
                records = await _unitOfWork.GstRecords.FindAsync(x => x.DocumentId == id);
                break;
            case "GPT":
            case "GPM":
                records = await _unitOfWork.GptRecords.FindAsync(x => x.DocumentId == id);
                break;
            case "GF":
                records = await _unitOfWork.GfRecords.FindAsync(x => x.DocumentId == id);
                break;
            case "DSPN":
                records = await _unitOfWork.DspnRecords.FindAsync(x => x.DocumentId == id);
                break;
            case "PROF":
                records = await _unitOfWork.ProfRecords.FindAsync(x => x.DocumentId == id);
                break;
            case "DF":
                records = await _unitOfWork.DfRecords.FindAsync(x => x.DocumentId == id);
                break;
            default:
                records = null;
                break;
        }

        ViewBag.Records = records;
        ViewBag.RecordsCount = records is IEnumerable<object> enumerable ? enumerable.Count() : 0;

        return View(document);
    }

    // ==========================================
    // ПРОВЕРКА ДОКУМЕНТА
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> CheckDocument(int documentId)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                return Json(new { success = false, message = "Документ не найден" });

            // Проверяем права (MO видит только свои документы)
            if (User.IsInRole("MO"))
            {
                var userHospitalCode = User.FindFirst("HospitalCode")?.Value;
                if (document.HospitalCode != userHospitalCode)
                    return Json(new { success = false, message = "Доступ запрещен" });
            }

            // Только документы со статусом Uploaded или Error можно проверять
            if (document.Status != DocumentStatus.Uploaded && document.Status != DocumentStatus.Error)
            {
                return Json(new { success = false, message = $"Документ уже проверяется или готов. Текущий статус: {document.Status.GetDisplayName()}" });
            }

            // Меняем статус на "Проверяется"
            document.Status = DocumentStatus.Checking;
            document.CheckAttempts = document.CheckAttempts.GetValueOrDefault(0) + 1;
            await _unitOfWork.SaveChangesAsync();

            var errors = new List<string>();

            // ==========================================
            // ЛОГИЧЕСКИЙ КОНТРОЛЬ (ТОЛЬКО ДЛЯ DSPN И PROF)
            // ==========================================
            if (document.FileType == "DSPN" || document.FileType == "PROF")
            {
                var logicResult = await _logicalValidator.ValidateAsync(document.Id);
                if (!logicResult.IsValid)
                {
                    errors.AddRange(logicResult.Errors);
                    document.Status = DocumentStatus.Error;
                    document.ValidationErrors = string.Join("; ", errors.Take(20));
                    document.IsValid = false;
                    await _unitOfWork.SaveChangesAsync();
                    return Json(new { success = false, errors = errors.Take(20).ToList() });
                }
            }

            // Все проверки пройдены
            document.Status = DocumentStatus.Ready;
            document.ValidationErrors = null;
            document.IsValid = true;
            document.CheckedAt = DateTime.Now;
            document.CheckedBy = User.Identity?.Name ?? "System";
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Документ {documentId} успешно прошел проверку");
            return Json(new { success = true, message = "Документ прошел все проверки и готов к экспорту" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка проверки документа {documentId}");
            
            try
            {
                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document != null)
                {
                    document.Status = DocumentStatus.Error;
                    document.ValidationErrors = $"Критическая ошибка: {ex.Message}";
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch { }

            return Json(new { success = false, errors = new[] { ex.Message } });
        }
    }

    // ==========================================
    // МАССОВАЯ ПРОВЕРКА
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> CheckAllDocuments()
    {
        try
        {
            var documents = await _unitOfWork.Documents
                .FindAsync(x => x.Status == DocumentStatus.Uploaded || x.Status == DocumentStatus.Error);

            var total = documents.Count();
            var checkedCount = 0;
            var errorCount = 0;

            foreach (var doc in documents)
            {
                doc.Status = DocumentStatus.Checking;
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    var result = await CheckDocumentLogic(doc);
                    if (result.success)
                    {
                        doc.Status = DocumentStatus.Ready;
                        doc.ValidationErrors = null;
                        doc.IsValid = true;
                        doc.CheckedAt = DateTime.Now;
                        doc.CheckedBy = User.Identity?.Name ?? "System";
                        checkedCount++;
                    }
                    else
                    {
                        doc.Status = DocumentStatus.Error;
                        doc.ValidationErrors = string.Join("; ", result.errors.Take(20));
                        doc.IsValid = false;
                        errorCount++;
                    }
                }
                catch
                {
                    doc.Status = DocumentStatus.Error;
                    errorCount++;
                }

                await _unitOfWork.SaveChangesAsync();
            }

            return Json(new { total, checkedCount, errorCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка массовой проверки документов");
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЙ МЕТОД: ЛОГИКА ПРОВЕРКИ
    // ==========================================
    private async Task<(bool success, List<string> errors)> CheckDocumentLogic(DnDocumentEntity document)
    {
        var errors = new List<string>();

        // Логический контроль (только для DSPN и PROF)
        if (document.FileType == "DSPN" || document.FileType == "PROF")
        {
            var logicResult = await _logicalValidator.ValidateAsync(document.Id);
            if (!logicResult.IsValid)
            {
                errors.AddRange(logicResult.Errors);
                return (false, errors);
            }
        }

        return (true, errors);
    }

    // ==========================================
    // ЭКСПОРТ ОТДЕЛЬНЫХ ФАЙЛОВ
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

    [HttpGet]
    public async Task<IActionResult> ExportDf(int documentId)
    {
        return await ExportFile(documentId, "DF");
    }

    private async Task<IActionResult> ExportFile(int documentId, string fileType)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                return NotFound();

            // Проверка прав
            if (User.IsInRole("MO"))
            {
                var userHospitalCode = User.FindFirst("HospitalCode")?.Value;
                if (document.HospitalCode != userHospitalCode)
                    return Forbid();
            }

            // Проверка статуса
            if (document.Status != DocumentStatus.Ready && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "Документ не прошел проверку. Сначала выполните проверку.";
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
                TempData["Error"] = $"Метод ExportAsync не найден";
                return RedirectToAction(nameof(Index));
            }

            var exportTask = exportMethod.Invoke(service, new object[] { documentId });
            var xmlContent = await (Task<string>)exportTask!;
            var fileName = document?.FileName ?? $"Export_{DateTime.Now:yyyyMMdd_HHmmss}";

            return await DownloadZipAsync(xmlContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка экспорта {fileType}");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // АГРЕГАЦИЯ (ТОЛЬКО ДЛЯ ADMIN)
    // ==========================================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> ExportAggregatedGst(string? period, string? moCode)
    {
        try
        {
            _logger.LogInformation("Агрегация GST: period={Period}, moCode={MoCode}", period, moCode);

            var allowedDiagnoses = await _diagnosisFilterService.GetActiveDiagnosisCodesAsync();
            if (!allowedDiagnoses.Any())
            {
                TempData["Error"] = "Нет активных диагнозов для выгрузки. Добавьте диагнозы в справочник DS_DN.";
                return RedirectToAction(nameof(Index));
            }

            var gsmDocuments = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GSM" && x.Status == DocumentStatus.Ready);

            if (!string.IsNullOrEmpty(period))
                gsmDocuments = gsmDocuments.Where(x => x.Period == period).ToList();

            if (!string.IsNullOrEmpty(moCode))
                gsmDocuments = gsmDocuments.Where(x => x.HospitalCode == moCode).ToList();

            if (!gsmDocuments.Any())
            {
                TempData["Error"] = $"Нет документов GSM для агрегации";
                return RedirectToAction(nameof(Index));
            }

            var docIds = gsmDocuments.Select(x => x.Id).ToList();
            var gstRecords = await _unitOfWork.GstRecords
                .FindAsync(x => docIds.Contains(x.DocumentId));

            if (!gstRecords.Any())
            {
                TempData["Error"] = "Нет записей GST для агрегации";
                return RedirectToAction(nameof(Index));
            }

            var filteredRecords = gstRecords
                .Where(x => x.DiagCode != null && allowedDiagnoses.Contains(x.DiagCode))
                .ToList();

            if (!filteredRecords.Any())
            {
                TempData["Error"] = $"Нет записей GST с разрешенными диагнозами";
                return RedirectToAction(nameof(Index));
            }

            var firstDoc = gsmDocuments.First();
            var regionCode = firstDoc.RegionCode ?? "19";
            var hospitalCode = !string.IsNullOrEmpty(moCode) ? moCode : firstDoc.HospitalCode ?? "000000";

            var header = new GstExportHeader
            {
                FileName = BuildAggregatedFileName("GST", regionCode, period, hospitalCode),
                RegionCode = regionCode,
                RecordsCount = filteredRecords.Count,
                FileNumber = 1,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportRecords = _mapper.Map<List<GstExportRecord>>(filteredRecords);
            var exportDto = new GstExportDto { Header = header, Records = exportRecords };

            var service = _serviceProvider.GetService<IXmlService<GstImportDto, GstExportDto, GstEntity>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис GST не найден";
                return RedirectToAction(nameof(Index));
            }

            var xmlContent = await service.SerializeToXmlAsync(exportDto);
            var fileName = BuildAggregatedFileName("GST", regionCode, period, hospitalCode);

            return await DownloadZipAsync(xmlContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка агрегации GST");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> ExportAggregatedGpt(string? period, string? moCode)
    {
        try
        {
            _logger.LogInformation("Агрегация GPT: period={Period}, moCode={MoCode}", period, moCode);

            var allowedDiagnoses = await _diagnosisFilterService.GetActiveDiagnosisCodesAsync();
            if (!allowedDiagnoses.Any())
            {
                TempData["Error"] = "Нет активных диагнозов для выгрузки.";
                return RedirectToAction(nameof(Index));
            }

            var gpmDocuments = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "GPM" && x.Status == DocumentStatus.Ready);

            if (!string.IsNullOrEmpty(period))
                gpmDocuments = gpmDocuments.Where(x => x.Period == period).ToList();

            if (!string.IsNullOrEmpty(moCode))
                gpmDocuments = gpmDocuments.Where(x => x.HospitalCode == moCode).ToList();

            if (!gpmDocuments.Any())
            {
                TempData["Error"] = $"Нет документов GPM для агрегации";
                return RedirectToAction(nameof(Index));
            }

            var docIds = gpmDocuments.Select(x => x.Id).ToList();
            var gptRecords = await _unitOfWork.GptRecords
                .FindAsync(x => docIds.Contains(x.DocumentId));

            if (!gptRecords.Any())
            {
                TempData["Error"] = "Нет записей GPT для агрегации";
                return RedirectToAction(nameof(Index));
            }

            var filteredRecords = gptRecords
                .Where(x => x.DsCode != null && allowedDiagnoses.Contains(x.DsCode))
                .ToList();

            if (!filteredRecords.Any())
            {
                TempData["Error"] = $"Нет записей GPT с разрешенными диагнозами";
                return RedirectToAction(nameof(Index));
            }

            var firstDoc = gpmDocuments.First();
            var regionCode = firstDoc.RegionCode ?? "19";
            var hospitalCode = !string.IsNullOrEmpty(moCode) ? moCode : firstDoc.HospitalCode ?? "000000";

            var header = new GptExportHeader
            {
                FileName = BuildAggregatedFileName("GPT", regionCode, period, hospitalCode),
                RegionCode = regionCode,
                RecordsCount = filteredRecords.Count,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportRecords = _mapper.Map<List<GptExportRecord>>(filteredRecords);
            var exportDto = new GptExportDto { Header = header, Records = exportRecords };

            var service = _serviceProvider.GetService<IXmlService<GptImportDto, GptExportDto, GptEntity>>();
            if (service == null)
            {
                TempData["Error"] = "Сервис GPT не найден";
                return RedirectToAction(nameof(Index));
            }

            var xmlContent = await service.SerializeToXmlAsync(exportDto);
            var fileName = BuildAggregatedFileName("GPT", regionCode, period, hospitalCode);

            return await DownloadZipAsync(xmlContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка агрегации GPT");
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // УДАЛЕНИЕ ДОКУМЕНТА
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(id);
            if (document == null)
                return Json(new { success = false, message = "Документ не найден" });

            if (User.IsInRole("MO"))
            {
                var userHospitalCode = User.FindFirst("HospitalCode")?.Value;
                if (document.HospitalCode != userHospitalCode)
                    return Json(new { success = false, message = "Доступ запрещен" });
            }

            switch (document.FileType)
            {
                case "GST":
                case "GSM":
                    var gstRecords = await _unitOfWork.GstRecords.FindAsync(x => x.DocumentId == id);
                    if (gstRecords.Any()) await _unitOfWork.GstRecords.DeleteRangeAsync(gstRecords);
                    break;
                case "GPT":
                case "GPM":
                    var gptRecords = await _unitOfWork.GptRecords.FindAsync(x => x.DocumentId == id);
                    if (gptRecords.Any()) await _unitOfWork.GptRecords.DeleteRangeAsync(gptRecords);
                    break;
                case "GF":
                    var gfRecords = await _unitOfWork.GfRecords.FindAsync(x => x.DocumentId == id);
                    if (gfRecords.Any()) await _unitOfWork.GfRecords.DeleteRangeAsync(gfRecords);
                    break;
                case "DSPN":
                    var dspnRecords = await _unitOfWork.DspnRecords.FindAsync(x => x.DocumentId == id);
                    if (dspnRecords.Any()) await _unitOfWork.DspnRecords.DeleteRangeAsync(dspnRecords);
                    break;
                case "PROF":
                    var profRecords = await _unitOfWork.ProfRecords.FindAsync(x => x.DocumentId == id);
                    if (profRecords.Any()) await _unitOfWork.ProfRecords.DeleteRangeAsync(profRecords);
                    break;
                case "DF":
                    var dfRecords = await _unitOfWork.DfRecords.FindAsync(x => x.DocumentId == id);
                    if (dfRecords.Any()) await _unitOfWork.DfRecords.DeleteRangeAsync(dfRecords);
                    break;
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

    // ==========================================
    // СЧЕТЧИКИ ДЛЯ МЕНЮ
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        try
        {
            var count = await _unitOfWork.Documents.CountAsync();
            return Json(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в GetCount");
            return Json(new { count = 0 });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStatusCounts()
    {
        try
        {
            var docs = await _unitOfWork.Documents
                .FindAsync(x => x.FileType == "DSPN" || x.FileType == "PROF" || x.FileType == "GSM" || x.FileType == "GPM" || x.FileType == "DF");

            return Json(new
            {
                uploaded = docs.Count(x => x.Status == DocumentStatus.Uploaded),
                checking = docs.Count(x => x.Status == DocumentStatus.Checking),
                ready = docs.Count(x => x.Status == DocumentStatus.Ready),
                error = docs.Count(x => x.Status == DocumentStatus.Error)
            });
        }
        catch
        {
            return Json(new { uploaded = 0, checking = 0, ready = 0, error = 0 });
        }
    }

    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ==========================================

    private async Task<IActionResult> DownloadZipAsync(string xmlContent, string fileName)
    {
        var zipFileName = $"{fileName}.zip";
        var xmlFileName = $"{fileName}.xml";

        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(xmlFileName, System.IO.Compression.CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, System.Text.Encoding.GetEncoding("windows-1251"));
            await writer.WriteAsync(xmlContent);
        }

        var zipData = memoryStream.ToArray();
        return File(zipData, "application/zip", zipFileName);
    }

    private string BuildAggregatedFileName(string fileType, string regionCode, string? period, string? hospitalCode)
    {
        string yearShort;
        string month;
        
        if (!string.IsNullOrEmpty(period) && period.Length >= 7)
        {
            var parts = period.Split('-');
            yearShort = parts[0].Substring(2, 2);
            month = parts[1];
        }
        else
        {
            yearShort = DateTime.Now.ToString("yy");
            month = DateTime.Now.ToString("MM");
        }

        var moCodeShort = "00";
        if (!string.IsNullOrEmpty(hospitalCode) && hospitalCode.Length >= 2)
        {
            moCodeShort = hospitalCode.Substring(hospitalCode.Length - 2, 2);
        }

        return $"{fileType}{regionCode}_{yearShort}{month}_{moCodeShort}";
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
            "DSPN" => _serviceProvider.GetService<IXmlServiceWithResponse<DspnImportDto, DspnExportDto, DspnEntity, DspnResponseDto>>(),
            "PROF" => _serviceProvider.GetService<IXmlServiceWithResponse<ProfImportDto, ProfExportDto, ProfEntity, ProfResponseDto>>(),
            "DF" => _serviceProvider.GetService<IXmlService<DfImportDto, DfExportDto, DfEntity>>(),
            _ => null
        };
    }
}
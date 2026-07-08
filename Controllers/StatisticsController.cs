using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Models.ViewModels;
using RegistrDN.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace RegistrDN.Controllers;

public class StatisticsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IUnitOfWork unitOfWork, ILogger<StatisticsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? period)
    {
        ViewBag.SelectedPeriod = period;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardData(string? period)
    {
        try
        {
            var documents = await _unitOfWork.Documents
                .FindAsync(x => true);

            if (!string.IsNullOrEmpty(period))
            {
                documents = documents.Where(x => x.Period == period).ToList();
            }

            // 1. Общая статистика
            var totalDocuments = documents.Count();
            var totalGst = documents.Count(x => x.FileType == "GST" || x.FileType == "GSM");
            var totalGpt = documents.Count(x => x.FileType == "GPT" || x.FileType == "GPM");
            var totalGf = documents.Count(x => x.FileType == "GF");
            var successCount = documents.Count(x => x.IsValid);
            var errorCount = totalDocuments - successCount;

            // 2. GST записи
            var gstRecords = await _unitOfWork.GstRecords
                .FindAsync(x => true);

            // 3. Статистика по периодам (БЕЗ Documents)
            var periodStats = documents
                .Where(x => !string.IsNullOrEmpty(x.Period))
                .GroupBy(x => x.Period)
                .Select(g => new PeriodStatViewModel
                {
                    Period = g.Key!,
                    Count = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList();

            // 4. Топ диагнозов
            var topDiagnoses = gstRecords
                .Where(x => !string.IsNullOrEmpty(x.DiagCode))
                .GroupBy(x => x.DiagCode)
                .Select(g => new TopDiagnosisViewModel
                {
                    DiagnosisCode = g.Key!,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // 5. Статистика по МО (БЕЗ Documents)
            var moStats = documents
                .Where(x => !string.IsNullOrEmpty(x.HospitalCode))
                .GroupBy(x => x.HospitalCode)
                .Select(g => new MoStatViewModel
                {
                    MoCode = g.Key!,
                    DocumentCount = g.Count()
                })
                .OrderByDescending(x => x.DocumentCount)
                .Take(10)
                .ToList();

            // 6. Статусы
            var statusStats = new StatusStatViewModel
            {
                SuccessCount = successCount,
                ErrorCount = errorCount,
                TotalCount = totalDocuments
            };

            // 7. Динамика по месяцам
            var monthlyStats = documents
                .GroupBy(x => new { x.UploadDate.Year, x.UploadDate.Month })
                .Select(g => new MonthlyStatViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Take(12)
                .ToList();

            // 8. Уникальные пациенты
            var uniquePatients = gstRecords
                .Select(x => x.ENP)
                .Distinct()
                .Count();

            var result = new DashboardDataViewModel
            {
                TotalDocuments = totalDocuments,
                TotalGst = totalGst,
                TotalGpt = totalGpt,
                TotalGf = totalGf,
                SuccessCount = successCount,
                ErrorCount = errorCount,
                UniquePatients = uniquePatients,
                PeriodStats = periodStats,
                TopDiagnoses = topDiagnoses,
                MoStats = moStats,
                StatusStats = statusStats,
                MonthlyStats = monthlyStats
            };

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения данных для дашборда");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPeriods()
    {
        var documents = await _unitOfWork.Documents
            .FindAsync(x => !string.IsNullOrEmpty(x.Period));

        var periods = documents
            .Select(x => x.Period)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();

        return Json(new { periods });
    }
}
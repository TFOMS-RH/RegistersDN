using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;

namespace RegistrDN.Controllers;

[Authorize] 
public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var totalDocs = await _unitOfWork.Documents.CountAsync();
            var totalGst = await _unitOfWork.GstRecords.CountAsync();
            var totalGpt = await _unitOfWork.GptRecords.CountAsync();
            var totalGf = await _unitOfWork.GfRecords.CountAsync();
            
            ViewBag.TotalDocuments = totalDocs;
            ViewBag.TotalGst = totalGst;
            ViewBag.TotalGpt = totalGpt;
            ViewBag.TotalGf = totalGf;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения статистики");
            ViewBag.TotalDocuments = 0;
            ViewBag.TotalGst = 0;
            ViewBag.TotalGpt = 0;
            ViewBag.TotalGf = 0;
        }

        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> TestDb()
        {
            try
            {
                var count = await _unitOfWork.Documents.CountAsync();
                return Content($"БД работает! Документов: {count}");
            }
            catch (Exception ex)
            {
                return Content($" Ошибка: {ex.Message}");
            }
        }
}
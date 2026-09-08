using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using System.Text;   // <-- ДОБАВИТЬ!

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

    [HttpGet]
    public async Task<IActionResult> TestEncoding()
    {
        var records = await _unitOfWork.DspnRecords
            .FindAsync(x => true);
        
        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset='windows-1251'></head><body>");
        sb.AppendLine("<h2>DSPN Records</h2>");
        sb.AppendLine("<table border='1'>");
        sb.AppendLine("<tr><th>ID</th><th>NPOLIS</th><th>FAM</th><th>IM</th><th>DIAG_CODE</th></tr>");
        
        foreach (var r in records.Take(20))
        {
            sb.AppendLine($"<tr><td>{r.Id}</td><td>{r.Npolis}</td><td>{r.Fam}</td><td>{r.Im}</td><td>{r.DiagCode}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("<p>Всего записей: " + records.Count() + "</p>");
        sb.AppendLine("</body></html>");
        
        return Content(sb.ToString(), "text/html", Encoding.GetEncoding("windows-1251"));
    }   
}
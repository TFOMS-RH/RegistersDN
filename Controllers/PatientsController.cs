using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Models.Entities;
using RegistrDN.Models.ViewModels;

namespace RegistrDN.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IUnitOfWork unitOfWork, ILogger<PatientsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, string? period, string? hospitalCode, int page = 1)
    {
        // 1. Получаем все GST записи
        var gstRecords = await _unitOfWork.GstRecords
            .FindAsync(x => true);

        // MO видит только своих пациентов
        if (User.IsInRole("MO"))
        {
            var userHospitalCode = User.FindFirst("HospitalCode")?.Value;
            if (!string.IsNullOrEmpty(userHospitalCode))
            {
                gstRecords = gstRecords.Where(x => x.Mcod == userHospitalCode).ToList();
            }
        }

        // 2. Формируем список пациентов (группировка по ENP)
        var patientsQuery = gstRecords
            .GroupBy(x => x.ENP)
            .Select(g => new PatientViewModel
            {
                ENP = g.Key,
                DnPatientId = g.First().DnPatientId,
                DiagCode = g.First().DiagCode,
                DiagDate = g.First().DiagDate,
                DateDnIn = g.First().DateDnIn,
                DateDnOut = g.First().DateDnOut,
                StatusDnIn = g.First().StatusDnIn,
                Mcod = g.First().Mcod,
                LastSlDate = g.First().LastSlDate,
                SourceFileType = "GST",
                SourceDocumentId = g.First().DocumentId,
                Period = g.First().Document?.Period
            })
            .ToList();

        // 3. Применяем фильтры
        if (!string.IsNullOrEmpty(search))
        {
            patientsQuery = patientsQuery.Where(x => 
                (x.ENP != null && x.ENP.Contains(search)) ||
                (x.DiagCode != null && x.DiagCode.Contains(search))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(period))
        {
            patientsQuery = patientsQuery.Where(x => x.Period == period).ToList();
        }

        if (!string.IsNullOrEmpty(hospitalCode))
        {
            patientsQuery = patientsQuery.Where(x => x.Mcod == hospitalCode).ToList();
        }

        // 4. Считаем статистику ДО пагинации
        var totalPatients = patientsQuery.Count;
        var onDnCount = patientsQuery.Count(x => x.DateDnOut == null);
        var offDnCount = patientsQuery.Count(x => x.DateDnOut != null);
        var uniqueDiagnoses = patientsQuery.Select(x => x.DiagCode).Distinct().Count();

        ViewBag.TotalPatients = totalPatients;
        ViewBag.OnDnCount = onDnCount;
        ViewBag.OffDnCount = offDnCount;
        ViewBag.UniqueDiagnoses = uniqueDiagnoses;

        // 5. Получаем список периодов для фильтра
        var documents = await _unitOfWork.Documents
            .FindAsync(x => !string.IsNullOrEmpty(x.Period));
        var periodList = documents
            .Select(x => x.Period)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();

        ViewBag.Periods = periodList;

        // 6. Пагинация (фиксировано 10 записей)
        const int pageSize = 10;
        var totalCount = patientsQuery.Count;
        var paginatedPatients = patientsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return View(paginatedPatients);
    }

    [HttpGet]
    public async Task<IActionResult> GetPatientDetails(string enp)
    {
        if (string.IsNullOrEmpty(enp))
            return Json(new { success = false, message = "ENP не указан" });

        var records = await _unitOfWork.GstRecords
            .FindAsync(x => x.ENP == enp);

        if (!records.Any())
            return Json(new { success = false, message = "Пациент не найден" });

        var patient = new PatientViewModel
        {
            ENP = records.First().ENP,
            DnPatientId = records.First().DnPatientId,
            DiagCode = records.First().DiagCode,
            DiagDate = records.First().DiagDate,
            DateDnIn = records.First().DateDnIn,
            DateDnOut = records.First().DateDnOut,
            StatusDnIn = records.First().StatusDnIn,
            Mcod = records.First().Mcod,
            LastSlDate = records.First().LastSlDate
        };

        var docIds = records.Select(x => x.DocumentId).Distinct();
        var documents = await _unitOfWork.Documents
            .FindAsync(x => docIds.Contains(x.Id));

        var recordList = new List<PatientRecordViewModel>();
        foreach (var doc in documents)
        {
            var docRecords = records.Where(x => x.DocumentId == doc.Id);
            foreach (var rec in docRecords)
            {
                recordList.Add(new PatientRecordViewModel
                {
                    Id = rec.Id,
                    FileType = doc.FileType,
                    Period = doc.Period,
                    HospitalCode = doc.HospitalCode,
                    UploadDate = doc.UploadDate,
                    Mcod = rec.Mcod,
                    DiagCode = rec.DiagCode,
                    DateDnIn = rec.DateDnIn,
                    DateDnOut = rec.DateDnOut,
                    StatusDnIn = rec.StatusDnIn
                });
            }
        }

        var result = new PatientDetailViewModel
        {
            Patient = patient,
            Records = recordList.OrderByDescending(x => x.UploadDate).ToList()
        };

        return Json(new { success = true, data = result });
    }

    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        var gstRecords = await _unitOfWork.GstRecords
            .FindAsync(x => true);

        // MO видит только своих пациентов
        if (User.IsInRole("MO"))
        {
            var userHospitalCode = User.FindFirst("HospitalCode")?.Value;
            if (!string.IsNullOrEmpty(userHospitalCode))
            {
                gstRecords = gstRecords.Where(x => x.Mcod == userHospitalCode).ToList();
            }
        }

        var uniquePatients = gstRecords.Select(x => x.ENP).Distinct().Count();
        return Json(new { count = uniquePatients });
    }
}
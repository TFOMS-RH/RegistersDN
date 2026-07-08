using Microsoft.AspNetCore.Mvc;
using RegistrDN.Data;
using RegistrDN.Models.Entities;
using RegistrDN.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace RegistrDN.Controllers;

public class PatientsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IUnitOfWork unitOfWork, ILogger<PatientsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, string? period, string? hospitalCode)
    {
        var gstRecords = await _unitOfWork.GstRecords
            .FindAsync(x => true);

        var patients = gstRecords
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
                SourceDocumentId = g.First().DocumentId
            })
            .ToList();

        // Фильтры
        if (!string.IsNullOrEmpty(search))
        {
            patients = patients.Where(x => 
                (x.ENP != null && x.ENP.Contains(search)) ||
                (x.DiagCode != null && x.DiagCode.Contains(search))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(period))
        {
            patients = patients.Where(x => x.Period == period).ToList();
        }

        if (!string.IsNullOrEmpty(hospitalCode))
        {
            patients = patients.Where(x => x.Mcod == hospitalCode).ToList();
        }

        var periods = await _unitOfWork.Documents
            .FindAsync(x => !string.IsNullOrEmpty(x.Period));
        var periodList = periods
            .Select(x => x.Period)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();

        ViewBag.Periods = periodList;
        ViewBag.TotalCount = patients.Count;

        // Пагинация (по 10 записей)
        int pageSize = 10;
        int page = 1;
        var paginated = patients.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return View(paginated);
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
        var uniquePatients = gstRecords.Select(x => x.ENP).Distinct().Count();
        return Json(new { count = uniquePatients });
    }
}
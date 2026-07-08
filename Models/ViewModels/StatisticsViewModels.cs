using RegistrDN.Models.Entities;

namespace RegistrDN.Models.ViewModels;

public class DashboardDataViewModel
{
    public int TotalDocuments { get; set; }
    public int TotalGst { get; set; }
    public int TotalGpt { get; set; }
    public int TotalGf { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int UniquePatients { get; set; }
    public List<PeriodStatViewModel>? PeriodStats { get; set; }
    public List<TopDiagnosisViewModel>? TopDiagnoses { get; set; }
    public List<MoStatViewModel>? MoStats { get; set; }
    public StatusStatViewModel? StatusStats { get; set; }
    public List<MonthlyStatViewModel>? MonthlyStats { get; set; }
}

public class PeriodStatViewModel
{
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }

}

public class TopDiagnosisViewModel
{
    public string DiagnosisCode { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MoStatViewModel
{
    public string MoCode { get; set; } = string.Empty;
    public int DocumentCount { get; set; }

}

public class StatusStatViewModel
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int TotalCount { get; set; }
}

public class MonthlyStatViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
    public string MonthName => $"{Month:D2}.{Year}";
}
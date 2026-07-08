namespace RegistrDN.Models.ViewModels;

public class PatientViewModel
{
    public int Id { get; set; }
    public string? ENP { get; set; }
    public string? DnPatientId { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? DiagCode { get; set; }
    public string? DiagName { get; set; }
    public DateTime? DiagDate { get; set; }
    public DateTime? DateDnIn { get; set; }
    public DateTime? DateDnOut { get; set; }
    public int? StatusDnIn { get; set; }
    public string? Mcod { get; set; }
    public string? HospitalCode { get; set; }
    public string? Period { get; set; }
    public DateTime? LastSlDate { get; set; }
    public string? SourceFileType { get; set; }
    public int? SourceDocumentId { get; set; }
}

public class PatientDetailViewModel
{
    public PatientViewModel Patient { get; set; } = new();
    public List<PatientRecordViewModel> Records { get; set; } = new();
}

public class PatientRecordViewModel
{
    public int Id { get; set; }
    public string? FileType { get; set; }
    public string? Period { get; set; }
    public string? HospitalCode { get; set; }
    public DateTime? UploadDate { get; set; }
    public string? Mcod { get; set; }
    public string? DiagCode { get; set; }
    public DateTime? DateDnIn { get; set; }
    public DateTime? DateDnOut { get; set; }
    public int? StatusDnIn { get; set; }
}
using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Import;

[XmlRoot("DN_LIST")]
public class GsmImportDto
{
    [XmlElement("ZGLV")]
    public GsmImportHeader? Header { get; set; }
    
    [XmlElement("ZAP")]
    public List<GsmImportRecord>? Records { get; set; }
}

public class GsmImportHeader
{
    [XmlElement("VERSION")]
    public string? Version { get; set; }
    [XmlElement("FILE_TYPE")]
    public string? FileType { get; set; }
    [XmlElement("DATA")]
    public string? Data { get; set; }
    [XmlElement("FILENAME")]
    public string? FileName { get; set; }
    [XmlElement("REGION_CD")]
    public string? RegionCode { get; set; }
    [XmlElement("NN_FILE")]
    public int? FileNumber { get; set; }
    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
}

public class GsmImportRecord
{
    [XmlElement("CODE_L")]
    public string? CodeL { get; set; }
    [XmlElement("ENP")]
    public string? ENP { get; set; }
    [XmlElement("CODE_PINF")]
    public string? CodePinf { get; set; }
    [XmlElement("DN_PATIENT_ID")]
    public string? DnPatientId { get; set; }
    [XmlElement("DIAG_CODE")]
    public string? DiagCode { get; set; }
    [XmlElement("DIAG_DATE")]
    public string? DiagDate { get; set; }
    [XmlElement("DATE_DN_IN")]
    public string? DateDnIn { get; set; }
    [XmlElement("DATE_DN_OUT")]
    public string? DateDnOut { get; set; }
    [XmlElement("DN_PRVS")]
    public int? DnPrvs { get; set; }
    [XmlElement("LAST_SL_MCOD")]
    public int? LastSlMcod { get; set; }
    [XmlElement("LAST_SL_NHISTORY")]
    public string? LastSlNhistory { get; set; }
    [XmlElement("LAST_SL_DATE")]
    public string? LastSlDate { get; set; }
    [XmlElement("STATUS_DN_IN")]
    public int? StatusDnIn { get; set; }
    [XmlElement("REASON_DN_OUT")]
    public string? ReasonDnOut { get; set; }
    [XmlElement("REASON_DN_IN")]
    public int ReasonDnIn { get; set; }
    [XmlElement("MCOD")]
    public string? Mcod { get; set; }
    [XmlElement("MO_ASSIGN")]
    public int? MoAssign { get; set; }
    [XmlElement("DATE_CHECKING")]
    public string? DateChecking { get; set; }
}
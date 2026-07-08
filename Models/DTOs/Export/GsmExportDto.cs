using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Export;

[XmlRoot("DN_LIST")]
public class GsmExportDto
{
    [XmlElement("ZGLV")]
    public GsmExportHeader? Header { get; set; }

    [XmlElement("ZAP")]
    public List<GsmExportRecord>? Records { get; set; }
}

public class GsmExportHeader
{
    [XmlElement("VERSION")]
    public string Version { get; set; } = "P1.20";
    [XmlElement("FILE_TYPE")]
    public string FileType { get; set; } = "GSM";
    [XmlElement("DATA")]
    public string Data { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    [XmlElement("FILENAME")]
    public string? FileName { get; set; }
    [XmlElement("REGION_CD")]
    public string? RegionCode { get; set; }
    [XmlElement("NN_FILE")]
    public int? FileNumber { get; set; }
    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
}

public class GsmExportRecord
{
    [XmlElement("CODE_L")]
    public string? CodeL { get; set; }
    [XmlElement("ENP")]
    public string? ENP { get; set; }
    [XmlElement("CODE_PINF")]
    public string CodePinf { get; set; } = "ДН";
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
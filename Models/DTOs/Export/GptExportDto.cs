using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Export;


/// Корневой элемент DN_PLAN - План-график (для экспорта)

[XmlRoot("DN_PLAN")]
public class GptExportDto
{
    [XmlElement("ZGLV")]
    public GptExportHeader? Header { get; set; }

    [XmlElement("ZAP")]
    public List<GptExportRecord>? Records { get; set; }
}


/// Заголовок файла GPT (для экспорта)

public class GptExportHeader
{
    [XmlElement("VERSION")]
    public string Version { get; set; } = "P3.20";

    [XmlElement("FILE_TYPE")]
    public string FileType { get; set; } = "GPT";

    [XmlElement("DATA")]
    public string Data { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    [XmlElement("FILENAME")]
    public string? FileName { get; set; }

    [XmlElement("REGION_CD")]
    public string? RegionCode { get; set; }

    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
}


/// Запись плана-графика (для экспорта)

public class GptExportRecord
{
    [XmlElement("CODE_P")]
    public string? CodeP { get; set; }

    [XmlElement("DN_PATIENT_ID")]
    public string? DnPatientId { get; set; }

    [XmlElement("ENP")]
    public string? ENP { get; set; }

    [XmlElement("CODE_PINF")]
    public string CodePinf { get; set; } = "ДН";

    [XmlElement("MCOD_PLAN")]
    public string? McodPlan { get; set; }

    [XmlElement("MO_PODR_ID")]
    public string? MoPodrId { get; set; }

    [XmlElement("MED_AREA_CODE")]
    public string? MedAreaCode { get; set; }

    [XmlElement("MO_ASSIGN")]
    public int MoAssign { get; set; }

    [XmlElement("END_DATE_INF")]
    public string? EndDateInf { get; set; }

    [XmlElement("PRIMARY_INF")]
    public int PrimaryInf { get; set; }

    [XmlElement("DS_CODE")]
    public string? DsCode { get; set; }

    [XmlElement("PLAN_DATE_START")]
    public string? PlanDateStart { get; set; }

    [XmlElement("PLAN_DATE_END")]
    public string? PlanDateEnd { get; set; }
}
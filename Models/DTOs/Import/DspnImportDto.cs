using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Import;

/// <summary>
/// DSPN — Сведения о ЗЛ на ДН от МО
/// Корневой элемент: ZL_LIST
/// </summary>
[XmlRoot("ZL_LIST")]
public class DspnImportDto
{
    [XmlElement("ZGLV")]
    public DspnImportHeader? Header { get; set; }

    [XmlElement("ZAP")]
    public List<DspnImportRecord>? Records { get; set; }
}

/// <summary>
/// Заголовок файла DSPN
/// </summary>
public class DspnImportHeader
{
    [XmlElement("VERSION")]
    public string? Version { get; set; } // "1.0"

    [XmlElement("DATA")]
    public string? Data { get; set; } // Формат: ГГГГММДД

    [XmlElement("FILENAME")]
    public string? FileName { get; set; }

    [XmlElement("YEAR")]
    public int Year { get; set; }

    [XmlElement("MONTH")]
    public int Month { get; set; }
}

/// <summary>
/// Запись DSPN
/// </summary>
public class DspnImportRecord
{
    // ==========================================
    // ZAP — основные данные
    // ==========================================
    [XmlElement("N_ZAP")]
    public long NZap { get; set; }

    [XmlElement("PACIENT")]
    public DspnPacient? Pacient { get; set; }

    [XmlElement("PLAN")]
    public DspnPlan? Plan { get; set; }

    [XmlElement("INF")]
    public List<DspnInf>? InfRecords { get; set; }
}

/// <summary>
/// Сведения о ЗЛ (PACIENT)
/// </summary>
public class DspnPacient
{
    [XmlElement("PACIENT_ID")]
    public string? PacientId { get; set; }

    [XmlElement("SMO")]
    public string? Smo { get; set; }

    [XmlElement("SPOLIS")]
    public string? Spolis { get; set; }

    [XmlElement("VPOLIS")]
    public int Vpolis { get; set; }

    [XmlElement("NPOLIS")]
    public string? Npolis { get; set; }

    [XmlElement("FAM")]
    public string? Fam { get; set; }

    [XmlElement("IM")]
    public string? Im { get; set; }

    [XmlElement("OT")]
    public string? Ot { get; set; }

    [XmlElement("DR")]
    public string? Dr { get; set; } // ГГГГММДД

    [XmlElement("W")]
    public int W { get; set; }

    [XmlElement("ADRES")]
    public string? Adres { get; set; }

    [XmlElement("TEL")]
    public string? Tel { get; set; }

    [XmlElement("MO_P")]
    public string? MoP { get; set; }

    [XmlElement("DIAG_CODE")]
    public string? DiagCode { get; set; }

    [XmlElement("IDDOKT")]
    public string? Iddokt { get; set; }

    [XmlElement("DATE_DN_IN")]
    public string? DateDnIn { get; set; }

    [XmlElement("DATE_DN_OUT")]
    public string? DateDnOut { get; set; }

    [XmlElement("DIAG_DATE")]
    public string? DiagDate { get; set; }

    [XmlElement("DN_PRVS")]
    public int DnPrvs { get; set; }

    [XmlElement("STATUS_DN_IN")]
    public int StatusDnIn { get; set; }

    [XmlElement("REASON_DN_OUT")]
    public string? ReasonDnOut { get; set; }

    [XmlElement("REASON_DN_IN")]
    public int ReasonDnIn { get; set; }
}

/// <summary>
/// Сведения о планируемом сроке и месте проведения ДН (PLAN)
/// </summary>
public class DspnPlan
{
    [XmlElement("MCOD_PLAN")]
    public string? McodPlan { get; set; }

    [XmlElement("MO_PODR_ID")]
    public string? MoPodrId { get; set; }

    [XmlElement("MED_AREA_CODE")]
    public string? MedAreaCode { get; set; }

    [XmlElement("MO_ASSIGN")]
    public int MoAssign { get; set; }

    [XmlElement("DS_CODE")]
    public string? DsCode { get; set; }

    [XmlElement("PLAN_DATE_START")]
    public string? PlanDateStart { get; set; }

    [XmlElement("PLAN_DATE_END")]
    public string? PlanDateEnd { get; set; }
}

/// <summary>
/// Сведения об информировании ЗЛ (INF)
/// </summary>
public class DspnInf
{
    [XmlElement("INFTYPE")]
    public int InfType { get; set; }

    [XmlElement("SPOSOBINF")]
    public int SposobInf { get; set; }

    [XmlElement("DATAINF")]
    public string? DataInf { get; set; }
}
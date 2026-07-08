using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Import;


/// Корневой элемент DN - Сведения ЗЛ на ДН (результаты верификации)

[XmlRoot("DN")]
public class GfImportDto
{

    [XmlElement("ZGLV")]
    public GfImportHeader? Header { get; set; }
    [XmlElement("ZAP")]
    public List<GfImportRecord>? Records { get; set; }
}


/// Заголовок файла GF

public class GfImportHeader
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
    [XmlElement("PERIOD")]
    public string? Period { get; set; }
    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
    [XmlElement("SD_VALITED_ENP")]
    public int ValidatedEnpCount { get; set; }
}


/// Запись о застрахованном лице (результаты верификации)

public class GfImportRecord
{

    [XmlElement("DN_PATIENT_ID")]
    public string? DnPatientId { get; set; }
    [XmlElement("ENP")]
    public string? ENP { get; set; }
    [XmlElement("W")]
    public int? Gender { get; set; }
    [XmlElement("DR")]
    public string? BirthDate { get; set; }
    [XmlElement("SMO")]
    public string? Smo { get; set; }
    [XmlElement("ATTACH_MCODE")]
    public string? AttachMcode { get; set; }
    [XmlElement("ATTACH_DATE")]
    public string? AttachDate { get; set; }
    [XmlElement("SMO_REGION_CD")]
    public string? SmoRegionCode { get; set; }

    [XmlElement("GROUP_RH_CD")]
    public int? GroupRhCode { get; set; }
    [XmlElement("GROUP_RH_DS")]
    public string? GroupRhDs { get; set; }
    [XmlElement("DN_PRVS")]
    public int? DnPrvs { get; set; }
    [XmlElement("GROUP_RH_PROFILE")]
    public string? GroupRhProfile { get; set; }
    [XmlElement("GROUP_RH_NAME")]
    public string? GroupRhName { get; set; }
    [XmlElement("DN_RULE_IN_NAME")]
    public string? DnRuleInName { get; set; }

    [XmlElement("DN_GIS")]
    public DnGisInfo? DnGis { get; set; }


    [XmlElement("DN_LIST")]
    public DnListResult? DnList { get; set; }
    [XmlElement("DN_PLAN")]
    public DnPlanResult? DnPlan { get; set; }

    [XmlElement("INSERT_DTTM")]
    public string? InsertDttm { get; set; }
    [XmlElement("UPDATE_DTTM")]
    public string? UpdateDttm { get; set; }
}



public class DnGisInfo
{

    [XmlElement("TRIGGER_SCHET_FILENAME")]
    public string? TriggerSchetnFilename { get; set; }
    [XmlElement("TRIGGER_SCHET_CODE")]
    public string? TriggerSchetnCode { get; set; }
    [XmlElement("TRIGGER_NSCHET")]
    public string? TriggerNschet { get; set; }
    [XmlElement("TRIGGER_DSCHET")]
    public string? TriggerDschet { get; set; }
    [XmlElement("TRIGGER_IDCASE")]
    public string? TriggerIdCase { get; set; }
    [XmlElement("TRIGGER_SL_ID")]
    public string? TriggerSlId { get; set; }
    [XmlElement("TRIGGER_SL_NHISTORY")]
    public string? TriggerSlNhistory { get; set; }
    [XmlElement("TRIGGER_DS_CD")]
    public string? TriggerDsCd { get; set; }
    [XmlElement("TRIGGER_MCODE")]
    public string? TriggerMcode { get; set; }
    [XmlElement("TRIGGER_DT")]
    public string? TriggerDt { get; set; }
}


public class DnListResult
{
 
    [XmlElement("DN_LIST_PERIOD_CD")]
    public int? DnListPeriodCode { get; set; }
    [XmlElement("DN_LIST_FILENAME")]
    public string? DnListFilename { get; set; }
    [XmlElement("CODE_L")]
    public string? CodeL { get; set; }
    [XmlElement("DN_LIST_RESULT_CODE")]
    public string? DnListResultCode { get; set; }
    [XmlElement("DN_LIST_DATE_CHEKING")]
    public string? DnListDateChecking { get; set; }
    [XmlElement("DN_LIST_RESULT_DESCR")]
    public string? DnListResultDescr { get; set; }
}


public class DnPlanResult
{

    [XmlElement("DN_PLAN_PERIOD")]
    public string? DnPlanPeriod { get; set; }
    [XmlElement("DN_PLAN_FILENAME")]
    public string? DnPlanFilename { get; set; }
    [XmlElement("CODE_P")]
    public string? CodeP { get; set; }
    [XmlElement("DN_PLAN_RESULT_CODE")]
    public int? DnPlanResultCode { get; set; }
    [XmlElement("DN_PLAN_DATE_CHEKING")]
    public string? DnPlanDateChecking { get; set; }
    [XmlElement("DN_PLAN_RESULT_DESCR")]
    public string? DnPlanResultDescr { get; set; }
}
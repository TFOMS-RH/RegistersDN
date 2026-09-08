using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Import;

/// <summary>
/// DF — Списки лиц для диспансеризации
/// Корневой элемент: DISP
/// </summary>
[XmlRoot("DISP")]
public class DfImportDto
{
    [XmlElement("ZGLV")]
    public DfImportHeader? Header { get; set; }

    [XmlElement("ZAP")]
    public List<DfImportRecord>? Records { get; set; }
}

public class DfImportHeader
{
    [XmlElement("VERSION")]
    public string? Version { get; set; } // "D1.00"

    [XmlElement("DATA")]
    public string? Data { get; set; } // ГГГГ-ММ-ДД

    [XmlElement("FILENAME")]
    public string? FileName { get; set; }

    [XmlElement("REGION_CD")]
    public string? RegionCode { get; set; }

    [XmlElement("PERIOD")]
    public string? Period { get; set; } // ГГГГММ

    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
}

public class DfImportRecord
{
    [XmlElement("MASTER_PERSON_ID")]
    public string? MasterPersonId { get; set; }

    [XmlElement("ENP")]
    public string? ENP { get; set; }

    [XmlElement("DR")]
    public string? BirthDate { get; set; }

    [XmlElement("POL_NAME")]
    public string? PolName { get; set; }

    [XmlElement("SMO")]
    public string? Smo { get; set; }

    [XmlElement("ATTACH_MO_CD")]
    public string? AttachMoCd { get; set; }

    [XmlElement("ATTACH_MO_VERS")]
    public string? AttachMoVers { get; set; }

    [XmlElement("ATTACH_DATE")]
    public string? AttachDate { get; set; }

    [XmlElement("SMO_REGION_CD")]
    public string? SmoRegionCd { get; set; }

    [XmlElement("DISPANS_TYPE")]
    public string? DispansType { get; set; }

    [XmlElement("DISPANS_TYPE_NAME")]
    public string? DispansTypeName { get; set; }

    [XmlElement("DISPANS_STATUS")]
    public string? DispansStatus { get; set; }
}
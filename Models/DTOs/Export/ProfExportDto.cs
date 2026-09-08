using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Export;

[XmlRoot("ZL_LIST")]
public class ProfExportDto
{
    [XmlElement("ZGLV")]
    public ProfExportHeader? Header { get; set; }

    [XmlElement("SV_PR_MER")]
    public List<ProfExportRecord>? Records { get; set; }
}

public class ProfExportHeader
{
    [XmlElement("VERSION")]
    public string Version { get; set; } = "1.1";

    [XmlElement("DATA")]
    public string Data { get; set; } = DateTime.Now.ToString("yyyyMMdd");

    [XmlElement("FILENAME")]
    public string? FileName { get; set; }

    [XmlElement("CODMO")]
    public string? CodMo { get; set; }
}

public class ProfExportRecord
{
    [XmlElement("NOMER_Z")]
    public long NomerZ { get; set; }

    [XmlElement("PERSON_ID")]
    public string? PersonId { get; set; }

    [XmlElement("SMOCOD")]
    public string? SmoCod { get; set; }

    [XmlElement("ENP")]
    public string? Enp { get; set; }

    [XmlElement("FAM")]
    public string? Fam { get; set; }

    [XmlElement("IM")]
    public string? Im { get; set; }

    [XmlElement("OT")]
    public string? Ot { get; set; }

    [XmlElement("DR")]
    public string? Dr { get; set; }

    [XmlElement("W")]
    public int W { get; set; }

    [XmlElement("DOCTYPE")]
    public string? DocType { get; set; }

    [XmlElement("DOCSER")]
    public string? DocSer { get; set; }

    [XmlElement("DOCNUM")]
    public string? DocNum { get; set; }

    [XmlElement("SNILS")]
    public string? Snils { get; set; }

    [XmlElement("VPOLIS")]
    public int Vpolis { get; set; }

    [XmlElement("SPOLIS")]
    public string? Spolis { get; set; }

    [XmlElement("NPOLIS")]
    public string? Npolis { get; set; }

    [XmlElement("TEL")]
    public string? Tel { get; set; }

    [XmlElement("IDDOKT")]
    public string? Iddokt { get; set; }

    [XmlElement("ADRES")]
    public string? Adres { get; set; }

    [XmlElement("KAT_LG")]
    public int? KatLg { get; set; }

    [XmlElement("YEAR")]
    public int Year { get; set; }

    [XmlElement("COMMENT")]
    public string? Comment { get; set; }

    [XmlElement("SV_PL_MER")]
    public List<ProfExportMer>? MerRecords { get; set; }
}

public class ProfExportMer
{
    [XmlElement("MONTH")]
    public int Month { get; set; }

    [XmlElement("DISP")]
    public string? Disp { get; set; }
}
using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Export;

[XmlRoot("ZL_LIST")]
public class ProfResponseDto
{
    [XmlElement("ZGLV")]
    public ProfResponseHeader? Header { get; set; }

    [XmlElement("ZAP")]
    public List<ProfResponseRecord>? Records { get; set; }
}

public class ProfResponseHeader
{
    [XmlElement("VERSION")]
    public string Version { get; set; } = "1.0";

    [XmlElement("DATA")]
    public string Data { get; set; } = DateTime.Now.ToString("yyyyMMdd");

    [XmlElement("FILENAME")]
    public string? FileName { get; set; }

    [XmlElement("FILENAME_1")]
    public string? FileNameOriginal { get; set; }

    [XmlElement("YEAR")]
    public int Year { get; set; }

    [XmlElement("MONTH")]
    public int Month { get; set; }
}

public class ProfResponseRecord
{
    [XmlElement("N_ZAP")]
    public long NZap { get; set; }

    [XmlElement("ENP")]
    public string? Enp { get; set; }

    [XmlElement("RPL")]
    public int Rpl { get; set; }

    [XmlElement("COMMENTR")]
    public string? Commentr { get; set; }
}
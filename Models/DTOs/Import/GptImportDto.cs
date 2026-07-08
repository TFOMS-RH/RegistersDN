using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Import;

/// <summary>
/// Корневой элемент DN_PLAN - Сведения о ДН (план-график)
/// </summary>
[XmlRoot("DN_PLAN")]
public class GptImportDto
{
    /// <summary>
    /// Заголовок файла (ZGLV)
    /// </summary>
    [XmlElement("ZGLV")]
    public GptImportHeader? Header { get; set; }

    /// <summary>
    /// Список записей (ZAP)
    /// </summary>
    [XmlElement("ZAP")]
    public List<GptImportRecord>? Records { get; set; }
}

/// <summary>
/// Заголовок файла GPT
/// </summary>
public class GptImportHeader
{
    /// <summary>
    /// Версия взаимодействия. Всегда "P3.20"
    /// </summary>
    [XmlElement("VERSION")]
    public string? Version { get; set; }

    /// <summary>
    /// Тип файла. Всегда "GPT"
    /// </summary>
    [XmlElement("FILE_TYPE")]
    public string? FileType { get; set; }

    /// <summary>
    /// Дата файла. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("DATA")]
    public string? Data { get; set; }

    /// <summary>
    /// Имя файла без расширения
    /// </summary>
    [XmlElement("FILENAME")]
    public string? FileName { get; set; }

    /// <summary>
    /// Двузначный код ТФОМС
    /// </summary>
    [XmlElement("REGION_CD")]
    public string? RegionCode { get; set; }

    /// <summary>
    /// Количество записей в файле
    /// </summary>
    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
}

/// <summary>
/// Запись плана-графика проведения профилактических мероприятий ДН
/// </summary>
public class GptImportRecord
{
    /// <summary>
    /// Уникальный идентификатор записи в ИС поставщика
    /// </summary>
    [XmlElement("CODE_P")]
    public string? CodeP { get; set; }

    /// <summary>
    /// Уникальный идентификатор записи о состоянии ЗЛ в ГИС ОМС
    /// </summary>
    [XmlElement("DN_PATIENT_ID")]
    public string? DnPatientId { get; set; }

    /// <summary>
    /// Единый номер полиса пациента
    /// </summary>
    [XmlElement("ENP")]
    public string? ENP { get; set; }

    /// <summary>
    /// Код повода информирования. Всегда "ДН"
    /// </summary>
    [XmlElement("CODE_PINF")]
    public string? CodePinf { get; set; }

    /// <summary>
    /// Код МО проведения обследования/осмотра
    /// </summary>
    [XmlElement("MCOD_PLAN")]
    public string? McodPlan { get; set; }

    /// <summary>
    /// Идентификатор адреса оказания МП
    /// </summary>
    [XmlElement("MO_PODR_ID")]
    public string? MoPodrId { get; set; }

    /// <summary>
    /// Номер или код врачебного участка МО
    /// </summary>
    [XmlElement("MED_AREA_CODE")]
    public string? MedAreaCode { get; set; }

    /// <summary>
    /// Признак места проведения: 1 - по прикреплению ПМСП, 0 - не по месту прикрепления
    /// </summary>
    [XmlElement("MO_ASSIGN")]
    public int MoAssign { get; set; }

    /// <summary>
    /// Срок выполнения информирования. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("END_DATE_INF")]
    public string? EndDateInf { get; set; }

    /// <summary>
    /// Тип информирования: 1 - первичное, 2 - повторное
    /// </summary>
    [XmlElement("PRIMARY_INF")]
    public int PrimaryInf { get; set; }

    /// <summary>
    /// Диагноз основной по МКБ-10
    /// </summary>
    [XmlElement("DS_CODE")]
    public string? DsCode { get; set; }

    /// <summary>
    /// Планируемая дата начала периода. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("PLAN_DATE_START")]
    public string? PlanDateStart { get; set; }

    /// <summary>
    /// Планируемая дата завершения периода. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("PLAN_DATE_END")]
    public string? PlanDateEnd { get; set; }
}
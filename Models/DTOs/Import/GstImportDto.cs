using System.Xml.Serialization;

namespace RegistrDN.Models.DTOs.Import;

/// <summary>
/// Корневой элемент DN_LIST - Сведения о ДН
/// </summary>
[XmlRoot("DN_LIST")]
public class GstImportDto
{
    /// <summary>
    /// Заголовок файла (ZGLV)
    /// </summary>
    [XmlElement("ZGLV")]
    public GstImportHeader? Header { get; set; }

    /// <summary>
    /// Список записей (ZAP)
    /// </summary>
    [XmlElement("ZAP")]
    public List<GstImportRecord>? Records { get; set; }
}

/// <summary>
/// Заголовок файла GST
/// </summary>
public class GstImportHeader
{
    /// <summary>
    /// Версия взаимодействия. Всегда "P1.20"
    /// </summary>
    [XmlElement("VERSION")]
    public string? Version { get; set; }

    /// <summary>
    /// Тип файла. Всегда "GST"
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
    /// Порядковый номер файла в отчетном периоде
    /// </summary>
    [XmlElement("NN_FILE")]
    public int? FileNumber { get; set; }

    /// <summary>
    /// Количество записей в файле
    /// </summary>
    [XmlElement("SD_Z")]
    public int RecordsCount { get; set; }
}

/// <summary>
/// Запись о состоянии на ДН
/// </summary>
public class GstImportRecord
{
    /// <summary>
    /// Уникальный идентификатор записи в ИС поставщика
    /// </summary>
    [XmlElement("CODE_L")]
    public string? CodeL { get; set; }

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
    /// Уникальный идентификатор записи о состоянии ЗЛ в ГИС ОМС
    /// </summary>
    [XmlElement("DN_PATIENT_ID")]
    public string? DnPatientId { get; set; }

    /// <summary>
    /// Код диагноза по МКБ-10
    /// </summary>
    [XmlElement("DIAG_CODE")]
    public string? DiagCode { get; set; }

    /// <summary>
    /// Дата установления хронического состояния. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("DIAG_DATE")]
    public string? DiagDate { get; set; }

    /// <summary>
    /// Дата постановки на диспансерное наблюдение. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("DATE_DN_IN")]
    public string? DateDnIn { get; set; }

    /// <summary>
    /// Дата снятия с диспансерного наблюдения. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("DATE_DN_OUT")]
    public string? DateDnOut { get; set; }

    /// <summary>
    /// Код специальности медицинского работника
    /// </summary>
    [XmlElement("DN_PRVS")]
    public int? DnPrvs { get; set; }

    /// <summary>
    /// Код МО последнего обращения за МП
    /// </summary>
    [XmlElement("LAST_SL_MCOD")]
    public int? LastSlMcod { get; set; }

    /// <summary>
    /// Номер ТАП / номер истории болезни
    /// </summary>
    [XmlElement("LAST_SL_NHISTORY")]
    public string? LastSlNhistory { get; set; }

    /// <summary>
    /// Дата последнего обращения. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("LAST_SL_DATE")]
    public string? LastSlDate { get; set; }

    /// <summary>
    /// Статус ДН: 1 - Установлено ранее, 2 - Установлено впервые
    /// </summary>
    [XmlElement("STATUS_DN_IN")]
    public int? StatusDnIn { get; set; }

    /// <summary>
    /// Причина закрытия эпизода ДН
    /// </summary>
    [XmlElement("REASON_DN_OUT")]
    public string? ReasonDnOut { get; set; }

    /// <summary>
    /// Обстоятельства включения в список
    /// </summary>
    [XmlElement("REASON_DN_IN")]
    public int ReasonDnIn { get; set; }

    /// <summary>
    /// Код МО прикрепления/наблюдения
    /// </summary>
    [XmlElement("MCOD")]
    public string? Mcod { get; set; }

    /// <summary>
    /// Признак места проведения ДН: 1 - по прикреплению, 0 - не по месту прикрепления
    /// </summary>
    [XmlElement("MO_ASSIGN")]
    public int? MoAssign { get; set; }

    /// <summary>
    /// Дата проверки записи. Формат "ГГГГ-ММ-ДД"
    /// </summary>
    [XmlElement("DATE_CHECKING")]
    public string? DateChecking { get; set; }
}
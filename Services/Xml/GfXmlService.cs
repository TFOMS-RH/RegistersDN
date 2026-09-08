using System.Xml;
using System.Xml.Serialization;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using RegistrDN.Data;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Models.Entities;
using RegistrDN.Services.Interfaces;

namespace RegistrDN.Services.Xml;

public class GfXmlService : IXmlService<GfImportDto, GfExportDto, GfEntity>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GfXmlService> _logger;

    public GfXmlService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GfXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<GfImportDto> ParseXmlAsync(string xmlContent)
    {
        try
        {
            var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            var utf8String = Encoding.UTF8.GetString(bytes);

            var serializer = new XmlSerializer(typeof(GfImportDto));
            using var reader = new StringReader(utf8String);
            var result = (GfImportDto?)serializer.Deserialize(reader);

            if (result == null)
                throw new InvalidOperationException("Не удалось распарсить XML");

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга XML");
            throw;
        }
    }

    private GfImportDto ConvertOldToNew(GfImportDtoOld oldDto)
    {
        return new GfImportDto
        {
            Header = new GfImportHeader
            {
                Version = oldDto.Header?.Version,
                FileType = oldDto.Header?.FileType,
                Data = oldDto.Header?.Data,
                FileName = oldDto.Header?.FileName,
                RegionCode = oldDto.Header?.RegionCode,
                Period = oldDto.Header?.Period,
                RecordsCount = oldDto.Header?.RecordsCount.ToString(),
                ValidatedEnpCount = oldDto.Header?.ValidatedEnpCount?.ToString()
            },
            Records = oldDto.Records?.Select(r => new GfImportRecord
            {
                DnPatientId = r.DnPatientId,
                ENP = r.ENP,
                Gender = r.Gender?.ToString(),
                BirthDate = r.BirthDate,
                Smo = r.Smo,
                AttachMcode = r.AttachMcode,
                AttachDate = r.AttachDate,
                SmoRegionCode = r.SmoRegionCode,
                GroupRhCode = r.GroupRhCode?.ToString(),
                GroupRhDs = r.GroupRhDs,
                DnPrvs = r.DnPrvs?.ToString(),
                GroupRhProfile = r.GroupRhProfile,
                GroupRhName = r.GroupRhName,
                DnRuleInName = r.DnRuleInName,
                DnGis = r.DnGis != null ? new DnGisImportInfo
                {
                    TriggerSchetnFilename = r.DnGis.TriggerSchetnFilename,
                    TriggerSchetnCode = r.DnGis.TriggerSchetnCode,
                    TriggerNschet = r.DnGis.TriggerNschet,
                    TriggerDschet = r.DnGis.TriggerDschet,
                    TriggerIdCase = r.DnGis.TriggerIdCase,
                    TriggerSlId = r.DnGis.TriggerSlId,
                    TriggerSlNhistory = r.DnGis.TriggerSlNhistory,
                    TriggerDsCd = r.DnGis.TriggerDsCd,
                    TriggerMcode = r.DnGis.TriggerMcode,
                    TriggerDt = r.DnGis.TriggerDt
                } : null,
                DnList = r.DnList != null ? new DnListImportResult
                {
                    DnListPeriodCode = r.DnList.DnListPeriodCode,
                    DnListFilename = r.DnList.DnListFilename,
                    CodeL = r.DnList.CodeL,
                    DnListResultCode = r.DnList.DnListResultCode,
                    DnListDateChecking = r.DnList.DnListDateChecking,
                    DnListResultDescr = r.DnList.DnListResultDescr
                } : null,
                DnPlan = r.DnPlan != null ? new DnPlanImportResult
                {
                    DnPlanPeriod = r.DnPlan.DnPlanPeriod,
                    DnPlanFilename = r.DnPlan.DnPlanFilename,
                    CodeP = r.DnPlan.CodeP,
                    DnPlanResultCode = r.DnPlan.DnPlanResultCode?.ToString(),
                    DnPlanDateChecking = r.DnPlan.DnPlanDateChecking,
                    DnPlanResultDescr = r.DnPlan.DnPlanResultDescr
                } : null,
                InsertDttm = r.InsertDttm,
                UpdateDttm = r.UpdateDttm
            }).ToList()
        };
    }

    public Task<string> SerializeToXmlAsync(GfExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GfExportDto));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.GetEncoding("windows-1251"),
                OmitXmlDeclaration = false,
                NewLineHandling = NewLineHandling.Entitize
            };

            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(stream, settings);
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            serializer.Serialize(writer, exportData, ns);
            writer.Flush();

            var result = Encoding.GetEncoding("windows-1251").GetString(stream.ToArray());
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сериализации GF XML");
            throw;
        }
    }

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;
            if (dto.Header == null) return Task.FromResult(false);
            if (dto.Header.FileType != "GF" && dto.Header.FileType != "GS") return Task.FromResult(false);
            if (dto.Header.Version != "P5.00" && dto.Header.Version != "G5.00") return Task.FromResult(false);
            if (dto.Records == null || dto.Records.Count == 0) return Task.FromResult(false);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<(bool success, string message, int recordsCount, List<string> errors)> ImportAsync(
        string xmlContent, int documentId)
    {
        var errors = new List<string>();
        var importedRecords = new List<GfEntity>();

        try
        {
            _logger.LogInformation($"Начало импорта GF для документа {documentId}");
            var importData = await ParseXmlAsync(xmlContent);

            if (importData == null)
                return (false, "Ошибка парсинга XML", 0, errors);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                return (false, "Документ не найден", 0, errors);

            var records = importData.Records ?? new List<GfImportRecord>();
            _logger.LogInformation($"Начинаем обработку {records.Count} записей");

            int processedCount = 0;
            foreach (var record in records)
            {
                processedCount++;
                if (processedCount % 10000 == 0)
                    _logger.LogInformation($"Обработано {processedCount} из {records.Count} записей");

                try
                {
                    if (string.IsNullOrWhiteSpace(record.ENP))
                    {
                        errors.Add($"Запись #{processedCount} пропущена: отсутствует ENP");
                        continue;
                    }

                    var entity = new GfEntity
                    {
                        DnPatientId = record.DnPatientId,
                        ENP = record.ENP ?? string.Empty,
                        Gender = ParseInt(record.Gender),
                        BirthDate = ParseDate(record.BirthDate),
                        Smo = record.Smo,
                        AttachMcode = record.AttachMcode,
                        AttachDate = ParseDate(record.AttachDate),
                        SmoRegionCode = record.SmoRegionCode,
                        GroupRhCode = ParseInt(record.GroupRhCode),
                        GroupRhDs = record.GroupRhDs,
                        DnPrvs = ParseInt(record.DnPrvs),
                        GroupRhProfile = TruncateString(record.GroupRhProfile, 255),
                        GroupRhName = TruncateString(record.GroupRhName, 255),
                        DnRuleInName = TruncateString(record.DnRuleInName, 255),
                        DnListPeriodCode = ParseInt(record.DnList?.DnListPeriodCode),
                        DnListFilename = record.DnList?.DnListFilename,
                        CodeL = record.DnList?.CodeL,
                        DnListResultCode = record.DnList?.DnListResultCode,
                        DnListDateChecking = ParseDate(record.DnList?.DnListDateChecking),
                        DnListResultDescr = record.DnList?.DnListResultDescr,
                        DnPlanPeriod = record.DnPlan?.DnPlanPeriod,
                        DnPlanFilename = record.DnPlan?.DnPlanFilename,
                        CodeP = record.DnPlan?.CodeP,
                        DnPlanResultCode = ParseInt(record.DnPlan?.DnPlanResultCode),
                        DnPlanDateChecking = ParseDate(record.DnPlan?.DnPlanDateChecking),
                        DnPlanResultDescr = record.DnPlan?.DnPlanResultDescr,
                        TriggerSchetnFilename = record.DnGis?.TriggerSchetnFilename,
                        TriggerSchetnCode = record.DnGis?.TriggerSchetnCode,
                        TriggerNschet = record.DnGis?.TriggerNschet,
                        TriggerDschet = ParseDate(record.DnGis?.TriggerDschet),
                        TriggerIdCase = record.DnGis?.TriggerIdCase,
                        TriggerSlId = record.DnGis?.TriggerSlId,
                        TriggerSlNhistory = record.DnGis?.TriggerSlNhistory,
                        TriggerDsCd = record.DnGis?.TriggerDsCd,
                        TriggerMcode = record.DnGis?.TriggerMcode,
                        TriggerDt = ParseDate(record.DnGis?.TriggerDt),
                        InsertDttm = ParseDate(record.InsertDttm) ?? DateTime.Now,
                        UpdateDttm = ParseDate(record.UpdateDttm) ?? DateTime.Now,
                        DocumentId = documentId
                    };

                    importedRecords.Add(entity);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка обработки записи #{processedCount}: {ex.Message}");
                }
            }

            _logger.LogInformation($"Успешно обработано {importedRecords.Count} записей из {records.Count}");

            if (importedRecords.Any())
            {
                try
                {
                    int batchSize = 10000;
                    int totalSaved = 0;

                    for (int i = 0; i < importedRecords.Count; i += batchSize)
                    {
                        var batch = importedRecords.Skip(i).Take(batchSize).ToList();
                        await _unitOfWork.GfRecords.AddRangeAsync(batch);
                        await _unitOfWork.SaveChangesAsync();
                        totalSaved += batch.Count;
                        _logger.LogInformation($"Сохранено {totalSaved} из {importedRecords.Count} записей");
                    }

                    _logger.LogInformation($"Сохранено {importedRecords.Count} записей GF");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка сохранения в БД");
                    errors.Add($"Ошибка сохранения в БД: {ex.Message}");
                    return (false, $"Ошибка сохранения в БД: {ex.Message}", 0, errors);
                }
            }

            return (true, $"Импортировано {importedRecords.Count} записей", importedRecords.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка импорта GF");
            errors.Add($"Критическая ошибка: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            var entities = await _unitOfWork.GfRecords.FindAsync(x => x.DocumentId == documentId);
            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var records = _mapper.Map<List<GfExportRecord>>(entities);
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var header = new GfExportHeader
            {
                FileName = document?.FileName,
                RegionCode = document?.RegionCode,
                Period = document?.Period ?? DateTime.Now.ToString("yyyyMM"),
                RecordsCount = records.Count,
                ValidatedEnpCount = document?.ValidatedEnpCount ?? records.Count,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportDto = new GfExportDto { Header = header, Records = records };
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта GF");
            throw;
        }
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;
        if (DateTime.TryParse(dateString, out var date)) return date;
        if (dateString.Length == 8 && DateTime.TryParseExact(dateString, "yyyyMMdd", 
            System.Globalization.CultureInfo.InvariantCulture, 
            System.Globalization.DateTimeStyles.None, out var date2)) return date2;
        return null;
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var result)) return result;
        return null;
    }

    private string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }

    private Encoding? GetEncodingFromXml(string xmlContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(xmlContent, @"encoding\s*=\s*[""']([^""']+)[""']");
        if (match.Success)
        {
            try { return Encoding.GetEncoding(match.Groups[1].Value); }
            catch { return null; }
        }
        return null;
    }
}

[XmlRoot("DN")]
public class GfImportDtoOld
{
    [XmlElement("ZGLV")] public GfImportHeaderOld? Header { get; set; }
    [XmlElement("ZAP")] public List<GfImportRecordOld>? Records { get; set; }
}

public class GfImportHeaderOld
{
    [XmlElement("VERSION")] public string? Version { get; set; }
    [XmlElement("FILE_TYPE")] public string? FileType { get; set; }
    [XmlElement("DATA")] public string? Data { get; set; }
    [XmlElement("FILENAME")] public string? FileName { get; set; }
    [XmlElement("REGION_CD")] public string? RegionCode { get; set; }
    [XmlElement("PERIOD")] public string? Period { get; set; }
    [XmlElement("SD_Z")] public int RecordsCount { get; set; }
    [XmlElement("SD_VALITED_ENP")] public int? ValidatedEnpCount { get; set; }
}

public class GfImportRecordOld
{
    [XmlElement("DN_PATIENT_ID")] public string? DnPatientId { get; set; }
    [XmlElement("ENP")] public string? ENP { get; set; }
    [XmlElement("W")] public int? Gender { get; set; }
    [XmlElement("DR")] public string? BirthDate { get; set; }
    [XmlElement("SMO")] public string? Smo { get; set; }
    [XmlElement("ATTACH_MCODE")] public string? AttachMcode { get; set; }
    [XmlElement("ATTACH_DATE")] public string? AttachDate { get; set; }
    [XmlElement("SMO_REGION_CD")] public string? SmoRegionCode { get; set; }
    [XmlElement("GROUP_RH_CD")] public int? GroupRhCode { get; set; }
    [XmlElement("GROUP_RH_DS")] public string? GroupRhDs { get; set; }
    [XmlElement("DN_PRVS")] public int? DnPrvs { get; set; }
    [XmlElement("GROUP_RH_PROFILE")] public string? GroupRhProfile { get; set; }
    [XmlElement("GROUP_RH_NAME")] public string? GroupRhName { get; set; }
    [XmlElement("DN_RULE_IN_NAME")] public string? DnRuleInName { get; set; }
    [XmlElement("DN_GIS")] public DnGisInfoOld? DnGis { get; set; }
    [XmlElement("DN_LIST")] public DnListResultOld? DnList { get; set; }
    [XmlElement("DN_PLAN")] public DnPlanResultOld? DnPlan { get; set; }
    [XmlElement("INSERT_DTTM")] public string? InsertDttm { get; set; }
    [XmlElement("UPDATE_DTTM")] public string? UpdateDttm { get; set; }
}

public class DnGisInfoOld
{
    [XmlElement("TRIGGER_SCHET_FILENAME")] public string? TriggerSchetnFilename { get; set; }
    [XmlElement("TRIGGER_SCHET_CODE")] public string? TriggerSchetnCode { get; set; }
    [XmlElement("TRIGGER_NSCHET")] public string? TriggerNschet { get; set; }
    [XmlElement("TRIGGER_DSCHET")] public string? TriggerDschet { get; set; }
    [XmlElement("TRIGGER_IDCASE")] public string? TriggerIdCase { get; set; }
    [XmlElement("TRIGGER_SL_ID")] public string? TriggerSlId { get; set; }
    [XmlElement("TRIGGER_SL_NHISTORY")] public string? TriggerSlNhistory { get; set; }
    [XmlElement("TRIGGER_DS_CD")] public string? TriggerDsCd { get; set; }
    [XmlElement("TRIGGER_MCODE")] public string? TriggerMcode { get; set; }
    [XmlElement("TRIGGER_DT")] public string? TriggerDt { get; set; }
}

public class DnListResultOld
{
    [XmlElement("DN_LIST_PERIOD")] public string? DnListPeriodCode { get; set; }
    [XmlElement("DN_LIST_FILENAME")] public string? DnListFilename { get; set; }
    [XmlElement("CODE_L")] public string? CodeL { get; set; }
    [XmlElement("DN_LIST_RESULT_CODE")] public string? DnListResultCode { get; set; }
    [XmlElement("DN_LIST_DATE_CHEKING")] public string? DnListDateChecking { get; set; }
    [XmlElement("DN_LIST_RESULT_DESCR")] public string? DnListResultDescr { get; set; }
}

public class DnPlanResultOld
{
    [XmlElement("DN_PLAN_PERIOD")] public string? DnPlanPeriod { get; set; }
    [XmlElement("DN_PLAN_FILENAME")] public string? DnPlanFilename { get; set; }
    [XmlElement("CODE_P")] public string? CodeP { get; set; }
    [XmlElement("DN_PLAN_RESULT_CODE")] public int? DnPlanResultCode { get; set; }
    [XmlElement("DN_PLAN_DATE_CHEKING")] public string? DnPlanDateChecking { get; set; }
    [XmlElement("DN_PLAN_RESULT_DESCR")] public string? DnPlanResultDescr { get; set; }
}
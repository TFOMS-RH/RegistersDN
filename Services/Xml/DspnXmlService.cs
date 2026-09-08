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

public class DspnXmlService : IXmlServiceWithResponse<DspnImportDto, DspnExportDto, DspnEntity, DspnResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DspnXmlService> _logger;

    public DspnXmlService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<DspnXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<DspnImportDto> ParseXmlAsync(string xmlContent)
    {
        try
    {
        var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
        var utf8String = Encoding.UTF8.GetString(bytes);

        var serializer = new XmlSerializer(typeof(DspnImportDto));
        using var reader = new StringReader(utf8String);
        var result = (DspnImportDto?)serializer.Deserialize(reader);

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

    public Task<string> SerializeToXmlAsync(DspnExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(DspnExportDto));
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
            _logger.LogError(ex, "Ошибка сериализации DSPN XML");
            throw;
        }
    }

    public Task<string> SerializeResponseToXmlAsync(DspnResponseDto responseData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(DspnResponseDto));
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

            serializer.Serialize(writer, responseData, ns);
            writer.Flush();

            var result = Encoding.GetEncoding("windows-1251").GetString(stream.ToArray());
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сериализации DSPN ответного файла");
            throw;
        }
    }

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;
            if (dto.Header == null || dto.Header.Version != "1.0")
                return Task.FromResult(false);
            if (dto.Records == null || dto.Records.Count == 0)
                return Task.FromResult(false);
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
        var importedRecords = new List<DspnEntity>();

        try
        {
            _logger.LogInformation($"Начало импорта DSPN для документа {documentId}");
            _logger.LogInformation($"Длина XML: {xmlContent?.Length ?? 0} символов");
            
            if (string.IsNullOrEmpty(xmlContent))
            {
                errors.Add("XML содержимое пустое");
                return (false, "XML содержимое пустое", 0, errors);
            }

            var preview = xmlContent.Length > 500 ? xmlContent.Substring(0, 500) : xmlContent;
            _logger.LogInformation($"Первые 500 символов XML: {preview}");

            var importData = await ParseXmlAsync(xmlContent);
            if (importData == null)
            {
                errors.Add("Не удалось распарсить XML");
                return (false, "Ошибка парсинга XML", 0, errors);
            }

            var period = $"{importData.Header?.Year}{importData.Header?.Month:D2}";
            var records = importData.Records ?? new List<DspnImportRecord>();
            _logger.LogInformation($"Найдено {records.Count} записей DSPN");

            foreach (var record in records)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(record.Pacient?.Npolis))
                    {
                        errors.Add($"Запись N_ZAP={record.NZap}: отсутствует NPOLIS/ЕНП");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.Pacient?.DiagCode))
                    {
                        errors.Add($"Запись N_ZAP={record.NZap}: отсутствует DIAG_CODE");
                        continue;
                    }

                    _logger.LogInformation($"Обработка записи: NZap={record.NZap}, NPOLIS={record.Pacient?.Npolis}, FAM={record.Pacient?.Fam}, IM={record.Pacient?.Im}");

                    var entity = new DspnEntity
                    {
                        N_Zap = record.NZap,
                        PacientId = record.Pacient?.PacientId,
                        Smo = record.Pacient?.Smo,
                        Spolis = record.Pacient?.Spolis,
                        Vpolis = record.Pacient?.Vpolis ?? 0,
                        Npolis = record.Pacient?.Npolis ?? string.Empty,
                        Fam = record.Pacient?.Fam ?? string.Empty,
                        Im = record.Pacient?.Im ?? string.Empty,
                        Ot = record.Pacient?.Ot,
                        Dr = ParseDate(record.Pacient?.Dr) ?? DateTime.Now,
                        W = record.Pacient?.W ?? 0,
                        Adres = record.Pacient?.Adres,
                        Tel = record.Pacient?.Tel,
                        MoP = record.Pacient?.MoP ?? string.Empty,
                        DiagCode = record.Pacient?.DiagCode ?? string.Empty,
                        Iddokt = record.Pacient?.Iddokt ?? string.Empty,
                        DateDnIn = ParseDate(record.Pacient?.DateDnIn) ?? DateTime.Now,
                        DateDnOut = ParseDate(record.Pacient?.DateDnOut),
                        DiagDate = ParseDate(record.Pacient?.DiagDate) ?? DateTime.Now,
                        DnPrvs = record.Pacient?.DnPrvs ?? 0,
                        StatusDnIn = record.Pacient?.StatusDnIn ?? 1,
                        ReasonDnOut = record.Pacient?.ReasonDnOut,
                        ReasonDnIn = record.Pacient?.ReasonDnIn ?? 101,
                        McodPlan = record.Plan?.McodPlan ?? string.Empty,
                        MoPodrId = record.Plan?.MoPodrId,
                        MedAreaCode = record.Plan?.MedAreaCode,
                        MoAssign = record.Plan?.MoAssign ?? 0,
                        DsCode = record.Plan?.DsCode,
                        PlanDateStart = ParseDate(record.Plan?.PlanDateStart) ?? DateTime.Now,
                        PlanDateEnd = ParseDate(record.Plan?.PlanDateEnd) ?? DateTime.Now,
                        InfType = record.InfRecords?.FirstOrDefault()?.InfType,
                        SposobInf = record.InfRecords?.FirstOrDefault()?.SposobInf,
                        DataInf = ParseDate(record.InfRecords?.FirstOrDefault()?.DataInf),
                        Period = period,
                        DocumentId = documentId,
                        IsProcessed = false
                    };

                    importedRecords.Add(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка обработки записи N_ZAP={record.NZap}");
                    errors.Add($"Ошибка обработки записи N_ZAP={record.NZap}: {ex.Message}");
                }
            }

            _logger.LogInformation($"Успешно обработано {importedRecords.Count} записей из {records.Count}");

            if (importedRecords.Any())
            {
                await _unitOfWork.DspnRecords.AddRangeAsync(importedRecords);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Сохранено {importedRecords.Count} записей DSPN");
            }

            return (true, $"Импортировано {importedRecords.Count} записей", importedRecords.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка импорта DSPN");
            errors.Add($"Критическая ошибка: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }

    public async Task<DspnResponseDto> GenerateResponseAsync(int documentId, List<string> errors)
    {
        try
        {
            var records = await _unitOfWork.DspnRecords.FindAsync(x => x.DocumentId == documentId);
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var responseRecords = records.Select(r => new DspnResponseRecord
            {
                NZap = r.N_Zap,
                Enp = r.Npolis,
                Rpl = string.IsNullOrEmpty(r.Npolis) ? 0 : 1,
                Commentr = string.IsNullOrEmpty(r.Npolis) ? "Отсутствует ЕНП" : "Успешно"
            }).ToList();

            return new DspnResponseDto
            {
                Header = new DspnResponseHeader
                {
                    FileName = $"DSPN_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}",
                    FileNameOriginal = document?.FileName,
                    Year = DateTime.Now.Year,
                    Month = DateTime.Now.Month,
                    Data = DateTime.Now.ToString("yyyyMMdd")
                },
                Records = responseRecords
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка генерации ответа DSPN");
            throw;
        }
    }

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            var entities = await _unitOfWork.DspnRecords.FindAsync(x => x.DocumentId == documentId);
            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var exportRecords = entities.Select(e => new DspnExportRecord
            {
                NZap = e.N_Zap,
                Pacient = new DspnExportPacient
                {
                    PacientId = e.PacientId,
                    Smo = e.Smo,
                    Spolis = e.Spolis,
                    Vpolis = e.Vpolis ?? 0,
                    Npolis = e.Npolis,
                    Fam = e.Fam,
                    Im = e.Im,
                    Ot = e.Ot,
                    Dr = e.Dr.ToString("yyyyMMdd"),
                    W = e.W,
                    Adres = e.Adres,
                    Tel = e.Tel,
                    MoP = e.MoP,
                    DiagCode = e.DiagCode,
                    Iddokt = e.Iddokt,
                    DateDnIn = e.DateDnIn.ToString("yyyyMMdd"),
                    DateDnOut = e.DateDnOut?.ToString("yyyyMMdd"),
                    DiagDate = e.DiagDate.ToString("yyyyMMdd"),
                    DnPrvs = e.DnPrvs,
                    StatusDnIn = e.StatusDnIn,
                    ReasonDnOut = e.ReasonDnOut,
                    ReasonDnIn = e.ReasonDnIn
                },
                Plan = new DspnExportPlan
                {
                    McodPlan = e.McodPlan,
                    MoPodrId = e.MoPodrId,
                    MedAreaCode = e.MedAreaCode,
                    MoAssign = e.MoAssign,
                    DsCode = e.DsCode,
                    PlanDateStart = e.PlanDateStart.ToString("yyyyMMdd"),
                    PlanDateEnd = e.PlanDateEnd.ToString("yyyyMMdd")
                },
                InfRecords = e.InfType.HasValue ? new List<DspnExportInf>
                {
                    new DspnExportInf
                    {
                        InfType = e.InfType.Value,
                        SposobInf = e.SposobInf ?? 0,
                        DataInf = e.DataInf?.ToString("yyyyMMdd")
                    }
                } : null
            }).ToList();

            var header = new DspnExportHeader
            {
                FileName = document?.FileName,
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month,
                Data = DateTime.Now.ToString("yyyyMMdd")
            };

            var exportDto = new DspnExportDto { Header = header, Records = exportRecords };
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта DSPN");
            throw;
        }
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;
        if (dateString.Length == 8 && DateTime.TryParseExact(dateString, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date)) return date;
        if (DateTime.TryParse(dateString, out var date2)) return date2;
        return null;
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
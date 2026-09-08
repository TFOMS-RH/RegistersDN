using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AutoMapper;
using Microsoft.Extensions.Logging;
using RegistrDN.Data;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Models.Entities;
using RegistrDN.Services.Interfaces;

namespace RegistrDN.Services.Xml;

public class DfXmlService : IXmlService<DfImportDto, DfExportDto, DfEntity>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DfXmlService> _logger;

    public DfXmlService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<DfXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    // public DfImportDto ParseXml(Stream xmlContent)
    // {
    //     xmlContent.Position = 0;

    //     if (!xmlContent.CanRead)
    //     {
    //         throw new Exception("");
            
    //     }

    //     var serializer = new XmlSerializer(typeof(DfImportDto));
    //     var entity = serializer.Deserialize(xmlContent);

    //     if(entity is not DfImportDto resultEntity)
    //     {
    //         throw new Exception("");
    //     }

    //     return resultEntity;
    // }

    public Task<DfImportDto> ParseXmlAsync(string xmlContent)
    {
        try
    {
        var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
        var utf8String = Encoding.UTF8.GetString(bytes);

        var serializer = new XmlSerializer(typeof(DfImportDto));
        using var reader = new StringReader(utf8String);
        var result = (DfImportDto?)serializer.Deserialize(reader);

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

    public Task<string> SerializeToXmlAsync(DfExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(DfExportDto));
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
            _logger.LogError(ex, "Ошибка сериализации DF XML");
            throw;
        }
    }

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;
            if (dto.Header == null || dto.Header.Version != "D1.00")
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
        var entities = new List<DfEntity>();

        try
        {
            _logger.LogInformation($"Начало импорта DF для документа {documentId}");
            var importData = await ParseXmlAsync(xmlContent);

            if (importData == null)
                return (false, "Ошибка парсинга XML", 0, errors);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                return (false, "Документ не найден", 0, errors);

            var period = document.Period ?? DateTime.Now.ToString("yyyyMM");
            var records = importData.Records ?? new List<DfImportRecord>();

            foreach (var record in records)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(record.ENP))
                    {
                        errors.Add($"Запись MASTER_PERSON_ID={record.MasterPersonId}: отсутствует ENP");
                        continue;
                    }

                    var entity = new DfEntity
                    {
                        MasterPersonId = record.MasterPersonId,
                        ENP = record.ENP ?? string.Empty,
                        BirthDate = ParseDate(record.BirthDate),
                        PolName = record.PolName,
                        Smo = record.Smo,
                        AttachMoCd = record.AttachMoCd,
                        AttachMoVers = record.AttachMoVers,
                        AttachDate = ParseDate(record.AttachDate),
                        SmoRegionCd = record.SmoRegionCd,
                        DispansType = record.DispansType,
                        DispansTypeName = record.DispansTypeName,
                        DispansStatus = record.DispansStatus,
                        Period = period,
                        DocumentId = documentId
                    };

                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка обработки записи ENP={record.ENP}: {ex.Message}");
                }
            }

            if (entities.Any())
            {
                await _unitOfWork.DfRecords.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Сохранено {entities.Count} записей DF");
            }

            var message = $"Импортировано {entities.Count} записей DF";
            if (errors.Any())
                message += $", ошибок: {errors.Count}";

            return (true, message, entities.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка импорта DF");
            errors.Add($"Критическая ошибка: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            var entities = await _unitOfWork.DfRecords.FindAsync(x => x.DocumentId == documentId);
            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var exportRecords = entities.Select(e => new DfExportRecord
            {
                MasterPersonId = e.MasterPersonId,
                ENP = e.ENP,
                BirthDate = e.BirthDate?.ToString("yyyy-MM-dd"),
                PolName = e.PolName,
                Smo = e.Smo,
                AttachMoCd = e.AttachMoCd,
                AttachMoVers = e.AttachMoVers,
                AttachDate = e.AttachDate?.ToString("yyyy-MM-dd"),
                SmoRegionCd = e.SmoRegionCd,
                DispansType = e.DispansType,
                DispansTypeName = e.DispansTypeName,
                DispansStatus = e.DispansStatus
            }).ToList();

            var header = new DfExportHeader
            {
                FileName = document?.FileName,
                RegionCode = document?.RegionCode,
                Period = document?.Period,
                RecordsCount = exportRecords.Count,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportDto = new DfExportDto { Header = header, Records = exportRecords };
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта DF");
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
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

public class GpmXmlService : IXmlService<GpmImportDto, GpmExportDto, GptEntity>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GpmXmlService> _logger;

    public GpmXmlService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GpmXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<GpmImportDto> ParseXmlAsync(string xmlContent)
    {
        try
        {
            var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            var utf8String = Encoding.UTF8.GetString(bytes);

            var serializer = new XmlSerializer(typeof(GpmImportDto));
            using var reader = new StringReader(utf8String);
            var result = (GpmImportDto?)serializer.Deserialize(reader);

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

    public Task<string> SerializeToXmlAsync(GpmExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GpmExportDto));
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
            _logger.LogError(ex, "Ошибка сериализации GPM XML");
            throw;
        }
    }

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;
            if (dto.Header == null || dto.Header.FileType != "GPM" || dto.Header.Version != "P3.20")
                return Task.FromResult(false);
            if (dto.Records == null || dto.Records.Count == 0 || dto.Records.Count != dto.Header.RecordsCount)
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

        try
        {
            var importData = await ParseXmlAsync(xmlContent);
            if (!await ValidateXmlAsync(xmlContent))
                return (false, "Ошибка валидации XML", 0, new List<string> { "Неверная структура XML" });

            var entities = new List<GptEntity>();

            foreach (var record in importData.Records ?? new List<GpmImportRecord>())
            {
                try
                {
                    var entity = _mapper.Map<GptEntity>(record);
                    entity.DocumentId = documentId;
                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка маппинга записи CODE_P={record.CodeP}: {ex.Message}");
                }
            }

            if (entities.Any())
            {
                await _unitOfWork.GptRecords.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();
            }

            return (true, $"Успешно импортировано {entities.Count} записей", entities.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта GPM");
            errors.Add($"Ошибка импорта: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            var entities = await _unitOfWork.GptRecords.FindAsync(x => x.DocumentId == documentId);
            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var records = _mapper.Map<List<GpmExportRecord>>(entities);
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var header = new GpmExportHeader
            {
                FileName = document?.FileName,
                RegionCode = document?.RegionCode,
                RecordsCount = records.Count,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportDto = new GpmExportDto { Header = header, Records = records };
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта GPM");
            throw;
        }
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
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

public class GpmXmlService : IXmlService<GpmImportDto, GpmExportDto, GptEntity>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GpmXmlService> _logger;

    public GpmXmlService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GpmXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<GpmImportDto> ParseXmlAsync(string xmlContent)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GpmImportDto));
            using var reader = new StringReader(xmlContent);
            var result = (GpmImportDto?)serializer.Deserialize(reader);

            if (result == null)
                throw new InvalidOperationException("Не удалось распарсить XML");

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга GPM XML");
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
                Encoding = System.Text.Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var writer = new StringWriter();
            using var xmlWriter = XmlWriter.Create(writer, settings);
            
            xmlWriter.WriteStartDocument();
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            
            serializer.Serialize(xmlWriter, exportData, ns);
            xmlWriter.Flush();

            return Task.FromResult(writer.ToString());
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

            if (dto.Header == null)
                return Task.FromResult(false);

            if (dto.Header.FileType != "GPM")
                return Task.FromResult(false);

            if (dto.Header.Version != "P3.20")
                return Task.FromResult(false);

            if (dto.Records == null || dto.Records.Count == 0)
                return Task.FromResult(false);

            if (dto.Records.Count != dto.Header.RecordsCount)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<(bool success, string message, int recordsCount, List<string> errors)> ImportAsync(
        string xmlContent,
        int documentId)
    {
        var errors = new List<string>();

        try
        {
            var importData = await ParseXmlAsync(xmlContent);

            if (!await ValidateXmlAsync(xmlContent))
            {
                return (false, "Ошибка валидации XML", 0, new List<string> { "Неверная структура XML" });
            }

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
            var entities = await _unitOfWork.GptRecords
                .FindAsync(x => x.DocumentId == documentId);

            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var records = _mapper.Map<List<GpmExportRecord>>(entities);

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                throw new InvalidOperationException($"Документ {documentId} не найден");

            var header = new GpmExportHeader
            {
                FileName = document.FileName,
                RegionCode = document.RegionCode,
                RecordsCount = records.Count
            };

            var exportDto = new GpmExportDto
            {
                Header = header,
                Records = records
            };

            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта GPM");
            throw;
        }
    }
}
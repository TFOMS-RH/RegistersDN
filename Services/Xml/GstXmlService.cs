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

public class GstXmlService : IXmlService<GstImportDto, GstExportDto, GstEntity>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GstXmlService> _logger;

    public GstXmlService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GstXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }


    /// Парсинг XML в DTO

    public Task<GstImportDto> ParseXmlAsync(string xmlContent)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GstImportDto));
            using var reader = new StringReader(xmlContent);
            var result = (GstImportDto?)serializer.Deserialize(reader);

            if (result == null)
                throw new InvalidOperationException("Не удалось распарсить XML");

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга GST XML");
            throw;
        }
    }


    /// Сериализация DTO в XML

    public Task<string> SerializeToXmlAsync(GstExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GstExportDto));
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
            _logger.LogError(ex, "Ошибка сериализации GST XML");
            throw;
        }
    }


    /// Валидация XML

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;

            // Проверяем заголовок
            if (dto.Header == null)
                return Task.FromResult(false);

            // Проверяем тип файла
            if (dto.Header.FileType != "GST")
                return Task.FromResult(false);

            // Проверяем версию
            if (dto.Header.Version != "P1.20")
                return Task.FromResult(false);

            // Проверяем наличие записей
            if (dto.Records == null || dto.Records.Count == 0)
                return Task.FromResult(false);

            // Проверяем количество записей
            if (dto.Records.Count != dto.Header.RecordsCount)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка валидации GST XML");
            return Task.FromResult(false);
        }
    }


    /// Импорт XML в БД

    public async Task<(bool success, string message, int recordsCount, List<string> errors)> ImportAsync(
        string xmlContent,
        int documentId)
    {
        var errors = new List<string>();

        try
        {
            // 1. Парсим XML
            var importData = await ParseXmlAsync(xmlContent);

            // 2. Валидируем
            if (!await ValidateXmlAsync(xmlContent))
            {
                return (false, "Ошибка валидации XML", 0, new List<string> { "Неверная структура XML" });
            }

            // 3. Маппим записи в Entity
            var entities = new List<GstEntity>();

            foreach (var record in importData.Records ?? new List<GstImportRecord>())
            {
                try
                {
                    var entity = _mapper.Map<GstEntity>(record);
                    entity.DocumentId = documentId;
                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка маппинга записи CODE_L={record.CodeL}: {ex.Message}");
                }
            }

            // 4. Сохраняем в БД
            if (entities.Any())
            {
                await _unitOfWork.GstRecords.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();
            }

            return (true, $"Успешно импортировано {entities.Count} записей", entities.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта GST");
            errors.Add($"Ошибка импорта: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }


    /// Экспорт данных в XML

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            // 1. Получаем данные из БД
            var entities = await _unitOfWork.GstRecords
                .FindAsync(x => x.DocumentId == documentId);

            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            // 2. Маппим в Export DTO
            var records = _mapper.Map<List<GstExportRecord>>(entities);

            // 3. Формируем заголовок
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                throw new InvalidOperationException($"Документ {documentId} не найден");

            var header = new GstExportHeader
            {
                FileName = document.FileName,
                RegionCode = document.RegionCode,
                RecordsCount = records.Count,
                FileNumber = document.FileNumber ?? 1,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // 4. Собираем полный DTO
            var exportDto = new GstExportDto
            {
                Header = header,
                Records = records
            };

            // 5. Сериализуем в XML
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта GST");
            throw;
        }
    }
}
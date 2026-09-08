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

    public Task<GstImportDto> ParseXmlAsync(string xmlContent)
    {
        try
        {
            var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            var utf8String = Encoding.UTF8.GetString(bytes);

            var serializer = new XmlSerializer(typeof(GstImportDto));
            using var reader = new StringReader(utf8String);
            var result = (GstImportDto?)serializer.Deserialize(reader);

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

    public Task<string> SerializeToXmlAsync(GstExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GstExportDto));

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
            _logger.LogError(ex, "Ошибка сериализации GST XML");
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

            if (dto.Header.FileType != "GST")
                return Task.FromResult(false);

            if (dto.Header.Version != "P1.20")
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

            var entities = new List<GstEntity>();

            foreach (var record in importData.Records ?? new List<GstImportRecord>())
            {
                try
                {
                    // Валидация полей
                    if (string.IsNullOrWhiteSpace(record.CodeL))
                    {
                        errors.Add($"Запись с ENP={record.ENP} пропущена: CODE_L пустой");
                        continue;
                    }
                    if (record.CodeL.Length > 36)
                    {
                        errors.Add($"Запись с CODE_L={record.CodeL} пропущена: CODE_L превышает 36 символов");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.ENP) || record.ENP.Length != 16 || !record.ENP.All(c => char.IsDigit(c)))
                    {
                        errors.Add($"Запись с CODE_L={record.CodeL} пропущена: ENP должен содержать 16 цифр");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.DiagCode) || !IsValidDiagnosisCode(record.DiagCode))
                    {
                        errors.Add($"Запись с CODE_L={record.CodeL} пропущена: неверный формат DIAG_CODE '{record.DiagCode}'");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.Mcod) || record.Mcod.Length != 6 || !record.Mcod.All(c => char.IsDigit(c)))
                    {
                        errors.Add($"Запись с CODE_L={record.CodeL} пропущена: MCOD должен содержать 6 цифр");
                        continue;
                    }

                    if (record.ReasonDnIn < 101 || record.ReasonDnIn > 107)
                    {
                        errors.Add($"Запись с CODE_L={record.CodeL} пропущена: REASON_DN_IN должен быть в диапазоне 101-107");
                        continue;
                    }

                    if (record.StatusDnIn.HasValue && (record.StatusDnIn.Value < 1 || record.StatusDnIn.Value > 2))
                    {
                        errors.Add($"Запись с CODE_L={record.CodeL} пропущена: STATUS_DN_IN должен быть 1 или 2");
                        continue;
                    }

                    var entity = _mapper.Map<GstEntity>(record);
                    entity.DocumentId = documentId;
                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка маппинга записи CODE_L={record.CodeL}: {ex.Message}");
                }
            }

            if (entities.Any())
            {
                await _unitOfWork.GstRecords.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();
            }

            var message = $"Импортировано {entities.Count} записей из {importData.Records?.Count ?? 0}";
            if (errors.Any())
            {
                message += $", пропущено {errors.Count} записей";
            }

            return (true, message, entities.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта GST");
            errors.Add($"Ошибка импорта: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            var entities = await _unitOfWork.GstRecords.FindAsync(x => x.DocumentId == documentId);

            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var validEntities = entities.Where(x => !string.IsNullOrWhiteSpace(x.CodeL)).ToList();

            if (!validEntities.Any())
                throw new InvalidOperationException("Нет валидных записей для экспорта");

            var records = _mapper.Map<List<GstExportRecord>>(validEntities);
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var header = new GstExportHeader
            {
                FileName = document?.FileName,
                RegionCode = document?.RegionCode,
                RecordsCount = records.Count,
                FileNumber = document?.FileNumber ?? 1,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportDto = new GstExportDto
            {
                Header = header,
                Records = records
            };

            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта GST");
            throw;
        }
    }

    private bool IsValidDiagnosisCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim();
        if (code.Length < 3 || code.Length > 6)
            return false;

        char first = code[0];
        if (first < 'A' || first > 'Z')
            return false;

        if (!char.IsDigit(code[1]) || !char.IsDigit(code[2]))
            return false;

        if (code.Length == 3)
            return true;

        if (code[3] != '.')
            return false;

        for (int i = 4; i < code.Length; i++)
        {
            if (!char.IsDigit(code[i]))
                return false;
        }

        return true;
    }

    private Encoding? GetEncodingFromXml(string xmlContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(xmlContent, @"encoding\s*=\s*[""']([^""']+)[""']");
        if (match.Success)
        {
            try
            {
                return Encoding.GetEncoding(match.Groups[1].Value);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }
}
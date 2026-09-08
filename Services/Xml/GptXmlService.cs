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

public class GptXmlService : IXmlService<GptImportDto, GptExportDto, GptEntity>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GptXmlService> _logger;

    public GptXmlService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GptXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<GptImportDto> ParseXmlAsync(string xmlContent)
    {
        try
        {
            var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            var utf8String = Encoding.UTF8.GetString(bytes);

            var serializer = new XmlSerializer(typeof(GptImportDto));
            using var reader = new StringReader(utf8String);
            var result = (GptImportDto?)serializer.Deserialize(reader);

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

    public Task<string> SerializeToXmlAsync(GptExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(GptExportDto));
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
            _logger.LogError(ex, "Ошибка сериализации GPT XML");
            throw;
        }
    }

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;
            if (dto.Header == null || dto.Header.FileType != "GPT" || dto.Header.Version != "P3.20")
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

            foreach (var record in importData.Records ?? new List<GptImportRecord>())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(record.CodeP) || record.CodeP.Length > 36)
                    {
                        errors.Add($"Запись с CODE_P={record.CodeP} пропущена: неверный CODE_P");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.ENP) || record.ENP.Length != 16 || !record.ENP.All(char.IsDigit))
                    {
                        errors.Add($"Запись с CODE_P={record.CodeP} пропущена: неверный ENP");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(record.McodPlan) || record.McodPlan.Length != 6 || !record.McodPlan.All(char.IsDigit))
                    {
                        errors.Add($"Запись с CODE_P={record.CodeP} пропущена: неверный MCOD_PLAN");
                        continue;
                    }

                    if (record.MoAssign < 0 || record.MoAssign > 1)
                    {
                        errors.Add($"Запись с CODE_P={record.CodeP} пропущена: MO_ASSIGN должен быть 0 или 1");
                        continue;
                    }

                    if (record.PrimaryInf < 1 || record.PrimaryInf > 2)
                    {
                        errors.Add($"Запись с CODE_P={record.CodeP} пропущена: PRIMARY_INF должен быть 1 или 2");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(record.DsCode) && !IsValidDiagnosisCode(record.DsCode))
                    {
                        errors.Add($"Запись с CODE_P={record.CodeP} пропущена: неверный DS_CODE '{record.DsCode}'");
                        continue;
                    }

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

            return (true, $"Импортировано {entities.Count} записей", entities.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта GPT");
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

            var validEntities = entities.Where(x => !string.IsNullOrWhiteSpace(x.CodeP)).ToList();
            if (!validEntities.Any())
                throw new InvalidOperationException("Нет валидных записей для экспорта");

            var records = _mapper.Map<List<GptExportRecord>>(validEntities);
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var header = new GptExportHeader
            {
                FileName = document?.FileName,
                RegionCode = document?.RegionCode,
                RecordsCount = records.Count,
                Data = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var exportDto = new GptExportDto { Header = header, Records = records };
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта GPT");
            throw;
        }
    }

    private bool IsValidDiagnosisCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();
        if (code.Length < 3 || code.Length > 6) return false;
        if (code[0] < 'A' || code[0] > 'Z') return false;
        if (!char.IsDigit(code[1]) || !char.IsDigit(code[2])) return false;
        if (code.Length == 3) return true;
        if (code[3] != '.') return false;
        for (int i = 4; i < code.Length; i++)
            if (!char.IsDigit(code[i])) return false;
        return true;
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
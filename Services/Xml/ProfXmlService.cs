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

public class ProfXmlService : IXmlServiceWithResponse<ProfImportDto, ProfExportDto, ProfEntity, ProfResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProfXmlService> _logger;

    public ProfXmlService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProfXmlService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<ProfImportDto> ParseXmlAsync(string xmlContent)
    {
       try
        {
            var bytes = Encoding.GetEncoding("windows-1251").GetBytes(xmlContent);
            var utf8String = Encoding.UTF8.GetString(bytes);

            var serializer = new XmlSerializer(typeof(ProfImportDto));
            using var reader = new StringReader(utf8String);
            var result = (ProfImportDto?)serializer.Deserialize(reader);

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

    public Task<string> SerializeToXmlAsync(ProfExportDto exportData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(ProfExportDto));
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
            _logger.LogError(ex, "Ошибка сериализации PROF XML");
            throw;
        }
    }

    public Task<string> SerializeResponseToXmlAsync(ProfResponseDto responseData)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(ProfResponseDto));
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
            _logger.LogError(ex, "Ошибка сериализации PROF ответного файла");
            throw;
        }
    }

    public Task<bool> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            var dto = ParseXmlAsync(xmlContent).Result;
            if (dto.Header == null) return Task.FromResult(false);
            if (dto.Header.Version != "1.0" && dto.Header.Version != "1.1") return Task.FromResult(false);
            if (dto.Records == null || dto.Records.Count == 0) return Task.FromResult(false);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<(bool isValid, List<string> errors)> ValidateXmlWithDetailsAsync(string xmlContent)
    {
        var errors = new List<string>();

        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
            var root = doc.Root;

            if (root == null || root.Name.LocalName != "ZL_LIST")
            {
                errors.Add("Корневой элемент должен быть <ZL_LIST>");
                return Task.FromResult((false, errors));
            }

            // Базовая валидация
            var zglv = root.Element("ZGLV");
            if (zglv == null)
            {
                errors.Add("Отсутствует обязательный элемент <ZGLV>");
                return Task.FromResult((false, errors));
            }

            var records = root.Elements("SV_PR_MER").ToList();
            if (!records.Any())
            {
                errors.Add("Нет ни одной записи <SV_PR_MER> в файле");
                return Task.FromResult((false, errors));
            }

            return Task.FromResult((true, errors));
        }
        catch (System.Xml.XmlException ex)
        {
            errors.Add($"Ошибка синтаксиса XML: {ex.Message}");
            return Task.FromResult((false, errors));
        }
        catch (Exception ex)
        {
            errors.Add($"Общая ошибка: {ex.Message}");
            return Task.FromResult((false, errors));
        }
    }

    public async Task<(bool success, string message, int recordsCount, List<string> errors)> ImportAsync(
    string xmlContent, int documentId)
    {
        var errors = new List<string>();
        var importedRecords = new List<ProfEntity>();

        try
        {
            _logger.LogInformation($"Начало импорта PROF для документа {documentId}");

            if (string.IsNullOrEmpty(xmlContent))
            {
                errors.Add("XML содержимое пустое");
                return (false, "XML содержимое пустое", 0, errors);
            }

            var importData = await ParseXmlAsync(xmlContent);
            if (importData == null)
            {
                errors.Add("Не удалось распарсить XML");
                return (false, "Ошибка парсинга XML", 0, errors);
            }

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
            {
                errors.Add($"Документ {documentId} не найден");
                return (false, "Документ не найден", 0, errors);
            }

            var period = DateTime.Now.ToString("yyyyMM");
            var records = importData.Records ?? new List<ProfImportRecord>();

            foreach (var record in records)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(record.Npolis))
                    {
                        errors.Add($"Запись NOMER_Z={record.NomerZ}: отсутствует NPOLIS");
                        continue;
                    }

                    var drParsed = ParseDateFlexible(record.Dr);
                    if (!drParsed.HasValue)
                    {
                        errors.Add($"Запись NOMER_Z={record.NomerZ}: неверный формат даты рождения");
                        continue;
                    }

                    var entity = new ProfEntity
                    {
                        NomerZ = record.NomerZ,
                        PersonId = record.PersonId,
                        SmoCod = record.SmoCod ?? string.Empty,
                        Enp = record.Enp,
                        Fam = record.Fam ?? string.Empty,
                        Im = record.Im ?? string.Empty,
                        Ot = record.Ot,
                        Dr = drParsed.Value,
                        W = record.W ?? 0,
                        DocType = record.DocType,
                        DocSer = record.DocSer,
                        DocNum = record.DocNum,
                        Snils = ParseSnils(record.Snils),
                        Vpolis = record.Vpolis ?? 0,
                        Spolis = record.Spolis,
                        Npolis = record.Npolis ?? string.Empty,
                        Tel = record.Tel,
                        Iddokt = ParseSnils(record.Iddokt) ?? string.Empty,
                        Adres = record.Adres,
                        KatLg = record.KatLg,
                        Year = record.Year ?? DateTime.Now.Year,
                        Comment = record.Comment,
                        Period = period,
                        DocumentId = documentId,
                        IsProcessed = false
                    };

                    // ⚠️ ВАЖНО: Сбрасываем Id перед сохранением
                    entity.Id = 0;
                    importedRecords.Add(entity);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка обработки записи NOMER_Z={record.NomerZ}: {ex.Message}");
                }
            }

            // ==========================================
            // ✅ СОХРАНЯЕМ РОДИТЕЛЬСКИЕ ЗАПИСИ И ПОЛУЧАЕМ ID
            // ==========================================
            if (importedRecords.Any())
            {
                // Добавляем записи по одной для получения Id
                foreach (var entity in importedRecords)
                {
                    await _unitOfWork.ProfRecords.AddAsync(entity);
                    await _unitOfWork.SaveChangesAsync(); // ⚡ Сохраняем каждую запись отдельно
                }

                _logger.LogInformation($"Сохранено {importedRecords.Count} записей PROF");

                // ==========================================
                // ✅ СОХРАНЯЕМ МЕРОПРИЯТИЯ (SV_PL_MER) С ПРАВИЛЬНЫМИ PROF_RECORD_ID
                // ==========================================
                var merRecords = new List<ProfMerEntity>();

                // Проходим по исходным данным и связываем мероприятия с сохранёнными записями
                int recordIndex = 0;
                foreach (var record in records)
                {
                    if (record.MerRecords?.Any() == true && recordIndex < importedRecords.Count)
                    {
                        var parentId = importedRecords[recordIndex].Id;
                        foreach (var mer in record.MerRecords)
                        {
                            merRecords.Add(new ProfMerEntity
                            {
                                ProfRecordId = parentId,
                                Month = mer.Month ?? 0,
                                Disp = mer.Disp ?? "ДВ4",
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    recordIndex++;
                }

                if (merRecords.Any())
                {
                    await _unitOfWork.ProfMerRecords.AddRangeAsync(merRecords);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation($"Сохранено {merRecords.Count} мероприятий");
                }
            }

            // Генерация ответного файла
            if (records.Any())
            {
                try
                {
                    var response = await GenerateResponseAsync(documentId, errors);
                    var responseXml = await SerializeResponseToXmlAsync(response);

                    var responseEntity = new ImportResponseEntity
                    {
                        SourceFileType = "PROF",
                        SourceDocumentId = documentId,
                        ResponseXml = responseXml,
                        ResponseFileName = $"PROF_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}",
                        RecordsTotal = records.Count,
                        RecordsApproved = importedRecords.Count,
                        RecordsRejected = records.Count - importedRecords.Count
                    };

                    await _unitOfWork.ImportResponses.AddAsync(responseEntity);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка генерации ответного файла");
                }
            }

            return (true, $"Импортировано {importedRecords.Count} записей", importedRecords.Count, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка импорта PROF");
            errors.Add($"Критическая ошибка: {ex.Message}");
            return (false, "Ошибка импорта", 0, errors);
        }
    }

    public async Task<ProfResponseDto> GenerateResponseAsync(int documentId, List<string> errors)
    {
        try
        {
            var records = await _unitOfWork.ProfRecords.FindAsync(x => x.DocumentId == documentId);
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

            var responseRecords = records.Select(r => new ProfResponseRecord
            {
                NZap = r.NomerZ,
                Enp = r.Npolis,
                Rpl = string.IsNullOrEmpty(r.Npolis) ? 0 : 1,
                Commentr = string.IsNullOrEmpty(r.Npolis) ? "Отсутствует ЕНП" : "Успешно"
            }).ToList();

            return new ProfResponseDto
            {
                Header = new ProfResponseHeader
                {
                    FileName = $"PROF_RESPONSE_{DateTime.Now:yyyyMMdd_HHmmss}",
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
            _logger.LogError(ex, "Ошибка генерации ответа PROF");
            throw;
        }
    }

    public async Task<string> ExportAsync(int documentId)
    {
        try
        {
            var entities = await _unitOfWork.ProfRecords.FindAsync(x => x.DocumentId == documentId);
            if (!entities.Any())
                throw new InvalidOperationException($"Нет данных для документа {documentId}");

            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            var exportRecords = new List<ProfExportRecord>();

            foreach (var entity in entities)
            {
                var merRecords = await _unitOfWork.ProfMerRecords.FindAsync(x => x.ProfRecordId == entity.Id);

                exportRecords.Add(new ProfExportRecord
                {
                    NomerZ = entity.NomerZ,
                    PersonId = entity.PersonId,
                    SmoCod = entity.SmoCod,
                    Enp = entity.Enp,
                    Fam = entity.Fam,
                    Im = entity.Im,
                    Ot = entity.Ot,
                    Dr = entity.Dr.ToString("yyyyMMdd"),
                    W = entity.W ?? 0,
                    DocType = entity.DocType,
                    DocSer = entity.DocSer,
                    DocNum = entity.DocNum,
                    Snils = entity.Snils,
                    Vpolis = entity.Vpolis ?? 0,
                    Spolis = entity.Spolis,
                    Npolis = entity.Npolis,
                    Tel = entity.Tel,
                    Iddokt = entity.Iddokt,
                    Adres = entity.Adres,
                    KatLg = entity.KatLg,
                    Year = entity.Year ?? DateTime.Now.Year,
                    Comment = entity.Comment,
                    MerRecords = merRecords.Select(m => new ProfExportMer
                    {
                        Month = m.Month,
                        Disp = m.Disp
                    }).ToList()
                });
            }

            var header = new ProfExportHeader
            {
                FileName = document?.FileName,
                CodMo = document?.HospitalCode ?? "000000",
                Data = DateTime.Now.ToString("yyyyMMdd")
            };

            var exportDto = new ProfExportDto { Header = header, Records = exportRecords };
            return await SerializeToXmlAsync(exportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта PROF");
            throw;
        }
    }

    private DateTime? ParseDateFlexible(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;
        if (DateTime.TryParse(dateString, out var date1)) return date1;
        if (dateString.Length == 8 && DateTime.TryParseExact(dateString, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date2)) return date2;
        return null;
    }

    private string? ParseSnils(string? snils)
    {
        if (string.IsNullOrWhiteSpace(snils)) return null;
        var cleaned = snils.Replace("-", "").Replace(" ", "").Replace("_", "");
        if (System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\d{11}$"))
            return cleaned;
        return snils;
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
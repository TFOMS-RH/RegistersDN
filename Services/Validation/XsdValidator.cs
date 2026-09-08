using System.Xml;
using System.Xml.Schema;
using System.Text;

namespace RegistrDN.Services.Validation;

/// <summary>
/// Сервис для валидации XML-файлов по XSD-схемам
/// </summary>
public class XsdValidator
{
    private readonly ILogger<XsdValidator> _logger;
    private readonly Dictionary<string, XmlSchemaSet> _schemas = new();
    private readonly string _schemasPath;

    public XsdValidator(ILogger<XsdValidator> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _schemasPath = Path.Combine(env.ContentRootPath, "Schemas");
        LoadAllSchemas();
    }

    /// <summary>
    /// Загрузка всех XSD-схем из папки Schemas
    /// </summary>
    private void LoadAllSchemas()
    {
        if (!Directory.Exists(_schemasPath))
        {
            _logger.LogWarning($"Папка со схемами не найдена: {_schemasPath}");
            return;
        }

        var xsdFiles = Directory.GetFiles(_schemasPath, "*.xsd");
        foreach (var xsdFile in xsdFiles)
        {
            try
            {
                var schemaName = Path.GetFileNameWithoutExtension(xsdFile);
                var schemaSet = new XmlSchemaSet();
                
                using var stream = File.OpenRead(xsdFile);
                var schema = XmlSchema.Read(stream, (sender, args) =>
                {
                    if (args.Severity == XmlSeverityType.Error)
                    {
                        _logger.LogError($"Ошибка загрузки схемы {schemaName}: {args.Message}");
                    }
                });
                
                if (schema != null)
                {
                    schemaSet.Add(schema);
                    schemaSet.Compile();
                    _schemas[schemaName] = schemaSet;
                    _logger.LogInformation($"Схема {schemaName}.xsd загружена успешно");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка загрузки схемы {xsdFile}");
            }
        }
    }

    /// <summary>
    /// Валидация XML по XSD-схеме
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate(string xmlContent, string schemaName)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(xmlContent))
        {
            errors.Add("XML-содержимое пустое");
            return (false, errors);
        }

        if (!_schemas.TryGetValue(schemaName, out var schemaSet))
        {
            errors.Add($"Схема {schemaName} не найдена. Доступные схемы: {string.Join(", ", _schemas.Keys)}");
            return (false, errors);
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                                   XmlSchemaValidationFlags.ProcessIdentityConstraints |
                                   XmlSchemaValidationFlags.ProcessInlineSchema,
                DtdProcessing = DtdProcessing.Ignore,
                CheckCharacters = true,
                CloseInput = true
            };

            settings.ValidationEventHandler += (sender, args) =>
            {
                var message = $"[{args.Severity}] {args.Message}";
                if (args.Severity == XmlSeverityType.Error)
                {
                    errors.Add(message);
                }
                else
                {
                    errors.Add($"⚠️ {message}");
                }
            };

            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (reader.Read()) { }
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, $"Ошибка парсинга XML при валидации по схеме {schemaName}");
            errors.Add($"Ошибка синтаксиса XML: {ex.Message} (строка {ex.LineNumber}, позиция {ex.LinePosition})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Неизвестная ошибка при валидации XML по схеме {schemaName}");
            errors.Add($"Ошибка валидации: {ex.Message}");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Определение типа файла по содержимому
    /// </summary>
    public string? DetectSchemaByContent(string xmlContent)
    {
        if (string.IsNullOrEmpty(xmlContent))
            return null;

        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
            var root = doc.Root;
            if (root == null) return null;

            return root.Name.LocalName switch
            {
                "ZL_LIST" => DetectZlListType(doc),
                "DN_LIST" => "GST",
                "DN_PLAN" => "GPT",
                "DN" => "GF",
                "DISP" => "DF",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private string? DetectZlListType(System.Xml.Linq.XDocument doc)
    {
        var root = doc.Root;
        if (root == null) return null;

        // Проверяем наличие SV_PR_MER (PROF) или ZAP с PACIENT (DSPN)
        var svPrMer = root.Element("SV_PR_MER");
        if (svPrMer != null)
            return "PROF";

        var zap = root.Element("ZAP");
        if (zap != null)
        {
            var pacient = zap.Element("PACIENT");
            if (pacient != null)
                return "DSPN";
        }

        return null;
    }

    /// <summary>
    /// Получение списка доступных схем
    /// </summary>
    public List<string> GetAvailableSchemas() => _schemas.Keys.ToList();
}
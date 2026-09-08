using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RegistrDN.Models.Enums;

namespace RegistrDN.Models.Entities;

[Table("DN_DOCUMENTS")]
public class DnDocumentEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string FileType { get; set; } = string.Empty;
    
    // ❌ XmlContent УДАЛЕН
    
    [MaxLength(10)]
    public string? RegionCode { get; set; }
    
    [MaxLength(10)]
    public string? HospitalCode { get; set; }
    
    public DateTime? FileDate { get; set; }
    public int? RecordsCount { get; set; }
    
    [MaxLength(50)]
    public string? Version { get; set; }
    
    public int? FileNumber { get; set; }
    public int? ValidatedEnpCount { get; set; }
    
    [MaxLength(10)]
    public string? Period { get; set; }
    
    // Старые поля (для совместимости)
    public bool IsValid { get; set; }
    public string? ValidationErrors { get; set; }
    
    // ==========================================
    // НОВЫЕ ПОЛЯ ДЛЯ УПРАВЛЕНИЯ СТАТУСОМ
    // ==========================================
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public DateTime? CheckedAt { get; set; }
    [MaxLength(50)]
    public string? CheckedBy { get; set; }
    public int? CheckAttempts { get; set; } = 0;
    
    public DateTime UploadDate { get; set; } = DateTime.Now;
    
    [MaxLength(50)]
    public string? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Навигационные свойства
    public virtual ICollection<GstEntity>? GstRecords { get; set; }
    public virtual ICollection<GptEntity>? GptRecords { get; set; }
    public virtual ICollection<GfEntity>? GfRecords { get; set; }
}
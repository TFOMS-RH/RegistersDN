using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("GF_RECORDS")]
public class GfEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string? DnPatientId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string ENP { get; set; } = string.Empty;
    
    public int? Gender { get; set; }
    
    public DateTime? BirthDate { get; set; }
    
    [MaxLength(10)]
    public string? Smo { get; set; }
    
    [MaxLength(10)]
    public string? AttachMcode { get; set; }
    
    public DateTime? AttachDate { get; set; }
    
    [MaxLength(10)]
    public string? SmoRegionCode { get; set; }
    
    public int? GroupRhCode { get; set; }
    
    [MaxLength(10)]
    public string? GroupRhDs { get; set; }
    
    public int? DnPrvs { get; set; }
    
    [MaxLength(255)]
    public string? GroupRhProfile { get; set; }
    
    [MaxLength(255)]
    public string? GroupRhName { get; set; }
    
    [MaxLength(255)]
    public string? DnRuleInName { get; set; }
    
    // Результаты DN_LIST
    public int? DnListPeriodCode { get; set; }
    
    [MaxLength(50)]
    public string? DnListFilename { get; set; }
    
    [MaxLength(50)]
    public string? CodeL { get; set; }
    
    [MaxLength(10)]
    public string? DnListResultCode { get; set; }
    
    public DateTime? DnListDateChecking { get; set; }
    
    public string? DnListResultDescr { get; set; }
    
    // Результаты DN_PLAN
    [MaxLength(10)]
    public string? DnPlanPeriod { get; set; }
    
    [MaxLength(50)]
    public string? DnPlanFilename { get; set; }
    
    [MaxLength(50)]
    public string? CodeP { get; set; }
    
    public int? DnPlanResultCode { get; set; }
    
    public DateTime? DnPlanDateChecking { get; set; }
    
    public string? DnPlanResultDescr { get; set; }
    
    // Данные ГИС ОМС
    [MaxLength(50)]
    public string? TriggerSchetnFilename { get; set; }
    
    [MaxLength(10)]
    public string? TriggerSchetnCode { get; set; }
    
    [MaxLength(20)]
    public string? TriggerNschet { get; set; }
    
    public DateTime? TriggerDschet { get; set; }
    
    [MaxLength(50)]
    public string? TriggerIdCase { get; set; }
    
    [MaxLength(50)]
    public string? TriggerSlId { get; set; }
    
    [MaxLength(50)]
    public string? TriggerSlNhistory { get; set; }
    
    [MaxLength(10)]
    public string? TriggerDsCd { get; set; }
    
    [MaxLength(10)]
    public string? TriggerMcode { get; set; }
    
    public DateTime? TriggerDt { get; set; }
    
    public DateTime InsertDttm { get; set; }
    
    public DateTime UpdateDttm { get; set; }
    
    public int DocumentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [ForeignKey(nameof(DocumentId))]
    public virtual DnDocumentEntity? Document { get; set; }
}
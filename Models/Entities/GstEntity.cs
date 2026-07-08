using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("GST_RECORDS")]
public class GstEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CodeL { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string ENP { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string CodePinf { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? DnPatientId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string DiagCode { get; set; } = string.Empty;
    
    public DateTime? DiagDate { get; set; }
    
    public DateTime? DateDnIn { get; set; }
    
    public DateTime? DateDnOut { get; set; }
    
    public int? DnPrvs { get; set; }
    
    public int? LastSlMcod { get; set; }
    
    [MaxLength(50)]
    public string? LastSlNhistory { get; set; }
    
    public DateTime? LastSlDate { get; set; }
    
    public int? StatusDnIn { get; set; }
    
    [MaxLength(50)]
    public string? ReasonDnOut { get; set; }
    
    public int ReasonDnIn { get; set; }
    
    [MaxLength(10)]
    public string? Mcod { get; set; }
    
    public int? MoAssign { get; set; }
    
    public DateTime DateChecking { get; set; }
    
    public int DocumentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; }
    
    [ForeignKey(nameof(DocumentId))]
    public virtual DnDocumentEntity? Document { get; set; }
}
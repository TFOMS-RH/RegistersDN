using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("GPT_RECORDS")]
public class GptEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CodeP { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? DnPatientId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string ENP { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string CodePinf { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string McodPlan { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? MoPodrId { get; set; }
    
    [MaxLength(50)]
    public string? MedAreaCode { get; set; }
    
    public int MoAssign { get; set; }
    
    public DateTime EndDateInf { get; set; }
    
    public int PrimaryInf { get; set; }
    
    [MaxLength(10)]
    public string? DsCode { get; set; }
    
    public DateTime PlanDateStart { get; set; }
    
    public DateTime PlanDateEnd { get; set; }
    
    public int DocumentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; }
    
    [ForeignKey(nameof(DocumentId))]
    public virtual DnDocumentEntity? Document { get; set; }
}
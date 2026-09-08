using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("DSPN_RECORDS")]
public class DspnEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Данные из ZAP
    public long N_Zap { get; set; }
    public string? PacientId { get; set; }
    public string? Smo { get; set; }
    public string? Spolis { get; set; }
    public int? Vpolis { get; set; }
    public string Npolis { get; set; } = string.Empty;
    public string Fam { get; set; } = string.Empty;
    public string Im { get; set; } = string.Empty;
    public string? Ot { get; set; }
    public DateTime Dr { get; set; }
    public int W { get; set; }
    public string? Adres { get; set; }
    public string? Tel { get; set; }
    public string MoP { get; set; } = string.Empty;
    public string DiagCode { get; set; } = string.Empty;
    public string Iddokt { get; set; } = string.Empty;
    public DateTime DateDnIn { get; set; }
    public DateTime? DateDnOut { get; set; }
    public DateTime DiagDate { get; set; }
    public int DnPrvs { get; set; }
    public int StatusDnIn { get; set; }
    public string? ReasonDnOut { get; set; }
    public int ReasonDnIn { get; set; }

    // Данные из PLAN
    public string McodPlan { get; set; } = string.Empty;
    public string? MoPodrId { get; set; }
    public string? MedAreaCode { get; set; }
    public int MoAssign { get; set; }
    public string? DsCode { get; set; }
    public DateTime PlanDateStart { get; set; }
    public DateTime PlanDateEnd { get; set; }

    // Данные из INF
    public int? InfType { get; set; }
    public int? SposobInf { get; set; }
    public DateTime? DataInf { get; set; }

    // Служебные
    public string Period { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual DnDocumentEntity? Document { get; set; }
}
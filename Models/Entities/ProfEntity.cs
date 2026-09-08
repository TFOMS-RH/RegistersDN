using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("PROF_RECORDS")]
public class ProfEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Данные из SV_PR_MER
    public long NomerZ { get; set; }
    public string? PersonId { get; set; }
    public string SmoCod { get; set; } = string.Empty;
    public string? Enp { get; set; }
    public string Fam { get; set; } = string.Empty;
    public string Im { get; set; } = string.Empty;
    public string? Ot { get; set; }
    public DateTime Dr { get; set; }
    public int? W { get; set; }
    public string? DocType { get; set; }
    public string? DocSer { get; set; }
    public string? DocNum { get; set; }
    public string? Snils { get; set; }
    public int? Vpolis { get; set; }
    public string? Spolis { get; set; }
    public string? Npolis { get; set; } = string.Empty;
    public string? Tel { get; set; }
    public string Iddokt { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public int? KatLg { get; set; }
    public int? Year { get; set; }
    public string? Comment { get; set; }

    // Служебные
    public string Period { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual DnDocumentEntity? Document { get; set; }

    public virtual ICollection<ProfMerEntity>? ProfMerRecords { get; set; }
}
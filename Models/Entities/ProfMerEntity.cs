using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("PROF_MER_RECORDS")]
public class ProfMerEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProfRecordId { get; set; }
    public int Month { get; set; }
    public string Disp { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(ProfRecordId))]
    public virtual ProfEntity? ProfRecord { get; set; }
}
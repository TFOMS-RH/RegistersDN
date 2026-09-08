using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("IMPORT_RESPONSES")]
public class ImportResponseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string SourceFileType { get; set; } = string.Empty; // DSPN или PROF
    public int SourceDocumentId { get; set; }
    public string ResponseXml { get; set; } = string.Empty;
    public string ResponseFileName { get; set; } = string.Empty;

    public int RecordsTotal { get; set; }
    public int RecordsApproved { get; set; }
    public int RecordsRejected { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? DownloadedAt { get; set; }

    [ForeignKey(nameof(SourceDocumentId))]
    public virtual DnDocumentEntity? SourceDocument { get; set; }
}
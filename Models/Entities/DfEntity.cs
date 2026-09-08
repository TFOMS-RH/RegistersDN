using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrDN.Models.Entities;

[Table("DF_RECORDS")]
public class DfEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Основные сведения о ЗЛ
    [MaxLength(50)]
    public string? MasterPersonId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ENP { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    [MaxLength(10)]
    public string? PolName { get; set; }

    [MaxLength(5)]
    public string? Smo { get; set; }

    [MaxLength(10)]
    public string? AttachMoCd { get; set; }

    [MaxLength(10)]
    public string? AttachMoVers { get; set; }

    public DateTime? AttachDate { get; set; }

    [MaxLength(10)]
    public string? SmoRegionCd { get; set; }

    [MaxLength(10)]
    public string? DispansType { get; set; }

    [MaxLength(100)]
    public string? DispansTypeName { get; set; }

    [MaxLength(10)]
    public string? DispansStatus { get; set; }

    // Служебные
    public string? Period { get; set; }
    public int DocumentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(DocumentId))]
    public virtual DnDocumentEntity? Document { get; set; }
}
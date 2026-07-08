using Microsoft.AspNetCore.Mvc.Rendering;

namespace RegistrDN.Models.ViewModels;

public class ImportViewModel
{
    public List<SelectListItem> Periods { get; set; } = new();
    public string? SelectedPeriod { get; set; }
}
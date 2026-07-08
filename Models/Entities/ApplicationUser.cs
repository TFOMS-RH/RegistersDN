using Microsoft.AspNetCore.Identity;

namespace RegistrDN.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? HospitalCode { get; set; }
    public string? RegionCode { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;  
}
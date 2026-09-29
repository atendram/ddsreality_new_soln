using System.ComponentModel.DataAnnotations;
namespace DdsCms.Models;

public static class Catalog
{
    public static readonly string[] Sites = ["realty", "dds"];
    public static readonly string[] Kinds = ["Project", "Banner", "Blog", "Team", "Testimonial", "Reel", "Service"];
    public static string Root(string site) => site == "dds" ? "/dds" : "";
    public static string Label(string site) => site == "dds" ? "DDS Digital" : "DDS Realty";
    public static string[] Lines(string? s) => (s ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
public class SiteSettings
{
    [Key, MaxLength(20)] public string SiteId { get; set; } = "realty";
    [Required, MaxLength(100)] public string Name { get; set; } = "DDS Realty";
    [MaxLength(300)] public string Tagline { get; set; } = "Find a place that feels like you.";
    [MaxLength(500)] public string LogoPath { get; set; } = "";
    [MaxLength(100)] public string AboutTitle { get; set; } = "Local knowledge. Personal guidance.";
    [MaxLength(15000)] public string AboutBody { get; set; } = "";
    [MaxLength(500)] public string AboutImage { get; set; } = "";
    [MaxLength(15000)] public string PrivacyText { get; set; } = "";
    [MaxLength(1000)] public string Address { get; set; } = "";
    [MaxLength(30), RegularExpression(@"^[+0-9 ()-]*$")] public string Phone { get; set; } = "";
    [MaxLength(120), EmailAddress] public string? Email { get; set; }
    [MaxLength(20), RegularExpression(@"^[0-9]*$")] public string WhatsApp { get; set; } = "";
    [MaxLength(500)] public string Facebook { get; set; } = "";
    [MaxLength(500)] public string Instagram { get; set; } = "";
    [MaxLength(500)] public string YouTube { get; set; } = "";
    [MaxLength(300)] public string FooterText { get; set; } = "Property guidance with a personal touch.";
    [MaxLength(160)] public string MetaDescription { get; set; } = "Explore properties and talk to the DDS Realty team.";
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string PrimaryColor { get; set; } = "#102C40";
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string AccentColor { get; set; } = "#E9B949";
    public bool ShowTeam { get; set; } = true;
    public bool ShowTestimonials { get; set; } = true;
    public bool ShowReels { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;
    [ConcurrencyCheck] public long Version { get; set; } = 1;
}
public class ContentEntry
{
    public int Id { get; set; }
    [MaxLength(20)] public string SiteId { get; set; } = "realty";
    [MaxLength(20)] public string Kind { get; set; } = "Project";
    [Required, MaxLength(160)] public string Title { get; set; } = "";
    [Required, MaxLength(160), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string Slug { get; set; } = "";
    [MaxLength(300)] public string Subtitle { get; set; } = "";
    [MaxLength(30000)] public string Body { get; set; } = "";
    [MaxLength(500)] public string ImagePath { get; set; } = "";
    [MaxLength(200)] public string AltText { get; set; } = "";
    [MaxLength(500)] public string LinkUrl { get; set; } = "";
    [MaxLength(50)] public string ButtonText { get; set; } = "Explore projects";
    [MaxLength(100)] public string Price { get; set; } = "";
    [MaxLength(300)] public string Location { get; set; } = "";
    [MaxLength(100)] public string Configuration { get; set; } = "";
    [MaxLength(100)] public string PropertyType { get; set; } = "Residential";
    [MaxLength(100)] public string ReraNumber { get; set; } = "";
    [MaxLength(100)] public string Possession { get; set; } = "";
    [MaxLength(5000)] public string MapUrl { get; set; } = "";
    [MaxLength(8000)] public string Amenities { get; set; } = "";
    [MaxLength(8000)] public string Highlights { get; set; } = "";
    [MaxLength(160)] public string MetaDescription { get; set; } = "";
    public bool Published { get; set; }
    public bool Featured { get; set; }
    [Range(0,99999)] public int SortOrder { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    [ConcurrencyCheck] public long Version { get; set; } = 1;
    public List<ProjectMedia> Media { get; set; } = [];
}
public class ProjectMedia
{
    public int Id { get; set; }
    public int ContentEntryId { get; set; }
    public ContentEntry? Entry { get; set; }
    [MaxLength(20)] public string Kind { get; set; } = "Gallery";
    [Required, MaxLength(160)] public string Caption { get; set; } = "";
    [Required, MaxLength(500)] public string ImagePath { get; set; } = "";
    [Range(0,99999)] public int SortOrder { get; set; }
}
public class Enquiry
{
    public int Id { get; set; }
    [MaxLength(20)] public string SiteId { get; set; } = "realty";
    public int? ProjectId { get; set; }
    [MaxLength(160)] public string ProjectTitle { get; set; } = "";
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(30), RegularExpression(@"^[+0-9 ()-]{7,30}$")] public string Phone { get; set; } = "";
    [EmailAddress, MaxLength(120)] public string? Email { get; set; }
    [Required, MaxLength(2000)] public string Message { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string Status { get; set; } = "New";
    [MaxLength(3000)] public string Notes { get; set; } = "";
    [ConcurrencyCheck] public long Version { get; set; } = 1;
}
public class AdminUser
{
    public int Id { get; set; }
    [MaxLength(120)] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public int FailedAttempts { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public bool Active { get; set; } = true;
    [ConcurrencyCheck] public long Version { get; set; } = 1;
}
public class AuditEvent
{
    public long Id { get; set; }
    [MaxLength(120)] public string Actor { get; set; } = "";
    [MaxLength(500)] public string Action { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

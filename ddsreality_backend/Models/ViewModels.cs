using System.ComponentModel.DataAnnotations;
namespace DdsCms.Models;
public class LoginModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class PasswordModel
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(12), MaxLength(128), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Compare("NewPassword"), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
public class PublicPage
{
    public required SiteSettings Settings { get; set; }
    public List<ContentEntry> Items { get; set; } = [];
    public ContentEntry? Entry { get; set; }
    public string Title { get; set; } = "";
    public string Query { get; set; } = "";
    public int Page { get; set; } = 1;
    public int Total { get; set; }
    public EnquiryInput Contact { get; set; } = new();
    public string Root => Catalog.Root(Settings.SiteId);
}
public class EnquiryInput
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(30), RegularExpression(@"^[+0-9 ()-]{7,30}$")] public string Phone { get; set; } = "";
    [EmailAddress, MaxLength(120)] public string? Email { get; set; }
    [Required, MaxLength(2000)] public string Message { get; set; } = "";
    public bool Consent { get; set; }
    public string? Website { get; set; }
    public int? ProjectId { get; set; }
}
public class AdminList
{
    public string Site { get; set; } = "realty";
    public string Kind { get; set; } = "Project";
    public string Query { get; set; } = "";
    public int Page { get; set; } = 1;
    public int Total { get; set; }
    public List<ContentEntry> Items { get; set; } = [];
}
public class Dashboard
{
    public int Projects { get; set; }
    public int Published { get; set; }
    public int Drafts { get; set; }
    public int NewEnquiries { get; set; }
    public List<ContentEntry> Recent { get; set; } = [];
}
public class MediaInput
{
    public int EntryId { get; set; }
    public long Version { get; set; }
    [Required, MaxLength(160)] public string Caption { get; set; } = "";
    [Required] public string Kind { get; set; } = "Gallery";
    [Range(0,99999)] public int SortOrder { get; set; }
}

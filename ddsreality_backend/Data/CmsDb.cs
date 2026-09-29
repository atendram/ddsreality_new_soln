using DdsCms.Models;
using Microsoft.EntityFrameworkCore;
namespace DdsCms.Data;
public class CmsDb(DbContextOptions<CmsDb> options) : DbContext(options)
{
    public DbSet<SiteSettings> Sites => Set<SiteSettings>();
    public DbSet<ContentEntry> Entries => Set<ContentEntry>();
    public DbSet<ProjectMedia> Media => Set<ProjectMedia>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("dds");
        b.Entity<ContentEntry>().HasIndex(x => new { x.SiteId, x.Kind, x.Slug }).IsUnique();
        b.Entity<ContentEntry>().HasIndex(x => new { x.SiteId, x.Kind, x.Published, x.SortOrder });
        b.Entity<ContentEntry>().HasOne<SiteSettings>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProjectMedia>().HasOne(x => x.Entry).WithMany(x => x.Media).HasForeignKey(x => x.ContentEntryId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Enquiry>().HasIndex(x => new { x.SiteId, x.CreatedUtc });
        b.Entity<AdminUser>().HasIndex(x => x.Email).IsUnique();
    }
}

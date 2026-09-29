using DdsCms.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace DdsCms.Data;
public static class SeedData
{
    public static async Task Run(CmsDb db,string root)
    {
        var options=new JsonSerializerOptions { PropertyNameCaseInsensitive=true };
        var payload=JsonSerializer.Deserialize<SeedPayload>(await File.ReadAllTextAsync(Path.Combine(root,"Database","seed-content.json")),options)!;
        await using var transaction=await db.Database.BeginTransactionAsync();
        foreach(var site in payload.Sites) if(!await db.Sites.AnyAsync(x=>x.SiteId==site.SiteId))db.Sites.Add(site);
        await db.SaveChangesAsync();
        foreach(var item in payload.Entries)if(!await db.Entries.AnyAsync(x=>x.SiteId==item.SiteId && x.Kind==item.Kind && x.Slug==item.Slug))db.Entries.Add(item);
        await db.SaveChangesAsync();await transaction.CommitAsync();Console.WriteLine("Initial content added. Existing records were not overwritten. Review project drafts before publishing.");
    }
    public class SeedPayload { public List<SiteSettings> Sites {get;set;}=[];public List<ContentEntry> Entries {get;set;}=[]; }
}

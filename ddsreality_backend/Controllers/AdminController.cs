using DdsCms.Data;
using DdsCms.Models;
using DdsCms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DdsCms.Controllers;
[Authorize(Roles="Admin"), Route("admin")]
public class AdminController(CmsDb db, MediaStore media) : Controller
{
    private static void Normalize(object value) {
        foreach (var property in value.GetType().GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite && p.Name != "Email"))
            if (property.GetValue(value) == null) property.SetValue(value, "");
    }
    private void Context(string site) { ViewBag.Site = site; }
    private void Audit(string action) => db.AuditEvents.Add(new AuditEvent { Actor = User.Identity?.Name ?? "Admin", Action = action });
    private static bool Valid(string site, string? kind = null) => Catalog.Sites.Contains(site) && (kind == null || Catalog.Kinds.Contains(kind));
    [HttpGet("")]
    public async Task<IActionResult> Index(string site = "realty") {
        if (!Valid(site)) return NotFound(); Context(site);
        var q = db.Entries.Where(x => x.SiteId == site);
        return View(new Dashboard { Projects = await q.CountAsync(x => x.Kind == "Project"), Published = await q.CountAsync(x => x.Published), Drafts = await q.CountAsync(x => !x.Published), NewEnquiries = await db.Enquiries.CountAsync(x => x.SiteId == site && x.Status == "New"), Recent = await q.AsNoTracking().OrderByDescending(x => x.UpdatedUtc).Take(8).ToListAsync() });
    }
    [HttpGet("content")]
    public async Task<IActionResult> Content(string site = "realty",string kind = "Project",string q = "", int page = 1) {
        if (!Valid(site,kind)) return NotFound(); Context(site); page = Math.Clamp(page,1,10000); q = q.Length > 160 ? q[..160] : q;
        var query = db.Entries.AsNoTracking().Where(x => x.SiteId == site && x.Kind == kind && (q == "" || x.Title.Contains(q)));
        return View(new AdminList { Site = site,Kind = kind,Query = q, Page = page,Total = await query.CountAsync(),Items = await query.OrderBy(x => x.SortOrder).ThenByDescending(x => x.Id).Skip((page-1)*20).Take(20).ToListAsync() });
    }
    [HttpGet("edit")]
    public async Task<IActionResult> Edit(int? id,string site="realty",string kind="Project") {
        if (!Valid(site,kind)) return NotFound();
        var entry = id.HasValue ? await db.Entries.AsNoTracking().Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == id) : new ContentEntry {SiteId = site,Kind = kind};
        if (entry == null) return NotFound(); Context(entry.SiteId); return View(entry);
    }
    [HttpPost("edit"), RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> Edit([Bind("Id,SiteId,Kind,Title,Slug,Subtitle,Body,ImagePath,AltText,LinkUrl,ButtonText,Price,Location,Configuration,PropertyType,ReraNumber,Possession,MapUrl,Amenities,Highlights,MetaDescription,Published,Featured,SortOrder,Version")] ContentEntry input, IFormFile? imageFile) {
        Normalize(input);
        if (!Valid(input.SiteId,input.Kind)) return NotFound(); Context(input.SiteId);
        var existing = input.Id == 0 ? null : await db.Entries.Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == input.Id);
        if (input.Id != 0 && (existing == null || existing.SiteId != input.SiteId || existing.Kind != input.Kind)) return NotFound();
        if (existing != null && existing.Version != input.Version) ModelState.AddModelError("","Someone else changed this item. Reload the page before saving.");
        if (!MediaStore.SafeLink(input.LinkUrl)) ModelState.AddModelError("LinkUrl","Use an HTTPS URL or a local path beginning with /.");
        if (!MediaStore.SafeMap(input.MapUrl)) ModelState.AddModelError("MapUrl","Paste only the https://www.google.com/maps/embed… URL, not iframe code.");
        if (!media.IsImage(input.ImagePath)) ModelState.AddModelError("ImagePath","Choose an existing /images/ or /media/ path, or upload an image.");
        if (await db.Entries.AnyAsync(x => x.SiteId == input.SiteId && x.Kind == input.Kind && x.Slug == input.Slug && x.Id != input.Id)) ModelState.AddModelError("Slug","This URL slug is already used.");
        if (ModelState.IsValid) {
            try { input.ImagePath = await media.Save(imageFile) ?? input.ImagePath; } catch (ArgumentException ex) { ModelState.AddModelError("ImagePath",ex.Message); }
        }
        if (!ModelState.IsValid) { input.Media = existing?.Media ?? []; return View(input); }
        input.UpdatedUtc = DateTime.UtcNow; input.Version++;
        if (existing == null) db.Entries.Add(input); else db.Entry(existing).CurrentValues.SetValues(input);
        Audit($"Saved {input.Kind}: {input.Title} ({input.SiteId}); published={input.Published}");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { ModelState.AddModelError("","This item changed in another session. Reload before saving."); input.Media = existing?.Media ?? []; return View(input); }
        catch (DbUpdateException) { ModelState.AddModelError("","Could not save. Check for a duplicate slug, then reload and retry."); input.Media = existing?.Media ?? []; return View(input); }
        TempData["Success"] = "Changes saved.";
        return RedirectToAction("Edit",new { id = existing?.Id ?? input.Id });
    }
    [HttpPost("publish")]
    public async Task<IActionResult> Publish(int id,long version) {
        var entry = await db.Entries.FindAsync(id); if (entry == null) return NotFound();
        if (entry.Version != version) { TempData["Error"] = "This item changed. Reload and try again."; }
        else {
            entry.Published = !entry.Published; entry.Version++; entry.UpdatedUtc = DateTime.UtcNow;
            Audit($"Changed publication: {entry.Kind} {id} → {entry.Published}");
            try { await db.SaveChangesAsync(); TempData["Success"] = entry.Published ? "Published on website." : "Moved to draft."; } catch (DbUpdateConcurrencyException) { TempData["Error"] = "This item changed. Please retry."; }
        }
        return RedirectToAction("Content",new {site = entry.SiteId,kind = entry.Kind});
    }
    [HttpPost("delete")]
    public async Task<IActionResult> Delete(int id,long version) {
        var entry = await db.Entries.FindAsync(id); if (entry == null) return NotFound();
        if (entry.Version != version) TempData["Error"] = "This item changed. Reload before deleting.";
        else { db.Entries.Remove(entry); Audit($"Deleted {entry.Kind} {id}: {entry.Title}");
            try { await db.SaveChangesAsync(); TempData["Success"] = "Content deleted. Uploaded images are retained for reuse."; } catch (DbUpdateConcurrencyException) { TempData["Error"] = "Item changed. Reload and try again."; }
        }
        return RedirectToAction("Content",new {site=entry.SiteId,kind=entry.Kind});
    }
    [HttpPost("media"), RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> AddMedia(MediaInput input,IFormFile? imageFile) {
        var entry = await db.Entries.FindAsync(input.EntryId); if (entry == null || entry.Kind != "Project") return NotFound();
        if (!new[]{"Gallery","FloorPlan"}.Contains(input.Kind) || entry.Version != input.Version) ModelState.AddModelError("","Invalid media type or outdated project. Reload and try again.");
        if (!ModelState.IsValid) { TempData["Error"] = "Check the caption, image and sort order, or reload the project if another editor changed it."; return RedirectToAction("Edit",new {id=entry.Id}); }
        try {
            var path = await media.Save(imageFile) ?? throw new ArgumentException("Choose an image.");
            db.Media.Add(new ProjectMedia {ContentEntryId=entry.Id,Kind=input.Kind,Caption=input.Caption,ImagePath=path,SortOrder=input.SortOrder});
            entry.Version++; entry.UpdatedUtc=DateTime.UtcNow; Audit($"Added {input.Kind} to project {entry.Id}");
            await db.SaveChangesAsync(); TempData["Success"] = "Image added.";
        } catch (ArgumentException ex) { TempData["Error"] = ex.Message; } catch (DbUpdateConcurrencyException) { TempData["Error"] = "Project changed. Reload and retry."; }
        return RedirectToAction("Edit",new {id=entry.Id});
    }
    [HttpPost("media/delete")]
    public async Task<IActionResult> DeleteMedia(int id,long version) {
        var item = await db.Media.Include(x=>x.Entry).SingleOrDefaultAsync(x=>x.Id==id); if (item == null) return NotFound();
        var entryId = item.ContentEntryId;
        if (item.Entry!.Version != version) TempData["Error"] = "Project changed. Reload first.";
        else { item.Entry.Version++; db.Media.Remove(item); Audit($"Removed media {id} from project {entryId}");
            try { await db.SaveChangesAsync(); TempData["Success"] = "Image removed from this project."; } catch (DbUpdateConcurrencyException) { TempData["Error"] = "Project changed. Reload first."; }
        }
        return RedirectToAction("Edit",new {id=entryId});
    }
    [HttpGet("settings")]
    public async Task<IActionResult> Settings(string site="realty") { if (!Valid(site)) return NotFound(); Context(site); return View(await db.Sites.AsNoTracking().SingleAsync(x=>x.SiteId==site)); }
    [HttpPost("settings"), RequestSizeLimit(12*1024*1024)]
    public async Task<IActionResult> Settings(SiteSettings input,IFormFile? logoFile,IFormFile? aboutFile) {
        Normalize(input);
        if (!Valid(input.SiteId)) return NotFound(); Context(input.SiteId);
        var current = await db.Sites.FindAsync(input.SiteId); if (current == null) return NotFound();
        if (current.Version != input.Version) ModelState.AddModelError("","Settings changed in another session. Reload before saving.");
        foreach (var url in new[]{input.Facebook,input.Instagram,input.YouTube}) if (!MediaStore.SafeLink(url)) ModelState.AddModelError("","Social links must be valid HTTPS URLs.");
        if (!media.IsImage(input.LogoPath) || !media.IsImage(input.AboutImage)) ModelState.AddModelError("","Use an existing image path or upload a new image.");
        if (ModelState.IsValid) try { input.LogoPath = await media.Save(logoFile) ?? input.LogoPath; input.AboutImage = await media.Save(aboutFile) ?? input.AboutImage; } catch (ArgumentException ex) { ModelState.AddModelError("",ex.Message); }
        if (!ModelState.IsValid) return View(input);
        input.Version++; db.Entry(current).CurrentValues.SetValues(input); Audit($"Updated site settings: {input.SiteId}");
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("","Settings changed. Reload and retry."); return View(input); }
        TempData["Success"] = "Website settings saved."; return RedirectToAction("Settings",new{site=input.SiteId});
    }
    [HttpGet("preview/{id:int}")]
    public async Task<IActionResult> Preview(int id) {
        var item=await db.Entries.AsNoTracking().Include(x=>x.Media).SingleOrDefaultAsync(x=>x.Id==id);
        if(item==null || (item.Kind!="Project" && item.Kind!="Blog")) return NotFound();
        var settings=await db.Sites.AsNoTracking().SingleAsync(x=>x.SiteId==item.SiteId);
        ViewBag.Settings=settings; ViewBag.Reels=new List<ContentEntry>(); ViewBag.Preview=true;
        return View("~/Views/Home/Detail.cshtml",new PublicPage {Settings=settings,Title="Preview: "+item.Title,Entry=item,Contact=new EnquiryInput {ProjectId=item.Kind=="Project"?item.Id:null}});
    }
    [HttpGet("enquiries")]
    public async Task<IActionResult> Enquiries(string site="realty",int page=1) {
        if (!Valid(site)) return NotFound(); Context(site); page=Math.Clamp(page,1,10000); ViewBag.Page=page;
        var q=db.Enquiries.AsNoTracking().Where(x=>x.SiteId==site); ViewBag.Total=await q.CountAsync();
        return View(await q.OrderByDescending(x=>x.Id).Skip((page-1)*20).Take(20).ToListAsync());
    }
    [HttpPost("enquiries/update")]
    public async Task<IActionResult> UpdateEnquiry(int id,long version,string status,string? notes) {
        var item=await db.Enquiries.FindAsync(id); if(item==null) return NotFound();
        if (item.Version != version || !new[]{"New","Contacted","Site visit","Closed"}.Contains(status) || (notes?.Length ?? 0)>3000) TempData["Error"]="Invalid values or enquiry changed. Reload and retry.";
        else { item.Status=status; item.Notes=notes ?? ""; item.Version++; Audit($"Updated enquiry {id} to {status}");
            try { await db.SaveChangesAsync(); TempData["Success"]="Enquiry updated."; } catch(DbUpdateConcurrencyException){TempData["Error"]="Enquiry changed. Reload and retry.";}
        }
        return RedirectToAction("Enquiries",new{site=item.SiteId});
    }
    [HttpPost("enquiries/delete")]
    public async Task<IActionResult> DeleteEnquiry(int id,long version) {
        var item=await db.Enquiries.FindAsync(id); if(item==null) return NotFound();
        if(item.Version!=version) TempData["Error"]="Enquiry changed. Reload first.";
        else {db.Enquiries.Remove(item); Audit($"Deleted enquiry {id}");try {await db.SaveChangesAsync();TempData["Success"]="Enquiry deleted.";}catch(DbUpdateConcurrencyException){TempData["Error"]="Enquiry changed. Reload first.";}}
        return RedirectToAction("Enquiries",new{site=item.SiteId});
    }
    [HttpGet("audit")]
    public async Task<IActionResult> AuditLog(int page=1) {page=Math.Clamp(page,1,10000);ViewBag.Page=page;ViewBag.Total=await db.AuditEvents.CountAsync();return View(await db.AuditEvents.AsNoTracking().OrderByDescending(x=>x.Id).Skip((page-1)*50).Take(50).ToListAsync());}
}

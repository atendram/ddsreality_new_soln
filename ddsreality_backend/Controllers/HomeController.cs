using DdsCms.Data;
using DdsCms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace DdsCms.Controllers;
public class HomeController(CmsDb db) : Controller
{
    private string Site => Request.Path.StartsWithSegments("/dds") ? "dds" : "realty";
    private async Task<PublicPage> Page(string title="") {
        var settings=await db.Sites.AsNoTracking().SingleAsync(x=>x.SiteId==Site);
        var reels=settings.ShowReels ? await db.Entries.AsNoTracking().Where(x=>x.SiteId==Site && x.Kind=="Reel" && x.Published).OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id).Take(6).ToListAsync() : [];
        ViewBag.Settings=settings; ViewBag.Reels=reels;
        return new PublicPage {Settings=settings,Title=title};
    }
    [HttpGet("/"), HttpGet("/dds")]
    public async Task<IActionResult> Index() {
        var model=await Page();
        // Each homepage section is bounded; a growing blog cannot make the homepage query unbounded.
        foreach(var kind in new[]{"Banner","Project","Blog","Team","Testimonial","Service"}) {
            var limit=kind=="Project" ? 6 : kind=="Blog" ? 3 : 6;
            model.Items.AddRange(await db.Entries.AsNoTracking().Where(x=>x.SiteId==Site && x.Kind==kind && x.Published).OrderByDescending(x=>x.Featured).ThenBy(x=>x.SortOrder).ThenBy(x=>x.Id).Take(limit).ToListAsync());
        }
        return View(model);
    }
    [HttpGet("/projects"),HttpGet("/dds/projects")]
    public Task<IActionResult> Projects(string q="",int page=1) => Listing("Project",q,page);
    [HttpGet("/blog"),HttpGet("/dds/blog")]
    public Task<IActionResult> Blog(string q="",int page=1) => Listing("Blog",q,page);
    private async Task<IActionResult> Listing(string kind,string q,int page) {
        page=Math.Clamp(page,1,10000);q=q.Length>160?q[..160]:q;
        var model=await Page(kind=="Project"?"Explore our projects":"Ideas & insights"); model.Query=q;model.Page=page;
        var query=db.Entries.AsNoTracking().Where(x=>x.SiteId==Site && x.Kind==kind && x.Published && (q=="" || x.Title.Contains(q) || x.Location.Contains(q)));
        model.Total=await query.CountAsync();model.Items=await query.OrderByDescending(x=>x.Featured).ThenBy(x=>x.SortOrder).ThenBy(x=>x.Id).Skip((page-1)*12).Take(12).ToListAsync();
        ViewBag.Kind=kind;return View("Listing",model);
    }
    [HttpGet("/projects/{slug}"),HttpGet("/dds/projects/{slug}")]
    public Task<IActionResult> Project(string slug) => Detail("Project",slug);
    [HttpGet("/blog/{slug}"),HttpGet("/dds/blog/{slug}")]
    public Task<IActionResult> Article(string slug) => Detail("Blog",slug);
    private async Task<IActionResult> Detail(string kind,string slug) {
        var entry=await db.Entries.AsNoTracking().Include(x=>x.Media).SingleOrDefaultAsync(x=>x.SiteId==Site && x.Kind==kind && x.Slug==slug && x.Published);
        if(entry==null) return NotFound();var model=await Page(entry.Title);model.Entry=entry;model.Contact.ProjectId=kind=="Project"?entry.Id:null;
        return View("Detail",model);
    }
    [HttpGet("/about"),HttpGet("/dds/about")]
    public async Task<IActionResult> About() {var model=await Page("About us");model.Items=await db.Entries.AsNoTracking().Where(x=>x.SiteId==Site && x.Published && (x.Kind=="Team" || x.Kind=="Testimonial")).OrderBy(x=>x.SortOrder).Take(30).ToListAsync();return View(model);}
    [HttpGet("/contact"),HttpGet("/dds/contact")]
    public async Task<IActionResult> Contact() => View(await Page("Let's talk"));
    [HttpPost("/contact"),HttpPost("/dds/contact"), EnableRateLimiting("enquiry")]
    public async Task<IActionResult> Contact([Bind(Prefix="Contact")] EnquiryInput input) {
        if(!string.IsNullOrEmpty(input.Website)) return BadRequest();
        if(!input.Consent) ModelState.AddModelError("Contact.Consent","Please agree to be contacted about your enquiry.");
        ContentEntry? project=null;
        if(input.ProjectId.HasValue) {project=await db.Entries.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.ProjectId && x.SiteId==Site && x.Kind=="Project" && x.Published);if(project==null) ModelState.AddModelError("","This project is unavailable. Please send a general enquiry.");}
        if(!ModelState.IsValid){var model=await Page("Let's talk");model.Contact=input;return View(model);}
        db.Enquiries.Add(new Enquiry {SiteId=Site,ProjectId=project?.Id,ProjectTitle=project?.Title ?? "General enquiry",Name=input.Name,Phone=input.Phone,Email=input.Email,Message=input.Message});
        await db.SaveChangesAsync();TempData["Success"]="Thank you. Your enquiry has been received by the DDS team.";return Redirect(Catalog.Root(Site)+"/contact");
    }
    [HttpGet("/privacy"),HttpGet("/dds/privacy")]
    public async Task<IActionResult> Privacy() => View(await Page("Privacy"));
    [HttpGet("/index.html")] public IActionResult OldHome()=>RedirectPermanent("/");
    [HttpGet("/about-us.html")] public IActionResult OldAbout()=>RedirectPermanent("/about");
    [HttpGet("/contact-form.html")] public IActionResult OldContact()=>RedirectPermanent("/contact");
    [HttpGet("/blogs.html")] public IActionResult OldBlog()=>RedirectPermanent("/blog");
    [HttpGet("/{legacy}.html")]
    public async Task<IActionResult> Legacy(string legacy){var slug=legacy.ToLowerInvariant();return await db.Entries.AnyAsync(x=>x.SiteId=="realty" && x.Kind=="Project" && x.Slug==slug && x.Published)?RedirectPermanent("/projects/"+slug):NotFound();}
    [Route("/error")] public IActionResult Error(){Response.StatusCode=500;return View();}
}

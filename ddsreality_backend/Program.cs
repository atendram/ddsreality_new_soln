using DdsCms.Data;
using DdsCms.Models;
using DdsCms.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CmsDb>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllersWithViews(o => { o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true; });
builder.Services.AddScoped<MediaStore>();
builder.Services.AddScoped<PasswordHasher<AdminUser>>();
builder.Services.AddDataProtection().SetApplicationName("DdsCms").PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(builder.Configuration["Storage:KeyPath"] ?? "App_Data/keys",builder.Environment.ContentRootPath)));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/account/login"; o.AccessDeniedPath = "/account/login";
    o.Cookie.Name = "__Host-DdsAdmin"; o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax; o.ExpireTimeSpan = TimeSpan.FromMinutes(45); o.SlidingExpiration = true;
    o.Events.OnValidatePrincipal = async context => {
        var db = context.HttpContext.RequestServices.GetRequiredService<CmsDb>();
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id) ||
            !await db.AdminUsers.AnyAsync(u => u.Id == id && u.Active && u.SecurityStamp == context.Principal!.FindFirstValue("stamp"))) context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
    o.AddPolicy("enquiry", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
});
var app = builder.Build();
// Offline provisioning: these commands exit without starting a public setup endpoint.
if (args.Contains("--schema")) {
    using var scope = app.Services.CreateScope();
    Console.WriteLine(scope.ServiceProvider.GetRequiredService<CmsDb>().Database.GenerateCreateScript()); return;
}
if (args.Contains("--seed") || args.Contains("--create-admin") || args.Contains("--reset-admin")) {
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<CmsDb>();
    if (args.Contains("--seed")) await SeedData.Run(db,app.Environment.ContentRootPath);
    else {
        var email = (builder.Configuration["Bootstrap:Email"] ?? "").Trim().ToLowerInvariant();
        var password = builder.Configuration["Bootstrap:Password"] ?? "";
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || password.Length < 12 || password.Length > 128)
            throw new InvalidOperationException("Set Bootstrap:Email and Bootstrap:Password (12–128 characters) using user-secrets or environment variables.");
        var user = await db.AdminUsers.SingleOrDefaultAsync(x => x.Email == email);
        if (user != null && !args.Contains("--reset-admin")) throw new InvalidOperationException("Administrator already exists. Use the reset command intentionally.");
        if (user == null) { user = new AdminUser { Email = email }; db.AdminUsers.Add(user); }
        user.PasswordHash = scope.ServiceProvider.GetRequiredService<PasswordHasher<AdminUser>>().HashPassword(user,password);
        user.SecurityStamp = Guid.NewGuid().ToString(); user.Active = true; user.FailedAttempts = 0; user.LockoutUntilUtc = null; user.Version++;
        db.AuditEvents.Add(new AuditEvent { Actor = "Offline administrator", Action = args.Contains("--reset-admin") ? "Reset administrator credential" : "Created administrator" });
        await db.SaveChangesAsync(); Console.WriteLine("Administrator saved. Remove Bootstrap secrets after provisioning.");
    }
    return;
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.Use(async (c,next) => {
    c.Response.Headers["X-Content-Type-Options"] = "nosniff";
    c.Response.Headers["X-Frame-Options"] = "DENY";
    c.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    c.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-src https://www.google.com; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    if (c.Request.Path.StartsWithSegments("/admin") || c.Request.Path.StartsWithSegments("/account")) c.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = c => c.Context.Response.Headers.CacheControl = "public,max-age=604800" });
var mediaRoot = Path.GetFullPath(builder.Configuration["Storage:MediaPath"] ?? "App_Data/media",app.Environment.ContentRootPath);
Directory.CreateDirectory(mediaRoot);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(mediaRoot), RequestPath = "/media", OnPrepareResponse = c => c.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable" });
app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
namespace DdsCms { public class EntryPoint { } }

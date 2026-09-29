using DdsCms.Data;
using DdsCms.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace DdsCms.Controllers;
[Route("account")]
public class AccountController(CmsDb db, PasswordHasher<AdminUser> hasher) : Controller
{
    [HttpGet("login")] public IActionResult Login(string? returnUrl) => User.Identity?.IsAuthenticated == true ? Redirect("/admin") : View(new LoginModel { ReturnUrl = returnUrl });
    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        var user = await db.AdminUsers.SingleOrDefaultAsync(x => x.Email == email);
        bool accepted = false;
        if (user != null && user.Active && !(user.LockoutUntilUtc > DateTime.UtcNow)) {
            var result = hasher.VerifyHashedPassword(user,user.PasswordHash,model.Password);
            accepted = result != PasswordVerificationResult.Failed;
            if (accepted) {
                user.FailedAttempts = 0; user.LockoutUntilUtc = null;
                if (result == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user,model.Password);
            } else {
                user.FailedAttempts++;
                if (user.FailedAttempts >= 5) { user.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(15); user.FailedAttempts = 0; }
            }
            user.Version++;
            try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { accepted = false; }
        }
        if (!accepted) { ModelState.AddModelError("","Sign-in failed. Check your details or try again after 15 minutes."); return View(model); }
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier,user!.Id.ToString()), new Claim(ClaimTypes.Name,user.Email), new Claim(ClaimTypes.Role,"Admin"), new Claim("stamp",user.SecurityStamp) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/admin");
    }
    [Authorize, HttpPost("logout")] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return Redirect("/"); }
    [Authorize, HttpGet("password")] public IActionResult Password() => View(new PasswordModel());
    [Authorize, HttpPost("password")] public async Task<IActionResult> Password(PasswordModel model)
    {
        var user = await db.AdminUsers.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (!ModelState.IsValid) return View(model);
        if (user == null || hasher.VerifyHashedPassword(user,user.PasswordHash,model.CurrentPassword) == PasswordVerificationResult.Failed) { ModelState.AddModelError("","Current password is incorrect."); return View(model); }
        user.PasswordHash = hasher.HashPassword(user,model.NewPassword); user.SecurityStamp = Guid.NewGuid().ToString(); user.Version++;
        db.AuditEvents.Add(new AuditEvent { Actor = user.Email, Action = "Changed password; revoked existing sessions" });
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("","Your account changed. Reload and try again."); return View(model); }
        await HttpContext.SignOutAsync(); TempData["Success"] = "Password changed. Please sign in again."; return RedirectToAction("Login");
    }
}

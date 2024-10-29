using Expense_Tracker.Models.Account;
using Expense_Tracker.Models.Settings;
using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

[Authorize]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public SettingsController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // Main settings page
    public IActionResult Index()
    {
        return View();
    }

    // Individual pages for settings
    public IActionResult ChangeName() => View();
    public IActionResult ChangeEmail() => View();
    public IActionResult ChangePassword() => View();
    public IActionResult ChangeTheme() => View();

    // Save Name
    [HttpPost]
    public async Task<IActionResult> SaveName(ChangeNameDTO model)
    {
        if (!ModelState.IsValid) return View("ChangeName", model);

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);

        if (user != null)
        {
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            await _context.SaveChangesAsync();

            // Update the claims in the current session
            await UpdateUserClaimsName(user.FirstName, user.LastName);

            TempData["SuccessMessage"] = "Name updated successfully!";
            return RedirectToAction("Index");
        }

        TempData["ErrorMessage"] = "User not found.";
        return View("ChangeName", model);
    }


    [HttpPost]
    public async Task<IActionResult> SaveEmail(ChangeEmailDTO model)
    {
        if (!ModelState.IsValid) return View("ChangeEmail", model);

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);

        if (user != null)
        {
            user.Email = model.NewEmail;
            await _context.SaveChangesAsync();

            // Update the email claim in the current session
            await UpdateUserClaimsEmail(user.Email);

            TempData["SuccessMessage"] = "Email updated successfully!";
            return RedirectToAction("Index");
        }

        TempData["ErrorMessage"] = "User not found.";
        return View("ChangeEmail", model);
    }


    // Save Password
    [HttpPost]
    public async Task<IActionResult> SavePassword(ChangePasswordDTO model)
    {
        if (!ModelState.IsValid) return View("ChangePassword", model);

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);

        if (user != null)
        {
            // Verify current password
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("CurrentPassword", "The current password is incorrect.");
                return View("ChangePassword", model);
            }

            // Hash and set new password
            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password updated successfully!";
            return RedirectToAction("Index");
        }

        TempData["ErrorMessage"] = "User not found.";
        return View("ChangePassword", model);
    }

    // Save Theme (if needed)
    [HttpPost]
    public async Task<IActionResult> SaveTheme(Theme model)
    {
        if (!ModelState.IsValid) return View("ChangeTheme", model);

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Check if the user has an existing theme preference
        var existingTheme = await _context.Themes.FirstOrDefaultAsync(t => t.UserId == userId);

        if (existingTheme != null)
        {
            // Update the existing theme
            existingTheme.IsDarkTheme = model.IsDarkTheme;
        }
        else
        {
            // Create a new theme record if it doesn't exist
            model.UserId = userId;
            _context.Themes.Add(model);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Theme updated successfully!";
        return RedirectToAction("Index");
    }

    private async Task UpdateUserClaimsName (string firstName, string lastName)
    {
        // Get the current claims identity
        var identity = (ClaimsIdentity)User.Identity;

        // Remove the existing "FullName" claim if it exists
        var fullNameClaim = identity.FindFirst("FullName");
        if (fullNameClaim != null)
        {
            identity.RemoveClaim(fullNameClaim);
        }

        // Add the updated "FullName" claim
        identity.AddClaim(new Claim("FullName", $"{firstName} {lastName}"));

        // Refresh the authentication cookie with the updated claims
        await HttpContext.SignOutAsync();
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
    }

    private async Task UpdateUserClaimsEmail(string email)
    {
        // Get the current claims identity
        var identity = (ClaimsIdentity)User.Identity;

        // Remove the existing "Email" claim if it exists
        var emailClaim = identity.FindFirst(ClaimTypes.Email);
        if (emailClaim != null)
        {
            identity.RemoveClaim(emailClaim);
        }

        // Add the updated "Email" claim
        identity.AddClaim(new Claim(ClaimTypes.Email, email));

        // Refresh the authentication cookie with the updated claims
        await HttpContext.SignOutAsync();
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
    }


}

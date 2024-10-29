using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Expense_Tracker.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Expense_Tracker.Models.Account.AccountDTO;
using Expense_Tracker.Models.Account;

namespace Expense_Tracker.Controllers
{
    public class AccountController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher) : Controller
    {
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View(model);
            }

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = passwordHasher.HashPassword(null, model.Password)
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            await SignInUser(user);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = context.Users.SingleOrDefault(u => u.Email == model.Email);
            if (user == null || passwordHasher.VerifyHashedPassword(null, user.PasswordHash, model.Password) != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Use the SignInUser method to sign in the user
            await SignInUser(user);

            return RedirectToAction("Index", "Dashboard");
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Dashboard");
        }

        private async Task SignInUser(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", $"{user.FirstName} {user.LastName}")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties();

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }
    }
}

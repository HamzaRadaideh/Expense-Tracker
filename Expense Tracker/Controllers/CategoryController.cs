using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using Expense_Tracker.Models.ExpenseTracker;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Expense_Tracker.Models.Account;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class CategoryController(ApplicationDbContext context) : Controller
    {

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Filter categories to show only those created by the logged-in user
            var userCategories = await context.Categories
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(userCategories);
        }

        // GET: Category/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View(new Category());
            else
            {
                // Find the category and ensure it belongs to the logged-in user
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var category = context.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId);
                if (category == null)
                {
                    return NotFound();  // Return 404 if the category doesn't belong to the user
                }
                return View(category);
            }
        }

        // POST: Category/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("CategoryId,Title,Icon,Type,UserId")] Category category)
        {
            if (ModelState.IsValid)
            {
                return View(category);
            }

            // Get the logged-in user's ID from claims (extracted from the authentication cookie)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (category.CategoryId == 0)
            {
                // New category, set UserId directly
                category.UserId = userId;  // Using UserId from cookie claim
                context.Add(category);
            }
            else
            {
                // Updating an existing category, ensure it belongs to the logged-in user
                var existingCategory = await context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId && c.UserId == userId);

                if (existingCategory == null)
                {
                    return NotFound("Category not found or does not belong to the user.");
                }

                // Update fields on the existing category
                existingCategory.Title = category.Title;
                existingCategory.Icon = category.Icon;
                existingCategory.Type = category.Type;

                context.Update(existingCategory);
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var category = await context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && c.UserId == userId);
            if (category == null)
            {
                return NotFound();  // Return 404 if the category doesn't belong to the user
            }

            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using Expense_Tracker.Models.ExpenseTracker;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transaction
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .ToListAsync();

            return View(transactions);
        }

        // GET: Transaction/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var transaction = _context.Transactions
                    .Include(t => t.Category)
                    .FirstOrDefault(t => t.TransactionId == id && t.UserId == userId);

                if (transaction == null)
                    return NotFound("Transaction not found or does not belong to the user.");

                return View(transaction);
            }
        }

        // POST: Transaction/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
        {

            if (ModelState.IsValid)
            {
                return View(transaction);
            }

            // Get the logged-in user's ID from claims (extracted from the authentication cookie)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            transaction.UserId = userId;

            if (transaction.TransactionId == 0)
            {
                // New transaction, set UserId directly
                transaction.UserId = userId;  // Using UserId from cookie claim
                _context.Add(transaction);
            }
            else
            {
                // Updating an existing transaction
                var existingTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionId == transaction.TransactionId && t.UserId == userId);

                if (existingTransaction == null)
                    return NotFound("Transaction not found or does not belong to the user.");

                // Update fields on the existing transaction
                existingTransaction.CategoryId = transaction.CategoryId;
                existingTransaction.Amount = transaction.Amount;
                existingTransaction.Note = transaction.Note;
                existingTransaction.Date = transaction.Date;

                _context.Update(existingTransaction);
            }

            PopulateCategories();
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId);

            if (transaction == null)
                return NotFound("Transaction not found or does not belong to the user.");

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public void PopulateCategories()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var categories = _context.Categories
                .Where(c => c.UserId == userId)
                .ToList();

            var defaultCategory = new Category { CategoryId = 0, Title = "Choose a Category" };
            categories.Insert(0, defaultCategory);
            ViewBag.Categories = categories;
        }
    }
}

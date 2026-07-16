using FinTrackAPI.Data;
using FinTrackAPI.DTOs;
using FinTrackAPI.Extensions;
using FinTrackAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class BudgetEndpoints
    {
        public static void MapBudgetEndpoints(this WebApplication app)
        {
            // GET - everything the Budget page needs for one specific month:
            // income/expense totals actually scoped to that month, the saved
            // overall target, saved per-category limits, and spending by category.
            app.MapGet("/budget", async (AppDbContext db, HttpContext http, int? year, int? month) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var today = DateTime.Today;
                var y = year ?? today.Year;
                var m = month ?? today.Month;

                if (m < 1 || m > 12) return Results.BadRequest("Month must be between 1 and 12.");

                var monthStart = new DateTime(y, m, 1);
                var monthEnd = monthStart.AddMonths(1);

                var totalIncome = await db.Income
                    .Where(i => i.UserId == userId.Value && i.DateCreated >= monthStart && i.DateCreated < monthEnd)
                    .SumAsync(i => (decimal?)i.Amount) ?? 0;

                var expensesInMonth = await db.Expenses
                    .Where(e => e.UserId == userId.Value && e.DateCreated >= monthStart && e.DateCreated < monthEnd)
                    .ToListAsync();

                var totalExpense = expensesInMonth.Sum(e => e.Amount);

                var byCategory = expensesInMonth
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key, Spent = g.Sum(e => e.Amount) })
                    .OrderByDescending(c => c.Spent)
                    .ToList();

                var targets = await db.BudgetTargets
                    .Where(b => b.UserId == userId.Value && b.Year == y && b.Month == m)
                    .ToListAsync();

                var overallTarget = targets.FirstOrDefault(t => string.IsNullOrEmpty(t.Category))?.Amount ?? 0;

                var categoryLimits = targets
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .Select(t => new { t.Category, t.Amount })
                    .ToList();

                return Results.Ok(new
                {
                    Year = y,
                    Month = m,
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    Balance = totalIncome - totalExpense,
                    OverallTarget = overallTarget,
                    CategoryLimits = categoryLimits,
                    ByCategory = byCategory
                });
            }).RequireAuthorization();

            // PUT - save the overall target and replace all category limits for that month
            app.MapPut("/budget", async (AppDbContext db, HttpContext http, BudgetUpdateRequest request) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                if (request.Month < 1 || request.Month > 12)
                    return Results.BadRequest("Month must be between 1 and 12.");

                // Upsert the overall target row
                var overallRow = await db.BudgetTargets.FirstOrDefaultAsync(b =>
                    b.UserId == userId.Value && b.Year == request.Year && b.Month == request.Month &&
                    (b.Category == null || b.Category == ""));

                if (overallRow == null)
                {
                    db.BudgetTargets.Add(new BudgetTarget
                    {
                        UserId = userId.Value,
                        Year = request.Year,
                        Month = request.Month,
                        Category = null,
                        Amount = request.OverallTarget
                    });
                }
                else
                {
                    overallRow.Amount = request.OverallTarget;
                }

                // Replace this month's category limits with whatever was submitted
                var existingCategoryRows = await db.BudgetTargets
                    .Where(b => b.UserId == userId.Value && b.Year == request.Year && b.Month == request.Month
                             && b.Category != null && b.Category != "")
                    .ToListAsync();

                db.BudgetTargets.RemoveRange(existingCategoryRows);

                foreach (var limit in request.CategoryLimits)
                {
                    if (limit.Amount > 0 && !string.IsNullOrWhiteSpace(limit.Category))
                    {
                        db.BudgetTargets.Add(new BudgetTarget
                        {
                            UserId = userId.Value,
                            Year = request.Year,
                            Month = request.Month,
                            Category = limit.Category,
                            Amount = limit.Amount
                        });
                    }
                }

                await db.SaveChangesAsync();
                return Results.Ok("Saved");
            }).RequireAuthorization();
        }
    }
}

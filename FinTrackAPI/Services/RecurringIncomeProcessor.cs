using FinTrackAPI.Data;
using FinTrackAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Services
{
    public static class RecurringIncomeProcessor
    {
        // Generates any Income rows that have come due for this user's active
        // recurring rules, and advances each rule to its next future date.
        // Safe to call on every page load - it only does work when something
        // is actually due, so it costs nothing extra the rest of the time.
        public static async Task ProcessDueAsync(AppDbContext db, int userId)
        {
            var today = DateTime.Today;

            var dueRules = await db.RecurringIncomes
                .Where(r => r.UserId == userId && r.IsActive && r.NextRunDate <= today)
                .ToListAsync();

            if (dueRules.Count == 0) return;

            foreach (var rule in dueRules)
            {
                // Safety cap: if a rule sat untouched for a long time (e.g. the
                // user didn't open the app for a year), don't silently generate
                // hundreds of backdated entries in one go.
                var safetyCap = 60;

                while (rule.NextRunDate <= today && safetyCap-- > 0)
                {
                    db.Income.Add(new Income
                    {
                        UserId = rule.UserId,
                        Amount = rule.Amount,
                        Source = rule.Source,
                        DateCreated = rule.NextRunDate
                    });

                    ActivityLogger.Log(db, rule.UserId, "Added", "Income", rule.Amount, rule.Source, "Auto-generated (recurring)");

                    rule.NextRunDate = rule.Frequency switch
                    {
                        "Weekly" => rule.NextRunDate.AddDays(7),
                        "Monthly" => rule.NextRunDate.AddMonths(1),
                        "Custom" => rule.NextRunDate.AddDays(rule.IntervalDays ?? 30),
                        _ => rule.NextRunDate.AddMonths(1)
                    };
                }
            }

            await db.SaveChangesAsync();
        }
    }
}

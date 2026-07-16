using FinTrackAPI.Data;
using FinTrackAPI.Extensions;
using FinTrackAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class RecurringIncomeEndpoints
    {
        private static readonly string[] ValidFrequencies = { "Weekly", "Monthly", "Custom" };

        public static void MapRecurringIncomeEndpoints(this WebApplication app)
        {
            // CREATE a recurring rule
            app.MapPost("/income/recurring", async (AppDbContext db, RecurringIncome rule, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(rule.Source))
                    return Results.BadRequest("Source is required.");

                if (rule.Amount <= 0)
                    return Results.BadRequest("Amount must be greater than 0.");

                if (!ValidFrequencies.Contains(rule.Frequency))
                    return Results.BadRequest("Frequency must be Weekly, Monthly, or Custom.");

                if (rule.Frequency == "Custom" && (rule.IntervalDays is null || rule.IntervalDays < 1))
                    return Results.BadRequest("IntervalDays must be at least 1 when Frequency is Custom.");

                rule.Id = 0;
                rule.UserId = userId.Value;
                rule.IsActive = true;
                rule.CreatedDate = DateTime.Now;
                if (rule.NextRunDate == default) rule.NextRunDate = DateTime.Today;

                db.RecurringIncomes.Add(rule);
                await db.SaveChangesAsync();

                return Results.Ok(rule);
            }).RequireAuthorization();

            // LIST this user's recurring rules
            app.MapGet("/income/recurring", async (AppDbContext db, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var rules = await db.RecurringIncomes
                    .Where(r => r.UserId == userId.Value)
                    .OrderBy(r => r.NextRunDate)
                    .ToListAsync();

                return Results.Ok(rules);
            }).RequireAuthorization();

            // PAUSE or RESUME a rule, without deleting it
            app.MapPut("/income/recurring/{id}/toggle", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var rule = await db.RecurringIncomes
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value);

                if (rule == null) return Results.NotFound();

                rule.IsActive = !rule.IsActive;
                await db.SaveChangesAsync();

                return Results.Ok(rule);
            }).RequireAuthorization();

            // DELETE a rule - this only stops future income from being generated.
            // Past Income entries it already created stay exactly as they are.
            app.MapDelete("/income/recurring/{id}", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var rule = await db.RecurringIncomes
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value);

                if (rule == null) return Results.NotFound();

                db.RecurringIncomes.Remove(rule);
                await db.SaveChangesAsync();

                return Results.Ok("Deleted successfully");
            }).RequireAuthorization();
        }
    }
}

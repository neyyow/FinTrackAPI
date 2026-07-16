using FinTrackAPI.Data;
using FinTrackAPI.Models;
using Microsoft.EntityFrameworkCore;
using FinTrackAPI.Extensions;
using FinTrackAPI.Services;

namespace FinTrackAPI.Endpoints
{
    public static class IncomeEndpoints
    {
        public static void MapIncomeEndpoints(this WebApplication app)
        {
            // CREATE
            app.MapPost("/income", async (AppDbContext db, Income income, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                if (income.Amount <= 0)
                    return Results.BadRequest("Amount must be greater than 0.");

                income.UserId = userId.Value;

                db.Income.Add(income);
                ActivityLogger.Log(db, userId.Value, "Added", "Income", income.Amount, income.Source);
                await db.SaveChangesAsync();

                return Results.Ok(income);
            }).RequireAuthorization();

            // GET ALL - only the signed-in user's income
            app.MapGet("/income", async (AppDbContext db, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                // Generate any income that's come due since the user last visited,
                // before returning the list - this is what makes recurring income
                // feel automatic without needing a background job running 24/7.
                await RecurringIncomeProcessor.ProcessDueAsync(db, userId.Value);

                var incomes = await db.Income
                    .Where(i => i.UserId == userId.Value)
                    .ToListAsync();

                return Results.Ok(incomes);
            }).RequireAuthorization();

            // GET BY ID - only if it belongs to the signed-in user
            app.MapGet("/income/{id}", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var income = await db.Income
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId.Value);

                return income is not null ? Results.Ok(income) : Results.NotFound();
            }).RequireAuthorization();

            // DELETE - only if it belongs to the signed-in user
            app.MapDelete("/income/{id}", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var income = await db.Income
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId.Value);

                if (income == null) return Results.NotFound();

                db.Income.Remove(income);
                ActivityLogger.Log(db, userId.Value, "Deleted", "Income", income.Amount, income.Source);
                await db.SaveChangesAsync();

                return Results.Ok("Deleted successfully");
            }).RequireAuthorization();
        }
    }
}

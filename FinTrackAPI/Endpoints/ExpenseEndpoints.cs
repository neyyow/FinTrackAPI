using FinTrackAPI.Data;
using FinTrackAPI.Models;
using FinTrackAPI.Extensions;
using FinTrackAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class ExpenseEndpoints
    {
        public static void MapExpenseEndpoints(this WebApplication app)
        {
            // CREATE
            app.MapPost("/expense", async (AppDbContext db, Expense expense, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                if (expense.Amount <= 0)
                    return Results.BadRequest("Amount must be greater than 0.");

                expense.UserId = userId.Value;

                db.Expenses.Add(expense);
                ActivityLogger.Log(db, userId.Value, "Added", "Expense", expense.Amount, expense.Category, expense.Description);
                await db.SaveChangesAsync();

                return Results.Ok(expense);
            }).RequireAuthorization();

            // GET ALL - only the signed-in user's expenses
            app.MapGet("/expense", async (AppDbContext db, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var expenses = await db.Expenses
                    .Where(e => e.UserId == userId.Value)
                    .ToListAsync();

                return Results.Ok(expenses);
            }).RequireAuthorization();

            // GET BY ID - only if it belongs to the signed-in user
            app.MapGet("/expense/{id}", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var expense = await db.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);

                return expense is not null ? Results.Ok(expense) : Results.NotFound();
            }).RequireAuthorization();

            // UPDATE - only if it belongs to the signed-in user
            app.MapPut("/expense/{id}", async (AppDbContext db, int id, Expense input, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var expense = await db.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);

                if (expense == null) return Results.NotFound();

                if (input.Amount <= 0)
                    return Results.BadRequest("Amount must be greater than 0.");

                var before = $"₱{expense.Amount:0.00} {expense.Category}";
                expense.Amount = input.Amount;
                expense.Category = input.Category;
                expense.Description = input.Description;
                var after = $"₱{expense.Amount:0.00} {expense.Category}";

                ActivityLogger.Log(db, userId.Value, "Edited", "Expense", expense.Amount, expense.Category, $"Changed {before} to {after}");

                await db.SaveChangesAsync();
                return Results.Ok(expense);
            }).RequireAuthorization();

            // DELETE - only if it belongs to the signed-in user
            app.MapDelete("/expense/{id}", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var expense = await db.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);

                if (expense == null) return Results.NotFound();

                db.Expenses.Remove(expense);
                ActivityLogger.Log(db, userId.Value, "Deleted", "Expense", expense.Amount, expense.Category, expense.Description);
                await db.SaveChangesAsync();

                return Results.Ok("Deleted successfully");
            }).RequireAuthorization();
        }
    }
}

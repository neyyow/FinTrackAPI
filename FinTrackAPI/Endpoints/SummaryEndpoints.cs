using FinTrackAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class SummaryEndpoints
    {
        public static void MapSummaryEndpoints(this WebApplication app)
        {
            app.MapGet("/summary", async (AppDbContext db, HttpContext http) =>
            {
                var userId = int.Parse(
                    http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                );

                var totalIncome = await db.Income
                    .Where(i => i.UserId == userId)
                    .SumAsync(i => (decimal?)i.Amount) ?? 0;

                var totalExpense = await db.Expenses
                    .Where(e => e.UserId == userId)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0;

                return Results.Ok(new
                {
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    Balance = totalIncome - totalExpense
                });
            }).RequireAuthorization();
        }
    }
}
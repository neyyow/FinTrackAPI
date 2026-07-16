using FinTrackAPI.Data;
using FinTrackAPI.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class LogsEndpoints
    {
        public static void MapLogsEndpoints(this WebApplication app)
        {
            // LIST - only the signed-in user's own activity, newest first, paged
            app.MapGet("/logs", async (AppDbContext db, HttpContext http, int page = 1, int pageSize = 25, string? action = null, string? entityType = null) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 25;
                if (pageSize > 100) pageSize = 100; // sensible upper bound - no huge pages

                var query = db.ActivityLogs.Where(l => l.UserId == userId.Value);

                if (!string.IsNullOrWhiteSpace(action))
                    query = query.Where(l => l.Action == action);

                if (!string.IsNullOrWhiteSpace(entityType))
                    query = query.Where(l => l.EntityType == entityType);

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Results.Ok(new
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }).RequireAuthorization();
        }
    }
}

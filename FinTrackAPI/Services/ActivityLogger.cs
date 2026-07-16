using FinTrackAPI.Data;
using FinTrackAPI.Models;

namespace FinTrackAPI.Services
{
    public static class ActivityLogger
    {
        // Just adds the log row to the same DbContext the caller is already
        // using - it gets saved together with whatever else that request is
        // doing (the income/expense add or delete), in one SaveChangesAsync.
        public static void Log(AppDbContext db, int userId, string action, string entityType, decimal amount, string label, string description = "")
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                Amount = amount,
                Label = label,
                Description = description ?? "",
                Timestamp = DateTime.Now
            });
        }
    }
}

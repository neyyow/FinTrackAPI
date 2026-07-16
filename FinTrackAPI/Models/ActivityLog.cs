namespace FinTrackAPI.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // "Added" or "Deleted"
        public string Action { get; set; }

        // "Income" or "Expense"
        public string EntityType { get; set; }

        public decimal Amount { get; set; }

        // The category (for expenses) or source (for income)
        public string Label { get; set; }

        // Optional extra detail - the expense description, if any
        public string Description { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

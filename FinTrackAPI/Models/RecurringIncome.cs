namespace FinTrackAPI.Models
{
    public class RecurringIncome
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Source { get; set; }

        // "Weekly", "Monthly", or "Custom"
        public string Frequency { get; set; }

        // Only used when Frequency is "Custom" - repeat every N days
        public int? IntervalDays { get; set; }

        // The next date this rule is due to generate a real Income entry.
        // Acts as both the "start date" when first created, and the
        // "next occurrence" once it's been running for a while.
        public DateTime NextRunDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

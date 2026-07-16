namespace FinTrackAPI.Models
{
    public class BudgetTarget
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; } // 1-12

        // Empty/null means this is the overall monthly target.
        // A category name means this is the limit for that specific category.
        public string? Category { get; set; }

        public decimal Amount { get; set; }
    }
}

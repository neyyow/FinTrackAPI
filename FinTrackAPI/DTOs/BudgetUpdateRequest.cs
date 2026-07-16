namespace FinTrackAPI.DTOs
{
    public class CategoryLimitDto
    {
        public string Category { get; set; }
        public decimal Amount { get; set; }
    }

    public class BudgetUpdateRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal OverallTarget { get; set; }
        public List<CategoryLimitDto> CategoryLimits { get; set; } = new();
    }
}

namespace FinTrackAPI.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        // Which wallet the money came from. Null = not assigned (all older expenses).
        public int? WalletId { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}

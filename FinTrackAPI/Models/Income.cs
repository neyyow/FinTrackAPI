namespace FinTrackAPI.Models
{
    public class Income
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Source { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}

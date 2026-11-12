namespace FinTrackAPI.Models
{
    // A place the user keeps money, e.g. "Cash", "GCash", "BDO Savings".
    // For now it only labels where an expense came from - it does not track a balance.
    public class Wallet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

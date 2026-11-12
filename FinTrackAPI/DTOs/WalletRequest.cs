namespace FinTrackAPI.DTOs
{
    // Body for creating or renaming a wallet. UserId is never accepted from
    // the client - it always comes from the JWT.
    public class WalletRequest
    {
        public string? Name { get; set; }
    }
}

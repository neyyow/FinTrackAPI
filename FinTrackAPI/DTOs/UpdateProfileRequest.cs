namespace FinTrackAPI.DTOs
{
    public class UpdateProfileRequest
    {
        // Email is intentionally NOT here. It's also the identity key Google
        // Sign-In uses to find/create the account (see GoogleAuthEndpoints),
        // so letting it be edited lets one account's email be reassigned to
        // point at another Google identity - silently handing over everything
        // tied to that UserId. Email is fixed at registration; only the
        // display name can be changed here.
        public string Username { get; set; } = string.Empty;
    }
}
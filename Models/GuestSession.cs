public class GuestSession
{
    public string UserId { get; set; } = SessionStore.GuestUserId;

    public string DisplayName { get; set; } = "Guest";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
}

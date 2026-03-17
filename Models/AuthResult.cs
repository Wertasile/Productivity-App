public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public bool RequiresEmailConfirmation { get; set; }
    public bool RequiresPasswordReset { get; set; }

    public bool IsAuthenticated { get; set; }
}
namespace AlAmalBusiness.Application.DTOs.Auth
{
    // What a successful sign-in or refresh hands back: a short-lived access
    // token for calling the API, and a long-lived refresh token that buys the
    // next access token without asking for the password again.
    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        // Seconds until Token expires, so a client can schedule ahead instead
        // of decoding the JWT or hard-coding the same number twice.
        public int ExpiresInSeconds { get; set; }
        // Seconds until RefreshToken expires — how long the session can be
        // kept alive without signing in again.
        public int RefreshExpiresInSeconds { get; set; }
        public string? Message { get; set; }

        public static LoginResult Success(string token, string refreshToken, int expiresInSeconds, int refreshExpiresInSeconds) => new()
        {
            IsSuccess = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresInSeconds = expiresInSeconds,
            RefreshExpiresInSeconds = refreshExpiresInSeconds,
            Message = "Login successful"
        };

        public static LoginResult Fail(string error) => new()
        {
            IsSuccess = false,
            Message = error
        };
    }
}

namespace AlAmalBusiness.Application.DTOs.Auth
{
    // Body for /api/Auth/refresh and /api/Auth/logout. The token travels in
    // the body rather than the URL so it never lands in a server access log.
    public class RefreshRequestDTO
    {
        public string? RefreshToken { get; set; }
    }
}

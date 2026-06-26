using Microsoft.AspNetCore.Http;
namespace MainBackend.Configurations;
public static class CookieOptionsFactory
{
    public static CookieOptions CreateAuthCookie()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(2000)
        };
    }
}
// AdminAuthHelper, Web tarafında JWT token içindeki rol bilgisini kontrol etmek için kullanılır.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FoodWise.Web.Helpers;

public static class AdminAuthHelper
{
    public static bool IsAdmin(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
            return false;

        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken.Claims.Any(x =>
            (x.Type == ClaimTypes.Role || x.Type == "role") &&
            x.Value == "Admin");
    }
}
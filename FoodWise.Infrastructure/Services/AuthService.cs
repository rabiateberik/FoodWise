using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FoodWise.Application.DTOs.Auth;
using FoodWise.Application.Interfaces;
using FoodWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FoodWise.Infrastructure.Services;

// AuthService, kullanıcı kayıt/giriş işlemlerini ve JWT token üretimini yönetir.
// Identity UserManager ile kullanıcı oluşturur, doğrular ve başarılı girişte rol bilgisi içeren token döner.
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto model)
    {
        var existingUser = await _userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Bu email adresi ile kayıtlı bir kullanıcı zaten var."
            };
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Email,

            City = model.City,
            District = model.District,
            Neighborhood = model.Neighborhood,

            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = string.Join(" | ", result.Errors.Select(x => x.Description))
            };
        }

        // Normal kayıt olan tüm kullanıcılar User rolüne atanır.
        await EnsureRoleExistsAsync("User");
        await _userManager.AddToRoleAsync(user, "User");

        var token = await GenerateJwtTokenAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Kayıt işlemi başarılı.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Token = token
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email veya şifre hatalı."
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Bu kullanıcı hesabı aktif değildir."
            };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);

        if (!isPasswordValid)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email veya şifre hatalı."
            };
        }

        var token = await GenerateJwtTokenAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Giriş işlemi başarılı.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Token = token
        };
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"]!;
        var jwtAudience = _configuration["Jwt:Audience"]!;
        var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"]);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName)
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
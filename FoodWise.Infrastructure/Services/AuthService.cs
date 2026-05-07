using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FoodWise.Application.DTOs.Auth;
using FoodWise.Application.Interfaces;
using FoodWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


// AuthService, kullanıcı kayıt/giriş işlemlerini ve JWT token üretimini yönetir.
// Identity UserManager ile kullanıcı oluşturur, doğrular ve başarılı girişte token döner.

namespace FoodWise.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto model)
    {
        // Aynı email ile daha önce kullanıcı oluşturulmuş mu kontrol edilir.
        var existingUser = await _userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Bu email adresi ile kayıtlı bir kullanıcı zaten var."
            };
        }

        // Yeni Identity kullanıcısı oluşturulur.
        // FoodWise sisteminde açık adres tutulmaz; kullanıcıdan yalnızca şehir, ilçe ve mahalle bilgisi alınır.
        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Email,

            // Kullanıcının bölgesel konum bilgileri kaydedilir.
            City = model.City,
            District = model.District,
            Neighborhood = model.Neighborhood,

            IsActive = true,
            CreatedAt = DateTime.Now
        };
        // Kullanıcı şifre ile birlikte Identity sistemine kaydedilir.
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = string.Join(" | ", result.Errors.Select(x => x.Description))
            };
        }

        // Kayıt başarılıysa kullanıcı için JWT token üretilir.
        var token = GenerateJwtToken(user);

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
        // Giriş yapmak isteyen kullanıcı email üzerinden bulunur.
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email veya şifre hatalı."
            };
        }

        // Kullanıcı pasifse girişe izin verilmez.
        if (!user.IsActive)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Bu kullanıcı hesabı aktif değildir."
            };
        }

        // Şifre doğrulaması yapılır.
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);

        if (!isPasswordValid)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email veya şifre hatalı."
            };
        }

        // Giriş başarılıysa JWT token üretilir.
        var token = GenerateJwtToken(user);

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

    private string GenerateJwtToken(ApplicationUser user)
    {
        // appsettings.json içindeki JWT ayarları okunur.
        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"]!;
        var jwtAudience = _configuration["Jwt:Audience"]!;
        var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"]);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Token içine kullanıcıya ait temel bilgiler claim olarak eklenir.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

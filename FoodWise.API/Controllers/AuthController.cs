// AuthController, kullanıcı kayıt ve giriş işlemleri için gerekli endpointleri içerir.
// Kimlik doğrulama işlemleri doğrudan burada yapılmaz, AuthService üzerinden yürütülür.

using FoodWise.Application.DTOs.Auth;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // Yeni kullanıcı kaydı oluşturmak için kullanılır.
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        var result = await _authService.RegisterAsync(model);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // Kullanıcının sisteme giriş yapmasını sağlar.
    // Başarılı girişte servis tarafında token ve kullanıcı bilgileri hazırlanır.
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        var result = await _authService.LoginAsync(model);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }
}


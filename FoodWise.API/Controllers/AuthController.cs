
// AuthController, kullanıcı kayıt ve giriş endpointlerini dışarıya açar.
// Register ve Login işlemleri IAuthService üzerinden yürütülür.
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

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        // Kullanıcı kayıt işlemi servis katmanına yönlendirilir.
        var result = await _authService.RegisterAsync(model);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        // Kullanıcı giriş işlemi servis katmanına yönlendirilir.
        var result = await _authService.LoginAsync(model);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }
}

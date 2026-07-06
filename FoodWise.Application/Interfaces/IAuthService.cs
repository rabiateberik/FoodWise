
using FoodWise.Application.DTOs.Auth;

namespace FoodWise.Application.Interfaces;

// Auth işlemlerinin servis katmanında hangi metotlarla yapılacağını tanımlar.
// Kayıt olma ve giriş yapma işlemleri bu interface üzerinden kullanılır.
public interface IAuthService
{
  
    Task<AuthResponseDto> RegisterAsync(RegisterDto model);


    Task<AuthResponseDto> LoginAsync(LoginDto model);
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Auth işlemlerinin dışarıya açılan sözleşmesidir.
// Gerçek implementasyonu Infrastructure katmanında yapılacaktır.
using FoodWise.Application.DTOs.Auth;

namespace FoodWise.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto model);

    Task<AuthResponseDto> LoginAsync(LoginDto model);
}

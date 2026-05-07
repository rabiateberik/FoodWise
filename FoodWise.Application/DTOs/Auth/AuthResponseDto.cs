using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Register ve Login işlemleri sonrasında kullanıcıya dönecek ortak cevap modelidir.
namespace FoodWise.Application.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = null!;

    public string? UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Token { get; set; }
}

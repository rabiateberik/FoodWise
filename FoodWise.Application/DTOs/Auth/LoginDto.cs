using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Kullanıcı giriş işlemi için gerekli email ve şifre bilgisini taşır.
namespace FoodWise.Application.DTOs.Auth;

public class LoginDto
{
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}

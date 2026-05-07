// Bu interface, FoodWise.Web katmanında kullanıcı giriş ve kayıt işlemleri için kullanılacak servis sözleşmesini tanımlar.
// Web projesi doğrudan veritabanına erişmez; bu servis aracılığıyla FoodWise.API üzerindeki Auth endpointlerine istek gönderir.

using FoodWise.Web.ViewModels.Auth;

namespace FoodWise.Web.Services;

public interface IAuthWebService
{
    Task<AuthResponseViewModel?> LoginAsync(LoginViewModel model);

    Task<AuthResponseViewModel?> RegisterAsync(RegisterViewModel model);
}
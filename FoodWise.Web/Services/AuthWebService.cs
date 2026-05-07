// Bu servis, FoodWise.Web ile FoodWise.API arasındaki kullanıcı kimlik doğrulama iletişimini sağlar.
// Login ve Register formlarından gelen verileri API'ye gönderir, API'den dönen token ve kullanıcı bilgilerini Web tarafına taşır.

using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Auth;

namespace FoodWise.Web.Services;

public class AuthWebService : IAuthWebService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthWebService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AuthResponseViewModel?> LoginAsync(LoginViewModel model)
    {
        var client = _httpClientFactory.CreateClient("FoodWiseApi");

        // Login formundan gelen email ve şifre bilgileri API'ye gönderilir.
        var response = await client.PostAsJsonAsync("/api/auth/login", model);

        return await ReadAuthResponseAsync(response);
    }

    public async Task<AuthResponseViewModel?> RegisterAsync(RegisterViewModel model)
    {
        var client = _httpClientFactory.CreateClient("FoodWiseApi");

        // API tarafı ConfirmPassword alanı beklemez.
        // Bu yüzden sadece register endpointinin ihtiyaç duyduğu bilgiler gönderilir.
        var registerRequest = new
        {
            model.FullName,
            model.Email,
            model.Password,
            model.City,
            model.District,
            model.Neighborhood
        };
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        return await ReadAuthResponseAsync(response);
    }

    private async Task<AuthResponseViewModel?> ReadAuthResponseAsync(HttpResponseMessage response)
    {
        try
        {
            // API başarılı veya başarısız olsa bile AuthResponseViewModel formatında cevap döndürür.
            var result = await response.Content.ReadFromJsonAsync<AuthResponseViewModel>(_jsonOptions);

            if (result != null)
                return result;

            return new AuthResponseViewModel
            {
                Success = false,
                Message = "API'den geçerli bir cevap alınamadı."
            };
        }
        catch
        {
            // API kapalıysa, port yanlışsa veya JSON okunamazsa kullanıcıya anlaşılır mesaj döndürülür.
            return new AuthResponseViewModel
            {
                Success = false,
                Message = "API ile bağlantı kurulurken bir hata oluştu."
            };
        }
    }
}
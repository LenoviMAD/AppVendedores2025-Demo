using System.Net.Http.Json;

namespace AppVendedores2025.Services;

public class SoporteAppConfigDto
{
    public string MailSoporte { get; set; } = "";
    public string LinkWhatsappSoporte { get; set; } = "";
}

public class SoporteAppConfigService
{
    private readonly ParametrosServices _oParam = new();

    public async Task<SoporteAppConfigDto?> GetConfigSoporteAsync()
    {
        try
        {
            var urlBase = _oParam.urlApi;
            if (string.IsNullOrWhiteSpace(urlBase))
                return null;

            using var httpClient = HttpHelper.CreateClient();
            httpClient.BaseAddress = new Uri(urlBase);
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            if (!string.IsNullOrWhiteSpace(_oParam.HeaderApiKey) && !string.IsNullOrWhiteSpace(_oParam.ApiKey))
                httpClient.DefaultRequestHeaders.Add(
                    CleanHeader(_oParam.HeaderApiKey),
                    CleanHeader(_oParam.ApiKey));

            var result = await httpClient.GetFromJsonAsync<SoporteAppConfigDto>("ParametrosApp/Soporte");

            if (result == null
                || string.IsNullOrWhiteSpace(result.MailSoporte)
                || string.IsNullOrWhiteSpace(result.LinkWhatsappSoporte))
                return null;

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SoporteAppConfigService] {ex.Message}");
            return null;
        }
    }

    private static string CleanHeader(string? value)
        => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
}

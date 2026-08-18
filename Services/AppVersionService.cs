using System.Net.Http.Json;

namespace AppVendedores2025.Services;

public class AppVersionDto
{
    public string VersionMinima { get; set; } = "";
    public string VersionActual { get; set; } = "";
    public bool ActualizacionObligatoria { get; set; }
    public string Mensaje { get; set; } = "";
    public string LinkAndroid { get; set; } = "";
    public string LinkIos { get; set; } = "";
}

public class AppVersionService
{
    private readonly ParametrosServices _oParam = new();

    public async Task<AppVersionDto?> GetVersionRequeridaAsync()
    {
        try
        {
            var urlBase = _oParam.urlApi;
            if (string.IsNullOrWhiteSpace(urlBase))
            {
                Console.Error.WriteLine("[AppVersionService] urlApi vacía, saltando validación de versión.");
                return null;
            }

            using var httpClient = HttpHelper.CreateClient();
            httpClient.BaseAddress = new Uri(urlBase);
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            httpClient.DefaultRequestHeaders.Add(
                CleanHeader(_oParam.HeaderApiKey),
                CleanHeader(_oParam.ApiKey));

            return await httpClient.GetFromJsonAsync<AppVersionDto>("AppVersion");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AppVersionService.GetVersionRequeridaAsync] {ex.Message}");
            return null;
        }
    }

    private static string CleanHeader(string? value)
        => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
}

using System.Net.Http.Json;

namespace APP_Preparadores.Servicios
{
    public class GetToken
    {
        public static async Task<string?> ObtenerTokenJwtAsync(string urlBase)
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlBase)
            };

            var response = await httpClient.GetAsync("auth/token");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            if (json != null && json.TryGetValue("token", out var token))
            {
                return token;
            }

            return null;
        }
    }
}

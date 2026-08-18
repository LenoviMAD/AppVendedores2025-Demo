using System.Net.Http;
using System.Net.Http.Headers;

namespace AppVendedores2025.Services
{
    /// <summary>
    /// Helper estático para obtener HttpClient desde IHttpClientFactory
    /// sin necesidad de inyección de dependencias en el constructor.
    /// Resuelve IHttpClientFactory desde el contenedor de servicios de MAUI.
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        /// Crea un HttpClient usando IHttpClientFactory resuelto desde el service provider de MAUI.
        /// Si no se puede resolver (ej: en pruebas unitarias), crea un HttpClient directo como fallback.
        /// Si hay un JWT guardado (login), lo agrega como Authorization: Bearer.
        /// </summary>
        public static HttpClient CreateClient()
        {
            HttpClient client;
            try
            {
                var factory = App.Current?.Handler?.MauiContext?.Services?.GetService<IHttpClientFactory>();
                client = factory != null ? factory.CreateClient() : new HttpClient();
            }
            catch
            {
                // Fallback silencioso si el contenedor no está disponible
                System.Diagnostics.Debug.WriteLine("[HttpHelper] IHttpClientFactory no disponible, usando HttpClient directo.");
                client = new HttpClient();
            }

            AttachBearerToken(client);
            return client;
        }

        public static void AttachBearerToken(HttpClient client)
        {
            try
            {
                var token = new ParametrosServices().JwtToken;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch
            {
                // Sin JWT guardado (ej: antes del primer login) — la llamada sigue sin Bearer.
            }
        }
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class LogAccionAppService
    {
        public static async Task TransmitirLogoutAsync(string urlApi, bool esCliente, int clienteId, string vendedorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(urlApi)) return;

                string idToLog = esCliente ? clienteId.ToString() : vendedorId;
                
                if (string.IsNullOrWhiteSpace(idToLog)) return;

                var urlApiLogout = $"{urlApi}VendedorItem/Logout/{idToLog}";
                
                // Fire and forget mechanism wrapped safely inside a Task.Run
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                        await httpClient.GetAsync(urlApiLogout);
                    }
                    catch
                    {
                        // Ignoramos fallos de red en el salir
                    }
                });
            }
            catch
            {
                // Ignorar errores generales del logoff HTTP para no entorpecer el proceso de cierre local
            }
        }
    }
}

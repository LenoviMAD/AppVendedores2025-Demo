using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using AppVendedores2025.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace AppVendedores2025.Services
{
    public class LocationService
    {
        private readonly IGeolocation geolocation;

        private CancellationTokenSource cancellationTokenSource;

        public double Latitud { get; set; } = 99.9999;
        public double Longitud { get; set; } = 99.9999;

        public LocationService(IGeolocation geolocation)
        {
            this.geolocation = geolocation;
        }

        public async Task StartTrackingLocationAsync(string NombreUsuario, string ClaveUsuario)
        {
            cancellationTokenSource = new CancellationTokenSource();

            var locationRequest = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));

            // Iniciar la tarea de ubicación en segundo plano
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var location = await geolocation.GetLocationAsync(locationRequest, cancellationTokenSource.Token);

                    if (location != null)
                    {
                        Latitud = location.Latitude;
                        Longitud = location.Longitude;

                        await SaveLocation(NombreUsuario, ClaveUsuario, location.Latitude, location.Longitude);

                    }
                    else
                    {
                        Console.WriteLine("Ubicación no disponible.");
                    }
                }
                catch (FeatureNotSupportedException fnsEx)
                {
                    Console.WriteLine("GPS no soportado en este dispositivo: " + fnsEx.Message);
                }
                catch (PermissionException pEx)
                {
                    Console.WriteLine("Permiso de ubicación denegado: " + pEx.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener ubicación: " + ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(30)); // Esperar antes de la siguiente actualización

            }
        }

        public void StopTrackingLocation()
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task SaveLocation(string NombreUsuario, string ClaveUsuario, double latitude, double longitude)
        {
            var gpsService = new GPSService();

            UsuarioGPSx usuarioGPSx = new UsuarioGPSx();

            if (NombreUsuario == "" || ClaveUsuario == "")
            {
                //var ultimoUsuarioLogueado = await gpsService.GetUltimoUsuarioLogueadoGPS();
                var ultimoUsuarioLogueado = await gpsService.GetUltimoUsuarioLogueado();

                if (ultimoUsuarioLogueado != null)
                {
                    usuarioGPSx.NombreUsuario = ultimoUsuarioLogueado.NombreUsuarioLogueado;
                    usuarioGPSx.ClaveUsuario = ultimoUsuarioLogueado.ClaveUsuarioLogueado;
                }

            }
            else
            {
                usuarioGPSx.NombreUsuario = NombreUsuario;
                usuarioGPSx.ClaveUsuario = ClaveUsuario;
            }


            usuarioGPSx.Latitude = latitude;
            usuarioGPSx.Longitude = longitude;


            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                await gpsService.AddUsuarioGPS(usuarioGPSx);

                //if(usuarioGPSx.NombreUsuario != "SIN INICIO SESSION" && usuarioGPSx.NombreUsuario.StartsWith("CD"))
                if (usuarioGPSx.NombreUsuario.StartsWith("CD"))
                {
                    var resultadoAddServidor = await AddSincronizarDatosServidor(usuarioGPSx);

                    if (resultadoAddServidor == 1)
                    {
                        //var resultadoDeleteUsuario = gpsService.DeleteUsuarioGPS(usuarioGPSx.IDUsuarioGPSx);
                    }

                }

            }
            else
            {
                Console.WriteLine("Sin conexion a internet");

                PuntosSinTxServidor oPuntosSinTxServidor = new PuntosSinTxServidor();

                oPuntosSinTxServidor.NombreUsuario = usuarioGPSx.NombreUsuario;
                oPuntosSinTxServidor.ClaveUsuario = usuarioGPSx.ClaveUsuario;
                oPuntosSinTxServidor.Latitud = usuarioGPSx.Latitude;
                oPuntosSinTxServidor.Longitud = usuarioGPSx.Longitude;
                oPuntosSinTxServidor.GPSTime = usuarioGPSx.GPSTimeDispositivo;

                await gpsService.AddPuntosSinTx(oPuntosSinTxServidor);


            }

        }


        public async Task<int> AddSincronizarDatosServidor(UsuarioGPSx usuarioGPSx)
        {
            try
            {
                //luego de agregar a la tabla, eliminar el registro del sqlite
                var gpsService = new GPSService();

                using HttpClient client = HttpHelper.CreateClient();

                // URL de la API

                string urlApi = Parametros.ApiURL_Fleteros_POST_PuntosGPS;
                string url = urlApi + usuarioGPSx.ClaveUsuario; //https://localhost:5101/Fleteros/PuntosGPS/lola

                string jsonUsuarioGPSx = JsonSerializer.Serialize(usuarioGPSx);
                StringContent content = new StringContent(jsonUsuarioGPSx, Encoding.UTF8, "application/json");

                HttpResponseMessage responsex = await client.PostAsync(url, content);

                if (responsex.IsSuccessStatusCode)
                {
                    string responseBody = await responsex.Content.ReadAsStringAsync();
                    //Console.WriteLine("Respuesta de la API:");
                    //Console.WriteLine(responseBody);

                    return 1;

                }
                else
                {
                    Console.WriteLine($"Error al llamar a la API. Código de estado: {responsex.StatusCode}");

                    return -1;

                }

                //----



            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al sincronizar con el servidor: " + ex.Message);
                return -1;

            }

        }


        public async Task<List<(Double Latitud, Double Longitud)>> GetLatitudYLongitudUsuarioGPS()
        {
            List<(Double Latitud, Double Longitud)> lsLatitudYLongitud = new List<(Double Latitud, Double Longitud)>();

            var locationRequest = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(0));

            var location = await geolocation.GetLocationAsync(locationRequest, cancellationTokenSource.Token);

            if (location != null)
            {
                lsLatitudYLongitud.Add((Latitud: location.Latitude, Longitud: location.Longitude));

                return lsLatitudYLongitud;
            }

            lsLatitudYLongitud.Add((Latitud: 99.9999d, Longitud: 99.9999d));

            return lsLatitudYLongitud;
        }

    }


}

using CoreLocation;
using System.Diagnostics;
using AppVendedores2025.Services;
using EntidadesAppVendedores;
using UIKit;

namespace AppVendedores2025.Platforms.iOS
{
    // En iOS no existen los Foreground Services de Android.
    // El equivalente es CLLocationManager con AllowsBackgroundLocationUpdates = true
    // + UIBackgroundModes: location en Info.plist.
    // Cuando llega una actualización iOS despierta el proceso y llama LocationsUpdated,
    // donde grabamos y transmitimos el punto via GpsTrackingService.
    public class ForegroundLocationService : CLLocationManagerDelegate
    {
        private readonly CLLocationManager _locationManager;
        private readonly GpsTrackingService _gpsService;

        public ForegroundLocationService(IGpsTrackingService gpsService)
        {
            _gpsService = (GpsTrackingService)gpsService;

            _locationManager = new CLLocationManager
            {
                Delegate = this,
                DesiredAccuracy = CLLocation.AccuracyBest,
                AllowsBackgroundLocationUpdates = true,
                PausesLocationUpdatesAutomatically = false,
                // Muestra el indicador azul en la barra de estado; iOS es menos agresivo
                // al pausar actualizaciones cuando este indicador está visible.
                ShowsBackgroundLocationIndicator = true,
                ActivityType = CLActivityType.Other
            };
        }

        public void StartLocationUpdates()
        {
            Debug.WriteLine($"[iOS GPS] Estado de permisos: {CLLocationManager.Status}");
            _locationManager.RequestAlwaysAuthorization();
            _locationManager.StartUpdatingLocation();
            Debug.WriteLine("[iOS GPS] StartUpdatingLocation iniciado");
        }

        public void StopLocationUpdates()
        {
            _locationManager.StopUpdatingLocation();
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            var location = locations.LastOrDefault();
            if (location == null) return;

            var ubicacion = new Ubicacion
            {
                latitud   = location.Coordinate.Latitude,
                longitud  = location.Coordinate.Longitude,
                Accuracy  = (decimal)location.HorizontalAccuracy,
                fecha     = ((DateTime)location.Timestamp).ToLocalTime()
            };

            Debug.WriteLine($"[iOS GPS] Punto recibido: {ubicacion.latitud:F5},{ubicacion.longitud:F5} @ {ubicacion.fecha:HH:mm:ss}");

            // BeginBackgroundTask extiende el tiempo de ejecución en segundo plano
            // para que el HTTP call tenga tiempo de completarse antes de que iOS
            // suspenda el proceso (sin esto, la transmisión puede ser cortada a mitad).
            nint bgTaskId = UIApplication.SharedApplication.BeginBackgroundTask(
                "GrabarUbicacion",
                () => Debug.WriteLine("[iOS GPS] Background task expiró antes de terminar transmisión"));

            Task.Run(async () =>
            {
                try
                {
                    await _gpsService.GrabarUbicacionV2(ubicacion, null);
                    Debug.WriteLine("[iOS GPS] GrabarUbicacionV2 OK");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[iOS GPS] Error al grabar ubicación: {ex.Message}");
                }
                finally
                {
                    UIApplication.SharedApplication.EndBackgroundTask(bgTaskId);
                }
            });
        }

        public override void Failed(CLLocationManager manager, Foundation.NSError error)
        {
            Debug.WriteLine($"[iOS GPS] CLLocationManager error: {error.LocalizedDescription}");
        }

        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        {
            Debug.WriteLine($"[iOS GPS] Autorización cambiada a: {status}");
            if (status == CLAuthorizationStatus.AuthorizedAlways)
                Debug.WriteLine("[iOS GPS] Permiso 'Siempre' concedido — segundo plano activo");
            else if (status == CLAuthorizationStatus.AuthorizedWhenInUse)
                Debug.WriteLine("[iOS GPS] Solo 'Al usar la app' — el segundo plano NO recibirá actualizaciones");
        }
    }
}

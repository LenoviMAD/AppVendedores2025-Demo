using CoreLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppVendedores2025.Services;

namespace AppVendedores2025Demo.Platforms.iOS
{
    public class LocationService : CLLocationManagerDelegate
    {
        private CLLocationManager _locationManager;

        private readonly IGeolocation geolocation;
        private AppVendedores2025.Services.LocationService locationService;

        public LocationService()
        {
            _locationManager = new CLLocationManager();
            _locationManager.Delegate = this;
            _locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
            _locationManager.AllowsBackgroundLocationUpdates = true;
            _locationManager.PausesLocationUpdatesAutomatically = false;

            this.locationService = new AppVendedores2025.Services.LocationService(geolocation);

            ////this.geolocation = geolocation;
            //this.geolocation = Geolocation.Default;
            //this.locationService = new LocationService(geolocation);
        }

        public void StartLocationUpdates()
        {
            _locationManager.RequestAlwaysAuthorization();
            _locationManager.StartUpdatingLocation();
        }

        public void StopLocationUpdates()
        {
            //_locationManager.StopUpdatingLocation();
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            //Services.LocationService oLocationService = new Services.LocationService(geolocation);

            var location = locations.LastOrDefault();
            if (location != null)
            {
                // Aquí puedes manejar la ubicación obtenida
                Console.WriteLine($"Latitud: {location.Coordinate.Latitude}, Longitud: {location.Coordinate.Longitude}");

                // Ejecuta la lógica de rastreo en segundo plano
                Task.Run(async () =>
                {
                    await locationService.StartTrackingLocationAsync("SIN INICIO SESSION", "SIN INICIO SESSION");
                });

                // Mostrar notificación
                var notificationService = new NotificationService();
                notificationService.ShowNotification("Ubicación Actualizada", $"Latitud: {location.Coordinate.Latitude}, Longitud: {location.Coordinate.Longitude}");
            }
        }
    }
}

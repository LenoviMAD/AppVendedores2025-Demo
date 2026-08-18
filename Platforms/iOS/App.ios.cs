namespace AppVendedores2025
{
    public partial class App
    {
        private AppVendedores2025.Platforms.iOS.ForegroundLocationService? _iosGps;

        partial void OnSleepPlatform()
        {
            _iosGps ??= new AppVendedores2025.Platforms.iOS.ForegroundLocationService(_gpsService);
            _iosGps.StartLocationUpdates();
            _ = _gpsService.StopAsync();
        }

        partial void OnResumePlatform()
        {
            _iosGps?.StopLocationUpdates();
        }
    }
}

using Android.Content;

namespace AppVendedores2025
{
    public partial class App
    {
        partial void OnSleepPlatform()
        {
            try
            {
                var intent = new Intent(
                    Android.App.Application.Context,
                    typeof(AppVendedores2025.Platforms.Android.LocationForegroundService));
                Android.App.Application.Context.StartForegroundService(intent);
            }
            catch (Java.Lang.IllegalStateException ex)
            {
                // Android 12+: ForegroundServiceStartNotAllowedException cuando el timing
                // de OnSleep coincide con que el proceso ya está considerado background.
                System.Diagnostics.Debug.WriteLine($"[App] Foreground service no permitido: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error en OnSleepPlatform: {ex.Message}");
            }
        }

        partial void OnResumePlatform()
        {
            try
            {
                var intent = new Intent(
                    Android.App.Application.Context,
                    typeof(AppVendedores2025.Platforms.Android.LocationForegroundService));
                Android.App.Application.Context.StopService(intent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error en OnResumePlatform: {ex.Message}");
            }
        }
    }
}

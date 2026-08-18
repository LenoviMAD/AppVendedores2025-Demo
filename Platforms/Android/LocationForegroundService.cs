using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace AppVendedores2025.Platforms.Android
{
    // Servicio keepalive: mantiene el proceso vivo en background para que el
    // loop de GpsTrackingService (Task.Run) siga ejecutándose.
    // No hace GPS propio; toda la lógica de ubicación está en GpsTrackingService.
    [Service(ForegroundServiceType = ForegroundService.TypeLocation)]
    public class LocationForegroundService : Service
    {
        private const string ChannelId = "gps_keepalive_channel";
        private const int NotificationId = 10;

        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            try
            {
                MostrarNotificacion();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Error al iniciar: {ex.Message}");
                StopSelf();
            }
            // NotSticky: si Android mata el servicio no lo reinicia automáticamente,
            // evitando el bucle de crashes "continúa fallando".
            return StartCommandResult.NotSticky;
        }

        public override void OnDestroy()
        {
            try { StopForeground(StopForegroundFlags.Remove); } catch { }
            base.OnDestroy();
        }

        private void MostrarNotificacion()
        {
            var manager = (NotificationManager)GetSystemService(NotificationService)!;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "GPS activo", NotificationImportance.Low)
                {
                    Description = "El servicio de GPS está enviando tu ubicación."
                };
                manager.CreateNotificationChannel(channel);
            }

            var notification = new Notification.Builder(this, ChannelId)
                .SetContentTitle("GPS activo")
                .SetContentText("Enviando ubicación en segundo plano.")
                .SetSmallIcon(Resource.Drawable.logo)
                .SetOngoing(true)
                .Build();

            try
            {
                // Android 10+ (Q) requiere el tipo; Android 14+ (UpsideDownCake) lanza
                // SecurityException si el permiso de ubicación no está concedido.
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                    StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
                else
                    StartForeground(NotificationId, notification);
            }
            catch (Exception)
            {
                // Fallback sin tipo: muestra la notificación pero sin vinculación a location.
                try { StartForeground(NotificationId, notification); }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] StartForeground falló: {ex2.Message}");
                    StopSelf();
                }
            }
        }
    }
}
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using AppVendedores2025Demo.Models;
using AppVendedores2025Demo.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;


namespace AppVendedores2025Demo.Platforms.Android
{
    [Service(Exported = true)]
    public class ForegroundService : Service
    {
        private const int ServiceNotificationId = 100;

        private readonly IGeolocation geolocation;
        private LocationService locationService;

        public ForegroundService()
        {
            //this.geolocation = geolocation;
            this.geolocation = Geolocation.Default;
            this.locationService = new LocationService(geolocation);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            // Configura la notificación
            var channelId = "foreground_service_channel";
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Servicio en primer plano", NotificationImportance.Low)
                {
                    Description = "Canal para notificaciones del servicio en primer plano."
                };
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }

            var notification = new Notification.Builder(this, channelId)
                .SetContentTitle("Servicio activo")
                .SetContentText("La aplicación está rastreando tu ubicación.")
                .SetSmallIcon(Resource.Drawable.ic_arrow_back_black_24) 
                .SetOngoing(true)
                .Build();

            //StartForeground(ServiceNotificationId, notification);

            try
            {
                StartForeground(ServiceNotificationId, notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar el servicio en primer plano: OnCreate " + ex.Message);
            }


        }

        //public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        //{
        //    // Ejecuta la lógica de rastreo en segundo plano
        //    Task.Run(async () =>
        //    {
        //        await locationService.StartTrackingLocationAsync("SIN INICIO SESSION", "SIN INICIO SESSION");
        //    });

        //    // Devuelve el resultado apropiado

        //    //await Task.Delay(TimeSpan.FromSeconds(30)); // Esperar antes de la siguiente actualización

        //    return StartCommandResult.Sticky;
        //}

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var channelId = "foreground_service_channel";

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Servicio en primer plano", NotificationImportance.Low)
                {
                    Description = "Canal para notificaciones del servicio en primer plano."
                };
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }

            var notification = new Notification.Builder(this, channelId)
                .SetContentTitle("Servicio activo")
                .SetContentText("La aplicación está rastreando tu ubicación.")
                .SetSmallIcon(Resource.Drawable.ic_mtrl_chip_checked_circle)
                .SetOngoing(true)
                .Build();

            //StartForeground(ServiceNotificationId, notification);

            try
            {
                StartForeground(ServiceNotificationId, notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar el servicio en primer plano: OnStartCommand " + ex.Message);
            }


            // Ejecutar rastreo en segundo plano
            Task.Run(async () =>
            {
                await locationService.StartTrackingLocationAsync("SIN INICIO SESSION", "SIN INICIO SESSION");
            });

            return StartCommandResult.Sticky;
        }


        public override IBinder OnBind(Intent intent) => null;

        public override void OnDestroy()
        {
            base.OnDestroy();
            StopForeground(true); // Detener el servicio en primer plano al destruirlo
        }


        public static bool IsServiceRunning(Context context, Type serviceClass)
        {
            var activityManager = (ActivityManager)context.GetSystemService(Context.ActivityService);
            foreach (var service in activityManager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Equals(serviceClass.Name))
                {
                    return true;
                }
            }
            return false;
        }


    }
}

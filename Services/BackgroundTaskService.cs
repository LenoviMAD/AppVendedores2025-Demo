#if ANDROID
using Android.Content;
#endif
using Microsoft.Extensions.Hosting;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace AppVendedores2025Demo.Services
{
    public class BackgroundTaskService : IHostedService
    {
        private readonly IGeolocation geolocation;


        //cerra app y volves a ingresar, se queda trabado, este metodo es el problema
        //        public async Task StartAsync(CancellationToken cancellationToken)
        //        {
        //            Console.WriteLine("Ejecutando StartAsync background service, hora: " + DateTime.Now);

        //#if ANDROID
        //            var context = Android.App.Application.Context;
        //            var intent = new Intent(context, typeof(Platforms.Android.ForegroundService));
        //            context.StartForegroundService(intent); // Iniciar el servicio en primer plano
        //#endif

        //            // Ejecutar tareas en segundo plano si es necesario
        //            await Task.CompletedTask;
        //        }

 
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Ejecutando StartAsync background service, hora: " + DateTime.Now);

#if ANDROID
            var context = Android.App.Application.Context;

            try
            {
                Console.WriteLine("Star Servucio seg plano");

                var activityManager = (Android.App.ActivityManager)context.GetSystemService(Context.ActivityService);

                // Obtener la lista de servicios en ejecución
                var runningServices = activityManager.GetRunningServices(int.MaxValue);
                Console.WriteLine("Servicios Corriendo");

                foreach ( var itms in runningServices)
                {
                    

                    var name =itms.Service.ClassName;
                    Console.WriteLine(name);

                }
                Console.WriteLine("----------------");

                // Verificar si el servicio ya está en ejecución
                var isServiceRunning = runningServices.Any(service =>
                    service.Service.ClassName == typeof(AppVendedores2025Demo.Platforms.Android.ForegroundService).FullName);

                if (!isServiceRunning)
                {
                    // Si no está en ejecución, iniciar el servicio en primer plano
                    var intent = new Intent(context, typeof(Platforms.Android.ForegroundService));
                    context.StartForegroundService(intent);
                }
                else
                {
                    Console.WriteLine("El servicio en primer plano ya está en ejecución.");

                    // Detener el servicio en primer plano
                    var stopTokenSource = new CancellationTokenSource();
                    await StopAsync(stopTokenSource.Token);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar o iniciar el servicio: {ex.Message}");
            }
#endif

            // Ejecutar tareas en segundo plano si es necesario
            await Task.CompletedTask;
        }





        public Task StopAsync(CancellationToken cancellationToken)
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(Platforms.Android.ForegroundService));
            context.StopService(intent); // Detener el servicio en primer plano
#endif

            return Task.CompletedTask;
        }




    }
}

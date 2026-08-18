using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UserNotifications;

namespace AppVendedores2025Demo.Platforms.iOS
{
    public class NotificationService
    {
        public void ShowNotification(string title, string message)
        {
            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = message,
                Sound = UNNotificationSound.Default
            };

            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
            var request = UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), content, trigger);

            UNUserNotificationCenter.Current.AddNotificationRequest(request, (error) =>
            {
                if (error != null)
                {
                    Console.WriteLine($"Error al mostrar la notificación: {error.LocalizedDescription}");
                }
            });
        }

        public void RequestNotificationPermission()
        {
            UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert, (granted, error) =>
            {
                if (granted)
                {
                    Console.WriteLine("Permiso para notificaciones concedido.");
                }
                else
                {
                    Console.WriteLine("Permiso para notificaciones denegado.");
                }
            });
        }
    }
}

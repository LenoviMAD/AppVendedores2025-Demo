#if IOS
using System;
using System.Linq;
using UIKit;
using Microsoft.Maui.Controls;

namespace AppVendedores2025
{
    public static class SafeAreaHelper
    {
        /// <summary>
        /// Calcula insets (notch/isla y home indicator) y los inyecta en Application.Current.Resources["SafeAreaPadding"].
        /// Con manejo de errores y fallbacks seguros.
        /// </summary>
        public static void TryUpdateSafeAreaPadding()
        {
            try
            {
                var app = Application.Current;
                if (app is null || app.Resources is null)
                    return;

                var window = GetKeyWindow();
                if (window is null)
                {
                    // Sin ventana aún, dejar en 0
                    SetPaddingResource(0, 0);
                    return;
                }

                var insets = window.SafeAreaInsets;

                double top = Math.Max(0, insets.Top);
                double bottom = Math.Max(0, insets.Bottom);

                if (top < 1) top = 0;
                if (bottom < 1) bottom = 0;

                SetPaddingResource(top, bottom);

                Console.WriteLine($"[SafeArea] Applied padding => top:{top} bottom:{bottom}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SafeArea] Error computing insets: {ex.Message}");
                SetPaddingResource(0, 0);
            }
        }

        private static UIWindow? GetKeyWindow()
        {
            try
            {
                var window = UIApplication.SharedApplication
                    .ConnectedScenes
                    .OfType<UIWindowScene>()
                    .SelectMany(s => s.Windows)
                    .FirstOrDefault(w => w.IsKeyWindow);

                window ??= UIApplication.SharedApplication.KeyWindow;

                return window;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SafeArea] GetKeyWindow error: {ex.Message}");
                return null;
            }
        }

        private static void SetPaddingResource(double top, double bottom)
        {
            try
            {
                var padding = new Thickness(0, top, 0, bottom);

                if (Application.Current!.Resources.ContainsKey("SafeAreaPadding"))
                    Application.Current.Resources["SafeAreaPadding"] = padding;
                else
                    Application.Current.Resources.Add("SafeAreaPadding", padding);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SafeArea] SetPaddingResource error: {ex.Message}");
            }
        }
    }
}
#endif

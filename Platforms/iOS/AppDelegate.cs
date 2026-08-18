#if IOS
using UIKit;
using Foundation;
using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace AppVendedores2025
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();


        public override void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);

            // Aplica márgenes seguros dinámicos (para notch / isla)
            if (UIApplication.SharedApplication.KeyWindow != null)
            {
                var insets = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets;
                Console.WriteLine($"Safe area top: {insets.Top}, bottom: {insets.Bottom}");
            }


            try
            {
                #if IOS
                    SafeAreaHelper.TryUpdateSafeAreaPadding();
                #endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SafeArea] OnActivated error: {ex}");
            }

        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var ok = base.FinishedLaunching(app, options);

            try
            {
                var windowScene = UIApplication.SharedApplication
                    .ConnectedScenes
                    .OfType<UIWindowScene>()
                    .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive);

                var window = windowScene?.Windows?.FirstOrDefault(w => w.IsKeyWindow)
                             ?? UIApplication.SharedApplication.KeyWindow;

                var insets = window?.SafeAreaInsets ?? UIEdgeInsets.Zero;
                System.Diagnostics.Debug.WriteLine($"[SAFEAREA] top={insets.Top}, bottom={insets.Bottom}, left={insets.Left}, right={insets.Right}");

                // Envolver RootViewController (si no lo hiciste antes)
                if (window?.RootViewController != null &&
                    window.RootViewController is not AppVendedores2025.Platforms.iOS.StatusBarHiddenController)
                {
                    window.RootViewController =
                        new AppVendedores2025.Platforms.iOS.StatusBarHiddenController(window.RootViewController);
                    window.RootViewController.SetNeedsStatusBarAppearanceUpdate();
                }

                // ⚠️ Compensar Safe Area con insets negativos (ojo con el cast)
                var root = window?.RootViewController;
                if (root != null)
                {
                    root.AdditionalSafeAreaInsets = new UIEdgeInsets(
                        -(nfloat)insets.Top,
                        -(nfloat)insets.Left,
                        -(nfloat)insets.Bottom,
                        -(nfloat)insets.Right
                    );

                    // “Empujón” de layout por si no recalcula
                    root.SetNeedsStatusBarAppearanceUpdate();
                    root.View?.SetNeedsLayout();
                    root.View?.LayoutIfNeeded();

                    System.Diagnostics.Debug.WriteLine("[SAFEAREA-FIX] Negative insets applied.");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AppDelegate] SafeArea log/fix failed: " + ex);
            }


            return ok;
        }
    }
}
#endif

using AppVendedores2025.Services;

namespace AppVendedores2025
{
    public partial class App : Application
    {
        private readonly IGpsTrackingService _gpsService;

        public App(IGpsTrackingService gpsService)
        {
            InitializeComponent();
            _gpsService = gpsService ?? throw new ArgumentNullException(nameof(gpsService));
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "AppVendedores2025" };
        }

        protected override async void OnStart()
        {
            try
            {
                await _gpsService.StartAsync(js: null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error al iniciar GPS: {ex}");
            }
        }

        protected override void OnSleep()
        {
            try { OnSleepPlatform(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[App] Error en OnSleep: {ex}"); }
        }

        protected override async void OnResume()
        {
            try
            {
                OnResumePlatform();
                await _gpsService.StartAsync(js: null);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[App] Error al reanudar GPS: {ex}"); }
        }

        // Implementadas por cada plataforma en Platforms/Android/App.android.cs
        // y Platforms/iOS/App.ios.cs
        partial void OnSleepPlatform();
        partial void OnResumePlatform();
    }
}

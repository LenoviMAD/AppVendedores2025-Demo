using AppVendedores2025.Services;
using Blazored.SessionStorage;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using AppVendedores2025.Localization;
using AppVendedores2025.Resources.Strings;

namespace AppVendedores2025
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder(); 
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => 
                { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); }); 
            builder.Services.AddBlazoredSessionStorage(); 
            builder.Services.AddMauiBlazorWebView(); 
            #if DEBUG 
            builder.Services.AddBlazorWebViewDeveloperTools(); builder.Logging.AddDebug(); 
            #endif 
            builder.Services.AddSingleton<AppVendedores2025.Services.IProductoListaItem, AppVendedores2025.Services.ProductoListaService>(); 
            builder.Services.AddSingleton<AppVendedores2025.Services.IClienteItemService, AppVendedores2025.Services.ClienteItemService>();
            builder.Services.AddSingleton<AppVendedores2025.Services.IPedidoCabeceraService, AppVendedores2025.Services.PedidoCabeceraService>(); 
            builder.Services.AddSingleton<AppVendedores2025.Services.IPedidosDetalleItemService, AppVendedores2025.Services.PedidosDetalleItemService>(); 
            builder.Services.AddSingleton<AppVendedores2025.Services.ICombosItemService, AppVendedores2025.Services.CombosItemService>(); //PARA VERIFICAR EL CAMBIO DE PAGINA Y VERIFICAR LA SINCRONIZACION CUANDO CAMBIE DE DIA //
            //builder.Services.AddScoped<VerificadorSincronizacion>(); 
            //builder.Services.AddScoped<ParametrosServices>(); //FIN 
            // DI del servicio como Singleton
            builder.Services.AddSingleton<IGpsTrackingService, GpsTrackingService>();

            builder.Services.AddSingleton<ParametrosServices>();

            //SERVICIO PARA PREGUNTAR UBICACION EN iOS                                                              
            builder.Services.AddSingleton<PermisosUbicacionService>(); //FIN
            
            // ✅ REGISTRO del servicio que te falta
            builder.Services.AddScoped<ClienteAltaService>();

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<AppVendedores2025.Services.FuncionesSincronizar>();
            //SERVICIO PARA EL MULTILENGUAJE
            // Localización para Blazor dentro de MAUI (si usas BlazorWebView):
            LocalizationResourceManager.Instance.SetResourceManager(AppResources.ResourceManager);
            builder.Services.AddSingleton(LocalizationResourceManager.Instance);

            // Cultura inicial como ya venías haciendo
            var ci = Services.LanguageService.GetCurrentCulture();
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            try { AppResources.Culture = ci; } catch { }

            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<AppVendedores2025.Services.GPSService>(); 
            builder.Services.AddSingleton<App.TimerService>(); // Registrar el servicio MercadoPagoService
                                                               
            builder.Services.AddSingleton<AppVendedores2025.Services.MercadoPagoService>(); 
            return builder.Build();
        }
    }
}

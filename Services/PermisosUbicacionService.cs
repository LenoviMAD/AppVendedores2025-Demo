using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class PermisosUbicacionService
    {
        public async Task<bool> SolicitarUbicacionSiempreAsync()
        {
            try
            {
#if IOS
                // 1) When In Use
                var whenInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (whenInUse != PermissionStatus.Granted)
                {
                    whenInUse = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (whenInUse != PermissionStatus.Granted)
                        return false;
                }

                // 2) Always
                var always = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                if (always != PermissionStatus.Granted)
                {
                    always = await Permissions.RequestAsync<Permissions.LocationAlways>();
                    if (always != PermissionStatus.Granted)
                        return false;
                }

                await HabilitarBackgroundLocationIOSAsync(); // opcional, solo si realmente trackeás en segundo plano
                return true;
#else
                // En otras plataformas: adaptá si necesitás.
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted) return false;

                var bg = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                if (bg != PermissionStatus.Granted)
                    bg = await Permissions.RequestAsync<Permissions.LocationAlways>();

                return bg == PermissionStatus.Granted;
#endif
            }
            catch (Exception ex)
            {
                // Manejo de errores visible
                try
                {
                    await App.Current.MainPage.DisplayAlert("Error de permisos", ex.Message, "OK");
                }
                catch (Exception alertEx) { System.Diagnostics.Debug.WriteLine($"[PermisosUbicacionService] No se pudo mostrar alerta: {alertEx.Message}"); }
                return false;
            }
        }

#if IOS
        private async Task HabilitarBackgroundLocationIOSAsync()
        {
            try
            {
                var manager = new CoreLocation.CLLocationManager
                {
                    PausesLocationUpdatesAutomatically = false,
                    AllowsBackgroundLocationUpdates = true,
                    DesiredAccuracy = CoreLocation.CLLocation.AccuracyBest
                };

                // Importante: iniciar/parar updates donde corresponda, no dejarlo siempre encendido:
                // manager.StartUpdatingLocation();
            }
            catch (Exception ex)
            {
                try
                {
                    await App.Current.MainPage.DisplayAlert("Atención",
                        "No se pudo habilitar background location: " + ex.Message, "OK");
                }
                catch (Exception alertEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[PermisosUbicacionService] Error mostrando alerta: {alertEx.Message}");
                }
            }
        }
#endif
    }
}
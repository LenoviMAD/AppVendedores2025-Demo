using System.Globalization;
using Microsoft.Maui.Storage; // Preferences
using System.Diagnostics;

namespace AppVendedores2025.Services
{
    /// <summary>
    /// Servicio para gestionar el idioma de la app. Maneja persistencia y fallback.
    /// </summary>
    public static class LanguageService
    {
        private const string PrefKey = "App.Language";
        private static readonly string DefaultCulture = "es-AR";

        /// <summary>
        /// Devuelve la cultura actual (persistida o por defecto).
        /// </summary>
        public static CultureInfo GetCurrentCulture()
        {
            try
            {
                var saved = Preferences.Get(PrefKey, DefaultCulture);
                return new CultureInfo(saved);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LanguageService] Error GetCurrentCulture: {ex}");
                // Fallback seguro
                return new CultureInfo(DefaultCulture);
            }
        }

        /// <summary>
        /// Intenta cambiar la cultura. Devuelve true si se pudo; false si no.
        /// </summary>
        public static bool TrySetCulture(string cultureCode, out string? error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(cultureCode))
                {
                    error = "Código de cultura vacío.";
                    return false;
                }

                // Normaliza guiones bajos a guion medio (ej: es_AR -> es-AR)
                cultureCode = cultureCode.Replace('_', '-').Trim();

                // Valida creando la cultura
                var ci = new CultureInfo(cultureCode);

                // Cambia culturas por defecto del hilo (afecta recursos, formatos, etc.)
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;

                // Persistir
                Preferences.Set(PrefKey, ci.Name);

                // Si usás AppResources (Resx generado), setea su cultura si corresponde:
                global::AppVendedores2025.Resources.Strings.AppResources.Culture = ci;

                return true;
            }
            catch (CultureNotFoundException)
            {
                error = $"Cultura no válida: {cultureCode}";
                return false;
            }
            catch (Exception ex)
            {
                error = $"No se pudo aplicar la cultura: {ex.Message}";
                return false;
            }
        }
    }
}

using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls;
using System;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using AppVendedores2025.Services;
using System.Diagnostics;
using System.Globalization;

#if ANDROID
using Android.Webkit;
#endif
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif
#if IOS
using WebKit;
#endif

namespace AppVendedores2025
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            On<iOS>().SetUseSafeArea(true);

            // Si el Designer aún no generó AppResources, no rompemos la UI.
            try
            {
                Title = global::AppVendedores2025.Resources.Strings.AppResources.Titulo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MainPage] AppResources no disponible aún: " + ex.Message);
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = GetBodyBgAndApplyToPageAsync();
            try
            {
#if IOS                                                                                                                                                     
                SafeAreaHelper.TryUpdateSafeAreaPadding();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SafeArea] OnAppearing error: {ex}");
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            try
            {
#if IOS
                SafeAreaHelper.TryUpdateSafeAreaPadding();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SafeArea] OnSizeAllocated error: {ex}");
            }
        }

        // === WEB → MAUI: leer background del <body> y aplicarlo a la Page ===
        private async System.Threading.Tasks.Task GetBodyBgAndApplyToPageAsync()
        {
            try
            {
                const string js = "(function(){return getComputedStyle(document.body).backgroundColor;})();";
                string? css = null;

#if IOS
                if (Bwv?.Handler?.PlatformView is WKWebView wk)
                {
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<string?>();
                    wk.EvaluateJavaScript(js, (result, error) =>
                    {
                        if (error is not null) tcs.TrySetException(new Exception(error.LocalizedDescription));
                        else tcs.TrySetResult(result?.ToString());
                    });
                    css = await tcs.Task.ConfigureAwait(false);
                }
#elif WINDOWS
                if (Bwv?.Handler?.PlatformView is WebView2 wv2 && wv2.CoreWebView2 is not null)
                {
                    css = await wv2.CoreWebView2.ExecuteScriptAsync(js);
                }
#elif ANDROID
                if (Bwv?.Handler?.PlatformView is Android.Webkit.WebView awv)
                {
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<string?>();
                    awv.EvaluateJavascript(js, new JsValueCallback(
                        v => tcs.TrySetResult(v),
                        e => tcs.TrySetException(e)));
                    css = await tcs.Task.ConfigureAwait(false);
                }
#endif

                var mauiColor = ParseCssColor(css);
                if (mauiColor != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { this.BackgroundColor = mauiColor; }
                        catch (Exception ex) { Debug.WriteLine("[BodyBG] Aplicando color a Page: " + ex); }
                    });
                }
                else
                {
                    Debug.WriteLine("[BodyBG] No se pudo parsear color CSS: " + css);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[BodyBG] Falló GetBodyBgAndApplyToPageAsync: " + ex);
            }
        }

        // Parse "rgb(...)" o "rgba(...)" → Color
        private static Microsoft.Maui.Graphics.Color? ParseCssColor(string? css)
        {
            if (string.IsNullOrWhiteSpace(css)) return null;
            try
            {
                css = css.Trim().Trim('"');
                var open = css.IndexOf('(');
                var close = css.IndexOf(')');
                if (open < 0 || close <= open) return null;

                var parts = css.Substring(open + 1, close - open - 1).Split(',');
                if (parts.Length < 3) return null;

                byte r = byte.Parse(parts[0].Trim());
                byte g = byte.Parse(parts[1].Trim());
                byte b = byte.Parse(parts[2].Trim());

                float a = 1f;
                if (parts.Length >= 4)
                    a = (float)double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);

                return Microsoft.Maui.Graphics.Color.FromRgba(r / 255f, g / 255f, b / 255f, a);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[BodyBG] ParseCssColor error: " + ex + " css=" + css);
                return null;
            }
        }

#if ANDROID
        // Helper para EvaluateJavascript en Android
        private sealed class JsValueCallback : Java.Lang.Object, IValueCallback
        {
            private readonly Action<string?> _ok;
            private readonly Action<Exception> _err;
            public JsValueCallback(Action<string?> ok, Action<Exception> err) { _ok = ok; _err = err; }
            public void OnReceiveValue(Java.Lang.Object? value)
            {
                try { _ok(value?.ToString()); }
                catch (Exception ex) { _err(ex); }
            }
        }
#endif

        private async void OnAceptarClicked(object sender, EventArgs e)
        {
            try
            {
                await DisplayAlert(
                    global::AppVendedores2025.Resources.Strings.AppResources.Titulo,
                    global::AppVendedores2025.Resources.Strings.AppResources.Bienvenido,
                    global::AppVendedores2025.Resources.Strings.AppResources.Aceptar
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnAceptarClicked error: {ex}");             
                // Si querés, mostrás un fallback:
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void ApplyLang(string code)
        {
            try
            {
                if (!LanguageService.TrySetCulture(code, out var error))
                {
                    Debug.WriteLine($"[Lang] {error}");
                    // Feedback mínimo en UI:
                    DisplayAlert("Idioma", error ?? "No se pudo cambiar el idioma.", "OK");
                    return;
                }

                // Refrescar título (si usás binding reactivo, esto sería opcional)
                Title = global::AppVendedores2025.Resources.Strings.AppResources.Titulo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MainPage] ApplyLang AppResources error: " + ex.Message);
                DisplayAlert("Idioma", ex.Message, "OK");
            }
        }

        //private void OnEsClicked(object sender, EventArgs e) => ApplyLang("es-AR");
        //private void OnEnClicked(object sender, EventArgs e) => ApplyLang("en");
        //private void OnPtClicked(object sender, EventArgs e) => ApplyLang("pt-BR");
    }
}

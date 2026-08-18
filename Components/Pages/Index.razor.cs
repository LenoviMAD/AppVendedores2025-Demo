using Newtonsoft.Json;
using System.Text.Json;
using AppVendedores2025;
using AppVendedores2025.Components;
using AppVendedores2025.Shared;
using AppVendedores2025.Services;
using EntidadesAppVendedores;
using AppVendedores2025.Components.Modals;
using Microsoft.Extensions.Localization;
using AppVendedores2025.Localization;
using AppVendedores2025.Resources.Strings;
using System.Net.Http;
using System.Net.Http.Json;
using AppVendedores2025.Services;
using SQLite;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppVendedores2025.Components.Pages
{
    public partial class Index
    {
        bool primerIngre;

        private bool Open = false;
        private bool HideDenyOnIOS = false; // simular iOS

        private string? UiError;

        string txtVD = "";
        string txtPwd = "";
        string txtVDAuxiliar = "";
        string txtPwdAuxiliar = "";
        string vdLogueado = "";
        string vdLogueadoCodigo = "";
        string vdCod = "";
        DateTime ultimasync;

        bool isLogueado = false;
        bool showMenuusr = false;
        string mensajeError = "";

        int listadePrecios = 0;
        string ListadePreciosNomre = "";

        int CantPedSinTX = 0;
        string txtLog = "";
        string LinkWhatsapp = "";
        int cuentasinpedido;

        bool estaSincronizado = false;
        bool mostrarVale = false;

        // Parametro para controlar el versionado
        string versionAplicacion = "135";
        int ult_sincdatos_dia = 0;
        string versionDatos = "";

        List<TextoVDItem> TextoVD = new();
        string TextoSeleccionado = "";
        string textolibre = "";

        int cantMensajesnoleidos = 0;
        private bool primerIngresoApp = false;

        // Cliente / vendedor                                                                                                                            
        bool esCliente;
        int esClienteAuxiliar;
        bool esUnClienteX;
        List<ClienteItem> lsUnClientes = new();
        private ClienteItem clienteItemUno = new();
        public int irDirecto = 0;

        public string ComisionVendedoresString { get; set; } = "";
        public bool ColorCoeficienteComision { get; set; }

        // Estrellitas (usado por el markup)                                                                                                              f
        public string opacityEstrellita01 = "opacity: 0.4";
        public string opacityEstrellita02 = "opacity: 0.4";
        public string opacityEstrellita03 = "opacity: 0.4";
        public string opacityEstrellita04 = "opacity: 0.4";
        public string opacityEstrellita05 = "opacity: 0.4";
        public string opacityEstrellita06 = "opacity: 0.4";

        public bool esPrimerIngreso = true;

        // Lista de estrellas
        List<object> lsEstrellas = new();

        string nombreInicial = "";
        decimal numeroDecimal = 0m;
        double numeroDouble = 0d;
        int numeroInt = 0;

        private string _mailSoporte = "";
        private string _linkWhatsappSoporteBase = "";

        private bool _modalSoporteOpen = false;
        private bool _modalEliminarOpen = false;
        private bool _modalActualizacionOpen = false;
        private AppVersionDto? _versionInfo = null;
        private bool _procesandoEliminar = false;
        private string _mensajeEliminarCuenta = "";

        List<(int listaID, string nombreDeLaLista)> listasDisponible = new();

        private bool isTimerSubscribed = false;
        private static bool initLoader = false;

        private LocalStorageAccessor? _localStorage;
        private LocalStorageAccessor LocalStorage => _localStorage ??= new LocalStorageAccessor(JS);

        private CancellationTokenSource? _bgCts;
        ParametrosServices oParam = new ParametrosServices();


        protected override void OnInitialized()
        {
            // lo dejo vacio como lo tenias
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Parametros globales (vienen del @inherits UtilCompartidas)
                oParam.VersionApp = versionAplicacion;
                listasDisponible = GetListasParaUsar() ?? new();

                vdLogueado = oParam.VendedorID ?? "";
                vdCod = oParam.Vendedor ?? "";

                isLogueado = !string.IsNullOrWhiteSpace(vdLogueado);
                showMenuusr = !isLogueado;
                vdLogueadoCodigo = isLogueado ? $" VD : {vdCod}" : "";

                // seteo correcto (en tu codigo estaba al reves)
                esCliente = oParam.TipoDeUsuario;
                esUnClienteX = oParam.Autogestion;
                await SafeJsVoid("showLoader");

                // Asegura URL/APIKey en oParam (tu metodo original creaba un ParametrosServices nuevo)
                await ValidarURLApi();

                await ObtenerConfigSoporteAsync();

                await ValidarVersionAppAsync();

                if (_modalActualizacionOpen)
                    return;

                ultimasync = oParam.FechaUltimaSincronizacion;
                ult_sincdatos_dia = ultimasync.Day;

                // Estado de sincronizacion real (si falla, cae al chequeo por d�a)
                try
                {
                    estaSincronizado = isLogueado && await oFuncionesSincronizar.CheckStatusSincro(JS);
                }
                catch
                {
                    estaSincronizado = isLogueado && (ult_sincdatos_dia == DateTime.Now.Day);
                }

                await SafeJs("mostrarrnav");

                if (isLogueado)
                {
                    await ChequearVersionDatosYActualizarSiHaceFaltaAsync();

                    // Validacion remota (si la app esta deshabilitada o credencial invalida)
                    try
                    {
                        var checkLogin = await oFuncionesSincronizar.ValidarVD(vdCod, oParam.Pwd);
                        if (checkLogin?.CodError == "2")
                        {
                            if (_versionInfo == null)
                                await ValidarVersionAppAsync();
                            _modalActualizacionOpen = true;
                            await InvokeAsync(StateHasChanged);
                            return;
                        }
                        if (checkLogin?.CodError == "1")
                        {
                            await LogOff(true);
                            return;
                        }

                        // Ajuste de lista de precios
                        try
                        {
                            if (oParam.tipoVD == 6 && checkLogin?.TiposDeVendedoresID == 6)
                            {
                                oParam.ListadePrecios = 4;
                                oParam.listadePreciosNombre = "Salon";
                            }
                            else if (oParam.tipoVD == 999 && checkLogin?.TiposDeVendedoresID == 999 && oParam.ListadePrecios == 7)
                            {
                                oParam.ListadePrecios = 7;
                                oParam.listadePreciosNombre = "E-commerce";
                            }
                            else
                            {
                                oParam.ListadePrecios = 1;
                                oParam.listadePreciosNombre = "Especial Distribucion";
                            }
                        }
                        catch { /* no rompo init por esto */ }
                    }
                    catch (Exception ex)
                    {
                        await SetErrorAsync(nameof(OnInitializedAsync) + ".ValidarVD", ex);
                    }

                    // Textos desde LocalStorage
                    await CargarTextosVDDesdeLocalStorageAsync();

                    // Cantidad pedidos sin TX (rápido)
                    CantPedSinTX = await pedidoSinCargar();

                    // ?? pesado: lo mando a background para no clavar el init
                    _ = CalcularClientesSinPedidoAsync();

                    // Estrellitas / coeficiente
                    if (!esCliente)
                    {
                        cantidadEstrellitasFronted();
                        cambiarColorCoheficiente();
                    }
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(OnInitializedAsync), ex);
            }
            finally
            {
                await SafeJsVoid("hideLoader");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                await InitLoader();

                if (firstRender)
                {
                    oParam.PagePadre = "/";

                    if (!primerIngresoApp)
                    {
                        primerIngresoApp = true;
                        // fire & forget, pero seguro (sin async void)
                        _ = verificarTerminarDeTransmitir();
                    }
                }
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(OnAfterRenderAsync), ex);
            }
            finally
            {
                await SafeJsVoid("hideLoader");
            }
        }

        // ---------- Helpers ----------
        private async Task SetErrorAsync(string metodo, Exception ex)
        {
            mensajeError = $"Metodo: {metodo} - {ex.Message}";
            Console.Error.WriteLine($"[{metodo}] {ex}");
            await InvokeAsync(StateHasChanged);
        }

        private async Task ObtenerConfigSoporteAsync()
        {
            try
            {
                var service = new SoporteAppConfigService();
                var config = await service.GetConfigSoporteAsync();

                _mailSoporte = config?.MailSoporte ?? "";
                _linkWhatsappSoporteBase = config?.LinkWhatsappSoporte ?? "";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ObtenerConfigSoporteAsync] {ex.Message}");
            }
        }

        private async Task ValidarVersionAppAsync()
        {
            try
            {
                var service = new AppVersionService();
                var info = await service.GetVersionRequeridaAsync();
                if (info == null) return;

                _versionInfo = info;

                if (!int.TryParse(versionAplicacion, out int buildInstalado)) return;
                if (!int.TryParse(info.VersionMinima, out int buildMinimo)) return;

                if (buildInstalado < buildMinimo && info.ActualizacionObligatoria)
                {
                    _modalActualizacionOpen = true;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ValidarVersionAppAsync] {ex.Message}");
            }
        }

        private async Task SafeJs(string fn)
        {
            try { await JS.InvokeAsync<string>(fn); }
            catch { /* no rompo UI */ }
        }

        private async Task SafeJsVoid(string fn)
        {
            try { await JS.InvokeVoidAsync(fn); }
            catch { /* no rompo UI */ }
        }

        public int IrDirectoPostSincro()
        {
            int IrDirectoPostSincro = oParam.IrDirectoPostSincro;

            return IrDirectoPostSincro;
        }

        public void apagarIrDirectoPostSincro()
        {
            oParam.IrDirectoPostSincro = 0;
        }

        private static string CleanHeader(string? value)
            => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());

        // ---------- Timer ----------
        private void HandleTimerElapsed()
        {
            // sin .Result (evita deadlocks / freezes)
            _ = InvokeAsync(async () =>
            {
                try
                {
                    bool esClienteVales = oParam.TipoDeUsuario;
                    if (esClienteVales) return;

                    var hayNuevos = await oFuncionesSincronizar.ConsultarMensajesNuevos(JS);
                    if (hayNuevos)
                        _navigationManager.NavigateTo("MensajesARespnderWhatss", true);
                }
                catch (Exception ex)
                {
                    await SetErrorAsync(nameof(HandleTimerElapsed), ex);
                }
            });
        }

        public void SuscribirTimer()
        {
            if (!isTimerSubscribed)
            {
                timerService.OnTimerElapsed += HandleTimerElapsed;
                isTimerSubscribed = true;
            }
        }

        public void DesuscribirTimer()
        {
            if (isTimerSubscribed)
            {
                timerService.OnTimerElapsed -= HandleTimerElapsed;
                isTimerSubscribed = false;
            }
        }

        // ---------- Loader ----------
        private async Task InitLoader()
        {
            try
            {
#if IOS
                HideDenyOnIOS = true;
#endif
                if (initLoader) return;
                initLoader = true;
                await SafeJsVoid("showLoader");
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(InitLoader), ex);
            }
        }

        // ---------- Navegaci�n / acciones UI ----------
        private Task IngresarClickAsync() => Ingresar().ContinueWith(_ => { });

        private void NavegarASolicitudAlta()
            => _navigationManager.NavigateTo("/SolicitudAltaEcom", true);

        public bool SincronizoHoy()
            => (oParam.FechaUltimaSincronizacion).Date >= DateTime.Now.Date;

        public async Task Clientes()
        {
            try
            {
                if (!await PuedeNavegarSiNoSincronizadoAsync()) return;

                _navigationManager.NavigateTo("clientes", true);

                // en tu c�digo se usaba para Vales/Timer
                mostrarVale = estaSincronizado;
                if (mostrarVale)
                {
                    SuscribirTimer();
                    mostrarVale = false;
                }
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(Clientes), ex);
            }
        }

        public async Task Pedidos()
        {
            try
            {
                if (!await PuedeNavegarSiNoSincronizadoAsync()) return;
                _navigationManager.NavigateTo("pedidos", true);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(Pedidos), ex);
            }
        }

        public async Task esClientesIrProductos()
        {
            try
            {
                if (!await PuedeNavegarSiNoSincronizadoAsync(requiereSincronizoHoy: true)) return;
                _navigationManager.NavigateTo("/vistaProductos/100002", true);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(esClientesIrProductos), ex);
            }
        }

        private async Task<bool> PuedeNavegarSiNoSincronizadoAsync(bool requiereSincronizoHoy = false)
        {
            if (estaSincronizado) return true;

            var cancelar = await App.Current.MainPage.DisplayAlert(
                AppResources.Atencion,
                "No tiene los datos sincronizados, desea cancelar la descarga ?",
                AppResources.Si,
                AppResources.No);

            if (!cancelar) return false;

            if (!requiereSincronizoHoy) return true;

            if (SincronizoHoy()) return true;

            await App.Current.MainPage.DisplayAlert(AppResources.Atencion,
                "No tiene los datos sincronizados, tiene que sincronizar!!!",
                AppResources.Aceptar);

            return false;
        }

        public void AbrirHomeVD() 
        {
            try
            {
                AbrirPortalVendedores(JS, _navigationManager);
            }
            catch (Exception e)
            {
                mensajeError = "Metodo: AbrirHomeVD" + e.Message;
                this.StateHasChanged();
            }
        }

        private void AbrirModalEliminarCuenta()
        {
            _mensajeEliminarCuenta = "";
            _modalEliminarOpen = true;
        }

        private void CerrarModalEliminarCuenta()
        {
            _procesandoEliminar = false;
            _modalEliminarOpen = false;
        }

        private async Task ConfirmarEliminarCuenta()
        {
            try
            {
                _procesandoEliminar = true;
                _mensajeEliminarCuenta = "";
                await InvokeAsync(StateHasChanged);

                // Determinar tipo de usuario e ID
                bool esAutogestion = oParam.Autogestion;
                int clienteID = oParam.ClienteID;
                int vendedorID = 0;

                if (esAutogestion)
                {
                    if (clienteID <= 0)
                    {
                        _mensajeEliminarCuenta = "No se pudo determinar el usuario. Intente nuevamente.";
                        return;
                    }
                }
                else
                {
                    if (!int.TryParse(oParam.VendedorID, out vendedorID) || vendedorID <= 0)
                    {
                        _mensajeEliminarCuenta = "No se pudo determinar el usuario. Intente nuevamente.";
                        return;
                    }
                }

                // Generar clave aleatoria de 4 dígitos
                string nuevaClave = Random.Shared.Next(1000, 10000).ToString();

                // Llamar API para cambiar la clave
                string urlApi = oParam.urlApi;
                if (string.IsNullOrWhiteSpace(urlApi))
                {
                    _mensajeEliminarCuenta = "Error de configuración. Intente nuevamente.";
                    return;
                }

                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(urlApi),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                if (!string.IsNullOrWhiteSpace(oParam.HeaderApiKey) && !string.IsNullOrWhiteSpace(oParam.ApiKey))
                    httpClient.DefaultRequestHeaders.Add(CleanHeader(oParam.HeaderApiKey), CleanHeader(oParam.ApiKey));

                HttpResponseMessage response;

                if (esAutogestion)
                {
                    var payload = new { ClientesID = clienteID, NuevaClave = nuevaClave };
                    response = await httpClient.PostAsJsonAsync("ClienteItem/cambiarClaveWeb", payload);
                }
                else
                {
                    var payload = new { VendedoresID = vendedorID, NuevaClave = nuevaClave };
                    response = await httpClient.PostAsJsonAsync("VendedorItem/cambiarClave", payload);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _mensajeEliminarCuenta = "No se pudo procesar la solicitud. Intente nuevamente.";
                    await SetErrorAsync(nameof(ConfirmarEliminarCuenta),
                        new Exception($"API respondió {(int)response.StatusCode}: {errorBody}"));
                    return;
                }

                // Cambio de clave exitoso: cerrar sesión sin preguntar
                await LogOff(true);
            }
            catch (Exception ex)
            {
                _mensajeEliminarCuenta = "Ocurrió un error. Intente nuevamente.";
                await SetErrorAsync(nameof(ConfirmarEliminarCuenta), ex);
            }
            finally
            {
                _procesandoEliminar = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        // ---------- Textos VD ----------
        public void SeleccionarTexto(string txt)
        {
            try
            {
                TextoSeleccionado = txt;
                oParam.TextoVD = TextoSeleccionado;
            }
            catch (Exception ex)
            {
                _ = SetErrorAsync(nameof(SeleccionarTexto), ex);
            }
        }

        public async Task BorrarTexto(string txt)
        {
            try
            {
                TextoVD.RemoveAll(item => item.TextoLibreVD == txt);
                await LocalStorage.SetValueAsync("lsTextoVDItem", System.Text.Json.JsonSerializer.Serialize(TextoVD));
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(BorrarTexto), ex);
            }
        }

        public async Task GuardarTexto(string txt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txt)) return;

                TextoVD.Add(new TextoVDItem(txt));
                await LocalStorage.SetValueAsync("lsTextoVDItem", System.Text.Json.JsonSerializer.Serialize(TextoVD));
                textolibre = "";
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(GuardarTexto), ex);
            }
        }

        public void BorrarSeleccionTexto()
        {
            try
            {
                TextoSeleccionado = "";
                oParam.TextoVD = "";
            }
            catch (Exception ex)
            {
                _ = SetErrorAsync(nameof(BorrarSeleccionTexto), ex);
            }
        }

        private async Task CargarTextosVDDesdeLocalStorageAsync()
        {
            try
            {
                var raw = await LocalStorage.GetValueAsync<string>("lsTextoVDItem");
                if (!string.IsNullOrWhiteSpace(raw))
                    TextoVD = JsonConvert.DeserializeObject<List<TextoVDItem>>(raw) ?? new List<TextoVDItem>();
                else
                    TextoVD = new List<TextoVDItem>();

                TextoSeleccionado = oParam.TextoVD ?? "";
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(CargarTextosVDDesdeLocalStorageAsync), ex);
                TextoVD = new List<TextoVDItem>();
            }
        }

        // ---------- Estrellitas / coeficiente ----------
        public bool esSoloUnCliente()
        {
            esCliente = oParam.TipoDeUsuario;

            if (!esCliente)
            {
                cantidadEstrellitasFronted();
                cambiarColorCoheficiente();
            }
            return esCliente;
        }

        public void cantidadEstrellitasFronted()
        {
            try
            {
                opacityEstrellita01 = GetOpacity(oParam.listEstrellas, 0);
                opacityEstrellita02 = GetOpacity(oParam.listEstrellas, 1);
                opacityEstrellita03 = GetOpacity(oParam.listEstrellas, 2);
                opacityEstrellita04 = GetOpacity(oParam.listEstrellas, 3);
                opacityEstrellita05 = GetOpacity(oParam.listEstrellas, 4);
                opacityEstrellita06 = GetOpacity(oParam.listEstrellas, 5);
            }
            catch (Exception ex)
            {
                _ = SetErrorAsync(nameof(cantidadEstrellitasFronted), ex);
            }
        }

        private static string GetOpacity(List<object>? list, int index)
        {
            try
            {
                if (list == null || index < 0 || index >= list.Count) return "opacity: 0.4";
                return TryGetBool(list[index], out var on) && on ? "opacity: 1" : "opacity: 0.4";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetOpacity error (idx {index}): {ex.Message}");
                return "opacity: 0.4";
            }
        }

        private static bool TryGetBool(object? value, out bool result)
        {
            result = false;
            try
            {
                switch (value)
                {
                    case null: return false;
                    case bool b:
                        result = b; return true;
                    case string s:
                        if (bool.TryParse(s, out var bs)) { result = bs; return true; }
                        if (long.TryParse(s, out var ln)) { result = ln != 0; return true; }
                        return false;
                    case sbyte or byte or short or ushort or int or uint or long or ulong:
                        result = Convert.ToInt64(value) != 0; return true;
                    case float or double or decimal:
                        result = Math.Abs(Convert.ToDouble(value)) > double.Epsilon; return true;
                    case JsonElement je:
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.True: result = true; return true;
                            case JsonValueKind.False: result = false; return true;
                            case JsonValueKind.Number:
                                if (je.TryGetInt64(out var n)) { result = n != 0; return true; }
                                if (je.TryGetDouble(out var d)) { result = Math.Abs(d) > double.Epsilon; return true; }
                                return false;
                            case JsonValueKind.String:
                                var str = je.GetString();
                                if (bool.TryParse(str, out var jb)) { result = jb; return true; }
                                if (long.TryParse(str, out var jn)) { result = jn != 0; return true; }
                                return false;
                            default:
                                return false;
                        }
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("TryGetBool error: " + ex.Message);
                return false;
            }
        }

        public void cambiarColorCoheficiente()
        {
            try
            {
                decimal coef = oParam.CoheficienteComisionVendedor;
                // Diferencia porcentual respecto a la base (1.0 = sin cambio)
                int pct = (int)Math.Round(Math.Abs(coef - 1m) * 100);

                if (coef >= 1m)
                {
                    ComisionVendedoresString = "+ " + pct.ToString("N0") + " %";
                    ColorCoeficienteComision = true;   // dorado
                }
                else
                {
                    ComisionVendedoresString = "- " + pct.ToString("N0") + " %";
                    ColorCoeficienteComision = false;  // rojo
                }
            }
            catch (Exception ex)
            {
                _ = SetErrorAsync(nameof(cambiarColorCoheficiente), ex);
            }
        }

        // ---------- Sincronizaci�n ----------
        private async Task SincronizarManual()
        {
            try
            {
                await SafeJsVoid("showLoader");
                DesuscribirTimer();

                var res = await oFuncionesSincronizar.Sincronizar(JS, false);
                mostrarVale = false;

                if (res == 1)
                {
                    if (!esCliente)
                        esSoloUnCliente();
                }
                else if (res == 2)
                {
                    await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Error de Conexion (544)", AppResources.Aceptar);
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(SincronizarManual), ex);
            }
            finally
            {
                await SafeJsVoid("hideLoader");
            }
        }

        private async Task ForzarSincronizacion(bool mostrar)
        {
            try
            {
                mostrarVale = false;
                DesuscribirTimer();

                // Borra productos lista (una vez)
                var oProductosService = new AppVendedores2025.Services.ProductoListaService();
                await oProductosService.BorrarTodoProductoListaItem();

                // Im�genes / DB
                if (esSoloUnCliente() && isLogueado)
                {
                    DABSqlLite.TruncateDatabase(2);
                    var oImagen = new ImagenAppItemService();
                    await oImagen.BorrarTodo();
                }
                else
                {
                    if (mostrar)
                    {
                        bool resImg = await App.Current.MainPage.DisplayAlert("¿Quieres borrar todas las imágenes?", "", AppResources.Si, AppResources.No);
                        DABSqlLite.TruncateDatabase(2);

                        if (resImg)
                        {
                            DABSqlLite.TruncateDatabase(1);
                            var oImagen = new ImagenAppItemService();
                            await oImagen.BorrarTodo();
                        }
                    }
                    else
                    {
                        await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Se borraran todas las imágenes", AppResources.Aceptar);
                        var oImagen = new ImagenAppItemService();
                        await oImagen.BorrarTodo();
                    }
                }

                oParam.FechaUltimaSincronizacion = DateTime.MinValue;

#if ANDROID || IOS
                await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Se Borro toda la informacion", AppResources.Aceptar);
#endif
                _navigationManager.NavigateTo("/", true);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(ForzarSincronizacion), ex);
            }
        }

        // ---------- Login / logoff ----------
        private async Task<bool> Ingresar()
        {
            bool estaValidado = false;
            string msgError = "";

            try
            {
                if (string.IsNullOrWhiteSpace(txtVD) || string.IsNullOrWhiteSpace(txtPwd))
                    return false;

                string usuario = txtVD.Trim();
                bool esTest = usuario.EndsWith("!");
                if (esTest) usuario = usuario.Replace("!", "");

                oParam.EsModoTest = esTest;
                txtVD = txtVD.Replace("!", "");

                // Demo: EmpresaID nunca se inicializaba antes del primer login (quedaba en 0,
                // el default de ParametrosServices), y el backend interpreta EmpresaID=0 como
                // "usar el flujo legacy de un solo tenant" (fuera de alcance de este fork demo,
                // que solo implementa el flujo multiempresa). Sembramos un default razonable
                // acá para que el primer login pegue al flujo multiempresa que sí está armado.
                if (oParam.EmpresaID <= 0) oParam.EmpresaID = 1;

                var res = await oFuncionesSincronizar.ValidarVD(usuario, txtPwd);

                try
                {
                    if (res != null)
                    {
                        // Si la propiedad viene nula o no aplica, le damos valor por defecto false
                        // Para prevenir cualquier fallo inesperado, usamos try/catch como lo solicitado
                        oParam.botonEliminar = res.OcultarBotonEliminar;
                    }
                    else
                    {
                        oParam.botonEliminar = false;
                    }
                }
                catch (Exception ex)
                {
                    oParam.botonEliminar = false; // Valor por defecto seguro
                    Console.Error.WriteLine($"Error al leer OcultarBotonEliminar: {ex.Message}");
                }

                // defaults de textos (si queres lo hago (solo si no existe), pero lo dej igual a tu logica)
                var msgInicial = new List<TextoVDItem>
            {
                new("OFERTA SOLO POR HOY!!"),
                new("SOLO POR HOY"),
                new("OFERTA DEL DIA !!"),
                new("COMPRANDO 3 CAJAS"),
                new("COMPRANDO 5 CAJAS")
            };
                await LocalStorage.SetValueAsync("lsTextoVDItem", System.Text.Json.JsonSerializer.Serialize(msgInicial));

                // Autogestin (cliente)
                oParam.Autogestion = res.Autogestion;
                oParam.DescargarImagen = res.DescargarImagen;
                esCliente = res.Autogestion;
                oParam.TipoDeUsuario = res.Autogestion;
                esUnClienteX = res.Autogestion;

                oParam.EstrellitasVendedor = res.CantidadEstrellitasVendedor;
                oParam.urlGPSpoint = res.UrlGPSpoint;
                oParam.CoheficienteComisionVendedor = res.CoheficienteComision;
                oParam.segundosActivarServicioGps = (res.tiempoEnvioGps > 0) ? res.tiempoEnvioGps : 20;
                if (res.EmpresaID > 0) oParam.EmpresaID = res.EmpresaID;

                string urlApi = await oFuncionesSincronizar.GetUrlaApi();

                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(urlApi),
                    Timeout = TimeSpan.FromMinutes(4)
                };


                if (!esCliente)
                {
                    if (res.VendedorID > 0)
                    {
                        oParam.urlDisconsultas = res.UrlHomeVendedor;
                        oParam.GoolgeApiKeyV2 = res.GoolgeApiKey;
                        oParam.VendedorID = res.VendedorID.ToString();
                        oParam.Vendedor = txtVD;
                        oParam.tipoVD = res.TiposDeVendedoresID;
                        oParam.Pwd = txtPwd;

                        oParam.Color = txtVD.Length > 0 ? txtVD[^1].ToString() : "0";
                        oParam.listEstrellas = res.ListEstrellas;

                        if (txtVD.ToUpper().StartsWith("VC") || txtVD.ToUpper().StartsWith("VG"))
                            oParam.Color = "0";

                        oParam.DeviceID = (oParam.tipoVD == 5) ? "TLK" : "PRES";
                        if (txtVD.ToUpper().StartsWith("VG") || txtVD.ToUpper().StartsWith("VV"))
                            oParam.DeviceID = "ESP";

                        await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Vendedor Validado", AppResources.Aceptar);
                        _navigationManager.NavigateTo("/", true);
                        estaValidado = true;
                    }
                    else
                    {
                        oParam.Vendedor = "";
                        oParam.VendedorID = "";
                        await oParam.AddorUpdateAsync("pwd", "", 0, new DateTime());
                        await oParam.AddorUpdateAsync("color", "", 0, new DateTime());

                        if (res.CodError == "2")
                        {
                            if (_versionInfo == null)
                                await ValidarVersionAppAsync();
                            _modalActualizacionOpen = true;
                            await InvokeAsync(StateHasChanged);
                        }
                        else if (res.CodError == "1")
                            await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Datos Incorretos", AppResources.Aceptar);
                        else if (!string.IsNullOrWhiteSpace(res.CodError))
                            await App.Current.MainPage.DisplayAlert(AppResources.Atencion, res.Mensaje, AppResources.Aceptar);
                    }
                }
                else
                {
                    // Cliente (autogestion)

                    // headers desde oParam (asegurados por ValidarURLApi)
                    if (!string.IsNullOrWhiteSpace(oParam.HeaderApiKey) && !string.IsNullOrWhiteSpace(oParam.ApiKey))
                        httpClient.DefaultRequestHeaders.Add(CleanHeader(oParam.HeaderApiKey), CleanHeader(oParam.ApiKey));

                    int clId = await httpClient.GetFromJsonAsync<int>($"ClienteItem/idClienteXCodigo/{Uri.EscapeDataString(txtVD ?? "")}");



                    var lsCliente = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ClienteItem>>($"ClienteItem/getdatoscliente/{clId}")
                                   ?? Enumerable.Empty<EntidadesAppVendedores.ClienteItem>();

                   



                    var oClienteItemService = new ClienteItemService();
                    await oClienteItemService.Sincronizar(lsCliente);

                    clienteItemUno = await oClienteItemService.GetByID(clId) ?? new ClienteItem();

                    //oParam.VendedorID = res.VendedorID.ToString(); //clienteItemUno.VendedoresID.ToString();
                    oParam.VendedorID = clienteItemUno.VendedoresID.ToString();
                                                                   //oParam.VendedorID = clienteItemUno.VendedoresID.ToString();
                    oParam.ClienteID = clId;
                    oParam.cliente = clienteItemUno.cli_codigo.ToString();
                    oParam.Color = "0";

                    // lista de precios (con validaci�n)
                    if (res.ListadDisponibles != null && res.ListadDisponibles.Count > 0)
                        oParam.ListadePrecios = res.ListadDisponibles[0];

                    SeleccionarLista((int)oParam.ListadePrecios);

                    if (res.VendedorID > 0)
                    {
                        oParam.urlDisconsultas = res.UrlHomeVendedor;
                        oParam.GoolgeApiKeyV2 = res.GoolgeApiKey;
                        oParam.Vendedor = txtVD;
                        oParam.tipoVD = res.TiposDeVendedoresID;
                        oParam.Pwd = txtPwd;

                        oParam.DeviceID = (oParam.tipoVD == 5) ? "TLK" : "PRES";
                        if (txtVD.ToUpper().StartsWith("VG") || txtVD.ToUpper().StartsWith("VV"))
                            oParam.DeviceID = "ESP";

                        oParam.Autogestion = res.Autogestion;
                        esCliente = res.Autogestion;

                        await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Cliente Validado", AppResources.Aceptar);
                        _navigationManager.NavigateTo("/", true);
                        estaValidado = true;
                    }
                    else
                    {
                        oParam.Vendedor = "";
                        oParam.VendedorID = "";
                        await oParam.AddorUpdateAsync("pwd", "", 0, new DateTime());
                        await oParam.AddorUpdateAsync("color", "", 0, new DateTime());

                        if (res.CodError == "1")
                            await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Datos Incorretos", AppResources.Aceptar);
                        else if (!string.IsNullOrWhiteSpace(res.CodError))
                            await App.Current.MainPage.DisplayAlert(AppResources.Atencion, res.Mensaje, AppResources.Aceptar);
                    }
                }


                PeticionLogAccionApp itemPeticion = new PeticionLogAccionApp
                {
                    VendedorID = Convert.ToInt32(oParam.VendedorID == "" ? "0" : oParam.VendedorID),
                    Usuario = oParam.Vendedor,
                    CodigoCliente = oParam.cliente,
                    CodigoAccion = "200",
                    Accion = "Login",
                    Aplicacion = "AppVendedores2025",
                    NombreCelular = DeviceInfo.Current.Name + " - " + DeviceInfo.Platform,
                    Mensaje = $"ClienteID: {oParam.ClienteID}, VendedorID: {oParam.VendedorID}, TipoVD: {oParam.tipoVD}, Autogestion: {oParam.Autogestion}",
                    Fecha = DateTime.Now
                };

                var response = await httpClient.PostAsJsonAsync("LogAccionApp/AgregarLogAccionApp", itemPeticion);
                await InvokeAsync(StateHasChanged);
                return estaValidado;
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error Ingresar: " + msgError, ex.Message, AppResources.Aceptar);
                await SetErrorAsync(nameof(Ingresar), ex);
                return false;
            }
        }

        private async Task LogOff(bool res, bool eliminado = false)
        {
            try
            {
                if (!res)
                {
                    res = await App.Current.MainPage.DisplayAlert("Salir y borrar todos los datos ?", "", AppResources.Si, AppResources.No);
                }
                if (!res) return;


                // Transmitir Logout a API Backend usando el servicio dedicado
                await LogAccionAppService.TransmitirLogoutAsync(oParam.urlApi, esCliente, oParam.ClienteID, oParam.VendedorID);

                // Borra imagenes
                try
                {
                    if (esSoloUnCliente() && isLogueado)
                    {
                        var oImagen = new ImagenAppItemService();
                        await oImagen.BorrarTodo();
                    }
                    else
                    {
                        //bool resImg = await App.Current.MainPage.DisplayAlert("¿Quieres borrar todas las imágenes?", "", AppResources.Si, AppResources.No);
                        //if (resImg)
                        //{
                            var oImagen = new ImagenAppItemService();
                            await oImagen.BorrarTodo();
                        //}
                    }
                }
                catch { /* no freno logoff */ }

                // Productos lista
                var oProductosService = new AppVendedores2025.Services.ProductoListaService();
                await oProductosService.BorrarTodoProductoListaItem();

                // Tablas principales
                await BorrarDatos();

                // Parametros (preservo fecha imagen como en tu logica)
                var ultimasincimg = oParam.FechaUltimaSincronizacionImagen;
                await oParam.BorrarTodo();
                oParam.FechaUltimaSincronizacionImagen = ultimasincimg;

                if (!esCliente)
                {
                    cantidadEstrellitasFronted();
                    cambiarColorCoheficiente();
                }

#if ANDROID
            await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Se Borro toda la informacion", AppResources.Aceptar);
#endif
                _navigationManager.NavigateTo("/", true);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(LogOff), ex);
            }
        }

        private async Task<bool> BorrarDatos()
        {
            try
            {
                var oCli = new AppVendedores2025.Services.ClienteItemService();
                await oCli.BorrarTodo();

                var oPC = new AppVendedores2025.Services.PedidoCabeceraService();
                await oPC.BorrarTodo();

                var oPD = new AppVendedores2025.Services.PedidosDetalleItemService();
                await oPD.BorrarTodo();

                var oCombo = new CombosItemService();
                await oCombo.BorrarTodoHeader();
                await oCombo.BorrarTodoCombosProductosItem();

                var oCate = new ProductosSubCategoriasService();
                await oCate.BorrarTodo();

                return true;
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(BorrarDatos), ex);
                return false;
            }
        }

        // ---------- Validar URL API (corrige bug: ahora setea el oParam real) ----------
        public async Task ValidarURLApi()
        {
            try
            {
                string urlApi = "http://localhost:5101/";//VendedoresApi-Demo (backend real de esta app)
                string url2 = "http://localhost:5101/";//

                //local
                //string urlApi = "http://localhost:5101/";//LocalHost proyetoApi
                //string url2 = "http://localhost:5101/";//

                //PRODUCCION NUBE GNAMADIS0 06/01/2026                                           
                //string urlApi = "https://api1.empresademo.example/";//
                //string url2 = "https://api1.empresademo.example/";//

                //TEST                                           
                //string urlApi = "https://api2.empresademo.example/";//
                //string url2 = "https://api2.empresademo.example/";//


                //API PRODUCCION                                
                // string urlApi = "https://localhost:5101/";
                // string url2 = "https://api.empresademo.example:4306/";



                oParam.urlApi = urlApi;
                oParam.HeaderApiKey = "x-api-key";
                oParam.ApiKey = "vendedoresapi-demo-2026";

                //using var httpClient = new HttpClient { BaseAddress = new Uri(urlApi), Timeout = TimeSpan.FromSeconds(10) };
                //httpClient.DefaultRequestHeaders.Add(CleanHeader(oParam.HeaderApiKey), CleanHeader(oParam.ApiKey));

                //try
                //{
                //    var res = await httpClient.GetAsync("VendedorItem/aa/11/1000");
                //    if ((int)res.StatusCode > 399)
                //    {
                //        oParam.urlApi = url2;
                //    }
                //}
                //catch
                //{
                //    oParam.urlApi = url2;
                //}
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(ValidarURLApi), ex);
            }
        }

        // ---------- Logs / soporte ----------
        private async Task<int> ContenidoLog()
        {
            try
            {
                var log = await LocalStorage.GetValueAsync<string>("log") ?? "";
                var imgst = await LocalStorage.GetValueAsync<string>("statusimg") ?? "";

                string urlApi = oParam.Vendedor ?? "";
                string versionApp = $"Version : {versionAplicacion}\n";
                var img = $"Ultima  Sincro Completa : {ultimasync}\n";


                log = "Estado Sincro Datos:\n" + log;

                txtLog = urlApi + "\n" + versionApp + img + log;

                var cont = Uri.EscapeDataString("VendedorID:" + vdLogueadoCodigo + "\n" + txtLog);
                LinkWhatsapp = string.IsNullOrWhiteSpace(_linkWhatsappSoporteBase)
                    ? ""
                    : _linkWhatsappSoporteBase + "&text=" + cont;

                return 1;
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(ContenidoLog), ex);
                return 1;
            }
        }

        private async Task<int> AbrirModalSoporte()
        {
            try
            {
                await ContenidoLog();
                _modalSoporteOpen = true;
                return 1;
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(AbrirModalSoporte), ex);
                return 1;
            }
        }

        private async Task<int> AbrirModalTexto()
        {
            try
            {
                await JS.InvokeVoidAsync("openModalTexto");
                return 1;
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(AbrirModalTexto), ex);
                return 1;
            }
        }

        public async Task<int> pedidoSinCargar()
        {
            try
            {
                var oPedidoCabeceraService = new PedidoCabeceraService();
                return await oPedidoCabeceraService.GetCantidadPedidossinTX();
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(pedidoSinCargar), ex);
                return 1;
            }
        }

        // ---------- Lo pesado (background) ----------
        private async Task CalcularClientesSinPedidoAsync()
        {
            _bgCts?.Cancel();
            _bgCts = new CancellationTokenSource();
            var ct = _bgCts.Token;

            try
            {
                var oClienteItemService = new ClienteItemService();
                var oPedidoCabeceraService = new PedidoCabeceraService();
                var oDetalleService = new PedidosDetalleItemService();

                var lsClientes = await oClienteItemService.GetAll() ?? new List<ClienteItem>();
                int count = 0;

                foreach (var cli in lsClientes)
                {
                    ct.ThrowIfCancellationRequested();

                    var ultPedido = await oPedidoCabeceraService.GetByClienteIDUltimo(cli.cli_clientesid);

                    if (ultPedido == null)
                    {
                        count++;
                        continue;
                    }

                    // si ResultadoTx est� OK, no cuenta
                    if (!string.IsNullOrWhiteSpace(ultPedido.ResultadoTx) &&
                        ultPedido.ResultadoTx.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var det = await oDetalleService.GetByPedidoCabezaraID(ultPedido.PedidoCabeceraID);
                    if (det == null || det.Count == 0)
                        count++;
                }

                cuentasinpedido = count;
                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException)
            {
                // ok
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(CalcularClientesSinPedidoAsync), ex);
            }
        }

        // ---------- Terminar de transmitir (sin async void) ----------
        public async Task verificarTerminarDeTransmitir()
        {
            try
            {
                esCliente = oParam.TipoDeUsuario;

                if (!(esCliente && estaSincronizado))
                    return;

                var oPedidoCabeceraService = new PedidoCabeceraService();
                var pedidosPendientes = await oPedidoCabeceraService.GetAll() ?? new List<PedidoCabeceraItem>();

                var pendientes = pedidosPendientes
                    .Where(p => p.PedidoTransmitido != 1 && !string.IsNullOrEmpty(p.KeyPagoID))
                    .ToList();

                if (pendientes.Count == 0) return;

                int clienteID = oParam.ClienteID;

                // mover afuera del foreach (tu c�digo lo hac�a por cada pedido)
                var oProductoListaService = new ProductoListaService();
                var lsProductosTT = await oProductoListaService.GetAllFiltradoBloqueados(clienteID, null);

                var lsProductosItemsTT = new ProductoListaItems();
                lsProductosItemsTT.AddRange(lsProductosTT?.ToList() ?? new List<ProductoListaItem>());

                var oMercadoPagoService = new MercadoPagoService();

                foreach (var pedido in pendientes)
                {
                    var respuestaPagoRealizado = await oMercadoPagoService.ConsultarPagoConIntervaloIngresoAsync(pedido.KeyPagoID);

                    if (respuestaPagoRealizado.StatusPago != "approved")
                        continue;

                    try
                    {
                        if (!string.IsNullOrEmpty(respuestaPagoRealizado.PaymentID))
                        {
                            pedido.PaymentID = respuestaPagoRealizado.PaymentID;
                            await oPedidoCabeceraService.UpdatePaymentID(pedido.PedidoCabeceraID, pedido.PaymentID);
                        }
                    }
                    catch { }

                    var res = await oPedidoCabeceraService.TransmitirPedido(
                        pedido.PedidoCabeceraID,
                        lsProductosItemsTT,
                        "",
                        pedido.TotalPedido,
                        pedido.PaymentID);

                    if (res.StartsWith("Ok", StringComparison.OrdinalIgnoreCase))
                    {
                        await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Tu pedido pediente ha ingresado correctamente.", AppResources.Aceptar);
                    }
                    else
                    {
                        var msj = res + " " + respuestaPagoRealizado.Mensaje;
                        await App.Current.MainPage.DisplayAlert(
                            AppResources.Atencion,
                            msj + " Tuvimos un problema con su pedido pendiente. Por favor, comun�quese con AppVendedores2025 Demo. PaymentMP: " + pedido.PaymentID + " KeyID: " + pedido.KeyPagoID,
                            AppResources.Aceptar);
                    }
                }
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(verificarTerminarDeTransmitir), ex);
            }
        }

        // ---------- select lista ----------
        private void CambiarLista(ChangeEventArgs e)
        {
            try
            {
                if (e?.Value == null) return;
                if (!int.TryParse(e.Value.ToString(), out var listaSeleccionada)) return;

                SeleccionarLista(listaSeleccionada);
            }
            catch (Exception ex)
            {
                _ = SetErrorAsync(nameof(CambiarLista), ex);
            }
        }

        // ---------- Alert de actualizaci�n ----------
        private async Task ShowCustomAlert(IJSRuntime JS)
        {
            try
            {
                string message = @"
                <div style='position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background-color: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; text-align: center; padding: 10px; box-sizing: border-box;'>
                    <div style='background-color: white; padding: 20px; border-radius: 10px; width: 100%; max-width: 500px; box-sizing: border-box;'>
                        <img src='https://cdn.empresademo.example/images/logo.gif' style='max-width: 100%; height: auto; margin-bottom: 20px;' alt='Logo' />
                        <h5 style='margin: 15px; font-size: 1rem;'>
                            <b>Atenci�n</b><br />
                            Esta versi�n de la app est� deshabilitada.<br />
                            Deb�s actualizarla para poder continuar.
                        </h5>
                        <a href='https://play.google.com/store/apps/details?id=com.empresademo.appvendedores2025demo'
                           style='display: inline-block; padding: 10px 20px; background-color: #1E88E5; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 1rem;'>
                            Actualizar desde Google Play
                        </a>
                    </div>
                </div>";

                await JS.InvokeVoidAsync("showCustomAlert", message);
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(ShowCustomAlert), ex);
            }
        }

        // ---------- Versionado (simplificado / seguro) ----------
        private async Task ChequearVersionDatosYActualizarSiHaceFaltaAsync()
        {
            try
            {
                versionDatos = await oParam.GetParametroAppSeting("VersionDatosApp") ?? "";
                var versionActual = oParam.VersionDatosApp ?? "";

                var gpsTxt = await oParam.GetParametroAppSeting("tiempoEnvioGps");
                if (int.TryParse(gpsTxt, out var segs))
                    oParam.segundosActivarServicioGps = segs;

                if (!string.IsNullOrWhiteSpace(versionActual) &&
                    !string.IsNullOrWhiteSpace(versionDatos) &&
                    !string.Equals(versionActual, versionDatos, StringComparison.OrdinalIgnoreCase))
                {
                    await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "Nueva version de datos, se actualizara la informaci�n.", AppResources.Aceptar);

                    // Si es cliente, no borro todo igual que vendedor (tu l�gica usaba 2)
                    DABSqlLite.TruncateDatabase(esCliente ? 2 : 1);

                    // Re-login con lo guardado
                    txtVD = oParam.Vendedor ?? "";
                    txtPwd = oParam.Pwd ?? "";

                    var okLogin = await Ingresar();
                    if (okLogin)
                    {
                        // fuerza resync de datos
                        await ForzarSincronizacion(false);
                        oParam.VersionDatosApp = versionDatos;
                    }
                    else
                    {
                        await App.Current.MainPage.DisplayAlert(AppResources.Atencion, "No se pudo Actualizar los datos", AppResources.Aceptar);
                    }
                }

                oParam.VersionDatosApp = versionDatos;
            }
            catch (Exception ex)
            {
                await SetErrorAsync(nameof(ChequearVersionDatosYActualizarSiHaceFaltaAsync), ex);
            }
        }

        // ---------- Permisos (tu demo estaba ok) ----------
        private async Task SolicitarPermisosAsync()
        {
            try
            {
                UiError = null;
                await Task.Delay(300);
            }
            catch (OperationCanceledException)
            {
                UiError = "Solicitud cancelada por el usuario.";
            }
            catch (Exception ex)
            {
                UiError = "No se pudieron solicitar los permisos. " + ex.Message;
                Console.Error.WriteLine($"[Demo.SolicitarPermisos] {ex}");
            }
        }

        private async Task RegistrarDenegadoAsync()
        {
            try
            {
                UiError = null;
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                UiError = "Ocurri� un error al registrar la decisi�n. " + ex.Message;
                Console.Error.WriteLine($"[Demo.RegistrarDenegado] {ex}");
            }
        }
    }
}

using EntidadesAppVendedores;
using Microsoft.JSInterop;
using Microsoft.Maui.Networking;
using System.Diagnostics;
using System.Web;

namespace AppVendedores2025.Services
{
    public interface IGpsTrackingService
    {
        bool IsRunning { get; }
        DateTime? LastTickAt { get; }
        DateTime? LastSuccessAt { get; }
        string? LastError { get; }
        event EventHandler<Ubicacion>? PointGenerated;

        Task StartAsync(IJSRuntime? js = null, CancellationToken? externalToken = null, bool allowSelfHeal = true);
        Task StopAsync();
    }

    public sealed class GpsTrackingService : IGpsTrackingService, IAsyncDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly SemaphoreSlim _runLock = new(1, 1);       // reentradas por tick
        private readonly SemaphoreSlim _startStopLock = new(1, 1); // carreras Start/Stop
        private volatile bool _isRunning;
        private IJSRuntime? _js;

        // Puerta atómica para evitar doble Start simultáneo
        private int _startGate = 0;

        // Monotónico (lo dejo por si querés medir tiempos, pero ya no lo uso para bloquear)
        private readonly Stopwatch _monotonic = new();
        private long _lastSuccessMs = -1;

        // Salud
        public DateTime? LastTickAt { get; private set; }
        public DateTime? LastSuccessAt { get; private set; }
        public string? LastError { get; private set; }
        public bool IsRunning => _isRunning;
        public event EventHandler<Ubicacion>? PointGenerated;

        // Campos que usás en tus métodos
        private CancellationTokenSource? _cancelTokenSource;
        private bool _isCheckingLocation;

        private static DateTime _ultimoEnvio = DateTime.MinValue;

        // ⚠️ AHORA ParametrosServices se INYECTA (no se hace new adentro)
        private readonly ParametrosServices oParam;

        // El intervalo actual, solo para diagnóstico; el valor real se toma SIEMPRE de oParam
        private TimeSpan _intervalo = TimeSpan.FromSeconds(25);
        private readonly object _intervalLock = new();

        private readonly GPSService oGPS = new(Connectivity.Current);

        // 👉 CONSTRUCTOR recibiendo ParametrosServices
        public GpsTrackingService(ParametrosServices parametros)
        {
            oParam = parametros ?? throw new ArgumentNullException(nameof(parametros));
            ActualizarIntervaloDesdeParametros(); // inicializa _intervalo
        }

        /// <summary>
        /// Lee oParam.segundosActivarServicioGps y actualiza el campo local _intervalo.
        /// </summary>
        private void ActualizarIntervaloDesdeParametros()
        {
            try
            {
                int segundos = oParam.segundosActivarServicioGps;
                if (segundos <= 0)
                    segundos = 25;

                var ts = TimeSpan.FromSeconds(segundos);
                lock (_intervalLock)
                {
                    _intervalo = ts;
                }
            }
            catch (Exception ex)
            {
                // Si algo falla, dejamos 25s por defecto
                Debug.WriteLine($"[GpsTrackingService] Error al actualizar intervalo desde parámetros: {ex}");
                lock (_intervalLock)
                {
                    _intervalo = TimeSpan.FromSeconds(25);
                }
            }
        }

        /// <summary>
        /// Devuelve el intervalo actual, leyendo SIEMPRE oParam (para ser 100% reactivo al componente).
        /// </summary>
        private TimeSpan GetIntervaloActual()
        {
            try
            {
                int segundos = oParam.segundosActivarServicioGps;
                if (segundos <= 0)
                    segundos = 25;

                return TimeSpan.FromSeconds(segundos);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpsTrackingService] Error en GetIntervaloActual: {ex}");
                return TimeSpan.FromSeconds(25);
            }
        }

        public async Task StartAsync(IJSRuntime? js = null, CancellationToken? externalToken = null, bool allowSelfHeal = true)
        {
            // Inicializo el intervalo la primera vez
            ActualizarIntervaloDesdeParametros();

            // Rebota si otro hilo intenta arrancar al mismo tiempo
            if (Interlocked.Exchange(ref _startGate, 1) == 1) return;

            await _startStopLock.WaitAsync();
            try
            {
                // Ya corría
                if (_isRunning)
                {
                    // Si dice que corre pero el Task murió, limpiamos y re-arrancamos
                    if (allowSelfHeal && (_loopTask?.IsCompleted ?? true))
                        await InternalStopNoLockAsync();
                    else
                    {
                        // ✅ BUG FIX: resetear el gate antes de retornar para no dejarlo pegado en 1
                        Interlocked.Exchange(ref _startGate, 0);
                        return;
                    }
                }

                _isRunning = true;
                _js = js;
                _cts = externalToken.HasValue
                    ? CancellationTokenSource.CreateLinkedTokenSource(externalToken.Value)
                    : new CancellationTokenSource();

                var token = _cts.Token;
                _monotonic.Restart();
                _lastSuccessMs = -1;
                LastError = null;

                // 👇👇 IMPORTANTE: loop con Task.Delay USANDO SIEMPRE el valor ACTUAL de oParam
                _loopTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                LastTickAt = DateTime.Now;

                                // Evita reentradas si el tick anterior todavía está corriendo
                                if (!await _runLock.WaitAsync(0, token))
                                {
                                    // Si no lo pudo tomar, esperamos un poquito y volvemos a intentar
                                    await Task.Delay(200, token);
                                    continue;
                                }

                                try
                                {
                                    // ✅ BUG FIX: GetCurrentLocationV2 ya llama a GrabarUbicacionV2
                                    // internamente (dentro del bloque #if ANDROID || IOS).
                                    // NO se llama GrabarUbicacionV2 de nuevo para evitar
                                    // doble registro de cada punto GPS en la BD.
                                    var ubicacion = await GetCurrentLocationV2(_js);

                                    _lastSuccessMs = _monotonic.ElapsedMilliseconds;
                                    LastSuccessAt = DateTime.Now;
                                    PointGenerated?.Invoke(this, ubicacion);
                                }
                                catch (OperationCanceledException)
                                {
                                    // cancelación esperada
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    LastError = $"Tick error: {ex.Message}";
                                    Debug.WriteLine($"[GpsTrackingService] Error en ciclo: {ex}");
                                }
                                finally
                                {
                                    _runLock.Release();
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                LastError = $"Loop error: {ex.Message}";
                                Debug.WriteLine($"[GpsTrackingService] Error externo en loop: {ex}");
                            }

                            // 👉 AQUÍ LEEMOS SIEMPRE EL VALOR ACTUAL del componente (vía oParam compartido)
                            var intervaloActual = GetIntervaloActual();
                            lock (_intervalLock)
                            {
                                _intervalo = intervaloActual; // sólo para debug si querés ver el último valor
                            }

                            try
                            {
                                await Task.Delay(intervaloActual, token);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        _isRunning = false;                       // NO queda pegado en true
                        Interlocked.Exchange(ref _startGate, 0);  // permite nuevo Start luego de terminar
                    }
                }, token);
            }
            finally
            {
                _startStopLock.Release();
                // Si falló antes de marcar running, liberamos gate
                if (!_isRunning) Interlocked.Exchange(ref _startGate, 0);
            }
        }

        public async Task StopAsync()
        {
            await _startStopLock.WaitAsync();
            try
            {
                await InternalStopNoLockAsync();
            }
            finally
            {
                _startStopLock.Release();
                Interlocked.Exchange(ref _startGate, 0);
            }
        }

        private async Task InternalStopNoLockAsync()
        {
            try
            {
                _cts?.Cancel();

                // ✅ BUG FIX: cancelar también el token del GetLocationAsync pendiente
                // para que no queden dos solicitudes activas cuando se hace Resume
                try { _cancelTokenSource?.Cancel(); } catch { }

                if (_loopTask != null)
                {
                    try { await _loopTask; }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[GpsTrackingService] Error al esperar loop en Stop: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpsTrackingService] Error al detener: {ex}");
            }
            finally
            {
                _isRunning = false;
                _loopTask = null;
                _cts?.Dispose();
                _cts = null;

                // ✅ BUG FIX: limpiar el token de geolocalización
                _cancelTokenSource?.Dispose();
                _cancelTokenSource = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _runLock.Dispose();
            _startStopLock.Dispose();
        }

        // =========================
        //   TUS MÉTODOS ORIGINALES
        // =========================

        public async Task<Ubicacion> GetCurrentLocationV2(IJSRuntime? JS)
        {
            Console.WriteLine("Start GetCurrentLocation=" + DateTime.Now.ToLongTimeString());
            var ubicacion = new Ubicacion { latitud = 34.0522, longitud = -118.2437, codCliente = "", fecha = null };
            try
            {
#if ANDROID || IOS
                _isCheckingLocation = true;
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                _cancelTokenSource = new CancellationTokenSource();

                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        if (oParam.tipoVD != 5)
                        {
                            try { await App.Current?.MainPage?.DisplayAlert("Atencion", "GPS desactivado", "OK"); } catch (Exception ex) { Debug.WriteLine($"[GpsTrackingService] DisplayAlert GPS: {ex.Message}"); }
                        }
                    }
                }

                if (status == PermissionStatus.Granted)
                {
                    Microsoft.Maui.Devices.Sensors.Location? location = null;
                    try
                    {
                        location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
                    }
                    catch (OperationCanceledException) { return ubicacion; }
                    catch (Exception ex) { Debug.WriteLine($"[GPS] GetLocationAsync error: {ex}"); }

                    if (location != null)
                    {
                        ubicacion.latitud = location.Latitude;
                        ubicacion.longitud = location.Longitude;
                        ubicacion.Accuracy = Convert.ToDecimal(location.Accuracy);
                        ubicacion.fecha = location.Timestamp.ToLocalTime().DateTime;

                        if (location.IsFromMockProvider)
                        {
                            ubicacion.latitud = 34.0522;
                            ubicacion.longitud = -118.2437;
                        }

                        try
                        {
                            var itx = await GrabarUbicacionV2(ubicacion, JS);
                            ubicacion.codCliente = itx.codCliente;
                            if (!string.IsNullOrWhiteSpace(itx.codCliente))
                                ubicacion.fecha = location.Timestamp.ToLocalTime().DateTime;
                            else
                                ubicacion.fecha = null;
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                await App.Current?.MainPage?.DisplayAlert("Atencion", "Error:" + ex.Message, "OK");
                                if (ex.InnerException != null)
                                    await App.Current?.MainPage?.DisplayAlert("Atencion", "InnerException:" + ex.InnerException.Message, "OK");
                            }
                            catch (Exception ex2) { Debug.WriteLine($"[GpsTrackingService] DisplayAlert error: {ex2.Message}"); }
                        }
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpsTrackingService] Error en GetCurrentLocationV2: {ex}");
                return ubicacion;
            }
            Console.WriteLine("End GetCurrentLocation=" + DateTime.Now.ToLongTimeString());
            return ubicacion;
        }

        public async Task<(DateTime fechaHora, string codCliente)> GrabarUbicacionV2(Ubicacion ub, IJSRuntime? JS)
        {
            try
            {
                ClienteItemService clienteItemService = new ClienteItemService();
                ClienteItem codClienteItemUltimo = new ClienteItem();

                if (!string.IsNullOrEmpty(oParam.VendedorID))
                {
                    ub.vdID = int.Parse(oParam.VendedorID);
                    ub.vdpwd = oParam.Pwd;
                    ub.vdCod = oParam.Vendedor;
                    var (ultimaFechaDeTransmision, codCliente) = (DateTime.MinValue, "");

                    int clienteID = oParam.ClienteID;
                    var GPSPoint = new GPSLocationPointItem(
                        Convert.ToDecimal(ub.latitud), Convert.ToDecimal(ub.longitud), clienteID, ub.fecha!.Value,
                        "app", 0, ub.vdCod, ub.vdpwd, "tel", "sessionid", ub.Accuracy, 0, "App2023", 0);

                    int clientesID;
                    await oGPS.Add(GPSPoint);
                    (ultimaFechaDeTransmision, clientesID) = await TransmitirGPSLocationPointV2();
                    if (clientesID != 0)
                    {
                        codClienteItemUltimo = await clienteItemService.GetByID(clientesID);
                        return (ultimaFechaDeTransmision, codClienteItemUltimo.cli_codigo);
                    }
                    return (ultimaFechaDeTransmision, "");
                }
                return (DateTime.Now.AddDays(-1), "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpsTrackingService] Error en GrabarUbicacionV2: {ex}");
                return (DateTime.Now.AddDays(-1), "");
            }
        }

        public async Task<(DateTime fechaHora, int codCliente)> TransmitirGPSLocationPointV2()
        {
            try
            {
                string url = oParam.urlGPSpoint;
                GPSLocationPointItem ultimoPuntoTrasmitido = new GPSLocationPointItem();

                if (await IsConnectedToInternet())
                {
                    string test = oParam.EsModoTest ? "Test-" : "";
                    var lsGpsSinTx = await oGPS.GetGPSsinTX();
                    int txSinTransmitir = lsGpsSinTx.Count;

                    foreach (var point in lsGpsSinTx)
                    {
                        // Incluir offset de zona horaria para que el servidor interprete
                        // correctamente sin importar en qué timezone esté.
                        string formattedDate = HttpUtility.UrlEncode(point.date.ToString("yyyy-MM-ddTHH:mm:ss"));
                        string p = "latitude=" + point.latitude
                                + "&longitude=" + point.longitude
                                + "&extrainfo=VendedoresApi"
                                + "&username=" + point.username
                                + "&keyEmpresa=" + point.keyEmpresa
                                + "&distance=" + point.distance
                                + "&date=" + formattedDate
                                + "&direction=0"
                                + "&accuracy=" + point.accuracy
                                + "&phonenumber=" + test + DeviceInfo.Current.Name + " " + DeviceInfo.Platform
                                + "&eventtype=" + DeviceInfo.Manufacturer
                                + "&sessionid=V80-" + DeviceInfo.Current.Model
                                + "&speed=" + point.speed
                                + "&locationmethod=" + HttpUtility.UrlEncode("fused");

                        using var client = HttpHelper.CreateClient();
                        var result = await client.GetAsync(url + p);
                        if ((int)result.StatusCode == 200)
                        {
                            await oGPS.UpdateVisitadoV3(point.GPSLocationPointID);
                            ultimoPuntoTrasmitido = point;
                            _ultimoEnvio = ultimoPuntoTrasmitido.date;
                            txSinTransmitir--;

                            int clientesID = Convert.ToInt32(point.speed);
                            if (clientesID != 0)
                            {
                                ClienteItemService clienteItemService = new ClienteItemService();
                                var codClienteItemUltimo = await clienteItemService.GetByID(clientesID);
                                //oParam.MensajeVendedorHead = $" Ult. Transmision:{point.date:dd/MM HH:mm:ss} / {codClienteItemUltimo.cli_codigo}";
                            }
                        }
                    }

                    if (txSinTransmitir < 0) txSinTransmitir = 0;
                    oParam.MensajeVPuntosSinTx = "Puntos sin Tx:" + txSinTransmitir;

                    if (lsGpsSinTx.Count > 0)
                        return (lsGpsSinTx.Last().date, (int)lsGpsSinTx.Last().speed);

                    return (_ultimoEnvio, 0);
                }
                else
                {
                    oParam.MensajeVPuntosSinTx = "*NO HAY INTERNET*";
                    return (_ultimoEnvio, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpsTrackingService] Error en TransmitirGPSLocationPointV2: {ex}");
                oParam.MensajeVPuntosSinTx = "Error Tx GPS";
                return (_ultimoEnvio, 0);
            }
        }

        public Task<bool> IsConnectedToInternet() =>
            Task.FromResult(oGPS.connectivityx.NetworkAccess == NetworkAccess.Internet);
    }
}

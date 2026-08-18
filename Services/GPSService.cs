using EntidadesAppVendedores;
using Microsoft.JSInterop;
using SQLite;
using System.Web;
using System.Net.NetworkInformation;
using static System.Net.WebRequestMethods;
using Microsoft.Maui.Networking;
//using Windows.Media.Protection.PlayReady;

namespace AppVendedores2025.Services
{
    public class GPSService
    {


        private SQLiteAsyncConnection _dbConnection;

        public GPSService()
        {
            SetUpDb();
        }
        private void SetUpDb()
        {
            if (_dbConnection == null)
            {
                _dbConnection = DABSqlLite.SetUpDb();
            }
        }
        public async Task<Ubicacion> GetUbicacionCliente(int clienteID)
        {
            var oClienteItemService = new ClienteItemService();
            var cli = await oClienteItemService.GetByID(clienteID);
            var cliUbicacion = new Ubicacion();
            cliUbicacion.latitud = cli.cli_latitud;
            cliUbicacion.longitud = cli.cli_longitud;

            return cliUbicacion;


        }


        private CancellationTokenSource _cancelTokenSource;
        private bool _isCheckingLocation;

        public async Task<Ubicacion> GetCurrentLocationV2(IJSRuntime JS)
        {
            Console.WriteLine("Start GetCurrentLocation=" + DateTime.Now.ToLongTimeString());
            var ubicacion = new Ubicacion();
            ubicacion.latitud = 34.0522;
            ubicacion.longitud = -118.2437;
            ubicacion.codCliente = "";
            ubicacion.fecha = null;
            try
            {
#if ANDROID || IOS
                _isCheckingLocation = true;

                GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));

                _cancelTokenSource = new CancellationTokenSource();



                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        // El usuario denegó el permiso de ubicación
                        //  return;
                        ParametrosServices oParam = new ParametrosServices();
                        if (oParam.tipoVD != 5)
                        {
                            await App.Current.MainPage.DisplayAlert("Atencion", "GPS desactivado", "OK");
                        }
                    }
                }

#endif

#if ANDROID || IOS
                //    var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status == PermissionStatus.Granted)
                {
                    Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

                    if (location != null)
                    {
                        //   Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");

                        ubicacion.latitud = location.Latitude;
                        ubicacion.longitud = location.Longitude;
                        ubicacion.Accuracy = Convert.ToDecimal(location.Accuracy);
                        ubicacion.fecha= location.Timestamp.ToLocalTime().DateTime;
                        

                        var EsMock = location.IsFromMockProvider;

                        if (EsMock)
                        {
                            ubicacion.latitud = 34.0522;
                            ubicacion.longitud = -118.2437;
                            //     await App.Current.MainPage.DisplayAlert("Atencion", "GPS Invalido", "OK");
                        }
                        try
                        {
                           var itx= await GrabarUbicacionV2(ubicacion, JS);
                            ubicacion.codCliente = itx.codCliente;
                            if (itx.codCliente != "")
                            {

                                ubicacion.fecha = location.Timestamp.ToLocalTime().DateTime;

                            }
                            else
                            {
                                ubicacion.fecha =null;
                            }
                        }
                        catch (Exception ex)
                        {

                            await App.Current.MainPage.DisplayAlert("Atencion", "Error:"+ex.Message, "OK");
                            if (ex.InnerException != null) 
                                {
                                await App.Current.MainPage.DisplayAlert("Atencion", "InnerException:" + ex.InnerException.Message, "OK");

                            }
                        }
                        
                    }
                }

#endif
            }
            // Catch one of the following exceptions:
            //   FeatureNotSupportedException
            //   FeatureNotEnabledException
            //   PermissionException
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPSService] Error GetCurrentLocationV2: {ex.Message}");
                return ubicacion;
                // Unable to get location
            }
            Console.WriteLine("End GetCurrentLocation=" + DateTime.Now.ToLongTimeString());


            return ubicacion;
        }

        public void CancelRequest()
        {
            if (_isCheckingLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
                _cancelTokenSource.Cancel();
        }


        public async Task<List<ClienteItem>> OrdenarClientesporDistancia(Ubicacion ubVD)
        {
            //coord vd
            //  var ubVD =await  GetCurrentLocation();
            ClienteItemService oClienteItemService = new ClienteItemService();
            List<ClienteItem> lsClientes    = new List<ClienteItem>();
            try
            {
                // Si no hay GPS (ubVD null, típico sin permiso de ubicación o en desktop sin
                // proveedor de posición), no se puede calcular distancia — pero igual hay que
                // mostrar los clientes sin ordenar en vez de perder la lista entera.
                if (ubVD == null)
                {
                    return await oClienteItemService.GetAll();
                }

                var posVD = new Location(ubVD.latitud, ubVD.longitud);
                //lista de clientes
                lsClientes = await oClienteItemService.GetAll();

                //calc dist a cada uno
                _isCheckingLocation = true;
                GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

                _cancelTokenSource = new CancellationTokenSource();
                //   Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

                foreach (var ClienteItem in lsClientes)
                {
                    var posCli = new Location(ClienteItem.cli_latitud, ClienteItem.cli_longitud);
                    ClienteItem.Distancia = Convert.ToDecimal(posVD.CalculateDistance(posCli, DistanceUnits.Kilometers));

                    if (ClienteItem.Distancia < 100)
                    {
                        ClienteItem.Visistado = true;

                        await oClienteItemService.UpdateVisitadoV2(ClienteItem.cli_clientesid);
                    }
                }
                lsClientes = lsClientes.OrderBy(c => c.Distancia).ToList();
                int ord = 1;
                foreach (var cli in lsClientes)
                {
                    cli.Orden = ord;
                    ord++;
                }
            }
            catch (Exception ex)
            {
                var _errorBuscador = $"Error leyendo el texto de búsqueda: {ex.Message}";

                // Fallback: si algo falló calculando distancias (coordenadas inválidas, etc.),
                // mostrar igual los clientes sin ordenar en vez de una lista vacía.
                if (lsClientes.Count == 0)
                {
                    try { lsClientes = await oClienteItemService.GetAll(); } catch { }
                }
            }

            return lsClientes;


        }







        //private GeoCoordinateWatcher geoWatcher;
        //public void test()
        //{
        //    geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
        //    geoWatcher.StatusChanged += GeoWatcher_StatusChanged;
        //}
        //public async Task<int> GrabarUbicacion(Ubicacion ub, IJSRuntime JS)
        //{

        //    ub.vdID = Convert.ToInt32(await JS.InvokeAsync<string>("getCookie", "vendedorID"));
        //    ub.vdpwd = await JS.InvokeAsync<string>("getCookie", "pwd");
        //    ub.vdCod = await JS.InvokeAsync<string>("getCookie", "vendedor");
        //    ub.fecha = DateTime.Now;
        //    var lsCoordVendedor = new List<Ubicacion>();
        //    LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);

        //    try
        //    {
        //        lsCoordVendedor = JsonConvert.DeserializeObject<List<Ubicacion>>(await LocalStorage.GetValueAsync<string>("lsCoordVendedor"));
        //    }
        //    catch
        //    {

        //    }

        //    lsCoordVendedor.Add(ub);
        //    await LocalStorage.SetValueAsync("lsCoordVendedor", System.Text.Json.JsonSerializer.Serialize(lsCoordVendedor));





        //    return 1;
        //}
        ParametrosServices oParam = new ParametrosServices();

        public async Task<(DateTime fechaHora, string codCliente)> GrabarUbicacionV2(Ubicacion ub, IJSRuntime JS)
        {
            ClienteItemService clienteItemService = new ClienteItemService();
            ClienteItem codClienteItemUltimo = new ClienteItem();

            if (oParam.VendedorID != "")
            {
                ub.vdID = Int32.Parse(oParam.VendedorID);
                ub.vdpwd = oParam.Pwd;
                ub.vdCod = oParam.Vendedor;
                var (ultimaFechaDeTransmision, codCliente) = (DateTime.MinValue, "");


                //     ub.fecha = DateTime.Now;



                int clienteID = oParam.ClienteID;

                //speed va a pasar a ser en la tabla clienteID 
                var GPSPoint = new GPSLocationPointItem(Convert.ToDecimal(ub.latitud), Convert.ToDecimal(ub.longitud), clienteID, ub.fecha.Value, "app", 0, ub.vdCod, ub.vdpwd, "tel", "sessionid", ub.Accuracy, 0, "App2023", 0);

                //   try
                //  {

                int clientesID;
                await Add(GPSPoint);
                (ultimaFechaDeTransmision, clientesID) = await TransmitirGPSLocationPointV2();
                if (clientesID != 0)
                {

                    codClienteItemUltimo = await clienteItemService.GetByID(clientesID);
                    return (ultimaFechaDeTransmision, codClienteItemUltimo.cli_codigo);
                }
                //}
                // catch
                // {

                //            }



                return (ultimaFechaDeTransmision, "");
            }
            return (DateTime.Now.AddDays(-1), "");

        }


        //public async Task<int> TransmitirGPSLocationPoint()
        //{
        //    var response = "";
        //    //string urlapi = "http://localhost:5101/";
        //    //string url = urlapi + "WebTracker2/UpdateLocation.aspx?";
        //    string urlapi = "http://localhost:5101/";
        //    string url = urlapi + "UpdateLocation.aspx?";


        //    var hayInternet = await IsConnectedToInternet();
        //    if (hayInternet)
        //    {
        //        var lsGpsSinTx = await GetGPSsinTX();
        //        try
        //        {
        //            foreach (var point in lsGpsSinTx)
        //            {

        //                //  http://localhost/gpstracker/UpdateLocation.aspx?longitude=-122.0214996&latitude=47.4758847&extrainfo=0&username=momo&distance=0.012262854&date=2014-09-16%2B17%253A49%253A57&direction=0&accuracy=65&phonenumber=867-5309&eventtype=android&sessionid=0a6dfd74-df4d-466e-b1b8-23234ef57512&speed=0&locationmethod=fused


        //                string formattedDate = point.date.ToString("yyyy-MM-ddTHH:mm:ss");

        //                formattedDate = HttpUtility.UrlEncode(formattedDate);



        //                string p = "latitude=" + point.latitude; //-122.0214996;

        //                p += "&longitude=" + point.longitude;// 47.4758847";
        //                p += "&extrainfo=0";
        //                p += "&username=" + point.username;// momo";
        //                p += "&keyEmpresa=" + point.keyEmpresa;// momo";
        //                p += "&distance=" + point.distance;// 0.012262854";
        //                p += "&date=" + formattedDate;//  HttpUtility.UrlEncode( point.date.ToString());// 2014-09-16%2B17%253A49%253A57";
        //                p += "&direction=0";
        //                p += "&accuracy=" + point.accuracy;//65";
        //                p += "&phonenumber=111111";
        //                p += "&eventtype=app2025";
        //                p += "&sessionid=0a6dfd74-df4d-466e-b1b8-23234ef57512";
        //                p += "&speed=0";
        //                p += "&locationmethod=" + HttpUtility.UrlEncode("fused");


        //                var urlFull = url + p;



        //                using (var client = new HttpClient())
        //                {

        //                    try
        //                    {
        //                        var result = await client.GetAsync(urlFull);
        //                    //    response = await result.Content.ReadAsStringAsync();
        //                        if ((int)result.StatusCode == 200)
        //                        {
        //                            //marcar transm
        //                            await UpdateVisitado(point.GPSLocationPointID);

        //                        }
        //                    }
        //                    catch
        //                    { 

        //                    }



        //                }
        //            }
        //        }
        //        catch
        //        {
        //        }
        //    }

        //    //FuncionesSincronizar oFuncionesSincronizar = new FuncionesSincronizar();
        //    //var msg =oFuncionesSincronizar.ConsultarMensajesNuevos("", "");
        //    return 1;
        //}

        private static DateTime _ultimoEnvio = DateTime.MinValue;
        //TODO: Cambio para que transmita cada 30seg. 23/06/25
        public async Task<(DateTime fechaHora, int codCliente)> TransmitirGPSLocationPointV2()
        {
            var response = "";
            string urlapi = oParam.urlApi + "GpsPosicion/fromapp?";
            string url = urlapi;
            GPSLocationPointItem ultimoPuntoTrasmitido = new GPSLocationPointItem();
            // Verificamos que hayan pasado al menos 30 segundos desde la última transmisión

            if ((DateTime.Now - _ultimoEnvio).TotalSeconds < 20)
            {
                // return (_ultimoEnvio, 0); // No transmite si no pasaron 30 segundos
            }

            var hayInternet = await IsConnectedToInternet();
            if (hayInternet)
            {

                string test = "";
                if (oParam.EsModoTest)
                {
                    test = "Test-";
                }


                var lsGpsSinTx = await GetGPSsinTX();
                //  try
                // {
                int txSinTransmitir = lsGpsSinTx.Count;
                // ParametrosServices oParam = new ParametrosServices();
                foreach (var point in lsGpsSinTx)
                {
                    string formattedDate = point.date.ToString("yyyy-MM-ddTHH:mm:ss");
                    formattedDate = HttpUtility.UrlEncode(formattedDate);

                    string p = "latitude=" + point.latitude;
                    p += "&longitude=" + point.longitude;
                    p += "&empresaID=" + oParam.EmpresaID;
                    p += "&username=" + point.username;
                    p += "&keyEmpresa=" + point.keyEmpresa;
                    p += "&distance=" + point.distance;
                    p += "&date=" + formattedDate;
                    p += "&direction=0";
                    p += "&accuracy=" + point.accuracy;
                    p += "&phonenumber=" + test + DeviceInfo.Current.Name + " " + DeviceInfo.Platform;
                    p += "&eventtype=" + DeviceInfo.Manufacturer;
                    p += "&sessionid=V80-" + DeviceInfo.Current.Model;
                    p += "&speed=" + point.speed;
                    p += "&locationmethod=" + HttpUtility.UrlEncode("fused");

                    var urlFull = url + p;

                    using (var client = HttpHelper.CreateClient())
                    {
                        //   try
                        {
                            var result = await client.GetAsync(urlFull);
                            if ((int)result.StatusCode == 200)
                            {
                                await UpdateVisitadoV3(point.GPSLocationPointID);
                                // Actualizamos el tiempo del último envío
                                ultimoPuntoTrasmitido = point;
                                _ultimoEnvio = ultimoPuntoTrasmitido.date;
                                txSinTransmitir--;

                                int clientesID = Convert.ToInt32(point.speed);
                                if (clientesID != 0)
                                {
                                    ClienteItemService clienteItemService = new ClienteItemService();
                                    var codClienteItemUltimo = await clienteItemService.GetByID(clientesID);
                                    //oParam.MensajeVendedorHead = $" Ult. Transmision:{point.date.ToString("dd/MM hh:mm:ss")} / {codClienteItemUltimo.cli_codigo}";

                                }
                                //return (_ultimoEnvio, (int)ultimoPuntoTrasmitido.speed);

                            }
                            //else
                            //{

                            //    return (_ultimoEnvio, 0);

                            //}
                        }
                        //txSinTransmitir++;
                        //catch
                        //{
                        //    // Manejo de error si lo deseas
                        //}
                    }
                }
                if (txSinTransmitir < 0) txSinTransmitir = 0;
                oParam.MensajeVPuntosSinTx = "Puntos sin Tx:" + txSinTransmitir;

                ultimoPuntoTrasmitido = lsGpsSinTx.Last();
                _ultimoEnvio = ultimoPuntoTrasmitido.date;
                return (_ultimoEnvio, (int)ultimoPuntoTrasmitido.speed);
                // Actualizamos el tiempo del último envío

                //return _ultimoEnvio;
                //}
                //catch
                //{
                //    // Manejo de error si lo deseas
                //}
            }
            else
            {

                oParam.MensajeVPuntosSinTx = "*NO HAY INTERNET*";
            }
            return (_ultimoEnvio, 0);
        }

        public async Task<int> Add(GPSLocationPointItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var horaPunto = item.date;

            return await _dbConnection.InsertAsync(item);
        }
        public async Task<List<GPSLocationPointItem>> GetGPSsinTX()
        {
            string sp = $"Select * From {nameof(GPSLocationPointItem)} where Tramsmitido<>1";


            var resultado = await _dbConnection.QueryAsync<GPSLocationPointItem>(sp);
            return resultado;

        }
        public async Task<string> MensajeUltinoTx()
        {
            string msg = "";
            var lsUltimoPunto = await GetUltimoTx();

            ClienteItemService clienteItemService = new ClienteItemService();
            ClienteItem codClienteItemUltimo = new ClienteItem();
            GPSLocationPointItem UltimoPunto = lsUltimoPunto.FirstOrDefault();

            if (UltimoPunto != null)
            {
                codClienteItemUltimo = await TraerCliente(Convert.ToInt32(UltimoPunto.speed));
                return "Codigo: " + codClienteItemUltimo.cli_codigo + "Ult. Sincronizacion: " + UltimoPunto.date;
            }

            return msg;
        }

        public async Task<ClienteItem> TraerCliente(int id)
        {
            ClienteItem codClienteItemUltimo = new ClienteItem();
            ClienteItemService clienteItemService = new ClienteItemService();
            return await clienteItemService.GetByID(id);
        }


        public async Task<List<GPSLocationPointItem>> GetUltimoTx()
        {
            string sp = $"Select * From {nameof(GPSLocationPointItem)} where Tramsmitido=1 ORDER BY Date DESC LIMIT 1;";
            var resultado = await _dbConnection.QueryAsync<GPSLocationPointItem>(sp);
            return resultado;

        }

        public async Task UpdateVisitadoV3(int id)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }



            var sql = $"UPDATE  {nameof(GPSLocationPointItem)}  SET Tramsmitido =1 where GPSLocationPointID=" + id;
            var result = await _dbConnection.ExecuteAsync(sql);



        }

        public readonly IConnectivity connectivityx;

        public GPSService(IConnectivity connectivity)
        {
            this.connectivityx = connectivity;
        }

        public async Task<bool> IsConnectedToInternet()
        {
            //try
            //{
            //    Ping myPing = new Ping();
            //    string host = "www.google.com";
            //    byte[] buffer = new byte[32];
            //    int timeout = 1000; // Tiempo de espera en milisegundos

            //    PingOptions options = new PingOptions();
            //    PingReply reply = myPing.Send(host, timeout, buffer, options);

            //    return (reply.Status == IPStatus.Success);
            //}
            //catch (Exception)
            //{
            //    return false;
            //}
            GPSService oGpsService = new(Connectivity.Current);

            if (oGpsService.connectivityx.NetworkAccess == NetworkAccess.Internet)
            {
                return true;
            }

            return false;

        }
    }
}

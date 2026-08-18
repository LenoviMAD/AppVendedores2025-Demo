//using Android.Content.Res;
using AppVendedores2025.Localization;
using AppVendedores2025.Resources.Strings;
using EntidadesAppVendedores;
using EntidadesGeneraleFrWks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;


//using static AppVendedores2025.Shared.UtilCompartidas;

namespace AppVendedores2025.Services
{


    public class FuncionesSincronizar
    {


        public static bool flagSincroenUso = false;

        string? version;

        bool esCliente;

        // Sincroniza Productos , Clientes y Combos

        public async Task<string> GetUrlaApi()
        {
            try
            {
                ParametrosServices oParam = new ParametrosServices();
                var url = oParam.urlApi; // ejemplo
                if (string.IsNullOrWhiteSpace(url))
                    throw new Exception("UrlApi vacía en parámetros.");

                // asegurar termina con /
                url = url.Trim();
                if (!url.EndsWith("/")) url += "/";

                return url;
            }
            catch (Exception ex)
            {
                // fallback
                // OJO: poné un fallback real si lo tenés
                throw new Exception("Error obteniendo UrlApi: " + ex.Message, ex);
            }
        }


        private readonly IHttpClientFactory _httpClientFactory;

        public FuncionesSincronizar(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        private HttpClient CrearHttp(string baseUrl, string headerName, string headerValue)
        {
            var http = _httpClientFactory.CreateClient("ApiTT");
            http.BaseAddress = new Uri(baseUrl);

            // limpiar y setear header (evita duplicados)
            if (http.DefaultRequestHeaders.Contains(headerName))
                http.DefaultRequestHeaders.Remove(headerName);

            http.DefaultRequestHeaders.Add(headerName, headerValue);

            return http;
        }

        public async Task<bool> SincronizarTT(string vdLogueado, IJSRuntime js, CancellationToken ct = default)
        {
            // Helpers JSON (evita exceptions por casing, etc.)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Helper para limpiar headers (como ya hacías)
            static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());

            async Task UiInitAsync()
            {
                await js.InvokeVoidAsync("resetProgressBar");
                await js.InvokeVoidAsync("showLoader");
                await js.InvokeVoidAsync("showMSG");
                await js.InvokeVoidAsync("updateProgressBar", 5);
            }

            async Task UiStepAsync(string msg, int progress)
            {
                // 1 sola “tanda” por paso (menos ruido)
                await js.InvokeVoidAsync("replaceSyncMessage", msg);
                await js.InvokeVoidAsync("updateProgressBar", progress);
            }

            async Task UiFinishOkAsync()
            {
                await js.InvokeVoidAsync("updateProgressBar", 100);
                await js.InvokeVoidAsync("replaceSyncMessage", "Finalizado");
                await js.InvokeVoidAsync("hideMSG");
            }

            async Task UiFinishFailAsync()
            {
                await js.InvokeVoidAsync("hideMSG");
            }

            // GET robusto con reintentos + errores controlados
            async Task<T?> SafeGetFromJsonAsync<T>(HttpClient http, string url, int retries = 2)
            {
                Exception? last = null;

                for (int attempt = 0; attempt <= retries; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, url);

                        // ResponseHeadersRead reduce memoria en payloads grandes
                        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                        if (resp.StatusCode == HttpStatusCode.NoContent)
                            return default;

                        if (!resp.IsSuccessStatusCode)
                        {
                            var body = await resp.Content.ReadAsStringAsync(ct);
                            throw new HttpRequestException($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase} - {url}. Body: {body}");
                        }

                        // Deserialize manual para usar jsonOptions
                        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                        return await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions, ct);
                    }
                    catch (Exception ex) when (attempt < retries)
                    {
                        last = ex;
                        // backoff simple
                        await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), ct);
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                        break;
                    }
                }

                // _logger?.LogError(last, "Error GET {Url}", url);
                return default; // devolvemos default y decide el caller si aborta o sigue
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                await UiInitAsync();

                // =========================
                // 1) Setup + Validaciones
                // =========================
                string urlApi = await GetUrlaApi(); // tu método
                var oParam = new ParametrosServices();

                var version = oParam.VersionApp;
                int color = int.TryParse(oParam.Color, out var col) ? col : 0;
                bool esCliente = oParam.TipoDeUsuario;
                int clienteID = oParam.ClienteID;
                int empresaID = oParam.EmpresaID;
                if (!int.TryParse(vdLogueado, out var vdId))
                    return false;

                // Internet
                var gps = new GPSService();
                if (!await gps.IsConnectedToInternet())
                    return false;

                // HttpClient reutilizable (factory)
                var http = _httpClientFactory.CreateClient("ApiTT");
                http.BaseAddress = new Uri(urlApi);

                // headers
                http.DefaultRequestHeaders.Remove(Clean(oParam.HeaderApiKey));
                http.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                HttpHelper.AttachBearerToken(http);

                var localStorage = new LocalStorageAccessor(js);

                await UiStepAsync("Validando usuario…", 8);

                //Enviamos la informacion de la accion 

                PeticionLogAccionApp itemPeticion = new PeticionLogAccionApp
                {
                    VendedorID = Convert.ToInt32(oParam.VendedorID),
                    Usuario = oParam.Vendedor,
                    CodigoCliente = oParam.cliente,
                    CodigoAccion = "200",
                    Accion = "Sincronizacion",
                    Aplicacion = "AppVendedores2025",
                    NombreCelular = DeviceInfo.Current.Name + " - " + DeviceInfo.Platform,
                    Mensaje = $"ClienteID: {oParam.ClienteID}, VendedorID: {oParam.VendedorID}, TipoVD: {oParam.tipoVD}, Autogestion: {oParam.Autogestion}",
                    Fecha = DateTime.Now
                };

                var response = await http.PostAsJsonAsync("LogAccionApp/AgregarLogAccionApp", itemPeticion);

                // Validar VD (ojo: esto NO usa http que estás configurando; si tu ValidarVD internamente crea otro http, conviene reescribirlo)
                string vendedorID = oParam.Vendedor;
                string pwd = oParam.Pwd;

                var respVD = await ValidarVD(vendedorID, pwd);
                oParam.DescargarImagen = respVD.DescargarImagen;

                oParam.EstrellitasVendedor = respVD.CantidadEstrellitasVendedor;
                oParam.CoheficienteComisionVendedor = respVD.CoheficienteComision;
                oParam.urlDisconsultas = respVD.UrlHomeVendedor;

                // Parametros MP (secuencial, pero podrías paralelizar si GetParametroAppSeting pega a red)
                oParam.accessTokenMP = await oParam.GetParametroAppSeting("accessTokenMP");
                oParam.publicKeyMP = await oParam.GetParametroAppSeting("publicKeyMP");
                oParam.MP_Test = await oParam.GetParametroAppSeting("MP_Test");

                // =========================
                // 2) Requests paralelizables (Unificadas)
                // =========================
                await UiStepAsync("Descargando datos iniciales…", 12);

                // Estos NO dependen entre sí => se utiliza el endpoint unificado
                var tDashboard = SafeGetFromJsonAsync<DashboardEcomResponse>(http, $"DashboardEcom/{vdId}/{color}/{empresaID}");
                
                Task<List<CombosXClientesItem>> tCombosCli = null;
                if (oParam.Autogestion)
                {
                    tCombosCli = SafeGetFromJsonAsync<List<CombosXClientesItem>>(http, $"combosXClienteAutogestion/{clienteID}/{empresaID}");
                }

                if (tCombosCli != null)
                {
                    await Task.WhenAll(tDashboard, tCombosCli);
                }
                else
                {
                    await tDashboard;
                }

                var dashboard = await tDashboard;
                var lsNuevosIngresos = dashboard?.NuevosIngresos ?? new List<int>();
                var lsLoMasPedido = dashboard?.RecomendadosTop ?? new List<LomasvendidoItem>();
                var lsMasMeGusta = dashboard?.MasTeGusta ?? new List<LoQueMasTeGustaItem>();
                var lsCategorias = dashboard?.Categorias ?? Array.Empty<CategoriaItem>();
                var lsSubCategorias = dashboard?.SubCategorias ?? Array.Empty<EntidadesAppVendedores.SubCategoriasApiEcomItem>();
                
                var lsCombosxCli = oParam.Autogestion 
                    ? (await tCombosCli ?? new List<CombosXClientesItem>()) 
                    : (dashboard?.CombosXCliente ?? new List<CombosXClientesItem>());

                // Guardar localstorage (también se podría paralelizar, pero JS interop conviene moderarlo)
                await UiStepAsync("Guardando datos…", 20);
                await localStorage.SetValueAsync("lsNuevosIngresos", JsonSerializer.Serialize(lsNuevosIngresos));
                await localStorage.SetValueAsync("lsLoMasPedido", JsonSerializer.Serialize(lsLoMasPedido));
                await localStorage.SetValueAsync("lsCategorias", JsonSerializer.Serialize(lsCategorias));
                await localStorage.SetValueAsync("lsSubCategorias", JsonSerializer.Serialize(lsSubCategorias));

                // Services (DB local)
                var oClienteItemService = new ClienteItemService();
                await oClienteItemService.SincronizarMasmeGusta(lsMasMeGusta);

                var oCombosxclienteService = new CombosxclienteService();
                await oCombosxclienteService.Sincronizar(lsCombosxCli);

                // =========================
                // 3) Cliente vs Vendedor
                // =========================
                await UiStepAsync("Actualizando perfil…", 28);

                if (!esCliente)
                {
                    // si querés "refrescar estrellas", ok, pero evitá 2 ValidarVD si no hace falta
                    var res2 = await ValidarVD(oParam.Vendedor, oParam.Pwd);
                    oParam.listEstrellas = res2.ListEstrellas;
                    oParam.EstrellitasVendedor = res2.CantidadEstrellitasVendedor;
                    oParam.CoheficienteComisionVendedor = res2.CoheficienteComision;

                    await UiStepAsync("Clientes…", 32);
                    var lsClientes = dashboard?.Clientes ?? Array.Empty<EntidadesAppVendedores.ClienteItem>();
                    await oClienteItemService.Sincronizar(lsClientes);
                }
                else
                {
                    await UiStepAsync("Cargando datos de cliente…", 32);

                    var clId = oParam.ClienteID;
                    var clienteItemUno = await oClienteItemService.GetByID(clId);

                    if (clienteItemUno != null)
                    {
                        var mp = new MercadoPagoService();
                        var percepciones = await mp.ConsultarPercepcionesPorCUIT(clienteItemUno.cli_CUIT);
                        await oParam.GuardarPercepcionesAsync(percepciones);
                    }

                    var montoMinimoApi = await oParam.GetParametroAppSeting("MontoMinimoAppClientes");
                    oParam.montoMinimo = decimal.TryParse(montoMinimoApi, out var monto) ? monto : 0m;
                }

                // =========================
                // 4) Fecha última sync
                // =========================
                DateTime ultimaFechaSync = DateTime.MinValue;
                string fechaUltimaSincro = "-1";
                try
                {
                    ultimaFechaSync = oParam.FechaUltimaSincronizacion;
                    fechaUltimaSincro = ultimaFechaSync.AddDays(-1).ToString("yyyyMMddTHHmmss");//20250131T153045
                                                                                                          //fechaUltimaSincro = "20250131T153045";
                }
                catch
                {
                    fechaUltimaSincro = "-1";
                }


                // =========================
                // 5) Productos (acá está lo más pesado)
                // =========================
                await UiStepAsync("Productos…", 45);

                // OJO: en tu código estás forzando "-1" siempre
                var urlProductos = $"ProductosItem/productosPorFecha/{vdLogueado}/0/{version}/-1/{empresaID}";
                var lsProductos = await SafeGetFromJsonAsync<List<EntidadesAppVendedores.ProductoListaItem>>(http, urlProductos)
                                 ?? new List<EntidadesAppVendedores.ProductoListaItem>();

                if (lsProductos.Count > 0 && lsProductos[0].ProductosID == 999999999)
                {
                    await App.Current.MainPage.DisplayAlert("Atención", lsProductos[0].Nombre, "OK");
                    App.Current.Quit();
                    return false;
                }

                var oProductosService = new ProductoListaService();

                // 🔥 Gran mejora posible:
                // En vez de BorrarTodo + Insertar, hacer incremental:
                // - si fechaUltimaSincro != "-1": upsert + borrar los desactivados
                // Pero te dejo tu lógica actual, solo ordenada y evitando enumeraciones innecesarias.

                await oProductosService.SincronizarSnapshotSinFechaAsync(lsProductos, js);

                // =========================
                // 6) Stock/Precios/Actualizaciones (paralelo)
                // =========================
                await UiStepAsync("Actualizando stock/precios…", 60);

                var stockYPrecios = await SafeGetFromJsonAsync<StockYPreciosResponse>(http, $"ProductosItem/StockYPreciosProductos/{fechaUltimaSincro}/{empresaID}");

                var lsStock = stockYPrecios?.Stock ?? new List<EntidadesAppVendedores.ProductoStockItem>();
                if (lsStock.Count > 0)
                    await oProductosService.UpdateStockEnProductoBloque(lsStock);

                var lsPrecios = stockYPrecios?.Precios ?? new List<EntidadesAppVendedores.ProductoPrecioItems>();
                if (lsPrecios.Count > 0)
                    await oProductosService.UpdatePreciosEnProductoBloque(lsPrecios);

                if (ultimaFechaSync.Date != DateTime.Now.Date)
                {
                    await UiStepAsync("Actualizando datos de productos…", 65);

                    var lsProdAct = await SafeGetFromJsonAsync<List<EntidadesAppVendedores.ProductosActualizarItems>>(
                                        http, $"ProductosItem/ProductosActualItems/{fechaUltimaSincro}/{empresaID}")
                                   ?? new List<EntidadesAppVendedores.ProductosActualizarItems>();

                    if (lsProdAct.Count > 0)
                        await oProductosService.UpdateProductoBloquePorItems(lsProdAct);
                }

                // =========================
                // 7) Combos
                // =========================
                await UiStepAsync("Combos…", 72);

                var oCombosItemService = new CombosItemService();
                IEnumerable<EntidadesAppVendedores.CombosItemJson> lsCombos;

                if (!esCliente)
                {
                    lsCombos = await SafeGetFromJsonAsync<IEnumerable<EntidadesAppVendedores.CombosItemJson>>(
                                  http, $"combosPorFechaMultiEmpresa/{vdId}/-1/{empresaID}")
                              ?? Array.Empty<EntidadesAppVendedores.CombosItemJson>();
                }
                else
                {
                    var clId = oParam.ClienteID;
                    lsCombos = await SafeGetFromJsonAsync<IEnumerable<EntidadesAppVendedores.CombosItemJson>>(
                                  http, $"combosPorFechaClienteMiltiEmpresa/{vdId}/{fechaUltimaSincro}/{clId}/{empresaID}")
                              ?? Array.Empty<EntidadesAppVendedores.CombosItemJson>();
                }

                var combosList = lsCombos as IList<EntidadesAppVendedores.CombosItemJson> ?? lsCombos.ToList();
                var idsCombosBorrar = combosList.Select(x => x.comb_combosid).ToList();

                await oCombosItemService.SincronizarActualizados(combosList, idsCombosBorrar);

                // =========================
                // 8) Productos bloqueados + subcategorías
                // =========================
                await UiStepAsync("Productos x Cliente / Subcategorías…", 80);

                var bloqueadosYSub = await SafeGetFromJsonAsync<BloqueadosYSubcategoriasResponse>(
                                    http, $"ProductosItem/bloqueadosYSubcategorias/{vdId}/{empresaID}");

                var bloqueados = (bloqueadosYSub?.Bloqueados ?? new List<EntidadesAppVendedores.ProductosBloqueadosxCliente>());
                var oPBlo = new ProductosBloqueadosxClienteService();
                await oPBlo.AddProductosbloqueados(bloqueados);

                var lsSub = bloqueadosYSub?.SubCategorias ?? new List<ProductosSubCategoriasItem>();
                var oProductosSubCategoriasService = new ProductosSubCategoriasService();
                await oProductosSubCategoriasService.SincronizarProductosSubCategorias(lsSub);

                // =========================
                // 9) Imágenes (solo vendedor)
                // =========================
                if (!esCliente && oParam.DescargarImagen)
                {
                    await UiStepAsync("Imágenes…", 90);

                    await ActualizarImagenesFromApi(js);
                    await oCombosItemService.ActualizarImagensComboPostSincro();

                    var oImagenAppItemService = new ImagenAppItemService();
                    await oImagenAppItemService.BuscarProductosSinImagen(js);
                }

                // =========================
                // 10) Reindex + cierre
                // =========================
                await UiStepAsync("Reindexando…", 98);

                var db = new DABSqlLite();
                // Si esto es caro, podés reindexar solo tablas tocadas o hacerlo 1 vez por día.
                await db.ReindexTableAsync("CombosHeadItem");
                await db.ReindexTableAsync("CombosProductosItem");
                await db.ReindexTableAsync("ClienteItem");
                await db.ReindexTableAsync("PedidoCabeceraItem");
                await db.ReindexTableAsync("PedidosDetalleItem");
                await db.ReindexTableAsync("ProductoListaItem");
                await db.ReindexTableAsync("ProductosSubCategoriasItem");
                await db.ReindexTableAsync("CombosXClientesItem");
                await db.ReindexTableAsync("LoQueMasTeGustaItem");
                await db.ReindexTableAsync("GPSLocationPointItem");
                await db.ReindexTableAsync("ParametrosItem");
                await db.ReindexTableAsync("ProductosBloqueadosxCliente");

                oProductosService.DeleteCacheProductos(true);
                oParam.IrDirectoPostSincro = 1;

                await UiFinishOkAsync();
                return true;
            }
            catch (OperationCanceledException)
            {
                await UiFinishFailAsync();
                return false;
            }
            catch (Exception)
            {
                // Acá podés loguear en sqlite/localstorage: “Error en Sincro TT”
                // _logger?.LogError(ex, "Error en SincronizarTT");

                await UiFinishFailAsync();
                return false;
            }
        }

        // ======= Dependencias que ya tenías (stubs para que compile el ejemplo) =======
        //private Task<string> GetUrlaApi() => Task.FromResult("https://tu-api/");
        //private Task<dynamic> ValidarVD(string v, string p) => Task.FromResult((dynamic)new
        //{
        //    CantidadEstrellitasVendedor = 0,
        //    CoheficienteComision = 0m,
        //    UrlHomeVendedor = "",
        //    ListEstrellas = new List<object>()
        //});

        //private Task ActualizarImagenesFromApi(IJSRuntime js) => Task.CompletedTask;


        //public async Task<bool> SincronizarTT(string vdLogueado, IJSRuntime JS)
        //{

        //    try
        //    {
        //        //A modo de prueba
        //        //****************************************************************************************************
        //        await JS.InvokeVoidAsync("resetProgressBar");
        //        // int cantPasos = 16;
        //        await JS.InvokeVoidAsync("showLoader");
        //        await JS.InvokeVoidAsync("showMSG");
        //        //    await JS.InvokeVoidAsync("replaceSyncMessage", "Paso 1/"+ cantPasos + "\nIniciando Sincro\n" + DateTime.Now);
        //        await JS.InvokeVoidAsync("updateProgressBar", 5);

        //        string urlApi = await GetUrlaApi();
        //        //     string urlApi = "http://localhost:5101/";
        //        ParametrosServices oParam = new ParametrosServices();
        //        version = oParam.VersionApp;
        //        int color = Int32.Parse(oParam.Color);
        //        var httpClient = new HttpClient();
        //        httpClient.BaseAddress = new Uri(urlApi);
        //        static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
        //        httpClient.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));

        //        //agregado para saber si es Cliente o Vendedor el usurio 5-8
        //        esCliente = oParam.TipoDeUsuario;

        //        httpClient.Timeout = TimeSpan.FromMinutes(4);
        //        LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);


        //        GPSService GpsS = new GPSService();
        //        var hayInternet = await GpsS.IsConnectedToInternet();
        //        if (!hayInternet) { return false; }

        //        DateTime ultimafechaSincronizacion = DateTime.MinValue;



        //        //****************************************************************************************************



        //        var vdId = Int32.Parse(vdLogueado);

        //        //agregado para saber si es Cliente o Vendedor el usurio 5-8
        //        esCliente = oParam.TipoDeUsuario;

        //        string vendedorID = oParam.Vendedor;
        //        string pwd = oParam.Pwd;
        //        var resp = await ValidarVD(vendedorID, pwd);
        //        oParam.EstrellitasVendedor = resp.CantidadEstrellitasVendedor;
        //        oParam.CoheficienteComisionVendedor = resp.CoheficienteComision;
        //        oParam.urlDisconsultas = resp.UrlHomeVendedor;
        //        int empresaID = oParam.EmpresaID;
        //        /* //traer Precios Modificados
        //         await JS.InvokeVoidAsync("replaceSyncMessage", "Precios Modificados");
        //         await JS.InvokeVoidAsync("updateProgressBar", 5);
        //         var lsPreciosModificados = await httpClient.GetFromJsonAsync<List<int>>("ProductosItem/PreciosModificados/");
        //         await LocalStorage.SetValueAsync("lsPreciosModificados", System.Text.Json.JsonSerializer.Serialize(lsPreciosModificados));

        //         //traer Stock Menor a 2 dias
        //         await JS.InvokeVoidAsync("replaceSyncMessage", "Stock Menor a 2 dias");
        //         await JS.InvokeVoidAsync("updateProgressBar", 5);
        //         var lsStockMenorDosDias = await httpClient.GetFromJsonAsync<List<int>>("ProductosItem/StockMenorDosDias/");
        //         await LocalStorage.SetValueAsync("lsStockMenorDosDias", System.Text.Json.JsonSerializer.Serialize(lsStockMenorDosDias));
        //        */

        //        oParam.accessTokenMP = await oParam.GetParametroAppSeting("accessTokenMP");
        //        oParam.publicKeyMP = await oParam.GetParametroAppSeting("publicKeyMP");
        //        oParam.MP_Test = await oParam.GetParametroAppSeting("MP_Test");

        //        //traer nuevos ingresos 
        //        await JS.InvokeVoidAsync("replaceSyncMessage", AppResources.NuevosIngresos);
        //        await JS.InvokeVoidAsync("updateProgressBar", 10);
        //        var lsNuevosIngresos = await httpClient.GetFromJsonAsync<List<int>>("ProductosItem/nuevosingresos/");
        //        //var lsNuevosIngresos = await httpClient.GetFromJsonAsync<List<int>>("ProductosItem/nuevosingresos/" + empresaID);
        //        await LocalStorage.SetValueAsync("lsNuevosIngresos", System.Text.Json.JsonSerializer.Serialize(lsNuevosIngresos));

        //        //var lsCategItem = await httpClient.GetFromJsonAsync<List<int>>("CategoriasEcom/1");
        //        //await LocalStorage.SetValueAsync("lsNuevosIngresos", System.Text.Json.JsonSerializer.Serialize(lsCategItem));

        //        ////traer lo mas pedido 
        //        await JS.InvokeVoidAsync("replaceSyncMessage", "Lo mas vendido");                                                  
        //        await JS.InvokeVoidAsync("updateProgressBar", 15);
        //        var lsLoMasPedido = await httpClient.GetFromJsonAsync<List<LomasvendidoItem>>("ProductosItem/getrecomendadosTop/" + vdId + "/" + color + "/");
        //        //var lsLoMasPedido = await httpClient.GetFromJsonAsync<List<LomasvendidoItem>>("ProductosItem/getrecomendadosTop/" + vdId + "/" + color + "/" + empresaID);
        //        await LocalStorage.SetValueAsync("lsLoMasPedido", System.Text.Json.JsonSerializer.Serialize(lsLoMasPedido));


        //        await JS.InvokeVoidAsync("replaceSyncMessage", " Actualizando estrellas");
        //        await JS.InvokeVoidAsync("updateProgressBar", 20);
        //        if (!esCliente)
        //        {
        //            var res2 = await ValidarVD(oParam.Vendedor, oParam.Pwd);
        //            oParam.listEstrellas = res2.ListEstrellas;
        //            oParam.EstrellitasVendedor = res2.CantidadEstrellitasVendedor;
        //            oParam.CoheficienteComisionVendedor = res2.CoheficienteComision;
        //        }


        //        //traer lo que mas le gusta al cliente
        //        await JS.InvokeVoidAsync("replaceSyncMessage", "Mas Comprado por Cliente");
        //        await JS.InvokeVoidAsync("updateProgressBar", 25);
        //        var lsmasmeGusta = await httpClient.GetFromJsonAsync<List<LoQueMasTeGustaItem>>("ClienteItem/mastegusta/" + vdId + "/" + color + "/");
        //        //var lsmasmeGusta = await httpClient.GetFromJsonAsync<List<LoQueMasTeGustaItem>>("ClienteItem/mastegusta/" + vdId + "/" + color + "/" + empresaID);
        //        ClienteItemService oClienteItemService = new ClienteItemService();
        //        await oClienteItemService.SincronizarMasmeGusta(lsmasmeGusta);


        //        //traer  combos habilitados por cliente
        //        await JS.InvokeVoidAsync("replaceSyncMessage", "Combos x Cliente");
        //        await JS.InvokeVoidAsync("updateProgressBar", 30);
        //        CombosxclienteService oCombosxclienteService = new CombosxclienteService();
        //        var lsCombosxCli = await httpClient.GetFromJsonAsync<List<CombosXClientesItem>>("combosxcliente/" + vdId);
        //        await oCombosxclienteService.Sincronizar(lsCombosxCli);

        //        //traer categorias    
        //        await JS.InvokeVoidAsync("replaceSyncMessage", " Categorias");
        //        await JS.InvokeVoidAsync("updateProgressBar", 35);
        //        var lsCategorias = await httpClient.GetFromJsonAsync<IEnumerable<CategoriaItem>>("CategoriasEcom/");
        //        //var lsCategorias = await httpClient.GetFromJsonAsync<IEnumerable<CategoriaItem>>("CategoriasEcom/" + empresaID);
        //        await LocalStorage.SetValueAsync("lsCategorias", System.Text.Json.JsonSerializer.Serialize(lsCategorias));

        //        //traer subcategorias    
        //        await JS.InvokeVoidAsync("replaceSyncMessage", "Subcategorias");
        //        await JS.InvokeVoidAsync("updateProgressBar", 40);
        //        var lsSubCategorias = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.SubCategoriasApiEcomItem>>("SubCategoriasEcom/");
        //        //var lsSubCategorias = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.SubCategoriasApiEcomItem>>("SubCategoriasEcom/" + empresaID);
        //        await LocalStorage.SetValueAsync("lsSubCategorias", System.Text.Json.JsonSerializer.Serialize(lsSubCategorias));

        //        //traer clientes de api
        //        //si no es cliente, solo si es vendedor
        //        if (!esCliente)
        //        {
        //            await JS.InvokeVoidAsync("replaceSyncMessage", "Clientes");
        //            await JS.InvokeVoidAsync("updateProgressBar", 45);
        //            var lsCientes = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ClienteItem>>("ClienteItem/" + vdId);
        //            await oClienteItemService.Sincronizar(lsCientes);

        //        }
        //        else
        //        {
        //            await JS.InvokeVoidAsync("replaceSyncMessage", "Clientes");
        //            await JS.InvokeVoidAsync("updateProgressBar", 45);

        //            var clId = oParam.ClienteID;
        //            var clienteItemUno = await oClienteItemService.GetByID(clId);
        //            MercadoPagoService oMercadoPagoService = new MercadoPagoService();
        //            var percepciones = await oMercadoPagoService.ConsultarPercepcionesPorCUIT(clienteItemUno.cli_CUIT);
        //            await oParam.GuardarPercepcionesAsync(percepciones);

        //            string montoMinimoApi = await oParam.GetParametroAppSeting("MontoMinimoAppClientes");
        //            if (decimal.TryParse(montoMinimoApi, out decimal montoMinimo))
        //            {
        //                oParam.montoMinimo = montoMinimo;
        //            }
        //            else
        //            {
        //                // Aquí puedes manejar el caso en el que no se pudo convertir el valor
        //                oParam.montoMinimo = 0;  // Por ejemplo, asignando un valor predeterminado
        //            }
        //            //funciono hasta 26-12, ahora tomamos parametro API
        //            //oParam.montoMinimo = await oMercadoPagoService.ObtenerMontoVentaMinima();

        //        }

        //        //cambios 28-01 
        //        string fechaUltimaSincro = "-1";
        //        try
        //        {
        //            ultimafechaSincronizacion = oParam.FechaUltimaSincronizacion;
        //            fechaUltimaSincro = ultimafechaSincronizacion.AddDays(-1).ToString("yyyyMMddTHHmmss");//20250131T153045
        //            //fechaUltimaSincro = "20250131T153045";
        //        }
        //        catch
        //        {
        //            fechaUltimaSincro = "-1";
        //        }


        //        //traer productos de api
        //        await JS.InvokeVoidAsync("replaceSyncMessage", " *Productos*");

        //        await JS.InvokeVoidAsync("updateProgressBar", 50);
        //        //Sincronizacion productos anterior
        //        //var lsProductoListaItem2 = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ProductoListaItem>>("ProductosItem/" + vdLogueado + "/0/" + version);
        //        //var lsProductoListaItem = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ProductoListaItem>>("ProductosItem/productosPorFecha/" + vdLogueado + "/0/" + version + "/" + fechaUltimaSincro);

        //        var lsProductoListaItem = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ProductoListaItem>>("ProductosItem/productosPorFecha/" + vdLogueado + "/0/" + version + "/" + "-1");

        //        //var ls10567 = lsProductoListaItem.Where(p => p.ProductosID == 10567);

        //        var prodIDParaBorrar = lsProductoListaItem.Select(p => p.ProductosID);
        //        ProductoListaService oProductosService = new ProductoListaService();
        //        //await oProductosService.DeleteProductoPorID(prodIDParaBorrar);
        //        await oProductosService.BorrarTodoProductoListaItem();
        //        if (lsProductoListaItem.Count() > 0)
        //        {
        //            if (lsProductoListaItem.ToList()[0].ProductosID == 999999999)
        //            {
        //                await App.Current.MainPage.DisplayAlert("Atencion", lsProductoListaItem.ToList()[0].Nombre, "OK");
        //                App.Current.Quit();

        //                return false; 
        //            }
        //        }


        //        //comento para probar que se actualice al final --
        //        //await JS.InvokeVoidAsync("replaceSyncMessage", " *Productos Actualizados*");

        //        //await JS.InvokeVoidAsync("updateProgressBar", 55);

        //        //await JS.InvokeVoidAsync("replaceSyncMessage", " *Productos Actualizados*");
        //        await oProductosService.SincronizarActualizados(lsProductoListaItem,JS);
        //        //comento para probar que se actualice al final ---

        //        //Sincronizacion productos anterior
        //        //await oProductosService.Sincronizar(lsProductoListaItem2);

        //        //Actaulizacion de STOCK


        //        await JS.InvokeVoidAsync("replaceSyncMessage", " *Actualizando stock*");

        //        await JS.InvokeVoidAsync("updateProgressBar", 60);
        //        var lsProdStk = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ProductoStockItem>>("ProductosItem/StockProductos/" + fechaUltimaSincro);      //////////////////////////        QUEDAMOS AQUIIII    

        //        if (lsProdStk.Count() > 0) 
        //        {
        //            var cantUpdate = await oProductosService.UpdateStockEnProductoBloque(lsProdStk);                    
        //        }

        //        //Actaulizacion de STOCK
        //        var lsPreciosProd = await httpClient.GetFromJsonAsync<List<EntidadesAppVendedores.ProductoPrecioItems>>("ProductosItem/PreciosProductos/" + fechaUltimaSincro);
        //        if (lsPreciosProd.Count() > 0)
        //        {
        //            var cantUpdatePreci = await oProductosService.UpdatePreciosEnProductoBloque(lsPreciosProd);

        //        }


        //        if (ultimafechaSincronizacion.Date != DateTime.Now.Date)
        //        {
        //            //Actualizacion de producto items

        //            await JS.InvokeVoidAsync("replaceSyncMessage", " *Productos!*");

        //            await JS.InvokeVoidAsync("updateProgressBar", 65);
        //            var lsProdAct = await httpClient.GetFromJsonAsync<List<EntidadesAppVendedores.ProductosActualizarItems>>("ProductosItem/ProductosActualItems/" + fechaUltimaSincro);
        //            if (lsProdAct.Count() > 0)
        //            {
        //                var cantUpdatePreci = await oProductosService.UpdateProductoBloquePorItems(lsProdAct);

        //            }
        //        }

        //        //await JS.InvokeVoidAsync("replaceSyncMessage", " *Productos¡*");

        //        //await JS.InvokeVoidAsync("updateProgressBar", 70);
        //        //Reindexa la tabla elegida
        //        var oDBase = new DABSqlLite();
        //        //await oDBase.ReindexTableAsync("ProductoListaItem");

        //        //traer lista de combos por vd


        //        await JS.InvokeVoidAsync("replaceSyncMessage", "Combos x VD");
        //        await JS.InvokeVoidAsync("updateProgressBar", 70);
        //        //Sincronizacion Combos anterior
        //        //var lsCombos2 = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.CombosItemJson>>("combosvd/" + vdId);             
        //        CombosItemService oCombosItemService = new CombosItemService();
        //        //await oCombosItemService.Sincronizar(lsCombos2);


        //        IEnumerable<EntidadesAppVendedores.CombosItemJson> lsCombos;

        //        if (!esCliente)
        //        {   //cambios 31-01  combosPorFecha
        //            //fechaUltimaSincro = "20000000T083000";
        //            lsCombos = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.CombosItemJson>>("combosPorFecha/" + vdId + "/" + fechaUltimaSincro);

        //        }
        //        else
        //        {
        //            var clId = oParam.ClienteID;
        //            //fechaUltimaSincro = "20000000T083000";
        //            lsCombos = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.CombosItemJson>>("combosPorFecha/" + vdId + "/" + fechaUltimaSincro+ "/" + clId);
        //        }



        //        var lsCombosParaBorrar = lsCombos.Select(p => p.comb_combosid);
        //        List<int> lsCombosIdsBorrar = lsCombosParaBorrar.ToList();
        //         await oCombosItemService.SincronizarActualizados(lsCombos, lsCombosIdsBorrar);


        //        //traer lista productos bloqueados por cliente
        //        await JS.InvokeVoidAsync("replaceSyncMessage", "Productos x Cliente");
        //        await JS.InvokeVoidAsync("updateProgressBar", 75);
        //        var lsBloquedos = await httpClient.GetFromJsonAsync<IEnumerable<EntidadesAppVendedores.ProductosBloqueadosxCliente>>("ClienteItem/productosbloqueadoxcliente/ " + vdId);
        //        //      CombosItemService oCombosItemService = new CombosItemService();
        //        ProductosBloqueadosxClienteService oPBlo = new ProductosBloqueadosxClienteService();
        //        await oPBlo.AddProductosbloqueados(lsBloquedos.ToList());




        //        //traer listado de producots pos subcategoria
        //        await JS.InvokeVoidAsync("replaceSyncMessage", " Subcategorias");
        //        await JS.InvokeVoidAsync("updateProgressBar", 80);
        //        var lsSub = await httpClient.GetFromJsonAsync<List<ProductosSubCategoriasItem>>("ProductosItem/subxprod");
        //        ProductosSubCategoriasService oProductosSubCategoriasService = new ProductosSubCategoriasService();
        //        await oProductosSubCategoriasService.SincronizarProductosSubCategorias(lsSub);


        //        //await ActualizarImagenesFromApi(JS);

        //        //await JS.InvokeVoidAsync("replaceSyncMessage", " Imagenes para combos");
        //        //await JS.InvokeVoidAsync("updateProgressBar", 5);
        //        //await oCombosItemService.ActualizarImagensComboPostSincro();

        //        //ImagenAppItemService oImagenAppItemService = new ImagenAppItemService();
        //        //await JS.InvokeVoidAsync("replaceSyncMessage", " Fotos Faltantes");
        //        //await JS.InvokeVoidAsync("updateProgressBar", 8);
        //        //await oImagenAppItemService.BuscarProductosSinImagen(JS);

        //        //cambios 23-1
        //        if (!esCliente)
        //        {
        //            ////FOTOSSSSSSSSSSSSSSSSSSSSS
        //             await ActualizarImagenesFromApi(JS);
        //            //await oProductosService.SincronizarActualizados(lsProductoListaItem);


        //            await JS.InvokeVoidAsync("replaceSyncMessage", " Imagenes para combos");
        //            await JS.InvokeVoidAsync("updateProgressBar", 85);
        //            await oCombosItemService.ActualizarImagensComboPostSincro();


        //            ImagenAppItemService oImagenAppItemService = new ImagenAppItemService();
        //            await JS.InvokeVoidAsync("replaceSyncMessage", " Fotos Faltantes");
        //            await JS.InvokeVoidAsync("updateProgressBar", 90);
        //            await oImagenAppItemService.BuscarProductosSinImagen(JS);

        //            //if (cantidadImg > 0)
        //            //{
        //            //    var img = "";
        //            //    var imgItem = await oImgService.GetByID(pid);
        //            //    if (imgItem != null)
        //            //    {
        //            //        img = imgItem.ImagenChica;

        //            //    }
        //            //    it.ImagenDataBase = img;

        //            //}

        //            //await JS.InvokeVoidAsync("replaceSyncMessage", " *Productos Actualizados*");

        //            ////await JS.InvokeVoidAsync("updateProgressBar", 55);



        //        }








        //        await JS.InvokeVoidAsync("updateProgressBar", 100);

        //        await JS.InvokeVoidAsync("replaceSyncMessage", " Finalizado");

        //        await oDBase.ReindexTableAsync("CombosHeadItem");
        //        await oDBase.ReindexTableAsync("CombosProductosItem");
        //        await oDBase.ReindexTableAsync("ClienteItem");
        //        await oDBase.ReindexTableAsync("PedidoCabeceraItem");
        //        await oDBase.ReindexTableAsync("PedidosDetalleItem");
        //        await oDBase.ReindexTableAsync("ProductoListaItem");
        //        await oDBase.ReindexTableAsync("ProductosSubCategoriasItem");
        //        await oDBase.ReindexTableAsync("CombosXClientesItem");
        //        await oDBase.ReindexTableAsync("LoQueMasTeGustaItem");
        //        await oDBase.ReindexTableAsync("GPSLocationPointItem");
        //        await oDBase.ReindexTableAsync("ParametrosItem");
        //        await oDBase.ReindexTableAsync("ProductosBloqueadosxCliente");


        //        //       await JS.InvokeVoidAsync("replaceSyncMessage", "Paso 17/" + cantPasos + "\nFINALIZO SINCRO DE DATOS\n" + DateTime.Now);
        //        oProductosService.DeleteCacheProductos(true);
        //        await JS.InvokeVoidAsync("hideLoader");


        //        oParam.IrDirectoPostSincro = 1;

        //        return true;
        //    }
        //    catch
        //    {
        //        //  LocalStorage.GuardarLog("Error en Sincro TT", JS);
        //        await JS.InvokeVoidAsync("hideLoader");
        //        return false;
        //    }

        //}


        //NUEVA SINC IMAGESN
        public async Task<int> ActualizarImagenesFromApi(IJSRuntime JS)
        {

            GPSService GpsS = new GPSService();
            var hayInternet = await GpsS.IsConnectedToInternet();

            if (hayInternet)
            {

                ParametrosServices oParam = new ParametrosServices();
                var fecha = oParam.FechaUltimaSincronizacionImagen;
                string urlApi = await GetUrlaApi();
                ImagenSqlItem proItem = new ImagenSqlItem();
                HttpClient httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(urlApi);
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                httpClient.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));



                httpClient.Timeout = TimeSpan.FromMinutes(2);
                string timeStr = fecha.Year.ToString() + "-" + fecha.Month.ToString() + "-" + fecha.Day.ToString();
                var lsImg = new List<int>();
                try
                {
                    lsImg = await httpClient.GetFromJsonAsync<List<int>>("ProductosItem/imagenesxfecha/" + timeStr);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FuncionesSincronizar] Error ActualizarImagenesFromApi lsImg: {ex.Message}");
                }

                AppVendedores2025.Services.ImagenAppItemService oImagenAppItemService = new AppVendedores2025.Services.ImagenAppItemService();
                var res = await oImagenAppItemService.ActualizarImagenFromApi(JS, lsImg);
            }

            return 1;
        }





        public async Task<ImagenSqlItem> SincronizarImagenFromApi(int productosID, DateTime fechaLocal, IJSRuntime JS)
        {
            ImagenSqlItem proItem = new ImagenSqlItem();
            ParametrosServices oParam = new ParametrosServices();

            GPSService GpsS = new GPSService();
            var hayInternet = await GpsS.IsConnectedToInternet();
            if (hayInternet)
            {

                string urlApi = await GetUrlaApi();

                var httpClient = HttpHelper.CreateClient();
                httpClient.BaseAddress = new Uri(urlApi);
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                httpClient.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));

                httpClient.Timeout = TimeSpan.FromMinutes(1);
                string timeStr = fechaLocal.Year.ToString() + "-" + fechaLocal.Month.ToString() + "-" + fechaLocal.Day.ToString();



                try
                {
                    proItem = await httpClient.GetFromJsonAsync<EntidadesAppVendedores.ImagenSqlItem>("ProductosItem/" + productosID + "/" + timeStr);
                }
                catch
                {

              
                    
                    //    await App.Current.MainPage.DisplayAlert("Atencion", "error  SincronizarImagenFromApi" + proItem.ProductosID, "OK");
                }
            }
            return proItem;
        }


        public async Task<ValidaUsuario> ValidarVD(string vd, string pwd)
        {


            var lsDisponiblePreciosCliente = new List<int>();

            if (string.IsNullOrEmpty(vd))
            {
                return new ValidaUsuario(lsDisponiblePreciosCliente);

            }

            if (string.IsNullOrEmpty(pwd))
            {
                return new ValidaUsuario(lsDisponiblePreciosCliente);

            }
            ParametrosServices oParam = new ParametrosServices();
            version = oParam.VersionApp;
          
            string urlApi = await GetUrlaApi();

            if (vd != "" && pwd != "")
            {
                var httpClient = HttpHelper.CreateClient();
                httpClient.BaseAddress = new Uri(urlApi);
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                httpClient.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                try
                {
                    // Codificar los valores para manejar caracteres especiales
                    string encodedVd = Uri.EscapeDataString(vd); // Codifica P5319# a P5319%23
                    string encodedPwd = Uri.EscapeDataString(pwd); // Codifica pwd si tiene caracteres especiales


                    // Realizar la llamada a la API con los valores decodificados
                    int empresaID = oParam.EmpresaID;
                    var res = await httpClient.GetAsync($"VendedorItem/{encodedVd}/{encodedPwd}/{version}/{empresaID}");


                    //var res = await httpClient.GetAsync("VendedorItem/" + vd + "/" + pwd + "/" + version);

                    var contents = await res.Content.ReadAsStringAsync();

                    var resu = System.Text.Json.JsonSerializer.Deserialize<ValidaUsuario>(contents);
                    //          ParametrosServices oParam = new ParametrosServices();
                    oParam.NombreDelVendedor = resu.NombreDelVendedor;

                    // Guardar el JWT que emite el login (header X-Auth-Token) para mandarlo
                    // como Authorization: Bearer en el resto de las llamadas protegidas.
                    if (res.Headers.TryGetValues("X-Auth-Token", out var tokenValues))
                        oParam.JwtToken = tokenValues.FirstOrDefault() ?? "";

                    return resu;
                }
                catch (TaskCanceledException ex)
                {
                    // Esto ocurre si se agota el timeout
                    await App.Current.MainPage.DisplayAlert("Atención", "Error de tiempo de espera agotado al validar el vendedor.", "OK");
                }
                catch (Exception ex)
                {
                    await App.Current.MainPage.DisplayAlert("Atención " + vd, "**Error** \n*Código: " + ex.HResult + "\n*Mensaje: Usuario / Contraseña incorrectas", "OK");
                }

            }
            return new ValidaUsuario(lsDisponiblePreciosCliente);
        }


        public async Task<int> Sincronizar(IJSRuntime JS, bool enIndex)
        {

            ParametrosServices oParam = new ParametrosServices();
            GPSService GpsS = new GPSService();
            EntDebug oEntDebug = new EntDebug();
            var hayInternet = await GpsS.IsConnectedToInternet();
            if (!hayInternet)
            {

                flagSincroenUso = false;
                return 2;
            }
            if (!flagSincroenUso || enIndex)
            {
                flagSincroenUso = true;


                var vdLogueado = oParam.VendedorID;
                //AppVendedores2025.Services.FuncionesSincronizar oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar();
                //var res = await oFuncionesSincronizar.SincronizarTT(vdLogueado, JS);
                //EntDebug.ModoDebug = true;
                //await EntDebug.MostrarMensajeDebug("Sincronizando");
                var res = await SincronizarTT(vdLogueado, JS);


                var modGps = "";
                modGps = await oParam.GetParametroAppSetingAsync("modoTestGps");
                oParam.ModoTestGps = modGps=="1";



                if (res)
                {
                    await JS.InvokeVoidAsync("replaceSyncMessage", " Finalizado");
                    oParam.FechaUltimaSincronizacion = DateTime.Now;
                    flagSincroenUso = false;
                   
                    return 1;
                }
                else
                {
                    flagSincroenUso = false;
                    return 2;

                }
            }
            return 3;

        }

        //public async Task<int> ValidarVersionApp(string version)
        //{
        //    //var httpClient = new HttpClient();
        //    //httpClient.BaseAddress = new Uri(urlApi);
        //    //var res = await httpClient.GetAsync($"VendedorItem/0/0/{version}");
        //    //return res;
        //}

        //public async Task<bool> CheckStatusSincro(IJSRuntime JS)
        //{
        //    ParametrosServices oParam = new ParametrosServices();
        //    bool stdatos = false;

        //    var ult_sincdatos = oParam.FechaUltimaSincronizacion;

        //    var vdLogueado = Int32.Parse(oParam.VendedorID);
        //    var dia_hoy = DateTime.Now;

        //    var dif = dia_hoy - ult_sincdatos;
        //    double horasdeultimaSincautomatica = dif.TotalHours;

        //    // Para Probar
        //    bool sincronizarSiOSi = false;


        //    if ((ult_sincdatos.Day != dia_hoy.Day || sincronizarSiOSi) && vdLogueado != null)
        //    {
        //        //borrar pedidos
        //        var oPedidosCabeceraService = new PedidoCabeceraService();
        //        await oPedidosCabeceraService.BorrarPedidoViejos();

        //        AppVendedores2025.Services.FuncionesSincronizar oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar();
        //        var res = await oFuncionesSincronizar.Sincronizar(JS, true);
        //        if (res == 1)
        //        {
        //            //      await App.Current.MainPage.DisplayAlert("Atencion", "Datos Actualizados", "OK");
        //            stdatos = true;
        //        }
        //        else if (res == 2)
        //        {
        //            stdatos = false;

        //        }
        //    }
        //    else
        //    {
        //        stdatos = true;
        //        //sincronizado datos

        //    }

        //    if (stdatos)
        //    {
        //        //           await JS.InvokeVoidAsync("CambiarStatus", 1);
        //    }
        //    else
        //    {
        //        //            await JS.InvokeVoidAsync("CambiarStatus", 2);
        //    }


        //    return stdatos;
        //}

        public async Task<bool> CheckStatusSincro(IJSRuntime JS)
        {
            ParametrosServices oParam = new ParametrosServices();
            bool stdatos = false;

            var ult_sincdatos = oParam.FechaUltimaSincronizacion;

            var vdLogueado = Int32.Parse(oParam.VendedorID);
            DateTime dia_hoy = DateTime.Now;
            //VALIDACION PARA PODER SINCRONIZAR COMPLETAMENTE LOS 1 DE CADA MES
            if (dia_hoy.Day == 1 && oParam.FechaUltimaSincronizacion == DateTime.MinValue)
                dia_hoy = dia_hoy.Date.AddDays(1).Add(dia_hoy.TimeOfDay);


            var dif = dia_hoy - ult_sincdatos;
            double horasdeultimaSincautomatica = dif.TotalHours;

            // Para Probar
            //bool sincronizarSiOSi = false;




            if (ult_sincdatos.Day != dia_hoy.Day && vdLogueado != null)
            {
                //borrar pedidos
                var oPedidosCabeceraService = new PedidoCabeceraService();
                await oPedidosCabeceraService.BorrarPedidoViejos();

                //AppVendedores2025.Services.FuncionesSincronizar oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar();
                var res = await Sincronizar(JS, true);
                if (res == 1)
                {
                    //      await App.Current.MainPage.DisplayAlert("Atencion", "Datos Actualizados", "OK");
                    stdatos = true;
                }
                else if (res == 2)
                {
                    stdatos = false;

                }
            }
            else
            {
                stdatos = true;
                //sincronizado datos

            }

            if (stdatos)
            {
                //           await JS.InvokeVoidAsync("CambiarStatus", 1);
            }
            else
            {
                //            await JS.InvokeVoidAsync("CambiarStatus", 2);
            }


            return stdatos;
        }




        static bool consultandomensajes = true;
        public  async Task<bool> ConsultarMensajesNuevos(IJSRuntime JS)
        {
            bool hayMensajes = false;

            string urlApi = await GetUrlaApi(); // tu método


            //if (consultandomensajes)
            {

                consultandomensajes = false;
              
                try
                {
                    //obterner de param
                    ParametrosServices oParam = new ParametrosServices();
                    string vdid = oParam.VendedorID;
                    string pwd = oParam.Pwd;
                   

                    string version = oParam.VersionApp;
                    //FuncionesSincronizar oFuncionesSincronizar = new FuncionesSincronizar();
                    if (vdid != "" && pwd != "")
                    {
                        var httpClient = HttpHelper.CreateClient();
                        static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                        httpClient.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                        httpClient.BaseAddress = new Uri(urlApi);
                        try
                        {

                            int empresaIDMsg = oParam.EmpresaID;
                            string urlMensajes = empresaIDMsg > 0
                                ? $"VendedorItem/mensajes/{vdid}/{pwd}/{empresaIDMsg}"
                                : $"VendedorItem/mensajes/{vdid}/{pwd}";
                            var res = await httpClient.GetAsync(urlMensajes);
                            var contents = await res.Content.ReadAsStringAsync();

                            var resu = System.Text.Json.JsonSerializer.Deserialize<List<MensajeAVendedorItem>>(contents);
                             
                            LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);
                            List<MensajeAVendedorItem> lsMSG = new List<MensajeAVendedorItem>();
                           
                         

                            //var lsMSGguardados = JsonConvert.DeserializeObject<List<MensajeAVendedorItem>>(await LocalStorage.GetValueAsync<string>("Mensajes") ?? "[]");
                            //if (lsMSGguardados.Any())
                            //{
                            //    lsMSG.AddRange(lsMSGguardados);
                            //}
                            if (resu.Any())
                            {
                                lsMSG.AddRange(resu);
                            }
                            //    var lsMSG =await LocalStorage.GetValueAsync<List<MensajeAVendedorItem>>("Mensajes");

                              LocalStorage.SetValueAsync("Mensajes", System.Text.Json.JsonSerializer.Serialize(lsMSG));

                            if (resu.Any())
                            {
                                hayMensajes = true;
                                
                            }
                               

                            //await App.Current.MainPage.DisplayAlert("Atencion",
                            //    "bloqueado:]" + resu.Bloqueado + "   mensaje:" + resu.MensajeTexto,
                            //    "OK"); //"error  Error de conexion login", "OK");
                        }
                        catch
                        {
                            //     await App.Current.MainPage.DisplayAlert("Atencion", "error  Error de conexion login", "OK");
                   //         consultandomensajes = true;
                        }

                    }
                }
                catch
                {

                 //   consultandomensajes = true;
                }

               // consultandomensajes = true;
               
            }
            consultandomensajes = true;
            return hayMensajes;
            //   return new MensajeAVendedorItem();
        }

    }

    public class DashboardEcomResponse
    {
        public List<LomasvendidoItem> RecomendadosTop { get; set; }
        public List<int> NuevosIngresos { get; set; }
        public List<LoQueMasTeGustaItem> MasTeGusta { get; set; }
        public List<CombosXClientesItem> CombosXCliente { get; set; }
        public IEnumerable<CategoriaItem> Categorias { get; set; }
        public IEnumerable<EntidadesAppVendedores.SubCategoriasApiEcomItem> SubCategorias { get; set; }
        public IEnumerable<EntidadesAppVendedores.ClienteItem> Clientes { get; set; }
    }

    public class StockYPreciosResponse
    {
        public List<EntidadesAppVendedores.ProductoStockItem> Stock { get; set; }
        public List<EntidadesAppVendedores.ProductoPrecioItems> Precios { get; set; }
    }

    public class BloqueadosYSubcategoriasResponse
    {
        public List<EntidadesAppVendedores.ProductosBloqueadosxCliente> Bloqueados { get; set; }
        public List<ProductosSubCategoriasItem> SubCategorias { get; set; }
    }

}

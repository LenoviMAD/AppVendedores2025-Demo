//using Microsoft.AspNetCore.Mvc.Razor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using EntidadesAppVendedores;
using AppVendedores2025.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
//using AppVendedores.Pages;
using System.Net.NetworkInformation;
//using Android.Media;
using System;
using static Microsoft.Maui.Controls.WebView;
namespace AppVendedores2025.Shared
{



    public abstract class UtilCompartidas : ComponentBase
    {

        //public class Portal
        //{
        //    private WebView webView;

        //    public Portal(WebView webView)
        //    {
        //        this.webView = webView;
        //    }

        //    public async Task OpenGoogle()
        //    {
        //        await webView.NavigateAsync(webView);
        //    }
        //}

        //[JSInvokable]
        //public void HandleBackButton()
        //{
        //    // C�digo para manejar el evento del bot�n "back"

        //    var stop = 0;
        //}
        //public async Task<List<ProductoListaItem>> GenerarCarritoProducto(int clienteID)
        //{
        //  //  int clienteID = Int32.Parse(await JS.InvokeAsync<string>("getCookie", "clienteID"));

        //    string textoFiltro = "";
        //    List<ProductoListaItem> cartview = new List<ProductoListaItem>();
        //    List<ProductoListaItem> lsProductosTT = new List<ProductoListaItem>();
        //    List<ProductoListaItem> lsProductos = new List<ProductoListaItem>();
        //    ProductoListaService oProductoListaService = new ProductoListaService();
        //    lsProductosTT = await oProductoListaService.GetAll();
        //    lsProductos = await oProductoListaService.LlenarCantidadEnCarrito(clienteID, lsProductosTT.Where(c => c.TextoParaBusqueda.IndexOf(textoFiltro, StringComparison.InvariantCultureIgnoreCase) != -1 && c.ComboID == 0).ToList());

        //    cartview = lsProductosTT.Where(w => w.CantidadEnCarrito != 0).ToList();

        //    return cartview;
        //}

        public async Task GuardarLog(string msg, IJSRuntime JS)
        {
            LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);

            var logmsg = await LocalStorage.GetValueAsync<string>("log");
            await LocalStorage.SetValueAsync("log", logmsg + DateTime.Now + "-> inicia sincro lsNuevosIngresos \n ");

        }

        public async Task<List<ProductoListaItem>> GenerarCarritoProductoCombos(int clienteID)
        {
            //      int clienteID = Int32.Parse(await JS.InvokeAsync<string>("getCookie", "clienteID"));


            //     List<ProductoListaItem> cartviewcombos = new List<ProductoListaItem>();
            //   List<ProductoListaItem> lsProductosTT = new List<ProductoListaItem>();
            List<ProductoListaItem> lsProductosEnCombo = new List<ProductoListaItem>();
            ProductoListaService oProductoListaService = new ProductoListaService();
            //     lsProductosTT = await oProductoListaService.GetAll();
            lsProductosEnCombo = await oProductoListaService.LlenarCantidadEnCarritoCombos(clienteID);


            return lsProductosEnCombo;
        }

        public async Task<List<PedidosDetalleItem>> BorrarCarritodelCliente_shared(int clienteID)
        {
            // int clienteID = Int32.Parse(await JS.InvokeAsync<string>("getCookie", "clienteID"));
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            List<PedidosDetalleItem> cart = new List<PedidosDetalleItem>();
            cart = await oPedidosDetalleItemService.GetByClienteIdSinTx(clienteID);
            if (cart.Any())
            {
                await oPedidosDetalleItemService.BorrarByClienteIdSinTx(cart[0].PedidoCabeceraID);
            }
            cart = await oPedidosDetalleItemService.GetByClienteIdSinTx(clienteID);
            //    totalCarrito = 0;
            //  Traer();
            this.StateHasChanged();

            return cart;
        }


        public async Task<int> ActualizarCantidadCarrito_Shared(ChangeEventArgs e, ProductoListaItem proIT, int cantincrementar, IJSRuntime JS, int listaPrecio, bool cf)
        {
            ParametrosServices oParam = new ParametrosServices();
            ClienteItemService oclienteItemService = new ClienteItemService();

            int VDID = Int32.Parse(oParam.VendedorID);
            int clienteID = oParam.ClienteID;
            string version = oParam.VersionApp;
            int cantNueva;
            var cliDatos = await oclienteItemService.GetByID(clienteID);

            PedidoCabeceraService oPedidoCabeceraService = new PedidoCabeceraService();



            if (!(e is null))
            {


                cantNueva = Convert.ToInt32(Math.Floor(Convert.ToDouble(e.Value.ToString())));


                if (cantNueva < 0) cantNueva = 0;
                if (cantNueva > proIT.StockEnUnidades) { cantNueva = proIT.StockEnUnidades; }

                await oPedidoCabeceraService.AgregarPedidosProductos(clienteID, proIT.ProductosID, cantNueva, listaPrecio , 0, 0,0, 0, cliDatos.cli_codigo, cliDatos.cli_direccion, proIT.Nombre, proIT.CodigoDeProducto, VDID, cf, proIT.UnidadesPorBulto, version);

            }
            else
            {
                cantNueva = proIT.CantidadEnCarrito + cantincrementar;
                if (cantNueva < 0) cantNueva = 0;
                if (cantNueva > proIT.StockEnUnidades) { cantNueva = proIT.StockEnUnidades; }
                await oPedidoCabeceraService.AgregarPedidosProductos(clienteID, proIT.ProductosID, cantNueva, listaPrecio, 0, proIT.PrecioUnitarioFinal(listaPrecio), proIT.PrecioUnitarioNeto(listaPrecio), 0, cliDatos.cli_codigo, cliDatos.cli_direccion, proIT.Nombre, proIT.CodigoDeProducto, VDID, cf, proIT.UnidadesPorBulto, version);
            }
            proIT.CantidadEnCarrito = cantNueva;





            return cantNueva;
        }




        //private async Task ActualizarCantidadCarritoCombos_shared(ChangeEventArgs e, ProductoListaItem proIT, int cantincrementar, IJSRuntime JS)
        //{

        //    int clienteID = Int32.Parse(await JS.InvokeAsync<string>("getCookie", "clienteID"));
        //    //  int lsprcios = 1;

        //    int cantNueva;

        //    if (!(e is null))
        //    {
        //        if (e.Value.ToString() == "") { cantNueva = 0; }
        //        else
        //        {
        //            cantNueva = Int32.Parse(e.Value.ToString());
        //        }

        //        if (cantNueva < 0) cantNueva = 0;
        //    }
        //    else
        //    {
        //        cantNueva = proIT.CantidadEnCarrito + cantincrementar;
        //        if (cantNueva < 0) cantNueva = 0;

        //    }
        //    proIT.CantidadEnCarrito = cantNueva;


        //    //----CC
        //    var cantCCenComboAbierto = lsProductosCC.Sum(c => c.CantidadEnCarrito);

        //    //ver si es dinamico
        //    if (comboHeadItem.CantidadDinamica != 0)
        //    {
        //        //dinamico
        //        var cantDinamicaGrp = comboHeadItem.GetCantidadesGrupo;
        //        var cantGrupoCarr = lsProductosCC.Where(c => c.NroDeGrupoCombo == proIT.NroDeGrupoCombo).Sum(c => c.CantidadEnCarrito);
        //        //limitar si es dinamico bajar a maximo prd cc
        //        if (cantGrupoCarr > cantDinamicaGrp[proIT.NroDeGrupoCombo] * cantidadDeCombo)
        //        {
        //            var sobra = cantDinamicaGrp[proIT.NroDeGrupoCombo] * cantidadDeCombo - cantGrupoCarr;
        //            proIT.CantidadEnCarrito += sobra;
        //            this.StateHasChanged();
        //        }
        //    }

        //    // validar cant total en combo cc
        //    var cantTotalCC = lsProductosCC.Where(c => c.ConCargo).Sum(c => c.CantidadEnCarrito);

        //    if (cantTotalCC > cantCC * cantidadDeCombo)
        //    {
        //        var sobra = cantCC * cantidadDeCombo - cantTotalCC;
        //        proIT.CantidadEnCarrito += sobra;
        //        this.StateHasChanged();
        //    }

        //    //----------SC
        //    var cantSCenComboAbierto = lsProductosSC.Sum(c => c.CantidadEnCarrito);



        //    // validar cant total en combo sc
        //    var cantTotalSC = lsProductosSC.Sum(c => c.CantidadEnCarrito);

        //    if (cantTotalSC > comboHeadItem.CantSCargo * cantidadDeCombo)
        //    {
        //        var sobra = comboHeadItem.CantSCargo * cantidadDeCombo - cantTotalSC;
        //        proIT.CantidadEnCarrito += sobra;
        //        this.StateHasChanged();
        //    }
        //    //----fin sc


        //    CheckStatusCombo1();
        //    //  CheckStatusCombo();
        //    this.StateHasChanged();

        //}

        public async Task<int> BorrarCombodelCArritoxClienteID_shared(int clienteID, int comboid, int pedidoCabeceraID)
        {
            // int clienteID = Int32.Parse(await JS.InvokeAsync<string>("getCookie", "clienteID"));
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            await oPedidosDetalleItemService.BorrarCombodelCArritoxClienteIDsinTX(clienteID, comboid, pedidoCabeceraID);


            this.StateHasChanged();

            return 1;
        }
        public List<(int listaID,string nombreDeLaLista)>  GetListasParaUsar()
        {
            ParametrosServices oParam = new ParametrosServices();
            var vdLogueadoID = oParam.VendedorID;
            var tipoVD=oParam.tipoVD;
            List<(int listaID, string nombreDeLaLista)> lsListas = new();

            if (tipoVD==0)
            {
                return lsListas;
            }

            switch (tipoVD)
            {
                case 999:
                    lsListas.Add(new ( 7, "E-commerce"));
                    break;
                case 6:
                    lsListas.Add(new(4, "Salon"));
                    break;


                default:
                    lsListas.Add(new(3, "Autoservicios"));
                    lsListas.Add(new(1, "Especial Distribucion"));
                    lsListas.Add(new(5, "Almacen"));
                    lsListas.Add(new(6, "Kioskos"));

                    break;


            }
            return lsListas;

        }
        public int SeleccionarLista(int ListaSeleccionada )
        {
            ParametrosServices oParam = new ParametrosServices();
            //   int ListaSeleccionada = Int32.Parse(e.Value.ToString());
            switch (ListaSeleccionada)
            {
                case 1:

                    oParam.AddorUpdateAsync("listadePrecios", "", ListaSeleccionada, new DateTime());
                    oParam.AddorUpdateAsync("listadePreciosNombre", "Especial Distribucion", 0, new DateTime());

                    break;
                case 6:
                    oParam.AddorUpdateAsync("listadePrecios", "", ListaSeleccionada, new DateTime());
                    oParam.AddorUpdateAsync("listadePreciosNombre", "Kioskos", 0, new DateTime());


                    break;
                case 3:
                    oParam.AddorUpdateAsync("listadePrecios", "", ListaSeleccionada, new DateTime());
                    oParam.AddorUpdateAsync("listadePreciosNombre", "Autoservicios", 0, new DateTime());



                    break;

                case 5:

                    oParam.AddorUpdateAsync("listadePrecios", "", ListaSeleccionada, new DateTime());
                    oParam.AddorUpdateAsync("listadePreciosNombre", "Almacenes", 0, new DateTime());

                    break;


                case 4:
                    oParam.AddorUpdateAsync("listadePrecios", "", ListaSeleccionada, new DateTime());
                    oParam.AddorUpdateAsync("listadePreciosNombre", "Salon", 0, new DateTime());

                    break;

                

                case 7:
                    oParam.AddorUpdateAsync("listadePrecios", "", ListaSeleccionada, new DateTime());
                    oParam.AddorUpdateAsync("listadePreciosNombre", "Ecom", 0, new DateTime());

                    break;

            }
            return ListaSeleccionada;

        }
        public string MostrarLista(int ListaSeleccionada)
        {
            string res = "";
            switch (ListaSeleccionada)
            {
                case 1:

                    res = "ES";

                    break;
                case 6:
                    res = "KI";
                    break;
                case 3:
                    res = "AU";
                    break;
                case 5:
                    res = "AL";
                    break;
                case 4:
                    res = "SA";
                    break;
                case 7:
                    res = "EC";
                    break; 
               
            }
            return res;

        }

        public async Task<List<ProductoListaItem>> GetLoQmasMeGusta(int clienteID, List<ProductoListaItem> lsProductosTotal, int Color)
        {
            try
            {
                ClienteItemService oClienteItemService = new ClienteItemService();
                var lsMasMeGustaTotal = await oClienteItemService.GetLoQueMasTeGustaByClienteID(clienteID);

                // Materializamos para evitar re-evaluaciones
                var lsMasMeGustaPropios = lsMasMeGustaTotal.Where(c => c.ClienteID == clienteID).ToList();
                var lsMasMeGustaNoPropios = lsMasMeGustaTotal.Where(c => c.ClienteID != clienteID).ToList();

                // Diccionarios de orden por ProductoID y por ProductosIDPadre (tomamos el "Orden" m�nimo si se repite)
                int MinOrDefault(int a, int b) => (a <= 0) ? b : (b <= 0 ? a : Math.Min(a, b));

                var ordenPropios = new Dictionary<int, int>();     // clave: ProductoID   valor: Orden
                var ordenNoPropios = new Dictionary<int, int>();   // clave: ProductoID   valor: Orden

                // Cargar diccionario de "propios"
                foreach (var m in lsMasMeGustaPropios)
                {
                    // Si hay duplicados del mismo producto, conservamos el orden m�nimo
                    if (ordenPropios.TryGetValue(m.ProductoID, out var ord))
                        ordenPropios[m.ProductoID] = MinOrDefault(ord, m.Orden);
                    else
                        ordenPropios[m.ProductoID] = m.Orden;
                }

                // Cargar diccionario de "no propios"
                foreach (var m in lsMasMeGustaNoPropios)
                {
                    if (ordenNoPropios.TryGetValue(m.ProductoID, out var ord))
                        ordenNoPropios[m.ProductoID] = MinOrDefault(ord, m.Orden);
                    else
                        ordenNoPropios[m.ProductoID] = m.Orden;
                }

                // Buscadores r�pidos (HashSet) para pertenencia
                var setPropios = new HashSet<int>(ordenPropios.Keys);
                var setNoPropios = new HashSet<int>(ordenNoPropios.Keys);

                // Listas resultado (materializamos y ordenamos luego)
                var propios = new List<ProductoListaItem>(256);
                var nopropios = new List<ProductoListaItem>(256);

                bool PasaColor(ProductoListaItem p) => (Color == 0) || (p.Color == Color);

                // Recorremos una sola vez el total y clasificamos
                foreach (var p in lsProductosTotal)
                {
                    if (!PasaColor(p)) continue;

                    // Chequeamos por ID o por ID Padre
                    bool esPropio = setPropios.Contains(p.ProductosID) || (p.ProductosIDPadre != 0 && setPropios.Contains(p.ProductosIDPadre));
                    bool esNoPropio = !esPropio && (setNoPropios.Contains(p.ProductosID) || (p.ProductosIDPadre != 0 && setNoPropios.Contains(p.ProductosIDPadre)));

                    if (esPropio)
                    {
                        p.ClientesIDAsociado = clienteID;
                        propios.Add(p);
                    }
                    else if (esNoPropio)
                    {
                        p.ClientesIDAsociado = 0;
                        nopropios.Add(p);
                    }
                }

                // Comparador por orden para propios / no propios
                int ObtenerOrden(Dictionary<int, int> dic, ProductoListaItem p)
                {
                    // Buscamos prioridad por ProductosID; si no, por ProductosIDPadre; si no hay, mandamos al final
                    if (dic.TryGetValue(p.ProductosID, out var o1)) return o1;
                    if (p.ProductosIDPadre != 0 && dic.TryGetValue(p.ProductosIDPadre, out var o2)) return o2;
                    return int.MaxValue; // sin orden => al final
                }

                propios.Sort((a, b) => ObtenerOrden(ordenPropios, a).CompareTo(ObtenerOrden(ordenPropios, b)));
                nopropios.Sort((a, b) => ObtenerOrden(ordenNoPropios, a).CompareTo(ObtenerOrden(ordenNoPropios, b)));

                // Unimos
                var lsResultado = new List<ProductoListaItem>(propios.Count + nopropios.Count);
                lsResultado.AddRange(propios);
                lsResultado.AddRange(nopropios);

                return lsResultado;
            }
            catch (Exception ex)
            {
                // Manejo de errores: devolvemos un fallback acotado para no tildar la UI
                System.Diagnostics.Debug.WriteLine($"GetLoQmasMeGusta ERROR: {ex}");
                // Fallback: primeros 100 que cumplan Color (o todos si Color=0)
                return (Color == 0 ? lsProductosTotal : lsProductosTotal.Where(p => p.Color == Color)).ToList();
            }
        }


        //public async Task<List<ProductoListaItem>> GetLoQmasMeGusta(int clienteID, List<ProductoListaItem> lsProductosTotal, int Color)
        //{
        //    ClienteItemService oClienteItemService = new ClienteItemService();
        //    var lsMasMeGustaTotal = await oClienteItemService.GetLoQueMasTeGustaByClienteID(clienteID);
        //    var lsMasMeGustaPropios= lsMasMeGustaTotal.Where(c=>c.ClienteID == clienteID).ToList();
        //    var lsMasMeGustaNoPropios = lsMasMeGustaTotal.Where(c => c.ClienteID != clienteID).ToList();

        //    //if (Color > 0)
        //    {
        //        var lsPropios = lsProductosTotal.Where(c => lsMasMeGustaPropios.Any(c2 => (c2.ProductoID == c.ProductosID || c2.ProductoID == c.ProductosIDPadre)) && (c.Color == Color || Color==0))
        //         .OrderBy(c => lsMasMeGustaPropios.FirstOrDefault(c2 => c2.ProductoID == c.ProductosID || c2.ProductoID == c.ProductosIDPadre)?.Orden);
        //        foreach (var item in lsPropios)
        //        {
        //            item.ClientesIDAsociado = clienteID;
        //        }

        //        var lsNoPropios = lsProductosTotal.Where(c => lsMasMeGustaNoPropios.Any(c2 => (c2.ProductoID == c.ProductosID || c2.ProductoID == c.ProductosIDPadre)) && (c.Color == Color || Color == 0))
        //         .OrderBy(c => lsMasMeGustaNoPropios.FirstOrDefault(c2 => c2.ProductoID == c.ProductosID || c2.ProductoID == c.ProductosIDPadre)?.Orden);
        //        foreach (var item in lsNoPropios)
        //        {
        //            item.ClientesIDAsociado = 0;
        //        }

        //        List < ProductoListaItem > lsResultado= new List < ProductoListaItem >();
        //        lsResultado.AddRange (lsPropios);
        //        lsResultado.AddRange(lsNoPropios);

        //        if (lsResultado == null || lsResultado.Count() == 0)
        //        {
        //            var lsPropiosDos = lsProductosTotal.Take(100).ToList();
        //            lsResultado.AddRange(lsPropiosDos);
        //        }

        //        return lsResultado.Take(150).ToList();//HACK: Cantidad Lo que mas me gusta cambie de 150 a 75

        //    }

        //}


        //public async Task<int> TraerCombo2(int comboid1, List<ProductoListaItem> lsProductosCC, List<ProductoListaItem> lsProductosSC, List<ProductoListaItem> lsProductosTT, CombosHeadItem comboHeadItem, int cantidadDeCombo, int clienteID, int cantCC, int cantSC)
        //{
        //    int comboid = comboid1;
        //    lsProductosCC.Clear();
        //    lsProductosSC.Clear();
        //    ProductoListaService oProductoItemService = new ProductoListaService();
        //    CombosItemService oCombosItemService = new CombosItemService();
        //    //taer header del combo
        //    var resHeader = (await oCombosItemService.GetCombosHeadItem(comboid));
        //    comboHeadItem = resHeader[0];
        //    cantidadDeCombo = 1;
        //    //taer detalles de cada prod/


        //    var lsProdCombo = await oCombosItemService.GetProductosByID(comboid);

        //    ProductoListaService oProductoListaService = new ProductoListaService();
        //    await oProductoListaService.LlenarCantidadEnCarrito(clienteID, lsProductosTT.ToList());

        //    if (comboHeadItem.CantidadDinamica != 0)
        //    {
        //        cantCC = comboHeadItem.CantidadDinamica;
        //    }
        //    else
        //    {
        //        cantCC = comboHeadItem.Cantidad;

        //    }

        //    cantSC = comboHeadItem.CantSCargo;

        //    foreach (var i in lsProdCombo)
        //    {
        //        var p = await oProductoItemService.GetByIdLista(i.ComboProductoID);

        //        if (i.ComboTipo == 0)
        //        {
        //            var itco = p;
        //            // itco.ConCargo = true;
        //            itco.PorcentajDescuento1 = Convert.ToDecimal(i.Descuento1);
        //            itco.PorcentajDescuento2 = Convert.ToDecimal(i.Descuento2);
        //            itco.NroDeGrupoCombo = i.NrGrupoDinamico;
        //            itco.CantidadMaximaPorGrupo = i.CantidadConCargo;
        //            lsProductosCC.Add(itco);

        //        }
        //        else
        //        {
        //            var itco = p;
        //            // itco.ConCargo = false;
        //            itco.PrecioUnitarioFinalAlamcene = 0;
        //            itco.PrecioUnitarioFinalAutoservicio = 0;
        //            itco.PrecioUnitarioFinalEspecial = 0;
        //            itco.PrecioUnitarioFinalKiosko = 0;
        //            itco.CantidadMaximaPorGrupo = i.CantidadConCargo;


        //            if (i.NrGrupoDinamico == 1)
        //            {
        //                itco.NroDeGrupoCombo = i.NrGrupoDinamico;

        //            }



        //            lsProductosSC.Add(itco);
        //        }
        //    }
        //    if (comboHeadItem.CantidadDinamica == 0)
        //    {
        //        foreach (var ccIt in lsProductosCC)
        //        {
        //            ccIt.CantidadEnCarrito = ccIt.CantidadMaximaPorGrupo * cantidadDeCombo;
        //        }
        //        foreach (var ccIt in lsProductosSC)
        //        {
        //            ccIt.CantidadEnCarrito = ccIt.CantidadMaximaPorGrupo * cantidadDeCombo;
        //        }


        //    }


        //    foreach (var ccIt in lsProductosCC)
        //    {
        //        var cantXGrupo = lsProductosCC.Where(c => c.NroDeGrupoCombo == ccIt.NroDeGrupoCombo).Count();
        //        if (cantXGrupo == 1)
        //        {
        //            ccIt.CantidadEnCarrito = ccIt.CantidadMaximaPorGrupo * cantidadDeCombo;
        //            ccIt.NombreDelCombo = "1";
        //        }
        //    }
        //    foreach (var ccIt in lsProductosSC)
        //    {
        //        var cantXGrupoSc = lsProductosSC.Where(c => c.NroDeGrupoCombo == ccIt.NroDeGrupoCombo).Count();
        //        if (cantXGrupoSc == 1)
        //        {
        //            ccIt.CantidadEnCarrito = ccIt.CantidadMaximaPorGrupo * cantidadDeCombo;
        //            ccIt.NombreDelCombo = "1";
        //        }
        //    }


        //    this.StateHasChanged();

        //    return 1;

        //}

        public decimal GetTolalLineaCombo(List<ProductoListaItem> lsCombo, ProductoListaItem it)
        {
            var totalcombo = lsCombo.Where(c => c.ComboID == it.ComboID).Sum(c => c.TotalLinea);
            return totalcombo;
        }

        public async Task<int> BorrarPedidoSinTx(int cliid)
        {
            var borrar = await App.Current.MainPage.DisplayAlert("Confirmar borrar el pedido?", "", "OK", "No");
            if (borrar)
            {

                PedidoCabeceraService oPedidoCabeceraService = new PedidoCabeceraService();
                await oPedidoCabeceraService.BorrarPedidobyclienteIdsinTX(cliid);
                //    Traer();

                //      this.StateHasChanged();
                return 0;
            }
            return 1;
        }
        public static string Base64Encode(string plainText)
        {
            byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(plainText);
            string enc = Convert.ToBase64String(encbuff);
            string urlenc = System.Web.HttpUtility.UrlEncode(enc);
            return urlenc;
        }
        public static string Base64Decode(string encodedText)
        {
            string urldec = System.Web.HttpUtility.UrlDecode(encodedText);
            byte[] decbuff = Convert.FromBase64String(urldec);
            string plainText = System.Text.Encoding.UTF8.GetString(decbuff);
            return plainText;
        }

        public async Task<int> AbrirPortalVendedores(IJSRuntime JS, NavigationManager _navigationManager)
        {


            ParametrosServices oParam = new ParametrosServices();
            string urlhome = "";
     
            string urlApi = oParam.urlDisconsultas + "portalvendedores/login/";
            string token = "";
            var vdLogueadoID = oParam.VendedorID;
            var vdCod = oParam.Vendedor;
            string pwd = oParam.Pwd;
            if (vdLogueadoID != "" && vdLogueadoID != null)
            {
                var strToken = vdCod + "|" + pwd + "|" + vdLogueadoID + "|" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                token = Base64Encode(strToken);

                //var tokendes = Base64Decode(token);
                //var dd = tokendes.Split('|');
                //var VdCod = dd[0];
                //var pwd = dd[1];

                urlhome = urlApi + token;
                //     _navigationManager.NavigateTo(url + token, true);
                //    urlhome = "https://localhost:7242/trafico";
                // ";
                //  urlhome = "https://cdn.empresademo.example/";
#if IOS
                var uri = new Uri(urlhome);

                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
#else
                _navigationManager.NavigateTo(urlhome, true);
#endif








                //   await JS.InvokeVoidAsync("openURL", urlhome);

            }
            else
            {
                App.Current.MainPage.DisplayAlert("Atencion", "Debe loguearse Primero", "OK");
            }




            return 1;
        }



}

}

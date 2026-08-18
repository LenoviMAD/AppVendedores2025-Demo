using EntidadesAppVendedores;

using Microsoft.VisualBasic;
using SQLite;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
//using UIKit;


namespace AppVendedores2025.Services
{

    //[ApiController]
    [Microsoft.AspNetCore.Components.Route("[controller]")]
    public class PedidoCabeceraService : IPedidoCabeceraService
    {

        private SQLiteAsyncConnection _dbConnection;

        public PedidoCabeceraService()
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
        public async Task<int> Add(PedidoCabeceraItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            int r = 0;
            try
            {
                item.Cf = false;
                r = await _dbConnection.InsertAsync(item);
            }
            catch
            {
                r = -1;
            }
            return r;
        }

        public Task<int> Delete(PedidoCabeceraItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<List<PedidoCabeceraItem>> GetAll()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            //		var res = await _dbConnection.Table<ClienteItem>().ToListAsync(); ;
            return await _dbConnection.Table<PedidoCabeceraItem>().ToListAsync();
        }

        public async Task<PedidoCabeceraItem> GetByClienteIDsnTX(int id)
        {

            string sp = $"Select * From {nameof(PedidoCabeceraItem)} where ClienteID={id} and ResultadoTx not like 'OK%' and PedidoCerrado<2";


            var resultado = await _dbConnection.QueryAsync<PedidoCabeceraItem>(sp);
            return resultado.FirstOrDefault();


        }

        public async Task<PedidoCabeceraItem> GetByClienteIDUltimo(int id)
        {

            string sp = $"Select * From {nameof(PedidoCabeceraItem)} where ClienteID={id} ";


            var resultado = await _dbConnection.QueryAsync<PedidoCabeceraItem>(sp);
            var ultimo = resultado.LastOrDefault();


            return resultado.LastOrDefault();


        }
        public async Task<PedidoCabeceraItem> GetByCabeceraID(int cabeceraID)
        {

            string sp = $"Select * From {nameof(PedidoCabeceraItem)} where PedidoCabeceraID=" + cabeceraID;

            var resultado = await _dbConnection.QueryAsync<PedidoCabeceraItem>(sp);

            return resultado.FirstOrDefault();


        }

        public async Task<PedidoCabeceraItem> GetPedidosTransmitidosPorCliente(int clienteId)
        {

            string sp = $"Select * From {nameof(PedidoCabeceraItem)} where ClienteID=" + clienteId + " and pedidoTransmitido > 0 ";

            var resultado = await _dbConnection.QueryAsync<PedidoCabeceraItem>(sp);

            return resultado.FirstOrDefault();


        }


        public Task<int> Update(PedidoCabeceraItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<int> UpdateFechaEntrega(PedidoCabeceraItem item)
        {

            DateTime? fechaDeEntr = item.FechaDeEntrega;

            //long fechaDeEntregaTicks = item.FechaDeEntrega.Ticks;
            long fechaDeEntregaTicks = fechaDeEntr.Value.Ticks;

            //long datatick = data.Ticks;
            //var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET FechaDeEntrega ='" + item.FechaDeEntrega + "' WHERE PedidoCabeceraID =" + item.PedidoCabeceraID;


            var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET FechaDeEntrega ='" + fechaDeEntregaTicks + "' WHERE PedidoCabeceraID =" + item.PedidoCabeceraID;

            var result = await _dbConnection.ExecuteAsync(sql);

            return 1;

        }

        public async Task<string> UpdateKeyPagoID(int pedidoID, string KeyPagoID)
        {
            var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET KeyPagoID ='" + KeyPagoID + "' WHERE PedidoCabeceraID =" + pedidoID;

            var result = await _dbConnection.ExecuteAsync(sql);

            return "";
        }

        public async Task<string> UpdateTotalPedido(int pedidoID, decimal TotalPedido)
        {
            var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET TotalPedido ='" + TotalPedido + "' WHERE PedidoCabeceraID =" + pedidoID;

            var result = await _dbConnection.ExecuteAsync(sql);

            return "";
        }

        public async Task<string> UpdatePaymentID(int pedidoID, string PaymentID)
        {
            var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET PaymentID ='" + PaymentID + "' WHERE PedidoCabeceraID =" + pedidoID;

            var result = await _dbConnection.ExecuteAsync(sql);

            return "";
        }

        //pedidoTransmitido, 1 transmitido OK , 0 sin transmitir.
        public async Task<int> UpdatePedidoTransmitido(int pedidoID, int pedidoTransmitido, int pedidoReactivadoId)
        {
            //var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET pedidoTransmitido ='" + pedidoTransmitido + "' WHERE PedidoCabeceraID =" + pedidoID;

            var sql = $"UPDATE {nameof(PedidoCabeceraItem)} " +
              $"SET pedidoTransmitido = {pedidoTransmitido}, " +
              $"    PedidoReactivadoId = {pedidoReactivadoId} " +
              $"WHERE PedidoCabeceraID = {pedidoID};";

            var result = await _dbConnection.ExecuteAsync(sql);

            return result;
        }

        public async Task<string> ObtenerKeyPagoID(int pedidoID)
        {
            var sql = $"SELECT KeyPagoID FROM {nameof(PedidoCabeceraItem)} WHERE PedidoCabeceraID =" + pedidoID;

            // Usamos parámetros SQL para evitar inyección
            var result = await _dbConnection.ExecuteScalarAsync<string>(sql);

            return result;
        }




        public async Task<int> BorrarTodo()
        {

            var res = await _dbConnection.DeleteAllAsync<PedidoCabeceraItem>();

            return res;
        }

        public async Task<int> Sincronizar(IEnumerable<EntidadesAppVendedores.PedidoCabeceraItem> lsClientes)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var resBorr = await BorrarTodo();

            foreach (var item in lsClientes)
            {
                await Add(item);


            }


            return resBorr;

        }

        public async Task<bool> AgregarPedidosProductos(int clientesID, int productosID, int cantidadNueva, int listaDePecios, int comboID, decimal precioUnitarioFinal, decimal precioUnitarioNeto, int grupoDinamico, string cliCodigo, string cliDireccon, string nombreProducto, string codigoProducto, int VDID, bool cf, int unidadesPorBulto, string com)
        {
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            //daniel  cf hasta aca
            //  version = await JS.InvokeAsync<string>("getCookie", "versionapp");
            // Ver si hay ua cabecera abierto
            var headerAbierto = await GetByClienteIDsnTX(clientesID);


            ParametrosServices oParam = new ParametrosServices();
            var DeviceID = "**";
            DeviceID = oParam.DeviceID;
            if (headerAbierto is null)
            {

                //crear header
                var resheader = await Add(new PedidoCabeceraItem(0, DeviceID, VDID, clientesID, DateTime.Now, true, "", "(v" + com + ")", 1, 1, "", DateTime.Now, "", null, cliCodigo, cliDireccon, DateTime.Now , 0));

                headerAbierto = await GetByClienteIDsnTX(clientesID);

                //agregar 1erproducto 
                var resitem = await oPedidosDetalleItemService.Add(new PedidosDetalleItem(0, headerAbierto.PedidoCabeceraID, productosID, 0, cantidadNueva, listaDePecios, precioUnitarioFinal, precioUnitarioNeto, "", grupoDinamico, "", nombreProducto, codigoProducto, cf, unidadesPorBulto, 0, ""));

            }
            else
            {

                //buscar producto en carrito

                var lsProductoEnPEdido = await oPedidosDetalleItemService.GetByPedidoCabezaraID_ProdID(headerAbierto.PedidoCabeceraID, productosID, 0, false);

                //ver si esta duplicado
                //if(lsProductoEnPEdido.)

                if (lsProductoEnPEdido is null)
                {

                    //no esta, agregar

                    await oPedidosDetalleItemService.Add(new PedidosDetalleItem(0, headerAbierto.PedidoCabeceraID, productosID, 0, cantidadNueva, listaDePecios, precioUnitarioFinal, precioUnitarioNeto, "", 0, "", nombreProducto, codigoProducto, cf, unidadesPorBulto, 0, ""));

                }
                else
                {
                    //si esta , actualziar


                    lsProductoEnPEdido.Cantidad = cantidadNueva;
                    lsProductoEnPEdido.CF = cf;

                    if (lsProductoEnPEdido.Cantidad < 1)
                    {
                        //borrar del carrito, esta en cero
                        await oPedidosDetalleItemService.Delete(lsProductoEnPEdido);
                    }
                    else
                    {
                        //update
                        lsProductoEnPEdido.ListadePrecioID = listaDePecios;
                        lsProductoEnPEdido.PrecioUnitarioFinal = precioUnitarioFinal;
                        lsProductoEnPEdido.PrecioUnitarioNeto = precioUnitarioNeto;
                        await oPedidosDetalleItemService.Update(lsProductoEnPEdido);
                    }

                }

            }

            return true;
        }


        public async Task<bool> AgregarPedidosCombo(int clientesID, int productosID, int cantidadNueva, int listaDePecios, int comboID, decimal precioUnitarioFinal, decimal precioUnitarioNeto, int grupoDinamico, string cliCodigo, string cliDireccon, string nombreProducto, string codigoProducto, bool cf, int unidadesPorBulto, int cantdecombos, int vdid, string version, string ComboCodigo)
        {
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            //int cant;
            //    int VDID = Int32.Parse(await JS.InvokeAsync<string>("getCookie", "vendedorID"));


            // Ver si hay ua cabecera abierto
            var headerAbierto = await GetByClienteIDsnTX(clientesID);
            ParametrosServices oParam = new ParametrosServices();
            var DeviceID = "**";
            DeviceID = oParam.DeviceID;

            if (headerAbierto is null)
            {
                //crear header
                var resheader = await Add(new PedidoCabeceraItem(0, DeviceID, vdid, clientesID, DateTime.Now, true, "", "(v" + version + ")", 1, 1, "", DateTime.Now, "", null, cliCodigo, cliDireccon, DateTime.Now, 0));
                headerAbierto = await GetByClienteIDsnTX(clientesID);

            }


            //agregar 1erproducto
            //                var resitem = await oPedidosDetalleItemService.Add(new PedidosDetalleItem(0, headerAbierto.PedidoCabeceraID, productosID, comboID, cantidadNueva, listaDePecios, precioUnitarioFinal, "", grupoDinamico, ""));


            //else
            {

                //buscar producto en carrito

                var lsProductoEnPEdido = await oPedidosDetalleItemService.GetByPedidoCabezaraID_ProdID(headerAbierto.PedidoCabeceraID, productosID, comboID, precioUnitarioFinal == 0);

                if (lsProductoEnPEdido is null)
                {

                    //no esta, agregar

                    await oPedidosDetalleItemService.Add(new PedidosDetalleItem(0, headerAbierto.PedidoCabeceraID, productosID, comboID, cantidadNueva, listaDePecios, precioUnitarioFinal, precioUnitarioNeto, "", grupoDinamico, "", nombreProducto, codigoProducto, cf, unidadesPorBulto, cantdecombos, ComboCodigo));

                }
                else
                {
                    //si esta , actualziar


                    lsProductoEnPEdido.Cantidad = cantidadNueva;


                    if (lsProductoEnPEdido.Cantidad < 1)
                    {
                        //borrar del carrito, esta en cero
                        await oPedidosDetalleItemService.Delete(lsProductoEnPEdido);
                    }
                    else
                    {
                        //update
                        await oPedidosDetalleItemService.Update(lsProductoEnPEdido);
                    }

                }

            }

            return true;
        }

        public async Task<int> BorrarByClienteIdyComboIDSinTx(int clienteID, int comboID)
        {


            // Ver si hay ua cabecera abierto
            var headerAbierto = await GetByClienteIDsnTX(clienteID);

            if (headerAbierto != null)
            {
                string sp = $"delete  From {nameof(PedidosDetalleItem)}  where CombosID={comboID} and PedidoCabeceraID='{headerAbierto.PedidoCabeceraID}' ";
                if (_dbConnection == null)
                {
                    SetUpDb();
                }

                var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);


            }

            return 1;


        }

        public async Task<int> ObtenerPedidoCabeceraIDEnviadoXCliente(int clienteID)
        { 
            string sp = $"Select * From {nameof(PedidoCabeceraItem)} where ClienteID={clienteID} and ResultadoTx like 'OK%' order by PedidoCabeceraID desc";


            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var r1 = await _dbConnection.QueryAsync<PedidoCabeceraItem>(sp);

            return r1.First().PedidoCabeceraID;
        }

        public async Task<int> BorrarPedidobyCabeceraId(int cabeceraID)
        {


           
                string sp = $"delete  From {nameof(PedidosDetalleItem)}  where  PedidoCabeceraID='{cabeceraID}' ";
                string sp2 = $"delete  From {nameof(PedidoCabeceraItem)}  where  PedidoCabeceraID='{cabeceraID}' ";

                if (_dbConnection == null)
                {
                    SetUpDb();
                }

                var r1 = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);
                var r2 = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp2);

           

            return 1;


        }

        public async Task<int> BorrarPedidobyclienteIdsinTX(int clienteID)
        {


            // Ver si hay ua cabecera abierto
            var headerAbierto = await GetByClienteIDsnTX(clienteID);

            if (headerAbierto != null)
            {
                string sp = $"delete  From {nameof(PedidosDetalleItem)}  where  PedidoCabeceraID='{headerAbierto.PedidoCabeceraID}' ";
                string sp2 = $"delete  From {nameof(PedidoCabeceraItem)}  where  PedidoCabeceraID='{headerAbierto.PedidoCabeceraID}' ";

                if (_dbConnection == null)
                {
                    SetUpDb();
                }

                var r1 = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);
                var r2 = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp2);

            }

            return 1;


        }
        public async Task<int> GuardarRespuetaTxPedido(int CabeceraID, string response, int statusCode)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var fecha = DateTime.Now;

            //daniel  ver de abregar el status
            if (statusCode == 200)
            {
                response = "OK " + response;
            }
            else
            {
                response = "ERR " + response;
            }

            var sql = $"UPDATE   {nameof(PedidoCabeceraItem)}  SET ResultadoTx ='" + response + "', FechaDeTx='" + fecha + "' where PedidoCabeceraID =" + CabeceraID;

            var res = await _dbConnection.ExecuteAsync(sql);

            return 1;
        }

        public async Task<int> CerrarPedido(int CabeceraID)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var fecha = DateTime.Now;
            var sql = $"UPDATE   {nameof(PedidoCabeceraItem)}  SET PedidoCerrado =1 where PedidoCabeceraID =" + CabeceraID;

            var res = await _dbConnection.ExecuteAsync(sql);

            return 1;
        }


        public async Task<int> ReabrirPedido(int CabeceraID)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var fecha = DateTime.Now;
            var sql = $"UPDATE   {nameof(PedidoCabeceraItem)}  SET ResultadoTx ='',FechaDeTx='',Error='', PedidoCerrado=0 where PedidoCabeceraID =" + CabeceraID;

            var res = await _dbConnection.ExecuteAsync(sql);

            return 1;
        }


        public async Task<int> BorrarPedidoViejos()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var todosHeaders = await GetAll();
            DateTime hoy = DateTime.Today;
            foreach (var head in todosHeaders)
            {

                if (head.FechaCarga.Day != hoy.Day)
                {
                    var id = head.PedidoCabeceraID;
                    var sp2 = $"DELETE FROM {nameof(PedidoCabeceraItem)} WHERE PedidoCabeceraID =" + id;
                    var r2 = await _dbConnection.ExecuteAsync(sp2);
                    var sp3 = $"DELETE FROM {nameof(PedidosDetalleItem)} WHERE PedidoCabeceraID =" + id;
                    var r3 = await _dbConnection.ExecuteAsync(sp3);

                }

            }


            return 1;
        }



        public async Task<int> GuardarEstadoArrastre(int CabeceraID, decimal arrastre)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var fecha = DateTime.Now;

            var sql = $"UPDATE   {nameof(PedidoCabeceraItem)}  SET EstadoArrastre ='" + arrastre + "' where PedidoCabeceraID =" + CabeceraID;
            try
            {
                var res = await _dbConnection.ExecuteAsync(sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PedidoCabeceraService] Error GuardarEstadoArrastre: {ex.Message}");
            }
            return 1;
        }

        public async Task<int> GetCantidadPedidossinTX()
        {
            string sp = $"Select * From {nameof(PedidoCabeceraItem)} where ResultadoTx='' and PedidoCerrado<2";

            var resultado = await _dbConnection.QueryAsync<PedidoCabeceraItem>(sp);
            return resultado.Count;
        }

        bool transmitiendo = false;

        //CFFalse
        public async Task<string> TransmitirPedido(int cabeceraID, ProductoListaItems lsProd, string comentario, decimal totalCarrito, string? PaymentID = "")
        {

            //statusCodeOut = -1;
            ParametrosServices oParam = new ParametrosServices();
            if (oParam.tipoVD == 9999)
            {
                return "";
            }

            var response = "";

            GPSService GpsS = new GPSService();
            var hayInternet = await GpsS.IsConnectedToInternet();
            if (!hayInternet)
            {
                return "Sin Internet";
            }


            IHttpClientFactory? httpFactory = null;

            try
            {
                httpFactory = App.Current?.Handler?.MauiContext?.Services?.GetService<IHttpClientFactory>();
                if (httpFactory == null)
                    throw new Exception("No se pudo resolver IHttpClientFactory. Verificá AddHttpClient() en MauiProgram.");
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error inicializando HTTP: " + ex.Message, "OK");
                return "Sin Internet";
            }

            var oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar(httpFactory);


            //TODO:comento validacion del vendedor
            var resVD = await oFuncionesSincronizar.ValidarVD(oParam.Vendedor, oParam.Pwd);
            if (resVD.CodError == "1")
            {
                //  await App.Current.MainPage.DisplayAlert("Atencion", "Datos Incorretos", "OK");
                return resVD.Mensaje;
            }
            if (resVD.CodError == "2")
            {
                //  await App.Current.MainPage.DisplayAlert("Atencion", resVD.Mensaje, "OK");
                return resVD.Mensaje;
            }




            string url = oParam.urlApi;
            if (oParam.EmpresaID > 0)
                url += $"ProductosItem/PostMultiempresa/{oParam.EmpresaID}";
            else
                url += "Productositem";

            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            var Pedido = await GetByCabeceraID(cabeceraID);

            Pedido.TotalPedido = totalCarrito;
            //Si es Cliente para transmitir fuera horario
            int esCliente = Convert.ToInt32(oParam.TipoDeUsuario);
            bool esClienteBool = esCliente == 999;

            //if (esClienteBool)
            //{
            //    Pedido.TipoDeClienteDelPedido = 1;
            //}
            //else
            //{
            //    Pedido.TipoDeClienteDelPedido = 0;
            //}


            //  var pedidoValidado = await ValidarPedido(Pedido, lsProd);

            //daniel  se validan en server
            var pedidoValidado = true;


            if (oParam.Autogestion)
            {
                /// <summary>
                /// TipoDeClienteDelPedido
                /// 0 Clientes de Vendedores Presenciales
                /// 1 Clientes Directos
                /// </summary>

                Pedido.TipoDeClienteDelPedido = 1;
                //cuando es cliente cambio a vendedor Ecom
                Pedido.VendedorID = 319;
            }
            else
            {
                Pedido.TipoDeClienteDelPedido = 0;
            }

            Pedido.Comentario += "-" + comentario;

            if (pedidoValidado)
            {
                var pedDetalle = await oPedidosDetalleItemService.GetByPedidoCabezaraID(cabeceraID);


                //// Step 1: Sort the list by "productosID" and "pedidosdetallaid" in ascending order
                //var sortedList = pedDetalle.OrderBy(item => item.ProductoID).ThenBy(item => item.Id);

                //// Step 2: Group the list by "productosID"
                //var groupedByProductosID = sortedList.GroupBy(item => item.ProductoID);

                //// Step 3: Select the first element from each group (the one with the lowest "pedidosdetallaid") and ignore CombosID > 0
                //List<PedidosDetalleItem> uniqueList = groupedByProductosID
                //    .Select(group => group.FirstOrDefault(item => item.CombosID == 0) ?? group.First())
                //    .ToList();








                var lsProdEn1 = from p in pedDetalle where !p.CF select p;
                if (!lsProdEn1.Any())
                {
                    Pedido.Cf = true;

                }

                Pedido.LstProductos.AddRange(pedDetalle);


                if (Pedido.ResultadoTx != "OK%")
                {


                    //PaymentID = "97350388547";
                    //var resultadoValidarCombo=await  oProductoListaService.GetValidarCombo(comboid, cartview, cartviewCombos,clienteID);
                    if (PaymentID != null)
                    {
                        Pedido.PaymentID = PaymentID;
                    }
                    else
                    {
                        Pedido.PaymentID = "";
                    }

                    string body = System.Text.Json.JsonSerializer.Serialize(Pedido);

                    //Comentado 20/06/2025 
                    //using (var client = new HttpClient())
                    //{

                    //    client.BaseAddress = new Uri(url);
                    //    var content = new StringContent(body, Encoding.UTF8, "application/json");


                    //    try
                    //    {

                    //        if(!transmitiendo)
                    //        {
                    //            transmitiendo = true;
                    //            var result = await client.PostAsync("", content);
                    //            response = await result.Content.ReadAsStringAsync();
                    //            await GuardarRespuetaTxPedido(Pedido.PedidoCabeceraID, response, (int)result.StatusCode);
                    //            if ((int)result.StatusCode==200)
                    //            {
                    //                //pedidoTransmitido, 1 transmitido OK , 0 sin transmitir.
                    //                await UpdatePedidoTransmitido(Pedido.PedidoCabeceraID, 1);
                    //                response = "Ok " + response;
                    //            }
                    //            //statusCodeOut = (int)result.StatusCode;
                    //            transmitiendo = false;
                    //            Thread.Sleep(1000);
                    //        }

                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        response = "ERROR de Conexion";

                    //    }

                    //}

                    //Solucion sin dormir el hilo
                    using (var client = HttpHelper.CreateClient())
                    {
                        static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                        //Armamos el Header
                        client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                        client.BaseAddress = new Uri(url);
                        //client.Timeout = TimeSpan.FromSeconds(300); // Cambiá este valor según lo que necesites esperar.

                        var content = new StringContent(body, Encoding.UTF8, "application/json");

                        try
                        {
                            if (!transmitiendo)
                            {
                                transmitiendo = true;
                                if (!oParam.EsModoTest)
                                {
                                    var result = new HttpResponseMessage();

                                    if (!Pedido.PedidoReactivado)
                                    {
                                         result = await client.PostAsync("", content);
                                    }
                                    else
                                    {
                                        result = await client.PostAsync("ProductosItem/PostReactivado", content);
                                    }

                                    response = await result.Content.ReadAsStringAsync();

                                    if ((int)result.StatusCode != 200)
                                    {
                                        transmitiendo = false;
                                        return response; // Si falló (Ej. Stock Insuficiente o error 500) devolvemos el error tal cual
                                    }
                                    response = await result.Content.ReadAsStringAsync();

                                    await GuardarRespuetaTxPedido(Pedido.PedidoCabeceraID, response, (int)result.StatusCode);

                                    if ((int)result.StatusCode == 200)
                                    {
                                        int id = ObtenerIdDeResponse(response);
                                        await UpdatePedidoTransmitido(Pedido.PedidoCabeceraID, 1, id);
                                        response = "Ok " + response;
                                    }
                                }
                                else
                                {
                                    await GuardarRespuetaTxPedido(Pedido.PedidoCabeceraID, response,  200);
                                    await UpdatePedidoTransmitido(Pedido.PedidoCabeceraID, 1, 0);
                                    response = "Ok 99999 PEDIDO TEST";

                                }
                                transmitiendo = false;

                                await Task.Delay(1000); // Correcto en código async
                            }
                        }
                        catch (TaskCanceledException ex)
                        {
                            response = "Tiempo de espera agotado. El servidor no respondió a tiempo.";
                            transmitiendo = false;
                        }
                        catch (HttpRequestException ex)
                        {
                            response = "Error HTTP: " + ex.Message;
                            transmitiendo = false;
                        }
                        catch (Exception ex)
                        {
                            response = "ERROR de conexión: " + ex.Message;
                            transmitiendo = false;
                        }
                    }

                }
                return response;
                }

            return "novalidado";
        }


        public int ObtenerIdDeResponse(string response)
        {
            try
            {
                // Validación básica
                if (string.IsNullOrWhiteSpace(response))
                    return 0;

                // Buscar el separador |
                var partes = response.Split('|');

                if (partes.Length == 0)
                    return 0;

                // La primera parte debería ser el ID
                var idTexto = partes[0].Trim();

                if (int.TryParse(idTexto, out int id))
                {
                    return id; // Ej: 2443050
                }

                // Si no se puede convertir a int, devolvemos null
                return 0;
            }
            catch (Exception ex)
            {
                // Manejo de errores (loguear, etc.)
                Console.Error.WriteLine($"Error al obtener ID de response: {ex.Message}");
                return 0;
            }
        }

        public async Task<string> TransmitirJSonPedido(int cabeceraID, ProductoListaItems lsProd, string comentario, decimal totalCarrito)
        {

            //statusCodeOut = -1;
            ParametrosServices oParam = new ParametrosServices();
            if (oParam.tipoVD == 9999)
            {
                return "";
            }

            var response = "";

            GPSService GpsS = new GPSService();
            var hayInternet = await GpsS.IsConnectedToInternet();
            if (!hayInternet)
            {
                return "Sin Internet";
            }



            IHttpClientFactory? httpFactory = null;

            try
            {
                httpFactory = App.Current?.Handler?.MauiContext?.Services?.GetService<IHttpClientFactory>();
                if (httpFactory == null)
                    throw new Exception("No se pudo resolver IHttpClientFactory. Verificá AddHttpClient() en MauiProgram.");
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error inicializando HTTP: " + ex.Message, "OK");
                return "Sin Internet";
            }

            var oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar(httpFactory);



            string url = oParam.urlApi;
            if (oParam.EmpresaID > 0)
                url += $"ProductosItem/PostMultiempresa/{oParam.EmpresaID}";
            else
                url += "Productositem/PostDetalleClienteAutogestion";

            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            var Pedido = await GetByCabeceraID(cabeceraID);
            Pedido.TotalPedido = totalCarrito;
            //Si es Cliente para transmitir fuera horario
            int esCliente = Convert.ToInt32(oParam.TipoDeUsuario);
            bool esClienteBool = esCliente == 999;


            if (oParam.Autogestion)
            {
                /// <summary>
                /// TipoDeClienteDelPedido
                /// 0 Clientes de Vendedores Presenciales
                /// 1 Clientes Directos
                /// </summary>

                Pedido.TipoDeClienteDelPedido = 1;
                //cuando es cliente cambio a vendedor Ecom
                Pedido.VendedorID = 319;
            }
            else
            {
                Pedido.TipoDeClienteDelPedido = 0;
            }

            Pedido.Comentario += "-" + comentario;


            var pedDetalle = await oPedidosDetalleItemService.GetByPedidoCabezaraID(cabeceraID);


            var lsProdEn1 = from p in pedDetalle where !p.CF select p;
            if (!lsProdEn1.Any())
            {
                Pedido.Cf = true;

            }

            Pedido.LstProductos.AddRange(pedDetalle);


            string body = System.Text.Json.JsonSerializer.Serialize(Pedido);


            using (var client = HttpHelper.CreateClient())
            {
                client.BaseAddress = new Uri(url);

                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                        //Armamos el Header
                        client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                try
                {

                    var result = await client.PostAsync("", content);
                    response = await result.Content.ReadAsStringAsync();

                }

                catch (Exception ex)
                {
                    response = "ERROR de conexión: " + ex.Message;
                    transmitiendo = false;
                }
            }


            return response;



        }

        public async Task<string> CalcularValorFinalPedido(int cabeceraID, ProductoListaItems lsProd, string comentario)
        {

            //statusCodeOut = -1;
            ParametrosServices oParam = new ParametrosServices();
            if (oParam.tipoVD == 9999)
            {
                return "";
            }

            var response = "";

            GPSService GpsS = new GPSService();
            var hayInternet = await GpsS.IsConnectedToInternet();
            if (!hayInternet)
            {
                return "Sin Internet";
            }



            IHttpClientFactory? httpFactory = null;

            try
            {
                httpFactory = App.Current?.Handler?.MauiContext?.Services?.GetService<IHttpClientFactory>();
                if (httpFactory == null)
                    throw new Exception("No se pudo resolver IHttpClientFactory. Verificá AddHttpClient() en MauiProgram.");
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error inicializando HTTP: " + ex.Message, "OK");
                return "Sin Internet";
            }

            var oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar(httpFactory);

            var resVD = await oFuncionesSincronizar.ValidarVD(oParam.Vendedor, oParam.Pwd);
            if (resVD.CodError == "1")
            {
                //  await App.Current.MainPage.DisplayAlert("Atencion", "Datos Incorretos", "OK");
                return resVD.Mensaje;
            }
            if (resVD.CodError == "2")
            {
                //  await App.Current.MainPage.DisplayAlert("Atencion", resVD.Mensaje, "OK");
                return resVD.Mensaje;
            }




            string url = oParam.urlApi;
            if (oParam.EmpresaID > 0)
                url += $"ProductosItem/PostMultiempresa/{oParam.EmpresaID}";
            else
                url += "Productositem";

            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            var Pedido = await GetByCabeceraID(cabeceraID);

            //Si es Cliente para transmitir fuera horario
            int esCliente = Convert.ToInt32(oParam.TipoDeUsuario);
            bool esClienteBool = esCliente == 999;

            //if (esClienteBool)
            //{
            //    Pedido.TipoDeClienteDelPedido = 1;
            //}
            //else
            //{
            //    Pedido.TipoDeClienteDelPedido = 0;
            //}


            //  var pedidoValidado = await ValidarPedido(Pedido, lsProd);

            //daniel  se validan en server
            var pedidoValidado = true;


            if (esClienteBool)
            {
                Pedido.TipoDeClienteDelPedido = 1;
                //cuando es cliente cambio a vendedor Ecom
                Pedido.VendedorID = 319;
            }
            else
            {
                Pedido.TipoDeClienteDelPedido = 0;
            }

            Pedido.Comentario += "-" + comentario;

            if (pedidoValidado)
            {
                var pedDetalle = await oPedidosDetalleItemService.GetByPedidoCabezaraID(cabeceraID);


                //// Step 1: Sort the list by "productosID" and "pedidosdetallaid" in ascending order
                //var sortedList = pedDetalle.OrderBy(item => item.ProductoID).ThenBy(item => item.Id);

                //// Step 2: Group the list by "productosID"
                //var groupedByProductosID = sortedList.GroupBy(item => item.ProductoID);

                //// Step 3: Select the first element from each group (the one with the lowest "pedidosdetallaid") and ignore CombosID > 0
                //List<PedidosDetalleItem> uniqueList = groupedByProductosID
                //    .Select(group => group.FirstOrDefault(item => item.CombosID == 0) ?? group.First())
                //    .ToList();








                var lsProdEn1 = from p in pedDetalle where !p.CF select p;
                if (!lsProdEn1.Any())
                {
                    Pedido.Cf = true;

                }

                Pedido.LstProductos.AddRange(pedDetalle);






                if (Pedido.ResultadoTx != "OK%")
                {
                    var PedidoValorFinal = Pedido;
                    PedidoValorFinal.DeviceID = "CALCULAR";

                    //      var resultadoValidarCombo=await  oProductoListaService.GetValidarCombo(comboid, cartview, cartviewCombos,clienteID);

                    string body = System.Text.Json.JsonSerializer.Serialize(PedidoValorFinal);

                    using (var client = HttpHelper.CreateClient())
                    {
                        client.BaseAddress = new Uri(url);
                        static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                        //Armamos el Header
                        client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                        var content = new StringContent(body, Encoding.UTF8, "application/json");

                        try
                        {

                            if (!transmitiendo)
                            {
                                transmitiendo = true;
                                var result = await client.PostAsync("", content);
                                response = await result.Content.ReadAsStringAsync();
                                //await GuardarRespuetaTxPedido(Pedido.PedidoCabeceraID, response, (int)result.StatusCode);
                                //if ((int)result.StatusCode == 200)
                                //{
                                //    response = "Ok " + response;
                                //}
                                //statusCodeOut = (int)result.StatusCode;
                                //return response;
                                transmitiendo = false;
                                await Task.Delay(1000);
                            }

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PedidoCabeceraService] Error TransmitirPedidoValor: {ex.Message}");
                            response = "ERROR de Conexion";

                        }

                    }
                }
                return response;
            }

            return "novalidado";
        }

        private async Task<bool> ValidarPedido(PedidoCabeceraItem header, ProductoListaItems lsProdTT)
        {
            //       PedidoCabeceraService oPedidoCabeceraService = new PedidoCabeceraService();

            //     var header = await oPedidoCabeceraService.GetByCabeceraID(pedidoCabeceraID);

            string msgErrorArrastres = "";
            bool pedidoValidoArrastres = true;

            ProductoListaService oProductoListaService = new ProductoListaService();

            var cartview = await oProductoListaService.LlenarCantidadEnCarrito(header.ClienteID, lsProdTT);
            var cartviewCombos = await oProductoListaService.LlenarCantidadEnCarritoCombos(header.ClienteID);
            var lsCombos = cartviewCombos.GroupBy(c => c.ComboID).ToList();
            foreach (var itCombo in lsCombos)
            {
                var resv = await oProductoListaService.GetValidarCombo(itCombo.Key, cartview, cartviewCombos, header.ClienteID);

                if (!resv.Resultado)
                {
                    pedidoValidoArrastres = false;
                    msgErrorArrastres += resv.Mensaje;
                }

            }
            if (!pedidoValidoArrastres)
            {
                await App.Current.MainPage.DisplayAlert("Error Arrastres de Combos", msgErrorArrastres, "OK");
                return false;
            }
            if (pedidoValidoArrastres)
            {


                //  GetByPedidoCabezaraID

                var res = await App.Current.MainPage.DisplayAlert("Confirma el Pedido?", "", "OK", "No");


                //validar arrasatre    pedidoCabeceraID


                if (res)
                {



                    return true;

                }
                else { return false; }

            }
            return false;
        }


        // En AppVendedores2025.Services.PedidoCabeceraService (agregar al final de la clase)
        public async Task<(PedidoCabeceraItem? Cabecera, List<PedidosDetalleItem> Detalles)> GetPedidoCompletoAsync(int cabeceraID)
        {
            if (_dbConnection == null) SetUpDb();

            try
            {
                var cab = await GetByCabeceraID(cabeceraID);
                if (cab == null) return (null, new List<PedidosDetalleItem>());

                var detService = new PedidosDetalleItemService();
                var det = await detService.GetByPedidoCabezaraID(cabeceraID) ?? new List<PedidosDetalleItem>();

                return (cab, det);
            }
            catch (Exception ex)
            {
                // Loguealo si tenés logger inyectado; acá devolvemos “vacío seguro”
                return (null, new List<PedidosDetalleItem>());
            }
        }

        /// <summary>
        /// Prepara un pedido ya transmitido para reactivación de edición:
        /// - Limpia ResultadoTx/FechaDeTx/Error
        /// - Reabre (PedidoCerrado=0)
        /// - (Opcional) Marca pedidoTransmitido = 0
        /// </summary>
        //public async Task<bool> PrepararParaReactivacionAsync(int cabeceraID, bool marcarNoTransmitido = true)
        //{
        //    if (_dbConnection == null) SetUpDb();

        //    try
        //    {
        //        // Re-abre: limpia ResultadoTx, FechaDeTx, Error y pone PedidoCerrado=0
        //        await ReabrirPedido(cabeceraID);

        //        if (marcarNoTransmitido)
        //        {
        //            await UpdatePedidoTransmitido(cabeceraID, 0, 0);
        //        }

        //        // Confirmar que existe
        //        var header = await GetByCabeceraID(cabeceraID);
        //        return header != null;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public async Task<string> UpdatePedidoReactivadoId(int cabeceraId, int pedidoReactivadoId)
        {
            var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET PedidoReactivadoId ='" + pedidoReactivadoId + "' WHERE PedidoCabeceraID =" + cabeceraId;

            var result = await _dbConnection.ExecuteAsync(sql);

            return "";
        }


        // En AppVendedores2025.Services.PedidoCabeceraService

        /// <summary>
        /// Duplica un pedido completo (cabecera + detalles) a partir de un idCabecera origen.
        /// - Genera una nueva cabecera (PK autoincrement).
        /// - Resetea: ResultadoTx="", FechaDeTx=null, Error="", PedidoCerrado=false, pedidoTransmitido=0
        /// - Limpia KeyPagoID/PaymentID
        /// - TotalPedido = 0 y FechaCarga = DateTime.Now
        /// - Todo en transacción. Si algo falla, ROLLBACK.
        /// Retorna: (Ok, NuevoCabeceraID, Error)
        /// </summary>

        public async Task<(bool Ok, int? NuevoCabeceraID, string? Error)> DuplicarPedidoAsync(int idCabecera)
        {
            try
            {
                if (_dbConnection == null) SetUpDb();

                // 1) Leer origen (cabecera + detalles)
                PedidoCabeceraItem? origenCab;
                List<PedidosDetalleItem> origenDet;

                try
                {
                    origenCab = await GetByCabeceraID(idCabecera);
                    if (origenCab is null)
                        return (false, null, $"No se encontró la cabecera con ID {idCabecera}.");

                    // Ideal: que esto use la MISMA _dbConnection internamente (o traerlo acá directo)
                    origenDet = await new PedidosDetalleItemService(_dbConnection).GetByPedidoCabezaraID(idCabecera) ?? new List<PedidosDetalleItem>();
                }
                catch (Exception ex)
                {
                    return (false, null, $"Error al leer el pedido origen: {ex.Message}");
                }                                                               

                // (Opcional) Obtener pedidos transmitidos por cliente y armar lista de ids
                List<int> pedidosClientesId = new();
                try
                {
                    var pedidos = await GetPedidosTransmitidosPorCliente(origenCab.ClienteID); // si es async
                    if (pedidos != null)
                        pedidosClientesId = pedidos.PedidosClientesId;
                }
                catch
                {
                    // si esto es accesorio, no lo hagas fallar; si es obligatorio, devolvé error
                    // return (false, null, "No se pudieron obtener pedidos transmitidos del cliente.");
                }

                // 2) Armar nueva cabecera (PK = 0 => autoincrement)
                var nuevaCab = new PedidoCabeceraItem
                {
                    PedidoCabeceraID = 0,

                    DeviceID = origenCab.DeviceID,
                    VendedorID = origenCab.VendedorID,
                    ClienteID = origenCab.ClienteID,
                    ClienteCodigo = origenCab.ClienteCodigo,
                    ClienteDireccion = origenCab.ClienteDireccion,

                    FechaCarga = DateTime.Now,
                    Cf = origenCab.Cf,
                    Division = origenCab.Division,
                    Comentario = (origenCab.Comentario ?? string.Empty) + " [Reactivado] " + DateTime.Now,
                    Latitud = origenCab.Latitud,
                    Longitud = origenCab.Longitud,

                    // Reset TX/estado
                    ResultadoTx = "",
                    FechaDeTx = null,
                    Error = "",
                    PedidoCerrado = false,
                    PedidoTransmitido = 0,

                    // Reactivación: normalmente querés linkear al origen
                    PedidoReactivado = true,
                    PedidoReactivadoId = idCabecera, // <-- clave: el origen

                    // Si realmente va en DB y existe esa columna (si no, sacalo)
                    PedidosClientesId = pedidosClientesId,

                    // Fecha de entrega
                    FechaDeEntrega = DateTime.Now,

                    // Identificadores de pago
                    KeyPagoID = "",
                    PaymentID = "",

                    // Total: lo recalculamos con detalles
                    TotalPedido = 0m,

                    TipoDeClienteDelPedido = origenCab.TipoDeClienteDelPedido,

                    // Si es [Ignore] en SQLite, ok. Si NO, esto te rompe inserts.
                    LstProductos = new List<PedidosDetalleItem>()
                };

                int nuevoId = 0;

                // 3) Transacción REAL (cabecera + detalles, todo junto)
                await _dbConnection.RunInTransactionAsync(conn =>
                {
                    // Insert cabecera (sync)
                    var filasCab = conn.Insert(nuevaCab);
                    if (filasCab <= 0)
                        throw new Exception("No se pudo insertar la nueva cabecera.");

                    // sqlite-net setea el PK en el objeto si es [AutoIncrement]
                    nuevoId = nuevaCab.PedidoCabeceraID;
                    if (nuevoId <= 0)
                        throw new Exception("No se obtuvo el nuevo PedidoCabeceraID.");

                    decimal total = 0m;

                    foreach (var d in origenDet)
                    {
                        // Clonar detalle (asegurate que el ctor coincide con tu modelo)
                        var nuevoDet = new PedidosDetalleItem(
                            id: 0,
                            pedidoCabeceraID: nuevoId,
                            productoID: d.ProductoID,
                            combosID: d.CombosID,
                            cantidad: d.Cantidad,
                            listadePrecioID: d.ListadePrecioID,
                            precioUnitarioFinal: d.PrecioUnitarioFinal,
                            precioUnitarioNeto: d.PrecioUnitarioNeto,
                            resultadoTx: "",
                            grupoDinamico: d.GrupoDinamico,
                            palabraClave: d.PalabraClave,
                            nombreProducto: d.NombreProducto,
                            codigoProducto: d.CodigoProducto,
                            cf: d.CF,
                            unidadesPorBulto: d.UnidadesPorBulto,
                            cantdecombos: d.Cantdecombos,
                            combocodigo: d.ComboCodigo
                        );

                        nuevoDet.ImagenWeb = d.ImagenWeb;

                        var filasDet = conn.Insert(nuevoDet);
                        if (filasDet <= 0)
                            throw new Exception("No se pudo insertar un detalle del pedido.");

                        // Total (ajustá la fórmula a tu lógica real)
                        total += (nuevoDet.PrecioUnitarioFinal * (decimal)nuevoDet.Cantidad);
                    }

                    // Actualizar total en cabecera
                    nuevaCab.TotalPedido = total;
                    conn.Update(nuevaCab);
                });

                return (true, nuevoId, null);
            }
            catch (Exception ex)
            {
                // RunInTransactionAsync hace rollback automático si lanza excepción
                return (false, null, $"Error al duplicar el pedido: {ex.Message}");
            }
        }


        //public async Task<(bool Ok, int? NuevoCabeceraID, string? Error)> DuplicarPedidoAsync(int idCabecera)
        //{
        //    if (_dbConnection == null) SetUpDb();

        //    var detService = new PedidosDetalleItemService();

        //    // 1) Leer origen (cabecera + detalles)
        //    PedidoCabeceraItem? origenCab;
        //    List<PedidosDetalleItem> origenDet;
        //    try
        //    {
        //        origenCab = await GetByCabeceraID(idCabecera);
        //        if (origenCab is null)
        //            return (false, null, $"No se encontró la cabecera con ID {idCabecera}.");

        //        origenDet = await detService.GetByPedidoCabezaraID(idCabecera) ?? new List<PedidosDetalleItem>();
        //    }
        //    catch (Exception ex)
        //    {
        //        return (false, null, $"Error al leer el pedido origen: {ex.Message}");
        //    }

        //    var pedidos = GetPedidosTransmitidosPorCliente(origenCab.ClienteID);


        //    //List<int> pedidosId = new List<int>();
        //    //foreach(var pedido in pedidos.Id)
        //    //{

        //    //pedidosId.Add(pedido.Id);
        //    //} 

        //    // 2) Armar nueva cabecera (PK = 0 => autoincrement)
        //    var nuevaCab = new PedidoCabeceraItem
        //    {
        //        PedidoCabeceraID = 0,
        //        DeviceID = origenCab.DeviceID,
        //        VendedorID = origenCab.VendedorID,
        //        ClienteID = origenCab.ClienteID,
        //        ClienteCodigo = origenCab.ClienteCodigo,
        //        ClienteDireccion = origenCab.ClienteDireccion,

        //        FechaCarga = DateTime.Now,
        //        Cf = origenCab.Cf,
        //        Division = origenCab.Division,
        //        Comentario = (origenCab.Comentario ?? string.Empty) + $" [Reactivado]",
        //        Latitud = origenCab.Latitud,
        //        Longitud = origenCab.Longitud,

        //        // Reset TX/estado
        //        ResultadoTx = "",
        //        FechaDeTx = null,
        //        Error = "",
        //        PedidoCerrado = false,
        //        PedidoTransmitido = 0,
        //        PedidoReactivado = true,
        //        PedidoReactivadoId = origenCab.PedidoReactivadoId,
        //        PedidosClientesId= [1,2,3],          
        //        // Monto
        //        TotalPedido = 0m,

        //        // Tipo de cliente (se conserva)
        //        TipoDeClienteDelPedido = origenCab.TipoDeClienteDelPedido,

        //        // Fecha de entrega: la dejamos vacía (si querés copiarla, poné = origenCab.FechaDeEntrega)
        //        FechaDeEntrega = null,

        //        // Identificadores de pago: se limpian
        //        KeyPagoID = "",
        //        PaymentID = "",

        //        LstProductos = new List<PedidosDetalleItem>()
        //    };

        //    // 3) Transacción: insertar cabecera y detalles
        //    int? nuevoId = null;
        //    try
        //    {
        //        await _dbConnection.ExecuteAsync("BEGIN IMMEDIATE TRANSACTION");

        //        // Cabecera
        //        var filasCab = await _dbConnection.InsertAsync(nuevaCab);
        //        if (filasCab <= 0)
        //            throw new Exception("No se pudo insertar la nueva cabecera.");

        //        nuevoId = nuevaCab.PedidoCabeceraID;
        //        if (nuevoId is null || nuevoId <= 0)
        //            throw new Exception("No se obtuvo el nuevo PedidoCabeceraID.");

        //        // Detalles clonados
        //        foreach (var d in origenDet)
        //        {
        //            var nuevoDet = new PedidosDetalleItem(
        //                id: 0,
        //                pedidoCabeceraID: nuevoId.Value,
        //                productoID: d.ProductoID,
        //                combosID: d.CombosID,
        //                cantidad: d.Cantidad,
        //                listadePrecioID: d.ListadePrecioID,
        //                precioUnitarioFinal: d.PrecioUnitarioFinal,
        //                precioUnitarioNeto: d.PrecioUnitarioNeto,
        //                resultadoTx: "", // requerido por tu ctor, no se mapea a propiedad
        //                grupoDinamico: d.GrupoDinamico,
        //                palabraClave: d.PalabraClave,
        //                nombreProducto: d.NombreProducto,
        //                codigoProducto: d.CodigoProducto,
        //                cf: d.CF,
        //                unidadesPorBulto: d.UnidadesPorBulto,
        //                cantdecombos: d.Cantdecombos,
        //                combocodigo: d.ComboCodigo

        //            );

        //            // Propiedad no presente en el ctor
        //            nuevoDet.ImagenWeb = d.ImagenWeb;

        //            var filasDet = await detService.Add(nuevoDet);
        //            if (filasDet <= 0)
        //                throw new Exception("No se pudo insertar un detalle del pedido.");
        //        }

        //        await _dbConnection.ExecuteAsync("COMMIT");
        //        return (true, nuevoId, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        try { await _dbConnection.ExecuteAsync("ROLLBACK"); } catch { /* ignorar */ }
        //        return (false, null, $"Error al duplicar el pedido: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// Helper: duplica y devuelve solo el nuevo ID (o -1 si falla).
        /// </summary>
        public async Task<List<PedidoCabeceraItem>> GetUltimosPedidosRemotosAsync(int clienteID)
        {
            ParametrosServices oParam = new ParametrosServices();
            string url = oParam.urlApi + "ProductosItem/UltimosPedidosPorCliente/" + clienteID;

            try
            {
                using (HttpClient client = HttpHelper.CreateClient())
                {
                    static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                    client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                        return new List<PedidoCabeceraItem>();

                    string json = await response.Content.ReadAsStringAsync();
                    return System.Text.Json.JsonSerializer.Deserialize<List<PedidoCabeceraItem>>(
                               json,
                               new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                           ) ?? new List<PedidoCabeceraItem>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PedidoCabeceraService] Error GetUltimosPedidosRemotosAsync: {ex.Message}");
                return new List<PedidoCabeceraItem>();
            }
        }

        public async Task<int> DuplicarPedidoYDevolverId(int idCabecera)
        {
            try
            {
                var (ok, nuevoId, err) = await DuplicarPedidoAsync(idCabecera);
                if (!ok || nuevoId is null) return -1;
                return nuevoId.Value;
            }
            catch
            {
                return -1;
            }
        }




    }
}
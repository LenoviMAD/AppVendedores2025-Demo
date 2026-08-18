//using Android.OS;
using EntidadesAppVendedores;
using Microsoft.JSInterop;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Newtonsoft.Json;
using Microsoft.JSInterop;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using EntidadesGeneraleFrWks;
using Microsoft.AspNetCore.Routing;
//agregado para la busqueda
using FuzzySharp;
using System.Globalization;
using System.Collections.Immutable;
//using AutoMapper;
using static AppVendedores2025.Services.ProductoListaService;

//using static Android.Content.ClipData;

//using static Android.Content.ClipData;
//using static Android.Content.ClipData;


//using static MudBlazor.CategoryTypes;

namespace AppVendedores2025.Services
{
    public class ProductoListaService : IProductoListaItem
    {
        //ListaDePreciosUsada
        //    int listaDePrecio = 4;
        private SQLiteAsyncConnection _dbConnection;

        public ProductoListaService()
        {
            SetUpDb();
        }
        private void SetUpDb()
        {


            if (_dbConnection == null)
            {
                _dbConnection =  DABSqlLite.SetUpDb();
            }
        }



        private SQLiteConnection _dbConnectionSinc;



        public async Task<int> Add(ProductoListaItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }
        public async Task<int> AddProductoListaItems(List<ProductoListaItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            //return await _dbConnection.UpdateAllAsync(items);

            return await _dbConnection.InsertAllAsync(items);
        }


        public async Task<int> UpsertProductosEnBloqueConImagenes(
    IEnumerable<ProductoListaItem> productos,
    IJSRuntime js,
    long syncId,
    int uiCada = 300)
        {
            try
            {
                if (_dbConnection == null) SetUpDb();
                await AsegurarColumnaLastSeenSyncId();

                var lista = productos?.ToList() ?? new List<ProductoListaItem>();
                if (lista.Count == 0) return 0;

                var imgService = new ImagenAppItemService();
                var cantImg = await imgService.GetCountImg();

                int total = lista.Count;
                int i = 0;

                // 1) Completar imágenes (con await permitido)
                foreach (var it in lista)
                {
                    i++;

                    if (cantImg > 0)
                    {
                        int pid = it.ProductosIDMaestro < 0 ? -it.ProductosIDMaestro : it.ProductosIDMaestro;

                        try
                        {
                            var imgItem = await imgService.GetByID(pid);
                            it.ImagenDataBase = imgItem?.ImagenChica ?? "";
                        }
                        catch
                        {
                            it.ImagenDataBase ??= "";
                        }
                    }

                    // UI cada N (NO por item)
                    if (i % uiCada == 0 || i == total)
                    {
                        await js.InvokeVoidAsync("replaceSyncMessage", $"*Productos* preparando ({i}/{total})");
                    }
                }

                // 2) Upsert en bloque con transacción
                return await UpsertProductosEnBloque(lista, syncId, js, uiCada);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error UpsertProductosEnBloqueConImagenes: " + ex.Message, "OK");
                return -1;
            }
        }


        public async Task<int> UpsertProductosEnBloque(List<ProductoListaItem> items, long syncId, IJSRuntime? js = null, int uiCada = 300)
        {
            if (items == null || items.Count == 0) return 0;

            try
            {
                if (_dbConnection == null) SetUpDb();

                await AsegurarColumnaLastSeenSyncId();

                var imgService = new ImagenAppItemService();
                var hayImgs = (await imgService.GetCountImg()) > 0;

                int total = items.Count;
                int procesados = 0;

                // SQLiteAsyncConnection tiene RunInTransactionAsync
                await _dbConnection.RunInTransactionAsync(conn =>
                {
                    foreach (var it in items)
                    {
                        // marcar "visto"
                        it.LastSeenSyncId = syncId;

                        // OJO: acá NO podemos await dentro del transaction.
                        // Por eso las imágenes las resolvemos fuera (ver método wrapper abajo).
                        conn.InsertOrReplace(it);
                        procesados++;
                    }
                });

                // UI: evitar 1 llamada por item
                if (js != null)
                {
                    await js.InvokeVoidAsync("replaceSyncMessage", $"*Productos* ({total} items) guardados");
                }

                return procesados;
            }
            catch (Exception ex)
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                    await App.Current.MainPage.DisplayAlert("Atención", "No hay conexión a internet.", "OK");
                else
                    await App.Current.MainPage.DisplayAlert("Atención", "Error UpsertProductosEnBloque: " + ex.Message, "OK");

                return -1;
            }
        }

        public async Task<int> BorrarNoVistosEnSync(long syncId)
        {
            try
            {
                if (_dbConnection == null) SetUpDb();
                await AsegurarColumnaLastSeenSyncId();

                // Borra los que no fueron “vistos” en este snapshot
                var sql = $"DELETE FROM {nameof(ProductoListaItem)} WHERE LastSeenSyncId <> ?";
                return await _dbConnection.ExecuteAsync(sql, syncId);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error borrando productos no vistos: " + ex.Message, "OK");
                return -1;
            }
        }



        public async Task AsegurarSchemaProductoListaItemAsync()
        {
            try
            {
                if (_dbConnection == null) SetUpDb();

                // Asegura tabla
                await _dbConnection.CreateTableAsync<ProductoListaItem>();

                // Detectar si existe columna (sin depender de exception)
                var cols = await _dbConnection.QueryAsync<TableInfoRow>($"PRAGMA table_info({nameof(ProductoListaItem)})");
                var existe = cols.Any(c => string.Equals(c.name, "LastSeenSyncId", StringComparison.OrdinalIgnoreCase));
                if (!existe)
                {
                    await _dbConnection.ExecuteAsync(
                        $"ALTER TABLE {nameof(ProductoListaItem)} ADD COLUMN LastSeenSyncId INTEGER NOT NULL DEFAULT 0");
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención",
                    "Error preparando schema ProductoListaItem: " + ex.Message, "OK");
            }
        }

        private class TableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; } = "";
            public string type { get; set; } = "";
            public int notnull { get; set; }
            public string dflt_value { get; set; } = "";
            public int pk { get; set; }
        }


        public async Task<int> SincronizarSnapshotSinFechaAsync(
    IEnumerable<ProductoListaItem> productos,
    IJSRuntime JS,
    int uiCada = 1)
        {
            try
            {
                if (_dbConnection == null) SetUpDb();

                await AsegurarSchemaProductoListaItemAsync();

                var lista = productos?.ToList() ?? new List<ProductoListaItem>();
                if (lista.Count == 0) return 0;

                // marca para esta corrida
                long syncId = DateTime.UtcNow.Ticks;

                await JS.InvokeVoidAsync("replaceSyncMessage", $"*Productos* preparando (0/{lista.Count})");

                // 1) Completar imágenes (si existen)
                var imgService = new ImagenAppItemService();
                int cantImg = 0;

                try { cantImg = await imgService.GetCountImg(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProductoListaService] GetCountImg error: {ex.Message}"); cantImg = 0; }

                int i = 0;
                foreach (var it in lista)
                {
                    i++;

                    // marcar visto
                    it.LastSeenSyncId = syncId;

                    if (cantImg > 0)
                    {
                        int pid = it.ProductosIDMaestro < 0 ? -it.ProductosIDMaestro : it.ProductosIDMaestro;
                        try
                        {
                            var imgItem = await imgService.GetByID(pid);
                            it.ImagenDataBase = imgItem?.ImagenChica ?? "";
                        }
                        catch
                        {
                            it.ImagenDataBase ??= "";
                        }
                    }

                    if (i % uiCada == 0 || i == lista.Count)
                        await JS.InvokeVoidAsync("replaceSyncMessage", $"*Productos* preparando ({i}/{lista.Count})");
                }

                // 2) UPSERT en bloque (transacción)
                await JS.InvokeVoidAsync("replaceSyncMessage", $"*Productos* guardando ({lista.Count})…");

                int upsertCount = 0;
                await _dbConnection.RunInTransactionAsync(conn =>
                {
                    foreach (var it in lista)
                    {
                        // InsertOrReplace = UPSERT por PK ProductosID
                        conn.InsertOrReplace(it);
                        upsertCount++;
                    }
                });

                // 3) Borrar lo que ya no vino en el snapshot
                await JS.InvokeVoidAsync("replaceSyncMessage", "*Productos* limpiando…");
                var borrados = await _dbConnection.ExecuteAsync(
                    $"DELETE FROM {nameof(ProductoListaItem)} WHERE LastSeenSyncId <> ?", syncId);

                await JS.InvokeVoidAsync("replaceSyncMessage",
                    $"*Productos* OK. Guardados: {upsertCount} / Borrados: {borrados}");

                return upsertCount;
            }
            catch (Exception ex)
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    await App.Current.MainPage.DisplayAlert("Atención",
                        "No hay conexión a internet. Verifique su conexión.", "OK");
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Atención",
                        "Error sincronizando productos (snapshot): " + ex.Message, "OK");
                }
                return -1;
            }
        }


        //public async Task<int> AddProductoListaItems(List<ProductoListaItem> items)
        //{
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();
        //    }

        //    int count = 0;
        //    await _dbConnection.RunInTransactionAsync(conn =>
        //    {
        //        count = conn.InsertAll(items);
        //    });
        //    return count;
        //}


        public async Task AsegurarColumnaLastSeenSyncId()
        {
            try
            {
                if (_dbConnection == null) SetUpDb();

                // Crear tabla si no existe (por las dudas)
                await _dbConnection.CreateTableAsync<ProductoListaItem>();

                // Intento ALTER (si ya existe la columna, va a fallar -> lo ignoramos)
                try
                {
                    await _dbConnection.ExecuteAsync($"ALTER TABLE {nameof(ProductoListaItem)} ADD COLUMN LastSeenSyncId INTEGER NOT NULL DEFAULT 0");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProductoListaService] ALTER TABLE LastSeenSyncId: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error preparando tabla ProductoListaItem: " + ex.Message, "OK");
            }
        }

        public async Task<int> UpDateAddListaItems(List<ProductoListaItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.UpdateAllAsync(items);
        }

        public Task<int> Delete(ProductoListaItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<List<int>> GetAllProdIDs()
        {
            if (_dbConnection == null)
            {
                SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
            }
            // Obtener todos los productos de la base de datos
            var lsProdTotal = await _dbConnection.Table<ProductoListaItem>().ToListAsync();
            // Extraer solo los ProductosID
            return lsProdTotal.Select(p => p.ProductosID).ToList();

        }
        //public async Task<string> UpdateTotalPedido(int pedidoID, decimal TotalPedido)
        //{
            //var sql = $"UPDATE  {nameof(PedidoCabeceraItem)}  SET TotalPedido ='" + TotalPedido + "' WHERE PedidoCabeceraID =" + pedidoID;

        //    var result = await _dbConnection.ExecuteAsync(sql);

        //    return "";
        //}
        public async Task<int> DeleteProductoPorID(IEnumerable<int> item)
        {
            if (item == null)
            {
                return 0; // Si la lista está vacía, no hay nada que borrar
            }
            int totalDeleted = 0;

            try
            {
                string lsProductosID = "";

                // Borrar producto por producto en la lista
                foreach (var productoID in item)
                {
                    lsProductosID += productoID + ",";

                }

                 lsProductosID = "(" + lsProductosID.Substring(0, lsProductosID.Length - 1) + ")";
                // Usar ExecuteAsync para borrar el producto con el ProductoID especificado
                //var query = $"DELETE FROM {nameof(ProductoListaItem)}";
                var query = $"DELETE FROM {nameof(ProductoListaItem)} WHERE ProductosID in " + lsProductosID ;

                // Ejecutar la consulta DELETE para cada producto
                var result = await _dbConnection.ExecuteAsync(query);
                //var result = await _dbConnection.ExecuteAsync(query, new { ProductoID = productoID });

                // Incrementar el contador si se borró un producto
                totalDeleted += result; // result es el número de filas afectadas (1 si se borra, 0 si no)


                return totalDeleted; // Devolver el total de productos eliminados
            }
            catch (Exception ex)
            {
                
                return 0; // O cualquier valor que decidas para indicar un error
            }
        }

        private static List<ProductoListaItem> lsProductosCache = null;

        public void DeleteCacheProductos(bool borrar)
        {

            if (borrar)
            {
                lsProductosCache = null;
            }

            //      throw new NotImplementedException();
        }

        private static DateTime? redDataProd = null;

        //public static void RestCache()
        //{
        //    redDataProd = null;

        //}


        public async Task<List<ProductoListaItem>> GetAllFiltradoBloqueados(int clienteID, List<ProductoListaItem> ProductosEnCache)
        {
            List<ProductoListaItem> lsTT;

            if (ProductosEnCache == null || !ProductosEnCache.Any())
            {
                lsTT = await GetAll();

            }
            else
            {
                lsTT = ProductosEnCache;
                return lsTT;
            }

            ParametrosServices oParam = new ParametrosServices();

            if (oParam.tipoVD == 5)
            {
                ProductosBloqueadosxClienteService oProductosBloqueadosxClienteService = new ProductosBloqueadosxClienteService();

                var lsBloqueados = await oProductosBloqueadosxClienteService.GetProductosBloqueadosxCliente(clienteID);

                lsTT = lsTT.Where(item1 => !lsBloqueados.Any(item2 => item2.ProductosID == item1.ProductosID)).ToList();

            }

            CombosxclienteService oCombosxclienteService = new CombosxclienteService();
            var lsCombosXCliOrdenados = await oCombosxclienteService.GetCombosxClientexCliID(clienteID);
            lsCombosXCliOrdenados = lsCombosXCliOrdenados.OrderBy(c => c.Orden).ToList();

            var lsIDCombosClienteActual = lsCombosXCliOrdenados.Select(c => c.CombosID).ToList();

            // Si el backend no envía restricciones de combos por cliente (CombosXClientesItem vacío,
            // como pasa en este demo, que no implementa esa asociación), no hay ninguna restricción real:
            // en ese caso habilitamos todos los combos en vez de ocultarlos por completo.
            bool hayRestriccionCombosCliente = lsIDCombosClienteActual.Any();

            // Valido los combos que tiene habilitado el cliente
            foreach (var ipro in lsTT)
            {
                ipro.LstComboCliente = new List<CombosEnUnProductoItem>();
                foreach (var icom in ipro.LstCombo)
                {

                    if (!hayRestriccionCombosCliente || lsIDCombosClienteActual.Contains(icom.CombosID))
                    {
                        ipro.LstComboCliente.Add(icom);
                    }
                }
            }

            var resultado = lsTT
                .Where(c => c.ProductosID > 0 || (c.ProductosID < 0 && (!hayRestriccionCombosCliente || lsIDCombosClienteActual.Contains(-c.ProductosID))))
                .ToList();

            // Ocultar padre si tiene hijos REALES (no auto-referencia) y no tiene combos propios
            var idsPadresConHijos = new HashSet<int>(
                resultado.Where(p => p.ProductosIDPadre > 0 && p.ProductosIDPadre != p.ProductosID)
                         .Select(p => p.ProductosIDPadre));

            if (idsPadresConHijos.Count > 0)
            {
                resultado = resultado.Where(p =>
                {
                    if (!idsPadresConHijos.Contains(p.ProductosID))
                        return true;

                    return p.LstComboCliente != null && p.LstComboCliente.Any();
                }).ToList();
            }

            return resultado;
        }





        public async Task<List<ProductoListaItem>> GetAll()
        {
            if (redDataProd.HasValue)
            {
                var tiempoTr = DateTime.Now - redDataProd.Value;
                if (tiempoTr.TotalMinutes > 30)
                {
                    lsProductosCache = null;
                }
                if (lsProductosCache != null && lsProductosCache.Count() > 1)
                {
                    return lsProductosCache;
                }
            }

            redDataProd = DateTime.Now;
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            CombosItemService combosItemService = new CombosItemService();
            //CombosProducto combosProductos=new CombosProductosItem();
            var lsProductosEnCombos = await combosItemService.GetProductosEnCombo();



            //----------------------------------------------

            //var productoQuery = _dbConnection.Table<ProductoListaItem>().ToListAsync();
            //var imagenQuery = _dbConnection.Table<ImagenAppItem>().ToListAsync();

            //await Task.WhenAll(productoQuery, imagenQuery);

            //var productos = productoQuery.Result;
            //var imagenes = imagenQuery.Result;

            //var query = from producto in productos
            //            join imagen in imagenes on producto.ProductosID equals imagen.ProductosID
            //            select new ProductoListaItem
            //            {
            //                // Asignar todas las propiedades de ProductoListaItem
            //                // utilizando la sintaxis de inicialización de objeto
            //                ProductosID = producto.ProductosID,
            //                ProductosIDPadre = producto.ProductosIDPadre,
            //                Orden = producto.Orden,
            //                // ... asignar todas las propiedades necesarias

            //                // Asignar el valor de imagenweb
            //                ImagenDataBase = imagen.ImagenChica
            //            };

            //lsProductosCache = query.ToList();






            //-----------------------------------------











            lsProductosCache = await _dbConnection.Table<ProductoListaItem>().ToListAsync();
            //Traigo todos los productos que estan en combos 
            var lsProductosConCmbos = lsProductosCache.Where(c => lsProductosEnCombos.Contains(c.ProductosID));


            foreach (var item in lsProductosConCmbos)
            {
                var lsCombosProd = await combosItemService.GetCombosDeUnProducto(item.ProductosID);
                item.LstCombo = lsCombosProd;
            }





            CombosItemService oCombosItemService = new CombosItemService();
            var lsCombosItem = await oCombosItemService.GetCombosHeadItem(0);

            foreach (var ic in lsCombosItem.OrderByDescending(c => c.TotalVenta))
            {
                var proIt = new ProductoListaItem();

                // var prodItem = await GetByID(ic.ComboProductoID);
                proIt.ImagenDataBase = ic.ImagenDataBase;
                proIt.CodigoDeProducto = "*" + ic.Codigo;
                proIt.ProductosID = -ic.CombosID;
                proIt.Nombre = "***" + ic.NombreCombo;
                proIt.Color = ic.Color;
                proIt.ComboCodigo = ic.Codigo;

                lsProductosCache.Add(proIt);

            }
            //foreach (var ic in lsCombosItem)
            //{
            //    var lsProductosCombo = await oCombosItemService.GetProductosByID(ic.comb_combosid);
            //    foreach (CombosProductosItem pc in lsProductosCombo)
            //    {


            //        var proIt = new ProductoItemJson();
            //        proIt.ImagenDataBase = pc.ImagenDataBase;
            //        //  (await GetByID(pc.comb_productosid)).ImagenDataBase;
            //        proIt.prod_cod = (await GetByID(pc.comb_productosid)).prod_cod;
            //        proIt.prod_id = pc.comb_productosid;
            //        proIt.prod_prod = (await GetByID(pc.comb_productosid)).prod_prod;
            //        proIt.combosID = pc.comb_combosid;

            //        lsProductos.Add(proIt);
            //    }
            //}
            //foreach(var it in lsProductosCache)
            //{
            //    it.ImagenDataBase = await GetStringIMG(it.ProductosIDMaestro);
            //        {


            //    }
            //}
            lsProductosCache = lsProductosCache.OrderBy(c => c.Orden).ToList();


            return lsProductosCache;
        }


        //public async Task<string> GetStringIMG(int id)
        //{
        //    string ImagenDataBase = "";
        //    ImagenAppItemService oImgService = new ImagenAppItemService();
        //    ImagenDataBase = (await oImgService.GetByID(id)).ImagenChica;
        //    //  var GG= oImgService.GetByID(id).GetAwaiter().GetResult().ImagenChica;
        //    //var type = "image/png";
        //    //string imagenData = $"data:{type};base64,{ImagenDataBase}";
        //    return ImagenDataBase;

        //}
        public async Task<List<ProductoListaItem>> GetAllLista()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var lsp = await GetAll();
            List<ProductoListaItem> lsSal = new List<ProductoListaItem>();
            CombosItemService oCombosItemService = new CombosItemService();


            ////  int listaDePrecio = 4;
            //foreach (var it in lsp)
            //{
            //   // string nombreCombo = await GetNombreCombo(oCombosItemService, it.ComboID);
            //    lsSal.Add(new ProductoListaItem(it.ProductosID, it.ProductosIDPadre, it.CodigoDeProducto, it.Nombre, it.StockEnUnidades, it.CantidadMultiplo
            //        ,
            //        it.DesactivadoParaLaVenta, it.Rubro, it.Marca, listaDePrecio, it.StockPropio, 1, it.UnidadesPorBulto,
            //        it.PrecioUnitarioFinalAutoservicio, it.PrecioUnitarioFinalEspecial, it.PrecioUnitarioFinalAlamcene, it.PrecioUnitarioFinalKiosko

            //        , 0, it.ImagenDataBase, it.ComboID));

            //}


            return lsSal;
        }



        public async Task<ProductoListaItem> GetByIdLista(int prouctosID)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var it = await GetByID(prouctosID);
            //  CombosItemService oCombosItemService = new CombosItemService();

            //string nombreCombo = await GetNombreCombo(oCombosItemService, it.combosID);
            ////it.co




            //    var lsSal=(new ProductoListaItem(it.prod_id, it.prod_idpadre, it.prod_cod, it.prod_prod, it.prod_stk, it.prod_cantmultped, it.prod_desactvta == 1, it.prod_rub, it.prod_mar, listaDePrecio, it.prod_stkprop == 1, 1, Convert.ToInt32(it.prod_UnixBto), Convert.ToDecimal(it.prod_prevta3), Convert.ToDecimal(it.prod_prevta1), Convert.ToDecimal(it.prod_prevta4), Convert.ToDecimal(it.prod_prevta5), 0, it.ImagenDataBase, it.combosID, nombreCombo));



            return it;
        }




        public async Task<List<ProductoListaItem>> GetFiltro(string filtro, int clienteID, List<ProductoListaItem> Productos)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
          
            Productos = await GetAllFiltradoBloqueados(clienteID, Productos);
            
            var lsFiltrp = Productos.Where(c => c.TextoParaBusqueda.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) != -1);
           
            return lsFiltrp.ToList();

        }


private static string QuitarAcentosYSímbolos(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        // Normaliza y elimina diacríticos (acentos) + símbolos/puntuación principales
        var norm = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(norm.Length);
        foreach (var ch in norm)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark &&            // acentos
                cat != UnicodeCategory.SpacingCombiningMark &&
                cat != UnicodeCategory.EnclosingMark &&
                cat != UnicodeCategory.OtherSymbol &&               // símbolos
                cat != UnicodeCategory.ModifierSymbol &&
                cat != UnicodeCategory.MathSymbol &&
                cat != UnicodeCategory.InitialQuotePunctuation &&   // puntuación
                cat != UnicodeCategory.FinalQuotePunctuation &&
                cat != UnicodeCategory.DashPunctuation &&
                cat != UnicodeCategory.OtherPunctuation)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // MISMA FIRMA Y SEMÁNTICA QUE LA TUYA:
    public static bool ContainsLike(string? source, string? term)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(term))
            return false;

        var a = QuitarAcentosYSímbolos(source).ToUpperInvariant();
        var b = QuitarAcentosYSímbolos(term).ToUpperInvariant();

        // Ordinal (sin cultura) y ya upper-invariant => rápido y compatible con HybridGlobalization
        return a.IndexOf(b, StringComparison.Ordinal) >= 0;
    }


    public async Task<List<ProductoListaItem>> GetFiltroDelCliente(string filtro, int clienteID, List<ProductoListaItem> Productos)
        {
            var lsFiltrp = new List<ProductoListaItem>();
            try {
                if (_dbConnection == null)
                {
                    SetUpDb();
                }

                if (Productos == null || !Productos.Any())
                {
                    Productos = await GetAllFiltradoBloqueados(clienteID, Productos);

                }

                //var lsFiltrp = Productos.Where(c => c.TextoParaBusqueda.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) != -1);
                 lsFiltrp = Productos.Where(p => ContainsLike(p.TextoParaBusqueda, filtro))
                        .ToList();
            }
            catch (Exception ex) {
                      //await JS.InvokeVoidAsync("console.error", $"Traer error: {ex.Message}");
                      Console.WriteLine(ex.Message.ToString());
            }
            


            return lsFiltrp.ToList();

        }



        // Función de normalización del texto
        //private static string NormalizeText(string text)
        //{
        //    if (string.IsNullOrEmpty(text))
        //        return text;

        //    // Convertir a minúsculas
        //    text = text.ToLowerInvariant();

        //    // Eliminar acentos
        //    var normalizedString = text.Normalize(NormalizationForm.FormD);
        //    var stringBuilder = new StringBuilder();

        //    foreach (var c in normalizedString)
        //    {
        //        var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
        //        if (unicodeCategory != UnicodeCategory.NonSpacingMark)
        //        {
        //            stringBuilder.Append(c);
        //        }
        //    }

        //    return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        //}


        public async Task<ProductoListaItem> GetByID(int id)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            string sp = $"Select * From {nameof(ProductoListaItem)} where ProductosID={id}";


            var resultado = await _dbConnection.QueryAsync<ProductoListaItem>(sp);
            var res = resultado.FirstOrDefault();
            if (res == null)
            {


                //sp = $"Select * From {nameof(ProductoListaItem)} where ProductosIDPadre={id}";
                //resultado = await _dbConnection.QueryAsync<ProductoListaItem>(sp);
                //res = resultado.FirstOrDefault();

                if (res == null)
                {
                    res = new ProductoListaItem();
                    res.Nombre = "Inexistente en la base";
                    res.CodigoDeProducto = "INX";
                }




            }
            //    var res = new ProductoListaItem();
            return res;




        }




        //public async Task<ProductoListaItem> GetByIDProductoListaItem(int id)
        //{
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();
        //    }
        //    string sp = $"Select * From {nameof(ProductoListaItem)} where ProductosID={id}";


        //    var resultado = await _dbConnection.QueryAsync<ProductoListaItem>(sp);
        //    return resultado.FirstOrDefault();

        //}

        public async Task<int> Update(ProductoListaItem item)
        {
            //if (_dbConnection == null)
            //{
            //    SetUpDb();
            //}
            throw new NotImplementedException();
            //string sql = "UPDATE ProductoListaItem SET ImagenDataBase ='" + item.ImagenDataBase + "' WHERE ProductosID = " + item.ProductosID;

            //return await _dbConnection.ExecuteAsync(sql);
        }

        public async Task<int> BorrarTodoProductoListaItem()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            //_dbConnection.CreateTableAsync<ProductoListaItem>();

            var res = await _dbConnection.DeleteAllAsync<ProductoListaItem>();

            return res;
        }

        public async Task<bool> GetImagen(int productosID, DateTime fechaActual)

        {

            return true;
        }

        public async Task<List<CategoriaItem>> GetCatgorias(IJSRuntime JS)
        {
            try
            {
                LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);
                return JsonConvert.DeserializeObject<List<CategoriaItem>>(await LocalStorage.GetValueAsync<string>("lsCategorias"));
            }
            catch
            {
                return new List<CategoriaItem> { };
            }

        }


        public async Task<int> Sincronizar(IEnumerable<EntidadesAppVendedores.ProductoListaItem> lsProductosJson)
        {
            int resBorr = 0;
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            try
            {
                resBorr = await BorrarTodoProductoListaItem();
            }
            catch (Exception ex)
            {
                // await  _dbConnection.CreateTableAsync<ProductoListaItem>();

                await App.Current.MainPage.DisplayAlert("Atencion", "error  sincronizar productosservice Nr4 " + Environment.NewLine + ex.Message, "OK");
            }
            resBorr = 1;
            var condata = true;

            while (condata)
            {
                string sp = $"Select * From {nameof(ProductoListaItem)}";

                try
                {


                    var resultado = await _dbConnection.QueryAsync<ProductoListaItem>(sp);
                    if (resultado.Any())
                    {

                        resBorr = await BorrarTodoProductoListaItem();

                    }
                    else
                    {

                        break;
                    }
                }
                catch (Exception ex)
                {
                    await App.Current.MainPage.DisplayAlert("Atencion", "error  sincronizar productosservice Nr2 " + Environment.NewLine + ex.Message, "OK");



                }



            }


            var lsProductosListaaGuardar = new List<ProductoListaItem>();
            foreach (var it in lsProductosJson)
            {
                try
                {
                    var pid = 0;
                    if (it.ProductosIDMaestro < 0) { pid = -it.ProductosIDMaestro; }
                    else
                    {
                        pid = it.ProductosIDMaestro;
                    }
                    ImagenAppItemService oImgService = new ImagenAppItemService();
                    var img = "";
                    var imgItem = await oImgService.GetByID(pid);
                    if (imgItem != null)
                    {
                        img = imgItem.ImagenChica;

                    }
                    it.ImagenDataBase = img;



                    //  string nombreCombo = await GetNombreCombo(oCombosItemService, it.combosID);
                    // await Add(new ProductoListaItem(it.prod_id, it.prod_idpadre, it.prod_cod, it.prod_prod, it.prod_stk, it.prod_cantmultped, it.prod_desactvta == 1, it.prod_rub, it.prod_mar, listaDePrecio, it.prod_stkprop == 1, 1, Convert.ToInt32(it.prod_UnixBto), Convert.ToDecimal(it.prod_prevta3), Convert.ToDecimal(it.prod_prevta1), Convert.ToDecimal(it.prod_prevta4), Convert.ToDecimal(it.prod_prevta5), 0, img, it.combosID,false,false));
                    //  await Add(it);

                    lsProductosListaaGuardar.Add(it);

                }
                catch (Exception ex)
                {
                    await App.Current.MainPage.DisplayAlert("Atencion", "error  sincronizar productosservice Nr3 " + Environment.NewLine + ex.Message, "OK");

                    //   await App.Current.MainPage.DisplayAlert("Atencion", "error  sincro productoservice agregar imagen " + it.Nombre, "OK");
                }
            }

            //
            //todo
            await AddProductoListaItems(lsProductosListaaGuardar);




            //      }
            //   SetImagenes(lsProductos);

            return resBorr;

        }

        public async Task<int> SincronizarActualizados(IEnumerable<ProductoListaItem> lsProductosJson, IJSRuntime JS)
        {
            try
            {
                if (_dbConnection == null) SetUpDb();

                if (lsProductosJson == null)
                    return 0;

                var lista = lsProductosJson.ToList();
                if (lista.Count == 0)
                    return 0;

                // “syncId” para marcar vistos y borrar lo que no venga
                long syncId = DateTime.UtcNow.Ticks;

                await JS.InvokeVoidAsync("replaceSyncMessage", "*Productos* iniciando…");

                // UPSERT + imágenes
                var upsertRes = await UpsertProductosEnBloqueConImagenes(lista, JS, syncId, uiCada: 400);
                if (upsertRes < 0) return -1;

                // Borrar los que ya no existen en el snapshot
                var borrados = await BorrarNoVistosEnSync(syncId);
                if (borrados < 0) return -1;

                await JS.InvokeVoidAsync("replaceSyncMessage", $"*Productos* OK. Guardados: {upsertRes}, Borrados: {borrados}");
                return 0;
            }
            catch (Exception ex)
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                    await App.Current.MainPage.DisplayAlert("Atención", "No hay conexión a internet. Verifique su conexión.", "OK");
                else
                    await App.Current.MainPage.DisplayAlert("Atención", "Error SincronizarActualizadosOptimizado: " + ex.Message, "OK");

                return -1;
            }
        }


        //public async Task<int> SincronizarActualizados(IEnumerable<EntidadesAppVendedores.ProductoListaItem> lsProductosJson, IJSRuntime JS)
        //{
        //    var lsProductosListaaGuardar = new List<ProductoListaItem>();
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();
        //    }
        //    try
        //    {
               
        //        //await BorrarTodoProductoListaItem();
        //    }
        //    catch { }
        //    ImagenAppItemService oImgService = new ImagenAppItemService();
        //    var cantidadImg = await oImgService.GetCountImg();
        //    var indice = 0;
        //    foreach (var it in lsProductosJson)
        //    {
        //        try
        //        {
        //            var pid = 0;
        //            if (it.ProductosIDMaestro < 0) { pid = -it.ProductosIDMaestro; }
        //            else
        //            {
        //                pid = it.ProductosIDMaestro;
        //            }


                  
        //            if (cantidadImg > 0)
        //            {
        //                var img = "";
        //                var imgItem = await oImgService.GetByID(pid);
        //                if (imgItem != null)
        //                {
        //                    img = imgItem.ImagenChica;

        //                }
        //                it.ImagenDataBase = img;

        //            }

        //            //string nombreCombo = await GetNombreCombo(oCombosItemService, it.combosID);
        //            //await Add(new ProductoListaItem(it.prod_id, it.prod_idpadre, it.prod_cod, it.prod_prod, it.prod_stk, it.prod_cantmultped, it.prod_desactvta == 1, it.prod_rub, it.prod_mar, listaDePrecio, it.prod_stkprop == 1, 1, Convert.ToInt32(it.prod_UnixBto), Convert.ToDecimal(it.prod_prevta3), Convert.ToDecimal(it.prod_prevta1), Convert.ToDecimal(it.prod_prevta4), Convert.ToDecimal(it.prod_prevta5), 0, img, it.combosID, false, false));
        //            //await Add(it);
        //            indice++;
        //            await JS.InvokeVoidAsync("replaceSyncMessage", $" *Productos* ({indice} / {lsProductosJson.Count()}) ");
        //            lsProductosListaaGuardar.Add(it);

        //        }
        //        catch (Exception ex)
        //        {
        //            //await App.Current.MainPage.DisplayAlert("Atencion", "error  sincronizar productosservice Nr3 " + Environment.NewLine + ex.Message, "OK");
                 
        //            // Verificar si hay conexión a Internet
        //            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
        //            {
        //                await App.Current.MainPage.DisplayAlert("Atención", "No hay conexión a internet. Verifique su conexión.", "OK");
        //                return -1;  // Salir de la función si no hay conexión a internet
        //            }
        //            else
        //            {
        //                await App.Current.MainPage.DisplayAlert("Atencion", "FUNCION ACTUALIZAR PRODUCTOS CON CAMBIOS /" + ex.Message + " /", "OK");
        //                return -1;
        //            }
        //        }
        //    }

        //    //grabar todo
        //    await AddProductoListaItems(lsProductosListaaGuardar);
        //    return 0;
        //}

       
        // private async Task SetImagenes(IEnumerable<ProductoItemJson> lsProductos)
        // {
        //     ProductosService oPoductosServices = new ProductosService();
        //     ImagenAppItemService oImagenService = new ImagenAppItemService();



        //     foreach (var item in lsProductos)
        //     {

        //         // Console.WriteLine(item.prod_prod);
        //         // var itemn = await oPoductosServices.UpdateImagenEnProducto(item, oImagenService);
        //         await Add(item);



        //     }
        ////     await ActualizarImagenes();
        //     RestCache();
        // }


        private async Task ActualizarImagenes()
        {
            //var conexion = DABSqlLite.GetSQLiteConnectionSinc();


            //SQLite.SQLiteCommand com = new SQLite.SQLiteCommand(conexion);


            //var sql = "UPDATE  ProductoItem  SET ImagenDataBase =ImagenAppItem.ImagenChica FROM ProductoItem INNER JOIN ImagenAppItem ON ProductoItem.prod_id = ImagenAppItem.ProductosID";
            //sql = "UPDATE  ProductoItem  SET prod_rub= '****' FROM ProductoItem ";


            //sql = "Select  * from   ProductoItem  ";





            //com.CommandText = sql;

            //com.ExecuteNonQuery();



            //if (_dbConnection == null)
            //{
            //    SetUpDb();
            //}
            ////    throw new NotImplementedException();
            ////string sql = "UPDATE ProductoItem SET ImagenDataBase ='" + item.ImagenDataBase + "' WHERE prod_id = " + item.prod_id;


            ////            var sql = "UPDATE  ProductoItem  SET ImagenDataBase =ImagenAppItem.ImagenChica FROM ProductoItem INNER JOIN ImagenAppItem ON ProductoItem.prod_id = ImagenAppItem.ProductosID";

            ////var sql = "UPDATE  ProductoItem  SET ImagenDataBase =ImagenAppItem.ImagenChica FROM ProductoItem INNER JOIN ImagenAppItem ON ProductoItem.prod_id = ImagenAppItem.ProductosID";

            //var sqlsub = "(select ImagenChica from ImagenAppItem where  ProductoItem.prod_id = ImagenAppItem.ProductosID)";

            //var sql = "UPDATE  ProductoItem  SET ImagenDataBase =" + sqlsub;


            ////'" + item.ImagenDataBase + "'

            //var result = await _dbConnection.ExecuteAsync(sql);
        }

        public async Task UpdateImagenEnProducto(int pid, string img)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            if (pid != 0)
            {
                var sql = $"UPDATE  {nameof(ProductoListaItem)}  SET ImagenDataBase ='" + img + "' where ProductosID=" + pid + " or ProductosIDPadre=" + pid;
                //  var prodImagen = await oImagenService.GetByID(producto.ProductosID);
                var result = await _dbConnection.ExecuteAsync(sql);

            }


            // return producto;

        }

        public async Task<int> UpdateStockEnProducto(IEnumerable<ProductoStockItem> lsProd)
        {
            // Verifica si la conexión a la base de datos está establecida, si no, configúralo
            if (_dbConnection == null)
            {
                SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
            }

            int cantUpdate = 0;
            // Recorre la lista de productos recibida
            foreach (var prod in lsProd)
            {
                // Asegúrate de que pid y stk tengan los valores correctos, y que pid no sea 0
                if (prod.id != 0)
                {
                    var sql = $"UPDATE  {nameof(ProductoListaItem)}  SET StockEnUnidades ='" + prod.stk+ "' where ProductosID=" + prod.id;
                    
                    var result = await _dbConnection.ExecuteAsync(sql);
                    cantUpdate = cantUpdate + result;
                }
            }

            // Si necesitas devolver algo, puedes hacerlo aquí. De momento, no devuelves nada.
            return cantUpdate;
        }

        public async Task<int> UpdateStockEnProductoBloque(IEnumerable<ProductoStockItem> lsProd)
        {
            // Verifica si la conexión a la base de datos está establecida, si no, configúralo
            if (_dbConnection == null)
            {
                SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
            }

            int cantUpdate = 0;
            try
            {
                // Verifica que la lista de productos no esté vacía
                if (lsProd.Any())
                {
                    // Construir una consulta SQL simple para las actualizaciones en bloque
                    var sql = "UPDATE ProductoListaItem SET StockEnUnidades = CASE ";

                    // Recorre la lista de productos para construir la parte dinámica del SQL
                    //foreach (var prod in lsProd)
                    //{
                    //    if (prod.id != 0) // Evitar actualizar si el ID es 0
                    //    {
                    //        // Agregar una condición WHEN para cada producto
                    //        sql += $"WHEN ProductosID = {prod.id} THEN {prod.stk} ";
                    //    }
                    //}

                    // Recorre la lista de productos para construir la parte dinámica del SQL
                    foreach (var prod in lsProd)
                    {
                        // Verificar si el ID es mayor a 0 y si el Stock no es null
                        if (prod.id > 0 && prod.stk != null)
                        {
                            // Verificar si prod.stk es un número válido y mayor o igual a 0
                            if (prod.stk >= 0)  // Verificar si stk es un valor válido
                            {
                                // Agregar una condición WHEN para cada producto con un valor válido
                                sql += $"WHEN ProductosID = {prod.id} THEN {prod.stk} ";
                            }
                            else
                            {
                                // Si stk es inválido, asignar un valor predeterminado (por ejemplo, 0)
                                sql += $"WHEN ProductosID = {prod.id} THEN 0 ";
                            }
                        }
                        else
                        {
                            // Si el ID es 0 o el Stock es null, asignar un valor predeterminado (por ejemplo, 0)
                            sql += $"WHEN ProductosID = {prod.id} THEN 0 ";
                        }
                    }

                    // Completa la consulta SQL
                    sql += "END WHERE ProductosID IN (";

                    // Agregar los IDs de los productos a la cláusula IN
                    foreach (var prod in lsProd)
                    {
                        if (prod.id != 0) // Evitar agregar IDs 0
                        {
                            sql += $"{prod.id}, ";
                        }
                    }

                    // Elimina la última coma
                    sql = sql.TrimEnd(',', ' ') + ")";

                    // Ejecutar la consulta SQL
                    cantUpdate = await _dbConnection.ExecuteAsync(sql);
                }
            }
            catch(Exception ex)
            {
                // Verificar si hay conexión a Internet
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    await App.Current.MainPage.DisplayAlert("Atención", "No hay conexión a internet. Verifique su conexión.", "OK");
                    return -1;  // Salir de la función si no hay conexión a internet
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Atencion", "FUNCION ACTUALIZAR STOCK POR PRODUCTOS " + ex.Message + "/ ", "OK");
                    return -1;
                }
            }

            // Retornar la cantidad de registros actualizados
            return cantUpdate;
        }

        public async Task<int> UpdatePreciosEnProductoBloque(List<ProductoPrecioItems> lsProd)
        {
            if (_dbConnection == null)
            {
                SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
            }
            int cantUpdate = 0;
            try
            {
                if (lsProd.Any())
                {
                    // Obtener todos los productos de la base de datos
                    var lsProdTotal = await _dbConnection.Table<ProductoListaItem>().ToListAsync();
                    // Extraer solo los ProductosID
                    var lsIDs = lsProd.Select(p => p.ProdID).ToList();
                    // Filtrar lsProd para obtener solo los productos cuyos ProductosID estén en lsIDs
                    var lsProdFilID = lsProdTotal.Where(p => lsIDs.Contains(p.ProductosID)).ToList();

                    // Ahora recorremos lsProdFilID (productos a actualizar) y actualizamos los precios con los valores de lsProd
                    foreach (var prodFil in lsProdFilID)
                    {
                        // Encontrar el producto correspondiente en lsProd usando el ProdID
                        var matchingProd = lsProd.FirstOrDefault(p => p.ProdID == prodFil.ProductosID);

                        if (matchingProd != null)
                        {
                            // Si se encuentra el producto, actualizamos los precios en prodFil (que es de lsProdFilID)
                            prodFil.PrecioUnitarioFinalAutoservicio = matchingProd.PFAut;
                            prodFil.PrecioUnitarioFinalEspecial = matchingProd.PFEsp;
                            prodFil.PrecioUnitarioFinalAlamcene = matchingProd.PFAl;
                            prodFil.PrecioUnitarioFinalKiosko = matchingProd.PFFKi;
                            prodFil.PrecioUnitarioSalon = matchingProd.PFSal;
                            prodFil.PrecioUnitarioEcom = matchingProd.PFEcom;
                            prodFil.PrecioUnitarioFinalAutoservicioNeto = matchingProd.PNAut;
                            prodFil.PrecioUnitarioFinalEspecialNeto = matchingProd.PNEsp;
                            prodFil.PrecioUnitarioFinalAlamceneNeto = matchingProd.PNAlm;
                            prodFil.PrecioUnitarioFinalKioskoNeto = matchingProd.PNKio;
                            prodFil.PrecioUnitarioSalonNeto = matchingProd.PNSal;
                            prodFil.PrecioUnitarioEcomNeto = matchingProd.PNEcom;
                        }
                    }
                    cantUpdate = await UpDateAddListaItems(lsProdFilID);

                }

                return cantUpdate;
            }
            catch (Exception ex)
            {
                // Verificar si hay conexión a Internet
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    await App.Current.MainPage.DisplayAlert("Atención", "No hay conexión a internet. Verifique su conexión.", "OK");
                    return -1;  // Salir de la función si no hay conexión a internet
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Atencion", "FUNCION ACTUALIZAR STOCK POR PRODUCTOS /" + ex.Message + " /", "OK");
                    return -1;
                }
            }

        }


        public async Task<int> UpdateProductoBloquePorItems(List<ProductosActualizarItems> lsProd)
        {
            if (_dbConnection == null)
            {
                SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
            }
            int cantUpdate = 0;
            try
            {
                if (lsProd.Any())
                {
                    // Obtener todos los productos de la base de datos
                    var lsProdTotal = await _dbConnection.Table<ProductoListaItem>().ToListAsync();
                    // Extraer solo los ProductosID
                    var lsIDs = lsProd.Select(p => p.ProdID).ToList();
                    // Filtrar lsProd para obtener solo los productos cuyos ProductosID estén en lsIDs
                    var lsProdFilID = lsProdTotal.Where(p => lsIDs.Contains(p.ProductosID)).ToList();

                    // Ahora recorremos lsProdFilID (productos a actualizar) y actualizamos los precios con los valores de lsProd
                    foreach (var prodFil in lsProdFilID)
                    {
                        // Encontrar el producto correspondiente en lsProd usando el ProdID
                        var matchingProd = lsProd.FirstOrDefault(p => p.ProdID == prodFil.ProductosID);

                        if (matchingProd != null)
                        {
                            // Si se encuentra el producto, actualizamos los precios en prodFil (que es de lsProdFilID)
                            prodFil.DiasDeStockDisponible = matchingProd.DiasStkDisp;
                            prodFil.DiasDePrecioModificado = matchingProd.DiasPrecMod;
                            prodFil.NuevaIncorporacoin = matchingProd.NuevaIncor;
                            prodFil.IngresosRecientes = matchingProd.IngreRec;
                            prodFil.ComisionDiferencial = matchingProd.ComisDif;

                        }
                    }
                    cantUpdate = await UpDateAddListaItems(lsProdFilID);

                }

                return cantUpdate;
            }
            catch (Exception ex)
            {
                // Verificar si hay conexión a Internet
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    await App.Current.MainPage.DisplayAlert("Atención", "No hay conexión a internet. Verifique su conexión.", "OK");
                    return -1;  // Salir de la función si no hay conexión a internet
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Atencion", "FUNCION ACTUALIZAR ITEMS POR PRODUCTOS /" + ex.Message + " /", "OK");
                    return -1;
                }
            }

        }

        //public async Task<int> UpdatePreciosEnProductoBloque(List<ProductoPrecioItems> lsProd)
        //{
        //    // Verifica si la conexión a la base de datos está establecida, si no, configúralo
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
        //    }

        //    int cantUpdate = 0;
        //    try
        //    {
        //        // Verifica que la lista de productos no esté vacía
        //        if (lsProd.Any())
        //        {
        //            // Construir una consulta SQL dinámica para las actualizaciones en bloque
        //            var sql = @"
        //    UPDATE ProductoListaItem
        //    SET 
        //        PrecioUnitarioFinalAutoservicio = CASE ProductosID ";

        //            // Recorre la lista de productos para construir las condiciones dinámicas del SQL
        //            foreach (var prod in lsProd)
        //            {
        //                // Verificar si el valor es null y asignar 0 si lo es
        //                decimal PFAut = prod.PFAut != null ? prod.PFAut : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PFAut} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalEspecial = CASE ProductosID ";

        //            // Recorre la lista de productos para agregar las condiciones del campo PrecioUnitarioFinalEspecial
        //            foreach (var prod in lsProd)
        //            {
        //                decimal PFEsp = (prod.PFEsp != null) ? prod.PFEsp : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PFEsp} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalAlamcene = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PFAl = (prod.PFAl != null) ? prod.PFAl : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PFAl} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalKiosko = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PFFKi = (prod.PFFKi != null) ? prod.PFFKi : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PFFKi} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioSalon = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PFSal = (prod.PFSal != null) ? prod.PFSal : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PFSal} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioEcom = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PFEcom = (prod.PFEcom != null) ? prod.PFEcom : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PFEcom} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalAutoservicioNeto = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PNAut = (prod.PNAut != null) ? prod.PNAut : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PNAut} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalEspecialNeto = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PNEsp = (prod.PNEsp != null) ? prod.PNEsp : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PNEsp} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalAlamceneNeto = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PNAlm = (prod.PNAlm != null) ? prod.PNAlm : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PNAlm} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioFinalKioskoNeto = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PNKio = (prod.PNKio != null) ? prod.PNKio : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PNKio} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioSalonNeto = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PNSal = (prod.PNSal != null) ? prod.PNSal : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PNSal} ";
        //            }

        //            sql += @"
        //    END,
        //    PrecioUnitarioEcomNeto = CASE ProductosID ";

        //            foreach (var prod in lsProd)
        //            {
        //                decimal PNEcom = (prod.PNEcom != null) ? prod.PNEcom : 0;  // Si es null, asigna 0
        //                sql += $"WHEN {prod.ProdID} THEN {PNEcom} ";
        //            }

        //            sql += @"
        //    END
        //    WHERE ProductosID IN (";

        //            // Agregar los ProductosID en la cláusula IN
        //            foreach (var prod in lsProd)
        //            {
        //                sql += $"{prod.ProdID}, ";
        //            }

        //            // Elimina la última coma y cierra la cláusula IN
        //            sql = sql.TrimEnd(',', ' ') + ");";

        //            // Ejecutar la consulta SQL
        //            cantUpdate = await _dbConnection.ExecuteAsync(sql);
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        await App.Current.MainPage.DisplayAlert("Atencion", "FUNCION ACTUALIZAR PRECIO POR PRODUCTOS " + ex.Message +"/ ", "OK");
        //        return -1;

        //    }

        //    // Retornar la cantidad de registros actualizados
        //    return cantUpdate;
        //}



        public Task<int> UpdateStockEnProductoSegunPlan(IEnumerable<ProductoStockItem> lsProd)
        {
            // Ejecutar la actualización en un hilo de segundo plano
            return Task.Run(async () =>
            {
                // Verifica si la conexión a la base de datos está establecida, si no, configúralo
                if (_dbConnection == null)
                {
                    SetUpDb();  // Asumo que SetUpDb establece la conexión a la base de datos
                }

                int cantUpdate = 0;

                // Recorre la lista de productos recibida
                foreach (var prod in lsProd)
                {
                    try
                    {
                        // Asegúrate de que pid y stk tengan los valores correctos, y que pid no sea 0
                        if (prod.id != 0)
                        {
                            var sql = $"UPDATE  {nameof(ProductoListaItem)}  SET StockEnUnidades ='" + prod.stk + "' where ProductosID=" + prod.id;
                            var result = await _dbConnection.ExecuteAsync(sql);
                            cantUpdate += result;  // Suma el número de registros actualizados
                        }
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }

                // Retorna la cantidad de registros actualizados
                return cantUpdate;
            });
        }


        ////test
        //public int LlenarCantidadEnCarrito2(int clienteID,out List<ProductoListaItem> lsProductos, out List<ProductoListaItem> lsProductosCombos,decimal Total)
        //{
        //    PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
        //    var lsProductosEnCarritoTT = await oPedidosDetalleItemService.GetByClienteIdSinTx(clienteID);
        //    var lsProductosEnCarritoTTSoloProductos = lsProductosEnCarritoTT.ToList().Where(c => c.CombosID == 0);
        //    var lsProctosIDEnCarrito = from p in lsProductosEnCarritoTTSoloProductos select p.ProductoID;

        //    lsProductos.ToList().ForEach(c => c.CantidadEnCarrito = 0);
        //    lsProductosCombos = lsProductos;

        //    foreach (var item in lsProductos.Where(c => c.ComboID == 0).Where(o => lsProctosIDEnCarrito.Contains(o.ProductosID)))
        //    {

        //        var lsCant = lsProductosEnCarritoTTSoloProductos.Where(c => c.ProductoID == item.ProductosID).ToList();


        //        if (lsCant.Any())

        //        {
        //            item.CantidadEnCarrito = lsCant[0].Cantidad;
        //            item.ListaDePreciosUsada = lsCant[0].ListadePrecioID;

        //        }





        //    }
        //    return 1;        }




        public async Task<List<ProductoListaItem>> LlenarCantidadEnCarrito(int clienteID, List<ProductoListaItem> lsProdin)
        {

            List<ProductoListaItem> cartview = new List<ProductoListaItem>();

            //ProductoListaService oProductoListaService = new ProductoListaService();
            //lsProductosTT = await oProductoListaService.GetAll();



            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            var lsProductosEnCarritoTT = await oPedidosDetalleItemService.GetByClienteIdSinTx(clienteID);
            var lsProductosEnCarritoTTSoloProductos = lsProductosEnCarritoTT.ToList().Where(c => c.CombosID == 0);
            var lsProctosIDEnCarrito = from p in lsProductosEnCarritoTTSoloProductos select p.ProductoID;

            lsProdin.ToList().ForEach(c => c.CantidadEnCarrito = 0);


            foreach (var item in lsProdin.Where(c => c.ComboID == 0).Where(o => lsProctosIDEnCarrito.Contains(o.ProductosID)))
            {

                var lsCant = lsProductosEnCarritoTTSoloProductos.Where(c => c.ProductoID == item.ProductosID).ToList();


                if (lsCant.Any())

                {
                    item.CantidadEnCarrito = lsCant[0].Cantidad;
                    item.ListaDePreciosUsada = lsCant[0].ListadePrecioID;
                    item.CF = lsCant[0].CF;

                    // TotalLinea no venía calculado acá — el carrito mostraba $0 en cada
                    // producto porque nadie más lo asigna antes de sumarlo/renderizarlo.
                    int unidades = item.UnidadesPorBulto > 0 ? item.UnidadesPorBulto : 1;
                    item.TotalLinea = item.CantidadEnCarrito * item.PrecioUnitarioFinal(item.ListaDePreciosUsada) * unidades;
                    item.TotalLineaNeto = item.CantidadEnCarrito * item.PrecioUnitarioNeto(item.ListaDePreciosUsada) * unidades;
                }





            }
            cartview = lsProdin.Where(w => w.CantidadEnCarrito != 0).ToList();
            return cartview;
        }

        public async Task<List<ProductoListaItem>> LlenarCantidadEnCarritoCombos(int clienteID)
        {

            CombosItemService combosItemService = new CombosItemService();
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            List<ProductoListaItem> lsProductosCombos = new List<ProductoListaItem>();






            var lsEnCarrito = await oPedidosDetalleItemService.GetByClienteIdSinTxCombos(clienteID);
            var lstEnCarrito = lsEnCarrito.ToList();


            foreach (var item in lstEnCarrito)
            {


                var prodLisIt = await GetByID(item.ProductoID);
                prodLisIt.CantidadEnCarrito = item.Cantidad;
                prodLisIt.PrecioUnitarioFinalAlamcene = (decimal)item.PrecioUnitarioFinal;
                prodLisIt.PrecioUnitarioFinalAutoservicio = (decimal)item.PrecioUnitarioFinal;
                prodLisIt.PrecioUnitarioFinalEspecial = (decimal)item.PrecioUnitarioFinal;
                prodLisIt.PrecioUnitarioFinalKiosko = (decimal)item.PrecioUnitarioFinal;
                prodLisIt.PrecioUnitarioSalon= (decimal)item.PrecioUnitarioFinal;
                prodLisIt.PrecioUnitarioEcom = (decimal)item.PrecioUnitarioFinal;
                prodLisIt.ComboID = item.CombosID;
                prodLisIt.NroDeGrupoCombo = item.GrupoDinamico;
                prodLisIt.ListaDePreciosUsada = item.ListadePrecioID;
                prodLisIt.NombreDelCombo = await combosItemService.GetNombreCombo(item.CombosID);
                prodLisIt.CF = item.CF;
                prodLisIt.ComboCodigo = item.ComboCodigo;

                // Mismo problema que en LlenarCantidadEnCarrito: sin esto, TotalLinea queda
                // en 0 y el carrito no muestra el precio de los productos del combo (los que
                // sí tienen cargo — item.PrecioUnitarioFinal ya viene en 0 para los gratis,
                // así que el resultado sigue siendo correcto en ese caso).
                int unidadesCombo = prodLisIt.UnidadesPorBulto > 0 ? prodLisIt.UnidadesPorBulto : 1;
                prodLisIt.TotalLinea = prodLisIt.CantidadEnCarrito * (decimal)item.PrecioUnitarioFinal * unidadesCombo;

                lsProductosCombos.Add(prodLisIt);



            }


            return lsProductosCombos;
        }



        //Task<ProductoListaItem> IProductoListaItem.GetByID(int id)
        //{
        //    throw new NotImplementedException();
        //}

        public Task<int> Add(ProductoItemJson item)
        {
            throw new NotImplementedException();
        }





        Task<List<ProductoListaItem>> IProductoListaItem.GetFiltro(string filtro)
        {
            throw new NotImplementedException();
        }

        public async Task<decimal> TotalEnCarrito(List<ProductoListaItem> listaProductos)
        {
            return listaProductos.Sum(c => c.TotalLinea);
        }
        public async Task<decimal> TotalEnCarritoCombos(List<ProductoListaItem> listaProductos)
        {

            return listaProductos.Sum(c => c.TotalLinea);
        }


        public async Task<ResultadoValidacioCombo> GetValidarCombo(int comboID, List<ProductoListaItem> listaProd, List<ProductoListaItem> lsCombos, int clienteID)
        {
            ResultadoValidacioCombo resultado = new ResultadoValidacioCombo(true, "", 0, 1);
            CombosItemService oCombosItemService = new CombosItemService();
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();

            var arrastrexCombo = await oCombosItemService.GetArrastreCombo(comboID);
            var pedabierto = await oPedidosDetalleItemService.GetByClienteIdSinTxCombos(clienteID);//.Where(c=> c.CombosID=itCombo.Key);

            var lsCantCombos = pedabierto.Where(c => c.CombosID == comboID).ToList();
            if (lsCantCombos.Any())
            {
                var cantCombos = lsCantCombos.FirstOrDefault().Cantdecombos;
                decimal totArrasteNecesario = arrastrexCombo * cantCombos;
                decimal vtaEnCombos = 0;
                if (await oCombosItemService.GetSumarCombosEnArrastre(comboID))
                {
                    vtaEnCombos += lsCombos.Where(c => c.ComboID != comboID).Sum(p => p.TotalLinea);
                }
                decimal arrastreProductos = listaProd.Sum(c => c.TotalLinea);

                decimal faltaArrastre = totArrasteNecesario - arrastreProductos - vtaEnCombos;
                var resultadoArrastre = faltaArrastre < 1;
                if (!resultadoArrastre)
                {
                    string mensajeDeLaValidacion = lsCantCombos.FirstOrDefault().CodigoProducto + "- Falta Arrastre Necesario " + totArrasteNecesario.ToString("N0") + " , Faltante:" + faltaArrastre.ToString() + "\n";
                    resultado.Resultado = false;
                    resultado.Mensaje = mensajeDeLaValidacion;
                    resultado.Valor = faltaArrastre;
                    resultado.TipoDeValidacionFail = 1;
                }
                else
                {
                    //     string mensajeDeLaValidacion = lsCantCombos.FirstOrDefault().CodigoProducto + "- Falta Arrastre Necesario " + totArrasteNecesario.ToString("N0") + " , Faltante:" + faltaArrastre.ToString() + "\n";
                    resultado.Resultado = true;
                    resultado.Mensaje = "";
                    resultado.Valor = 0;
                    resultado.TipoDeValidacionFail = 0;
                }
            }
            if (resultado.Resultado)
            {
                // Mas Validaciones
            }

            return resultado;
        }

        public async Task<decimal> EstadoArrastreParaCombos(List<ProductoListaItem> listaProd, List<ProductoListaItem> lsCombos, int clienteID)
        {
            var totProd = await TotalEnCarrito(listaProd);
            decimal totArrasteNecesario = 0;
            decimal faltaArrastre = 0;
            CombosItemService oCombosItemService = new CombosItemService();
            PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();
            PedidoCabeceraService oPedidoCabeceraService = new PedidoCabeceraService();
            var lsCombosAgripado = lsCombos.GroupBy(c => c.ComboID);
            var pedabierto = await oPedidosDetalleItemService.GetByClienteIdSinTxCombos(clienteID);//.Where(c=> c.CombosID=itCombo.Key);

            if (pedabierto.Any())
            {
                decimal vtaEnCombos = 0;
                foreach (var itCombo in lsCombosAgripado.ToList())
                {

                    //var comboHead = await oCombosItemService.GetCombosHeadItem(itCombo.Key);//.Result.FirstOrDefault().ImporteProductosFueraDeCombo;
                    var arrastrexCombo = await oCombosItemService.GetArrastreCombo(itCombo.Key);
                    var lsCantCombos = pedabierto.Where(c => c.CombosID == itCombo.Key).ToList();
                    if (lsCantCombos.Any())
                    {
                        var cantCombos = lsCantCombos.FirstOrDefault().Cantdecombos;
                        totArrasteNecesario += arrastrexCombo * cantCombos;
                        if (await oCombosItemService.GetSumarCombosEnArrastre(itCombo.Key))
                        {
                            vtaEnCombos += lsCombos.Where(c => c.ComboID != itCombo.Key).Sum(p => p.TotalLinea);
                        }

                    }
                }
                decimal arrastreProductos = listaProd.Sum(c => c.TotalLinea);

                faltaArrastre = totArrasteNecesario - arrastreProductos;

                await oPedidoCabeceraService.GuardarEstadoArrastre(pedabierto[0].PedidoCabeceraID, faltaArrastre);
            }
            return faltaArrastre;
        }



        //public async Task<ResultadoValidacioCombo> GetValidarComboalConfirmar(int pedidoCabeceraID)
        //{
        //    ResultadoValidacioCombo resultado = new ResultadoValidacioCombo(true, "", 0, 1);
        //    CombosItemService oCombosItemService = new CombosItemService();
        //    PedidosDetalleItemService oPedidosDetalleItemService = new PedidosDetalleItemService();

        //    var arrastrexCombo = await oCombosItemService.GetArrastreCombo(comboID);
        //    var pedabierto = await oPedidosDetalleItemService.GetByClienteIdSinTxCombos(clienteID);//.Where(c=> c.CombosID=itCombo.Key);
        //                                                                                           //  mensajeDeLaValidacion = "";



        //    ////
        //    var lsCantCombos = pedabierto.Where(c => c.CombosID == comboID).ToList();
        //    if (lsCantCombos.Any())
        //    {
        //        var cantCombos = lsCantCombos.FirstOrDefault().Cantdecombos;
        //        decimal totArrasteNecesario = arrastrexCombo * cantCombos;
        //        decimal vtaEnCombos = 0;
        //        if (await oCombosItemService.GetSumarCombosEnArrastre(comboID))
        //        {
        //            vtaEnCombos += lsCombos.Where(c => c.ComboID != comboID).Sum(p => p.TotalLinea);
        //        }
        //        decimal arrastreProductos = listaProd.Sum(c => c.TotalLinea);

        //        decimal faltaArrastre = totArrasteNecesario - arrastreProductos - vtaEnCombos;
        //        var resultadoArrastre = faltaArrastre < 0;
        //        string mensajeDeLaValidacion = "Falta Arrastre Necesario " + totArrasteNecesario.ToString("N0") + " , Faltante:" + faltaArrastre.ToString();
        //        resultado.Resultado = false;
        //        resultado.Mensaje = mensajeDeLaValidacion;
        //        resultado.Valor = faltaArrastre;
        //        resultado.TipoDeValidacionFail = 1;
        //    }
        //    if (resultado.Resultado)
        //    {
        //        // Mas Validaciones
        //    }

        //    return resultado;
        //}



        public async Task<List<SubCategoriasApiEcomItem>> GetSubCatgorias(IJSRuntime JS)
        {
            try
            {
                LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);
                return JsonConvert.DeserializeObject<List<SubCategoriasApiEcomItem>>(await LocalStorage.GetValueAsync<string>("lsSubCategorias"));
            }
            catch
            {
                return new List<SubCategoriasApiEcomItem> { };
            }

        }

        public async Task<List<ProductoListaItem>> GetLoMasPedidoService(IJSRuntime JS, ProductoListaItems lsProd, int color)
        {
            try
            {
                Lomasvendido oLomasvendido = new Lomasvendido();
                var lsLomasVendido = await oLomasvendido.GetLoMasVendidso(JS);

                // LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);
                //  var lsLomasVendido = (JsonConvert.DeserializeObject<List<LomasvendidoItem>>(await LocalStorage.GetValueAsync<string>("lsLoMasPedido")));

                //        var lsLomasVendidoIDs = lsLomasVendido.Select(x => x.productosID).ToArray();

                //  var LsLoMasPedidos = lsProd.Where(c => lsLoMasPedidosIDs.Contains(c.ProductosID)).ToList();

                return lsProd.GetLoMasVendidos(lsLomasVendido, color);

                //if (Color > 0)
                //{
                //    var LsLoMasPedidos = lsProd.Where(id => lsLomasVendido.Any(p => p.productosID == id.ProductosID  ) && id.Color == Color).ToList();
                //    var LsLoMasPedidos =
                //    return LsLoMasPedidos;
                //}
                //else
                //{
                //    var LsLoMasPedidos = lsProd.Where(id => lsLomasVendido.Any(p => p.productosID == id.ProductosID || p.productosID == id.ProductosIDPadre )).ToList();

                //    return LsLoMasPedidos;



                //var LsLoMasPedidos = lsProd.Where(c => lsLomasVendido.Contains(c.ProductosID))
                //           .OrderBy(c => Array.IndexOf(lsLomasVendido, c.ProductosID)).ToList();
                //return LsLoMasPedidos;


                // }




            }
            catch
            {
                return new List<ProductoListaItem> { };
            }

        }
        public async Task<List<ProductoListaItem>> GetNuevosIngresosService(IJSRuntime JS, List<ProductoListaItem> lsProdN, int Color)
        {
            try
            {
                LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);
                var json = await LocalStorage.GetValueAsync<string>("lsNuevosIngresos");
                if (string.IsNullOrWhiteSpace(json))
                    return new List<ProductoListaItem> { };

                var LsNuevosIngresosIDs = JsonConvert.DeserializeObject<List<int>>(json);

                if (LsNuevosIngresosIDs == null || !LsNuevosIngresosIDs.Any())
                    return new List<ProductoListaItem> { };

                var idsSet = new HashSet<int>(LsNuevosIngresosIDs);

                if (Color > 0)
                {
                    var LsNuevosIngresos = lsProdN
                        .Where(c => (idsSet.Contains(c.ProductosIDPadre) || idsSet.Contains(c.ProductosID)) && c.Color == Color)
                        .ToList();
                    return LsNuevosIngresos;
                }
                else
                {
                    var LsNuevosIngresos = lsProdN
                        .Where(c => idsSet.Contains(c.ProductosIDPadre) || idsSet.Contains(c.ProductosID))
                        .ToList();
                    return LsNuevosIngresos;
                }
            }
            catch
            {
                return new List<ProductoListaItem> { };
            }

        }




        //public async Task<int> SincronizarProductosSubCategorias(List<ProductosSubCategoriasItem> lsSucatprod)
        //    {
        //        int resBorr = 0;
        //        if (_dbConnection == null)
        //        {
        //            SetUpDb();
        //        }
        //        try
        //        {
        //            resBorr = await BorrarTodo();
        //        }
        //        catch { }





        //        return 1;
        //    }

        
        


    }


    public class Lomasvendido
    {

        public async Task<List<LomasvendidoItem>> GetLoMasVendidso(IJSRuntime JS)
        {
            try
            {
                LocalStorageAccessor LocalStorage = new LocalStorageAccessor(JS);
                return JsonConvert.DeserializeObject<List<LomasvendidoItem>>(await LocalStorage.GetValueAsync<string>("lsLoMasPedido"));
            }
            catch
            {
                return new List<LomasvendidoItem> { };
            }

        }

    }


    


}

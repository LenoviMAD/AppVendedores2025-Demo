using EntidadesAppVendedores;
using Microsoft.Maui.ApplicationModel.DataTransfer;

using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static MudBlazor.CategoryTypes;

namespace AppVendedores2025.Services
{
    public class CombosItemService : ICombosItemService
    {

        private SQLiteAsyncConnection _dbConnection;
        private SQLiteConnection _sincronicoDbConnection;  // Conexión sincrónica adicional

        public CombosItemService()
        {
            SetUpDb();
        }
        private void SetUpDb()
        {

            if (_dbConnection == null)
            {
                _dbConnection =  DABSqlLite.SetUpDb();
            }
            // Configurar la conexión sincrónica si no está configurada
            if (_sincronicoDbConnection == null)
            {
                _sincronicoDbConnection = DABSqlLite.GetSQLiteConnectionSinc();  // Aquí usamos una función para la configuración síncrona
            }

        }
        public async Task<int> Add(CombosHeadItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }
        public async Task<int> AddCombosHeadItems(List<CombosHeadItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAllAsync(items);
        }
        public async Task<int> AddProductosItem(CombosProductosItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }
        public async Task<int> AddProductosItems(List<CombosProductosItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAllAsync(items);
        }

        public Task<int> Delete(CombosHeadItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<List<CombosHeadItem>> GetAll()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            //		var res = await _dbConnection.Table<ClienteItem>().ToListAsync(); ;
            return await _dbConnection.Table<CombosHeadItem>().ToListAsync();
        }

        public async Task< decimal> GetArrastreCombo(int combosID)
        { 
          
                var comboHead = await GetCombosHeadItem(combosID);//.Result.FirstOrDefault().ImporteProductosFueraDeCombo;
            if (comboHead.Any())
            {
                var arrastrexCombo = comboHead[0].ImporteProductosFueraDeCombo;
                return arrastrexCombo;
            }
            return 0;
            
        }
        public async Task<bool> GetSumarCombosEnArrastre(int combosID)
        {

            var comboHead = await GetCombosHeadItem(combosID);//.Result.FirstOrDefault().ImporteProductosFueraDeCombo;
            if (comboHead.Any())
            {
                var arrastrexCombo = comboHead[0].SumarCombosEnArrastre==1;
                return arrastrexCombo;
            }
            return false;

        }
        public async Task<List<CombosHeadItem>> GetCombosHeadItem(int combosID)
        {

            string sp;
            if (combosID>0)
                sp = $"Select * From {nameof(CombosHeadItem)} where CombosID={combosID}   ";
            else
                sp = $"Select * From {nameof(CombosHeadItem)} ";


            var resultado = await _dbConnection.QueryAsync<CombosHeadItem>(sp);
            return resultado;

     
        } //quiere bija 

        static Dictionary<int, List<CombosProductosItem>> lsComborProductoCahe = new Dictionary<int, List<CombosProductosItem>>();
       
        public async Task<List<CombosProductosItem>> GetProductosByID(int id)
        {

            if (lsComborProductoCahe.ContainsKey(id))
            {
                return lsComborProductoCahe[id];
            }
            string sp = $"Select * From {nameof(CombosProductosItem)} where ComboId={id}";

            ProductoListaService productosService = new ProductoListaService();
            var resultado = await _dbConnection.QueryAsync<CombosProductosItem>(sp);
            foreach (var item in resultado)
            {
                var itp =await productosService.GetByID(item.ComboProductoID);
                item.ProductoListaItem = itp;
            }
            try
            {
                lsComborProductoCahe.Add(id, resultado);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombosItemService] Error cache combo ID={id}: {ex.Message}");
            }
            
            return resultado;

        }


      
        public async Task<List<int>> GetProductosEnCombo()
        {

           
            string sp = $"Select * From {nameof(CombosProductosItem)} ";

            var resultado = await _dbConnection.QueryAsync<CombosProductosItem>(sp);
            List<int> listProductosEnCombo = new List<int>();

            
            foreach (var item in resultado)
            {

                if (!listProductosEnCombo.Contains(item.ComboProductoID))
                {
                    listProductosEnCombo.Add(item.ComboProductoID);
                }
            }
            

            return listProductosEnCombo;





        }

              

        public async Task<List<CombosEnUnProductoItem>> GetCombosDeUnProducto(int productosID)
        {
            

            string sp = $"Select * From {nameof(CombosProductosItem)} and ComboProductoID="+productosID;


            //string sp2 = "SELECT        CombosHeadItem.CombosID, CombosHeadItem.NombreCombo, CombosHeadItem.Codigo FROM   CombosHeadItem INNER JOIN CombosProductosItem ON CombosHeadItem.CombosID = CombosProductosItem.ComboId WHERE        (CombosProductosItem.ComboProductoID  =" + productosID
            //   + "  ) GROUP BY CombosHeadItem.CombosID, CombosHeadItem.NombreCombo, CombosHeadItem.Codigo";


            string sp2 = "SELECT        CombosHeadItem.CombosID, CombosHeadItem.NombreCombo, CombosHeadItem.Codigo , MAX(CombosHeadItem.TotalVenta) AS TotalVenta FROM   CombosHeadItem INNER JOIN CombosProductosItem ON CombosHeadItem.CombosID = CombosProductosItem.ComboId  WHERE        (CombosProductosItem.ComboProductoID  =" + productosID
              + "  )  GROUP BY CombosHeadItem.CombosID, CombosHeadItem.NombreCombo, CombosHeadItem.Codigo  Order By CombosHeadItem.TotalVenta desc ";

            /*
            SELECT CombosHeadItem.CombosID, CombosHeadItem.NombreCombo, CombosHeadItem.Codigo
FROM            CombosHeadItem INNER JOIN
                         CombosProductosItem ON CombosHeadItem.CombosID = CombosProductosItem.ComboId
WHERE(CombosProductosItem.ComboProductoID = 11)
            */
            var ls = await _dbConnection.QueryAsync<CombosEnUnProductoItem>(sp2);

            var lsOrd = ls.OrderByDescending(it => it.TotalVenta).ToList();
            //var resultado = await _dbConnection.QueryAsync<CombosEnUnProductoItem>(sp2);      
            // return resultado.ToList();
            return lsOrd.ToList();
        }
        //TODO: Update de combo no implementado
        public Task<int> Update(CombosHeadItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<int> BorrarTodoHeader()
        {

            var res = await _dbConnection.DeleteAllAsync<CombosHeadItem>();
            return res;
        }
        public async Task<int> BorrarTodoCombosProductosItem()
        {

            var res = await _dbConnection.DeleteAllAsync<CombosProductosItem>();

            return res;
        }
        public async Task<int> Sincronizar(IEnumerable<EntidadesAppVendedores.CombosItemJson> lsCombos)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var resBorrP = await BorrarTodoCombosProductosItem();
            var resBorrH = await BorrarTodoHeader();
            var lsCombosProductosItem = new List<CombosProductosItem>();
            foreach (var i in lsCombos)
            {
                //   CombosHeadItem ih = new CombosHeadItem();
                var itemProdCombo = new CombosProductosItem(i.comb_combosid, i.comb_productosid, i.comb_cantidad, i.comb_cantidad, i.comb_tipo, i.comb_descuento1, i.comb_descuento2, i.comb_dpcID, i.comb_NrGpDin,i.TextoPrecioCalculado1,i.TextoPrecioCalculado2,i.TextoPrecioCalculado3,i.TextoPrecioCalculado4,i.TextoPrecioCalculado5,i.TextoPrecioCalculado6,i.TextoPrecioCalculado7,i.TextoPrecioCalculado8,i.PrecioNoCalculado);
                //new CombosHeadItem(item.comb_combosid,);
                lsCombosProductosItem.Add(itemProdCombo);
            // await AddProductosItem(itemProdCombo);

            }
            await AddProductosItems(lsCombosProductosItem);
            //----  generar y guardar headers

            //    var ls = await GetAll();
            //   return await _dbConnection.Table<CombosHeadItem>().ToListAsync();


            var lsGrp = from p in lsCombos.ToList()
                        group p by p.comb_combosid into g
                        select new
                        {
                            ComboID = g.Key,
                            NombreCombo = g.Max(c => c.comb_nombre),
                            CodigodeCombo = g.Max(c => c.comb_codigo),
                            ProductosID = g.Max(c => c.comb_productosid),
                            CantMax = g.Max(c => c.comb_cantmax),
                            Cantidad = g.Max(c => c.comb_cantidad),
                            Grupo1 = g.Max(c => c.comb_grp1),
                            Grupo2 = g.Max(c => c.comb_grp2),
                            Grupo3 = g.Max(c => c.comb_grp3),
                            Grupo4 = g.Max(c => c.comb_grp4),
                            Grupo5 = g.Max(c => c.comb_grp5),
                            Grupo6 = g.Max(c => c.comb_grp6),
                            Grupo7 = g.Max(c => c.comb_grp7),
                            CantidadDinamica = g.Max(c => c.comb_CantDinam),
                            CantidadSinCargo = g.Max(c => c.comb_CantSCargo),
                            rubro = g.Max(c => c.comb_RubroComb),
                            CamtidadMaxFactura = g.Max(c => c.comb_CantMaxXFact),
                            ComboIntro = g.Max(c => c.comb_Intro),                                           
                            ImporteProductosFueraDeCombo = g.Max(c => c.comb_ImporteProdFComb),
                            TotalVtaParaOrden = g.Max(c => c.TotalVtaParaOrden)


                        };

            ImagenAppItemService oImgService= new ImagenAppItemService();
            ProductoListaService oProductoListaService = new ProductoListaService();

            var lsCombosHeadItem = new List<CombosHeadItem>();
            foreach (var item in lsGrp)
            {
                var coMaestro = await oProductoListaService.GetByIdLista(item.ProductosID);
                              var img = "";
                var imgItem = await oImgService.GetByID(coMaestro.ProductosIDMaestro);
                var prod1 = await oProductoListaService.GetByID(item.ProductosID);
                var Color = prod1.Color;
                if (imgItem != null)
                {
                    img = imgItem.ImagenChica;

                }

                lsCombosHeadItem.Add(new CombosHeadItem(item.ComboID, item.NombreCombo, item.CodigodeCombo, item.CantMax, item.Cantidad, item.Grupo1, item.Grupo2, item.Grupo3, item.Grupo4, item.Grupo5, item.Grupo6, item.Grupo7, item.CantidadDinamica, item.CantidadSinCargo, item.rubro, item.CamtidadMaxFactura, item.ComboIntro, img, item.ProductosID, Color, item.ImporteProductosFueraDeCombo, item.ImporteProductosFueraDeCombo
                    ,item.TotalVtaParaOrden
                    ));


            //    _ = Add(new CombosHeadItem(item.ComboID, item.NombreCombo, item.CodigodeCombo, item.CantMax, item.Cantidad, item.Grupo1, item.Grupo2, item.Grupo3, item.Grupo4, item.Grupo5, item.Grupo6, item.CantidadDinamica, item.CantidadSinCargo, item.rubro, item.CamtidadMaxFactura, item.ComboIntro, img, item.ProductosID,Color, item.ImporteProductosFueraDeCombo,item.ImporteProductosFueraDeCombo));

            }
            await AddCombosHeadItems(lsCombosHeadItem);




            return resBorrP;

        }


        public async Task<int> DeleteCombosProductosItemIDs(IEnumerable<int> item)
        {
            if (item == null)
            {
                return 0; // Si la lista está vacía, no hay nada que borrar
            }
            int totalDeleted = 0;

            try
            {
                string lsCombosID = "";

                //Borrar producto por producto en la lista
                foreach (var comoboID in item)
                {
                    lsCombosID += comoboID + ",";

                }

                if (!string.IsNullOrEmpty(lsCombosID))
                {
                    lsCombosID = "(" + lsCombosID.Substring(0, lsCombosID.Length - 1) + ")";
                    var query = $"DELETE FROM {nameof(CombosProductosItem)} WHERE ComboId in " + lsCombosID;
                    //var query = $"DELETE FROM {nameof(CombosProductosItem)}";
                    var result = await _dbConnection.ExecuteAsync(query);
                    totalDeleted += result;
                }

                return totalDeleted; // Devolver el total de productos eliminados
            }
            catch (Exception ex)
            {

                return 0; // O cualquier valor que decidas para indicar un error
            }
        }


        public async Task<int> DeleteCombosHeadItemIDs(IEnumerable<int> item)
        {
            if (item == null)
            {
                return 0; // Si la lista está vacía, no hay nada que borrar
            }
            int totalDeleted = 0;

            try
            {
                string lsCombosID = "";

                // Borrar producto por producto en la lista
                foreach (var comoboID in item)
                {
                    lsCombosID += comoboID + ",";

                }

                if (lsCombosID.Length > 0)
                {
                    lsCombosID = "(" + lsCombosID.Substring(0, lsCombosID.Length - 1) + ")";
                    //Usar ExecuteAsync para borrar el producto con el ProductoID especificado
                    var query = $"DELETE FROM {nameof(CombosHeadItem)} WHERE CombosID in " + lsCombosID;
                    //var query = $"DELETE FROM {nameof(CombosHeadItem)}";

                    // Ejecutar la consulta DELETE para cada producto
                    var result = await _dbConnection.ExecuteAsync(query);
                    //var result = await _dbConnection.ExecuteAsync(query, new { ProductoID = productoID });

                    // Incrementar el contador si se borró un producto
                    totalDeleted += result; // result es el número de filas afectadas (1 si se borra, 0 si no)


                }
              
                return totalDeleted; // Devolver el total de productos eliminados
            }
            catch (Exception ex)
            {

                return 0; // O cualquier valor que decidas para indicar un error
            }
        }

        public async Task<int> SincronizarActualizados(IEnumerable<EntidadesAppVendedores.CombosItemJson> lsCombos, List<int> combosIdsAct)
       {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var resBorrP = await DeleteCombosProductosItemIDs(combosIdsAct);
            var resBorrH = await DeleteCombosHeadItemIDs(combosIdsAct);
            var lsCombosProductosItem = new List<CombosProductosItem>();
            foreach (var i in lsCombos)
            {
                //   CombosHeadItem ih = new CombosHeadItem();
                var itemProdCombo = new CombosProductosItem(i.comb_combosid, i.comb_productosid, i.comb_cantidad, i.comb_cantidad, i.comb_tipo, i.comb_descuento1, i.comb_descuento2, i.comb_dpcID, i.comb_NrGpDin, i.TextoPrecioCalculado1, i.TextoPrecioCalculado2, i.TextoPrecioCalculado3, i.TextoPrecioCalculado4, i.TextoPrecioCalculado5, i.TextoPrecioCalculado6, i.TextoPrecioCalculado7, i.TextoPrecioCalculado8, i.PrecioNoCalculado);
                //new CombosHeadItem(item.comb_combosid,);
                lsCombosProductosItem.Add(itemProdCombo);
                // await AddProductosItem(itemProdCombo);

            }



            await AddProductosItems(lsCombosProductosItem);
            //----  generar y guardar headers

            //    var ls = await GetAll();
            //   return await _dbConnection.Table<CombosHeadItem>().ToListAsync();


            var lsGrp = from p in lsCombos.ToList()
                        group p by p.comb_combosid into g
                        select new
                        {
                            ComboID = g.Key,
                            NombreCombo = g.Max(c => c.comb_nombre),
                            CodigodeCombo = g.Max(c => c.comb_codigo),
                            ProductosID = g.Max(c => c.comb_productosid),
                            CantMax = g.Max(c => c.comb_cantmax),
                            Cantidad = g.Max(c => c.comb_cantidad),
                            Grupo1 = g.Max(c => c.comb_grp1),
                            Grupo2 = g.Max(c => c.comb_grp2),
                            Grupo3 = g.Max(c => c.comb_grp3),
                            Grupo4 = g.Max(c => c.comb_grp4),
                            Grupo5 = g.Max(c => c.comb_grp5),
                            Grupo6 = g.Max(c => c.comb_grp6),
                            Grupo7 = g.Max(c => c.comb_grp7),

                            CantidadDinamica = g.Max(c => c.comb_CantDinam),
                            CantidadSinCargo = g.Max(c => c.comb_CantSCargo),
                            rubro = g.Max(c => c.comb_RubroComb),
                            CamtidadMaxFactura = g.Max(c => c.comb_CantMaxXFact),
                            ComboIntro = g.Max(c => c.comb_Intro),
                            ImporteProductosFueraDeCombo = g.Max(c => c.comb_ImporteProdFComb),
                            TotalVtaParaOrden = g.Max(c => c.TotalVtaParaOrden)


                        };

            ImagenAppItemService oImgService = new ImagenAppItemService();
            ProductoListaService oProductoListaService = new ProductoListaService();

            var lsCombosHeadItem = new List<CombosHeadItem>();
            foreach (var item in lsGrp)
            {
                var coMaestro = await oProductoListaService.GetByIdLista(item.ProductosID);
                var img = "";
                var imgItem = await oImgService.GetByID(coMaestro.ProductosIDMaestro);
                var prod1 = await oProductoListaService.GetByID(item.ProductosID);
                var Color = prod1.Color;
                if (imgItem != null)
                {
                    img = imgItem.ImagenChica;

                }

                lsCombosHeadItem.Add(new CombosHeadItem(item.ComboID, item.NombreCombo, item.CodigodeCombo, item.CantMax, item.Cantidad, item.Grupo1, item.Grupo2, item.Grupo3, item.Grupo4, item.Grupo5, item.Grupo6, item.Grupo7, item.CantidadDinamica, item.CantidadSinCargo, item.rubro, item.CamtidadMaxFactura, item.ComboIntro, img, item.ProductosID, Color, item.ImporteProductosFueraDeCombo, item.ImporteProductosFueraDeCombo
                    , item.TotalVtaParaOrden
                    ));


                //    _ = Add(new CombosHeadItem(item.ComboID, item.NombreCombo, item.CodigodeCombo, item.CantMax, item.Cantidad, item.Grupo1, item.Grupo2, item.Grupo3, item.Grupo4, item.Grupo5, item.Grupo6, item.CantidadDinamica, item.CantidadSinCargo, item.rubro, item.CamtidadMaxFactura, item.ComboIntro, img, item.ProductosID,Color, item.ImporteProductosFueraDeCombo,item.ImporteProductosFueraDeCombo));

            }
            await AddCombosHeadItems(lsCombosHeadItem);


            return resBorrP;

        }
        public async Task<int>  ActualizarImagensComboPostSincro()
        {
            ProductoListaService oProductoListaService = new ProductoListaService();
            string sp = $"Select * From {nameof(CombosHeadItem)} where ImagenDataBase==''";

            var resultado = await _dbConnection.QueryAsync<CombosHeadItem>(sp);

            foreach(CombosHeadItem item in resultado)
            {
                try
                {
                    var pItem = await oProductoListaService.GetByID(item.ProductoIDdeReferencia);
                    if (pItem.ImagenDataBase     != "")
                    {


                        var sql = $"UPDATE  {nameof(CombosHeadItem)}  SET ImagenDataBase ='" + pItem.ImagenDataBase + "' where CombosID=" + item.CombosID;
                        var resUpdate = await _dbConnection.QueryAsync<CombosHeadItem>(sql);

                    }
                }catch(Exception ex){ System.Diagnostics.Debug.WriteLine($"[CombosItemService] Error actualizando imagen combo {item.CombosID}: {ex.Message}"); }

            }
          

            return 1;
        }


        static Dictionary<int, string> lsNomresComboCache = new Dictionary<int, string>();


        public   async Task<String> GetNombreCombo(int combosID)
        {
            if (lsNomresComboCache.ContainsKey(combosID))

            {
                return lsNomresComboCache[combosID];
            }
            string nombreCombo = "";
            if (combosID > 0)
            {
                var resul = await GetCombosHeadItem(combosID);
                try
                {
                    nombreCombo = resul[0].NombreCombo;

              
                    lsNomresComboCache[combosID] = nombreCombo;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CombosItemService] Error GetNombreCombo({combosID}): {ex.Message}");
                }



            }

            return nombreCombo;
        }
        public Task<List<CombosHeadItem>> GetListaDeCombo(int combosID)
        {
            throw new NotImplementedException();
        }
        public async Task<List<ProductoListaItem>> GetTextoPrecioCalculadoEnCombo(CombosHeadItem comboHead,List< ProductoListaItem> lsProd, int sc)
        {
            try
            {
                foreach (var it in lsProd)
                {
                    string sp = $"Select * From {nameof(CombosProductosItem)} where ComboId={comboHead.CombosID} and ComboProductoID={it.ProductosID} and ComboTipo={sc}";
                    var resultado = await _dbConnection.QueryAsync<CombosProductosItem>(sp);
                    var res1 = resultado.FirstOrDefault();
                    it.TextoPrecioCalculado1 = res1.TextoPrecioCalculado1;
                    it.TextoPrecioCalculado2 = res1.TextoPrecioCalculado2;
                    it.TextoPrecioCalculado3 = res1.TextoPrecioCalculado3;
                    it.TextoPrecioCalculado4 = res1.TextoPrecioCalculado4;
                    it.TextoPrecioCalculado5 = res1.TextoPrecioCalculado5;
                    it.TextoPrecioCalculado6 = res1.TextoPrecioCalculado6;
                    it.TextoPrecioCalculado7 = res1.TextoPrecioCalculado7;
                    it.TextoPrecioCalculado8 = res1.TextoPrecioCalculado8;
                    it.PrecioNoCalculado = res1.PrecioNoCalculado;

                }
            } 
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CombosItemService] Error GetTextoPrecioCalculadoEnCombo ComboID={comboHead.CombosID} sc={sc}: {ex.Message}");
                return lsProd; // Devolver la lista original sin modificaciones en caso de error
            }

           


            //ProductoListaService productosService = new ProductoListaService();
            //var res1 = resultado.FirstOrDefault();
            //var res2 = res1.TextoPrecioCalculado1;

            return lsProd;
        }

        //public async Task<string> ObtenerPrimerProdIDCombo(int comboID)
        public string ObtenerPrimerProdIDCombo(int comboID)
        {
            string ProdIDCombo = "";

            var sql = $"SELECT ComboProductoID FROM {nameof(CombosProductosItem)} WHERE ComboId =" + comboID + " LIMIT 1";
              
            var result = _sincronicoDbConnection.ExecuteScalar<string>(sql);


            var sqlProdIDPadre = $"SELECT ProductosIDPadre FROM {nameof(ProductoListaItem)} WHERE ProductosID =" + result;

            var resultProdIDPadre = _sincronicoDbConnection.ExecuteScalar<string>(sqlProdIDPadre);
    
            
            ProdIDCombo = resultProdIDPadre;

            if (ProdIDCombo == "0")
            {
                ProdIDCombo = result;
            }

            
            return ProdIDCombo;
            
        }

        public void UpdateImageIDWebCombo(string urlWeb, int comboID)
        {

            var sql = $"UPDATE  {nameof(CombosHeadItem)}  SET ImagenDataBase ='" + urlWeb + "' WHERE CombosID =" + comboID;

            var result = _sincronicoDbConnection.Execute(sql);

        }
    }
}
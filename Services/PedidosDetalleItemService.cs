using EntidadesAppVendedores;
using SQLite;

namespace AppVendedores2025.Services
{
    public class PedidosDetalleItemService : IPedidosDetalleItemService
    {

        private SQLiteAsyncConnection _dbConnection;

        public PedidosDetalleItemService(SQLiteAsyncConnection db) => _dbConnection = db;


        public PedidosDetalleItemService()
        {
            SetUpDb();
        }
        private void SetUpDb()
        {


            if (_dbConnection == null)
            {
                _dbConnection =   DABSqlLite.SetUpDb();
            }
        }
        public async Task<int> Add(PedidosDetalleItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }

        public async Task<int> Delete(PedidosDetalleItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            try
            {
                return await _dbConnection.DeleteAsync(item);
            }catch
            {
                return-1;   
            }
        }

        public async Task<List<PedidosDetalleItem>> GetAll()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            //		var res = await _dbConnection.Table<ClienteItem>().ToListAsync(); ;
            return await _dbConnection.Table<PedidosDetalleItem>().ToListAsync();
        }

        public async Task<PedidosDetalleItem> GetByID(int id)
        {

            string sp = $"Select * From {nameof(PedidosDetalleItem)} where prod_id={id}";


            var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);
            return resultado.FirstOrDefault();





        }

        public async Task<int> Update(PedidosDetalleItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            


             string sql = "UPDATE PedidosDetalleItem SET Cantidad =@Cantidad , ListadePrecioID=@ListaDePrecios , PrecioUnitarioFinal=@PrecioUnitarioFinal, CF=@cf  WHERE Id = " + item.Id;
            
            
 





            return await _dbConnection.ExecuteAsync(sql,item.Cantidad,item.ListadePrecioID,item.PrecioUnitarioFinal,item.CF);
        }

        public async Task<int> BorrarTodo()
        {

            var res = await _dbConnection.DeleteAllAsync<PedidosDetalleItem>();

            return res;
        }

        public async Task<int> BorrarByClienteIdSinTx(int PedidoCabeceraID)
        {





            string sp = $"delete  From {nameof(PedidosDetalleItem)}  where PedidoCabeceraID='{PedidoCabeceraID}' ";
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);
            return 1;

        }
        public async Task<int> BorrarCombodelCArritoxClienteIDsinTX(int clienteID, int comboid, int pedidoCabeceraID)
        {
            string sp = $"Delete From {nameof(PedidosDetalleItem)} where CombosID = {comboid} ";
            sp += $"and PedidosDetalleItem.PedidoCabeceraID in (Select PedidoCabeceraID from PedidoCabeceraItem ";
            sp += $"where PedidoCabeceraItem.PedidoCabeceraID = PedidosDetalleItem.PedidoCabeceraID   and ClienteID = {clienteID}  and PedidosDetalleItem.PedidoCabeceraID = {pedidoCabeceraID})";


            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);

         
            return 1;

        }


        public async Task<List<PedidosDetalleItem>> GetByClienteIdSinTxCombos(int clientesID)
        {

            //    string sp = $"Select * From {nameof(PedidosDetalleItem)} where PedidoCabeceraID={clientesID}";
            string sp = $"Select * From {nameof(PedidoCabeceraItem)}   INNER JOIN {nameof(PedidosDetalleItem)} ";
            sp += $"ON {nameof(PedidoCabeceraItem)}.PedidoCabeceraID ={nameof(PedidosDetalleItem)}.PedidoCabeceraID  ";
            sp += $" where ClienteID={clientesID} and {nameof(PedidoCabeceraItem)}.ResultadoTx not like 'OK%' and CombosID<>0";
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);

         //   var resEnCombo = resultado.Where(c => c.CombosID != 0).ToList();
            return resultado;


        }

        
  public async Task<List<PedidosDetalleItem>> GetPedidosDetallexCliente(int clientesID)
        {
         //   var lsPedido = new List<PedidosDetalleItem>();
            var ls1= await GetByClienteIdSinTx( clientesID);
            //   ImagenAppItemService oImagenAppItemService = new ImagenAppItemService();
            var lsc =await GetByClienteIdSinTxCombos(clientesID);
            ls1.AddRange(lsc);

               ProductoListaService oProductoListaService= new ProductoListaService    ();



            foreach (var item in ls1)
            {
               item.ImagenWeb=(await oProductoListaService.GetByID(item.ProductoID)).ImagenWeb;
              //  item.Total = item.Cantidad * item.PrecioUnitarioFinal * item.UnidadesPorBulto;
            }
          






            return ls1;
        }
        public async Task<List<PedidosDetalleItem>> GetByClienteIdSinTx(int clientesID)
        {
         
            //    string sp = $"Select * From {nameof(PedidosDetalleItem)} where PedidoCabeceraID={clientesID}";
            string sp = $"Select * From {nameof(PedidoCabeceraItem)}   INNER JOIN {nameof(PedidosDetalleItem)} ";
            sp += $"ON {nameof(PedidoCabeceraItem)}.PedidoCabeceraID ={nameof(PedidosDetalleItem)}.PedidoCabeceraID  ";
            sp += $" where ClienteID={clientesID} and {nameof(PedidoCabeceraItem)}.ResultadoTx not like 'OK%'";
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);

            var resSinCombo = resultado.Where(c => c.CombosID == 0).ToList();
            return resSinCombo;

        }



        public async Task<List<PedidosDetalleItem>> GetByPedidoCabezaraID(int pedidoCabezaraID)
        {

            string sp = $"Select * From {nameof(PedidosDetalleItem)} where PedidoCabeceraID={pedidoCabezaraID}";


            var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);
            return resultado;


        }

        public async Task<PedidosDetalleItem> GetByPedidoCabezaraID_ProdID(int pedidoCabezaraID, int productoID, int comboID, bool traerSoloSinCargo)
        {
            string sp;

            if (comboID == 0)

            {
                sp = $"Select * From {nameof(PedidosDetalleItem)} where PedidoCabeceraID={pedidoCabezaraID} and ProductoID={productoID} and CombosID= 0";

            }
            else
            {

                if (traerSoloSinCargo)
                {
                    sp = $"Select * From {nameof(PedidosDetalleItem)} where PedidoCabeceraID={pedidoCabezaraID} and ProductoID={productoID} and CombosID= {comboID} and PrecioUnitarioFinal=0";
                }
                else
                {
                    sp = $"Select * From {nameof(PedidosDetalleItem)} where PedidoCabeceraID={pedidoCabezaraID} and ProductoID={productoID} and CombosID= {comboID} and PrecioUnitarioFinal>0 ";

                }
            }

                var resultado = await _dbConnection.QueryAsync<PedidosDetalleItem>(sp);

                return resultado.FirstOrDefault();


            }

        }
    }
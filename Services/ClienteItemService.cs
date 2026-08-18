
//using Microsoft.Maui.ApplicationModel.DataTransfer;
using EntidadesAppVendedores;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class ClienteItemService : IClienteItemService
    {

        private SQLiteAsyncConnection _dbConnection;

        public ClienteItemService()
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
        public async Task<int> Add(ClienteItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }

        public Task<int> Delete(ClienteItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ClienteItem>> GetAll()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            //		var res = await _dbConnection.Table<ClienteItem>().ToListAsync(); ;
            return await _dbConnection.Table<ClienteItem>().ToListAsync();
        }

        public async Task<ClienteItem> GetByID(int id)
        {

            string sp = $"Select * From {nameof(ClienteItem)} where cli_clientesid={id}";


            var resultado = await _dbConnection.QueryAsync<ClienteItem>(sp);
            return resultado.FirstOrDefault();

        }

        public async Task<string> GetCUITByID(int id)
        {
            // Ejecutar la consulta sin parámetros y obtener el resultado
            string sp = $"SELECT cli_CUIT FROM {nameof(ClienteItem)} WHERE cli_clientesid = {id}";

            // Ejecutar la consulta
            var resultado = await _dbConnection.QueryAsync<ClienteItem>(sp);

            // Si no se encuentra ningún resultado, devolver null o string.Empty
            if (resultado == null || !resultado.Any())
            {
                return null;  // o string.Empty si prefieres devolver una cadena vacía en lugar de null
            }

            // Obtener el primer resultado (si existe)
            var cliente = resultado.FirstOrDefault();

            // Retornar el CUIT obtenido
            return cliente?.cli_CUIT;
        }

        public Task<int> Update(ClienteItem item)
        {
            throw new NotImplementedException();
        }
        //No hay que usarlo
        public async Task UpdateVisitadoV2(int id)
        {
            //return;
            if (_dbConnection == null)
            {
                SetUpDb();
            }
      


            var sql = $"UPDATE  {nameof(ClienteItem)}  SET Visistado =1 where cli_clientesid=" + id;
            //  var prodImagen = await oImagenService.GetByID(producto.ProductosID);
            var result = await _dbConnection.ExecuteAsync(sql);


        }
        public async Task<int> BorrarTodo()
        {

            var res = await _dbConnection.DeleteAllAsync<ClienteItem>();

            return res;
        }

        public async Task<int> Sincronizar(IEnumerable<EntidadesAppVendedores.ClienteItem> lsClientes)
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

        public async Task<int> SincronizarMasmeGusta(IEnumerable<LoQueMasTeGustaItem> lsLoQueMasTeGustaItem)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var resBorr = await BorrarTodoLoQueMasTeGustaItem();

        //    foreach (var item in lsLoQueMasTeGustaItem)
            {
                await AddLoQueMasTeGustaItems(lsLoQueMasTeGustaItem.ToList());


            }


            return resBorr;

        }
        public async Task<int> BorrarTodoLoQueMasTeGustaItem()
        {

            var res = await _dbConnection.DeleteAllAsync<LoQueMasTeGustaItem>();

            return res;
        }
        public async Task<int> AddLoQueMasTeGustaItems(List<LoQueMasTeGustaItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAllAsync(items);
        }

        public async Task<List<LoQueMasTeGustaItem>> GetLoQueMasTeGustaByClienteID(int id)
        {

            string sp = $"Select * From {nameof(LoQueMasTeGustaItem)} where ClienteID={id}";


            var resultado = await _dbConnection.QueryAsync<LoQueMasTeGustaItem>(sp);

                // Trae los productos de otros clientes
                sp = $"Select * From {nameof(LoQueMasTeGustaItem)} where ClienteID<>{id}";

                var resultadoOtros = await _dbConnection.QueryAsync<LoQueMasTeGustaItem>(sp);

                foreach (var item in resultadoOtros)
                {
                    var yaExiste = resultado.Where(c => c.ProductoID == item.ProductoID).Any();
                    if (!yaExiste)
                    {
                        resultado.Add(item);
                    }


                }

            

            return resultado;

        }
        public async Task<int> EstadosDePedidosxClientes()
        {
            var lsClientes = await GetAll();




            PedidoCabeceraService oPedidoCabeceraService = new PedidoCabeceraService();
            foreach (var item in lsClientes)
            {
             var ultPedido=await    oPedidoCabeceraService.GetByClienteIDUltimo(item.cli_clientesid);

                if (ultPedido != null )
                {
                    if(ultPedido.ResultadoTx.StartsWith("OK"))
                    {

                        //transmitido
                        return 1;
                    }
                    else
                    {
                        var opeds=new PedidosDetalleItemService();
                        var res = await opeds.GetByPedidoCabezaraID(ultPedido.PedidoCabeceraID);

                        if(res.Any())
                        {
                            //error o no transm
                            return 2;

                        }

                        //error o no transm
                        return 3;
                    }


                } else
                {
                    //no hay pedido
                    return 3;
                }



            }




                return 1;
        }

    }
}
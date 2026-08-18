using EntidadesAppVendedores;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppVendedores2025.Services;

namespace EntidadesGeneraleFrWks
{
    public class ProductosBloqueadosxClienteService
    {
        private SQLiteAsyncConnection _dbConnection;

        public ProductosBloqueadosxClienteService()
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
        public async Task<int> AddProductosbloqueados(List<ProductosBloqueadosxCliente> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            BorrarTodoProductosBloquedos();
            return await _dbConnection.InsertAllAsync(items);
        }
        public async Task<int> BorrarTodoProductosBloquedos()
        {

            var res = await _dbConnection.DeleteAllAsync<ProductosBloqueadosxCliente>();

            return res;
        }
        public async Task<List<ProductosBloqueadosxCliente>> GetProductosBloqueadosxCliente(int clienteID)
        {
            string sp = $"Select * From {nameof(ProductosBloqueadosxCliente)} where ClientesID={clienteID}";

            var resultado = await _dbConnection.QueryAsync<ProductosBloqueadosxCliente>(sp);



            return resultado.ToList();

        }
    }
}

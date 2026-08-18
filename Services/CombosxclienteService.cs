using EntidadesAppVendedores;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    internal class CombosxclienteService : ICombosxclienteService
    {

        private SQLiteAsyncConnection _dbConnection;

        public CombosxclienteService()
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
        public async Task<int> Add(List<CombosXClientesItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAllAsync(items);
        }

        public async Task<int> Sincronizar(IEnumerable<CombosXClientesItem> lsCombosxcli)
        {
           await  BorrarTodor();
    

            await Add(lsCombosxcli.ToList());
            return 1;
        }
        public async Task<int> BorrarTodor()
        {

            var res = await _dbConnection.DeleteAllAsync<CombosXClientesItem>();

            return res;
        }
       public async  Task <List<CombosXClientesItem>> GetCombosxClientexCliID(int clienteID)
        {
            string sp = $"Select * From {nameof(CombosXClientesItem)} where ClientesID={clienteID}";

            var resultado = await _dbConnection.QueryAsync<CombosXClientesItem>(sp);



            return resultado.ToList();

        }


    }
}

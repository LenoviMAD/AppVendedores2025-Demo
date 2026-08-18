using EntidadesAppVendedores;
using SQLite;


namespace AppVendedores2025.Services
{
    public class ProductosSubCategoriasService : IProductosSubCategoriasService
    {

        private SQLiteAsyncConnection _dbConnection;

        public ProductosSubCategoriasService()
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



   //     private SQLiteConnection _dbConnectionSinc;



        public async Task<int> Add(ProductosSubCategoriasItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }
        public async Task<int> AddListaProductosSubCategoriasItem(List<ProductosSubCategoriasItem> items)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAllAsync(items);
        }

        public Task<int> Delete(ProductosSubCategoriasItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<int> BorrarTodo()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var res = await _dbConnection.DeleteAllAsync<ProductosSubCategoriasItem>();

            return res;
        }
        public async Task<int> SincronizarProductosSubCategorias(List<ProductosSubCategoriasItem> lsSucatprod)
        {
            int resBorr = 0;
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            try
            {
                resBorr = await BorrarTodo();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProductosSubCategoriasService] Error borrando: {ex.Message}"); }
            await AddListaProductosSubCategoriasItem(lsSucatprod);
            //foreach (var item in lsSucatprod)
            //{
            //    await Add(item);
            //}



            return 1;
        }

        public async Task<List<ProductosSubCategoriasItem>> GetProductosIDxSubcategoria(int subcat)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }


            string sp = $"Select * From {nameof(ProductosSubCategoriasItem)} where SubCategiruasID={subcat}";


            var resultado = await _dbConnection.QueryAsync<ProductosSubCategoriasItem>(sp);
            return resultado;

        }



    }
}

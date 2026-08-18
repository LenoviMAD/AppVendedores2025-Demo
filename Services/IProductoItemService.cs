using EntidadesAppVendedores;

using Microsoft.JSInterop;

namespace AppVendedores2025.Services
{
    public interface IProductoListaItem
    {
         Task<List<ProductoListaItem>> GetAllFiltradoBloqueados(int clienteID, List<ProductoListaItem> ProductosEnCache);
        Task<List<ProductoListaItem>> GetAll();
        Task<ProductoListaItem> GetByID(int id);
       
        Task<int> Add(ProductoListaItem item);
        Task<int> Update(ProductoListaItem item);
        Task<int> Delete(ProductoListaItem item); 
        Task<int> BorrarTodoProductoListaItem();
        Task<List<ProductoListaItem>> GetFiltro(string filtro);
        Task<List<ProductoListaItem>> GetAllLista();


        Task<List<ProductoListaItem>> LlenarCantidadEnCarrito(int clienteID, List<ProductoListaItem> lsProd);
        Task<List<ProductoListaItem>> LlenarCantidadEnCarritoCombos(int clienteID);
        Task<decimal> EstadoArrastreParaCombos(List<ProductoListaItem> listaProd, List<ProductoListaItem> lsCombos, int clienteID);
        Task<decimal> TotalEnCarrito(List<ProductoListaItem>  listaProductos);
        Task<decimal> TotalEnCarritoCombos(List<ProductoListaItem> listaProductos);
        Task<List<CategoriaItem>> GetCatgorias(IJSRuntime JS);
		Task<List<EntidadesAppVendedores.SubCategoriasApiEcomItem>> GetSubCatgorias(IJSRuntime JS);
        Task<List<ProductoListaItem>> GetLoMasPedidoService(IJSRuntime JS, ProductoListaItems lsProd, int Color);
        Task<List<ProductoListaItem>> GetNuevosIngresosService(IJSRuntime JS, List<ProductoListaItem> lsProdN, int Color);
        Task<ResultadoValidacioCombo> GetValidarCombo(int comboID, List<ProductoListaItem> listaProd, List<ProductoListaItem> lsCombos, int clienteID);

       


    }
}

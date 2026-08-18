using EntidadesAppVendedores;
namespace AppVendedores2025.Services
{
    public interface ICombosItemService
    {
        Task<List<CombosHeadItem>> GetAll();

        Task<List<CombosHeadItem>> GetListaDeCombo(int combosID);
        Task<List<CombosProductosItem>> GetProductosByID(int id);
       
        Task<List<CombosHeadItem>> GetCombosHeadItem(int combosID);
        Task<int> Add(CombosHeadItem item);
        Task<int> Update(CombosHeadItem item);
        Task<int> Delete(CombosHeadItem item);
        Task<int> BorrarTodoHeader();
        Task<int> BorrarTodoCombosProductosItem();

        Task<String> GetNombreCombo(int combosID);

        Task<List<ProductoListaItem>> GetTextoPrecioCalculadoEnCombo(CombosHeadItem comboHead, List<ProductoListaItem> lsProd, int sc);

        string ObtenerPrimerProdIDCombo(int comboID);
        void UpdateImageIDWebCombo(string urlWeb, int comboID);

        //Task<List<ClienteItem>> GetFiltro(string filtro);
    }
}

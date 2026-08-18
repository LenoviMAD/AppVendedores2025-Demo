using EntidadesAppVendedores;
namespace AppVendedores2025.Services
{
    public interface IPedidoCabeceraService
    {
        Task<List<PedidoCabeceraItem>> GetAll();
        Task<PedidoCabeceraItem> GetByClienteIDsnTX(int id);
        Task<int> Add(PedidoCabeceraItem item);
        Task<int> Update(PedidoCabeceraItem item);
        Task<int> Delete(PedidoCabeceraItem item);
        Task<int> BorrarTodo();
        Task<int> ReabrirPedido(int CabeceraID);
        Task<bool> AgregarPedidosProductos(int clientesID, int productosID, int cantidad,  int listaDePecios, int comboID, decimal precioFinal, decimal precioFinalNeto, int NroDeGrupoCombo,string cliCodigo,string cliDireccion,string nombreProducto,string codigoProducto,int VDID,bool cf, int unidadesPorBulto, string com);
        
        Task<int> BorrarPedidoViejos();
        
    }
}

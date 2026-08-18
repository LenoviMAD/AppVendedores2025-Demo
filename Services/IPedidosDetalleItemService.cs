using EntidadesAppVendedores;
namespace AppVendedores2025.Services
{
    public interface IPedidosDetalleItemService
    {
        Task<List<PedidosDetalleItem>> GetAll();
        Task<PedidosDetalleItem> GetByID(int id);

        Task<List<PedidosDetalleItem>> GetByPedidoCabezaraID(int pedidoCabezaraID);
        Task<List<PedidosDetalleItem>> GetByClienteIdSinTx(int clientesID);
        Task<List<PedidosDetalleItem>> GetByClienteIdSinTxCombos(int clientesID);
        Task<int> Add(PedidosDetalleItem item);
        Task<int> Update(PedidosDetalleItem item);
        Task<int> Delete(PedidosDetalleItem item);
        Task<int> BorrarTodo();

    }
}


using EntidadesAppVendedores;

namespace AppVendedores2025.Services
{
    public interface IClienteItemService
    {
        Task<List<ClienteItem>> GetAll();
        Task<ClienteItem> GetByID(int id);
        Task<int> Add(ClienteItem item);
        Task<int> Update(ClienteItem item);
        Task<int> Delete(ClienteItem item);
        Task<int> BorrarTodo();
        //Task<List<ClienteItem>> GetFiltro(string filtro);
    }
}

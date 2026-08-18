using EntidadesAppVendedores;

namespace AppVendedores2025.Services
{
    public interface IProductosSubCategoriasService
    {

         Task<int> BorrarTodo();

        Task<int> Add(ProductosSubCategoriasItem item);
    }
}
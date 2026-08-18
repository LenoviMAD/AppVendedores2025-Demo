using EntidadesAppVendedores;
namespace AppVendedores2025.Services
{
	public interface IProductoImagenServices
	{
		Task<List<ImagenAppItem>> GetAll();
		Task<ImagenAppItem> GetByID(int id);
		Task<int> Add(ImagenAppItem item);
		Task<int> Update(ImagenAppItem item);
		Task<int> Delete(ImagenAppItem item);
		Task<int> BorrarTodo();
		Task<List<ImagenAppItem>> GetFiltro(string filtro);
	}
}

using EntidadesAppVendedores;


namespace AppVendedores2025.Services
{
    internal interface ICombosxclienteService
    {
        Task<int> Add(List<CombosXClientesItem> items);
        Task<int> Sincronizar(IEnumerable<CombosXClientesItem> lsCombosxcli);
        Task<int> BorrarTodor();

        Task<List<CombosXClientesItem>> GetCombosxClientexCliID(int clienteID);
    }
  
}

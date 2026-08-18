namespace AppVendedores2025.Services
{
    public interface ISecureTokenProvider
    {
        Task<string?> GetTokenAsync(CancellationToken ct = default);
        Task SaveTokenAsync(string token, CancellationToken ct = default);
        Task ClearTokenAsync(CancellationToken ct = default);
    }
}

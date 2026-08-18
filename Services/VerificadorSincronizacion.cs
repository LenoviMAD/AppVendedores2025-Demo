using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using AppVendedores2025.Services;
using Microsoft.JSInterop;

public class VerificadorSincronizacion : IDisposable
{

    private readonly IJSRuntime JS;

    //ParametrosServices oParam = new ParametrosServices();

    private readonly NavigationManager _navegacion;
    private readonly ParametrosServices oParam;
    private bool _suscripto;

    public VerificadorSincronizacion(NavigationManager navegacion, ParametrosServices parametros, IJSRuntime js)
    {
        _navegacion = navegacion ?? throw new ArgumentNullException(nameof(navegacion));
        oParam = parametros ?? throw new ArgumentNullException(nameof(parametros));

        _navegacion.LocationChanged += AlCambiarPagina;
        _suscripto = true;
        JS = js ?? throw new ArgumentNullException(nameof(js));

    }

    private async void AlCambiarPagina(object? sender, LocationChangedEventArgs e)
    {

        //var path = new Uri(e.Location).AbsolutePath.ToLower();

        //if (path == "/" || path.Contains("/solicitudaltaecom"))
        //    return;

        //await VerificarYSincronizarAsync();
    }

    public async Task VerificarYSincronizarAsync(CancellationToken ct = default)
    {
        try
        {
            DateTime hoy = DateTime.Today;
            //DateTime tomorrow = DateTime.Today;
            //var tomaHoy = oParam.FechaUltimaSincronizacion;
            //var vendedorrr = oParam.Vendedor;

            if ((oParam.FechaUltimaSincronizacion == DateTime.MinValue ||
                oParam.FechaUltimaSincronizacion < hoy) && oParam.Vendedor != null && oParam.Vendedor != "")
            {
                try
                {
                    await JS.InvokeVoidAsync("alert", "Día finalizado, sincronice manualmente por favor.");
                    _navegacion.NavigateTo("/");
                }
                catch (OperationCanceledException)
                {
                    throw; // cancelación pedida
                }
                catch (Exception exSync)
                {
                    Console.Error.WriteLine($"Error al sincronizar: {exSync}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error en VerificarYSincronizarAsync: {ex}");
        }
    }

    // Simulación: reemplazar con tu código real
    private Task EjecutarSincronizacionAsync(CancellationToken ct) =>
        Task.Delay(500, ct); // simulación

    // Simulación: reemplazar con tu código real
    private Task GuardarParametrosAsync(ParametrosServices p, CancellationToken ct) =>
        Task.CompletedTask;

    public void Dispose()
    {
        if (_suscripto)
        {
            _navegacion.LocationChanged -= AlCambiarPagina;
            _suscripto = false;
        }
    }
}
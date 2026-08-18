using EntidadesAppVendedores;
//using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.JSInterop;
using SQLite;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AppVendedores2025.Services
{
    public class ImagenAppItemService : IProductoImagenServices
    {
        public bool flagSincroenUsoImagenes = false;

        private SQLiteAsyncConnection _dbConnection;


        public ImagenAppItemService()
        {
            SetUpDb();
        }
        private void SetUpDb()
        {
            if (_dbConnection == null)
            {
                _dbConnection = DABSqlLite.SetUpDb();
            }
        }
        public async Task<int> Add(ImagenAppItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(item);
        }

        public Task<int> Delete(ImagenAppItem item)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ImagenAppItem>> GetAll()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.Table<ImagenAppItem>().ToListAsync();
        }
        public async Task<List<ImagenAppItem>> GetFiltro(string filtro)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            throw new NotImplementedException();

        }
        public async Task<ImagenAppItem> GetByID(int id)
        {

            if (_dbConnection == null)
            {
                SetUpDb();
            }
            string sp = $"Select * From {nameof(ImagenAppItem)} where ProductosID={id}";


            var resultado = await _dbConnection.QueryAsync<ImagenAppItem>(sp);
            return resultado.FirstOrDefault();





        }
        public async Task<int> GetCountImg()
        {
            try
            {
                if (_dbConnection == null)
                {
                    SetUpDb();
                }

                string sql = $"SELECT COUNT(*) FROM {nameof(ImagenAppItem)}";

                // Retorna un entero directamente
                var resultado = await _dbConnection.ExecuteScalarAsync<int>(sql);
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al contar imágenes: {ex.Message}");
                return -1; // o lanzar excepción según tu lógica
            }
        }


        public async Task<int> Update(ImagenAppItem item)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            //	return await _dbConnection.UpdateAsync(item);

            string sql = "UPDATE ImagenAppItem SET ImagenChica ='" + item.ImagenChica + "' ,FechaUltimaAct='" + item.FechaUltimaAct + "'  WHERE ProductosID = " + item.ProductosID;

            return await _dbConnection.ExecuteAsync(sql);


        }



        public async Task<int> BuscarProductosSinImagen(IJSRuntime JS)
        {

            //daniel  revisar
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var sqlSinImg = $"Select * From {nameof(ProductoListaItem)} where ImagenDataBase = ''";
            var resultado = await _dbConnection.QueryAsync<ProductoListaItem>(sqlSinImg);

            int avance = 0;
            int totcount = resultado.Count();
            ImagenSqlItem imaganApi;

            IHttpClientFactory? httpFactory = null;

            try
            {
                httpFactory = App.Current?.Handler?.MauiContext?.Services?.GetService<IHttpClientFactory>();
                if (httpFactory == null)
                    throw new Exception("No se pudo resolver IHttpClientFactory. Verificá AddHttpClient() en MauiProgram.");
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Atención", "Error inicializando HTTP: " + ex.Message, "OK");
                return avance;
            }

            var oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar(httpFactory);

            ProductoListaService oProductosService = new ProductoListaService();
            ImagenAppItemService oImgService = new ImagenAppItemService();
            ParametrosServices oParametrosServices = new ParametrosServices();
            oParametrosServices.MensajeErrores = "";
            foreach (var item in resultado)
            {
                avance++;
                var imgItem = await oImgService.GetByID(item.ProductosIDMaestro);
                if (imgItem == null)
                {
                    imaganApi = await oFuncionesSincronizar.SincronizarImagenFromApi(item.ProductosIDMaestro, new DateTime(2010, 1, 1), JS);
                    try
                    {
                        await Add(new ImagenAppItem(item.ProductosIDMaestro, imaganApi.ImagenChica, DateTime.Now));

                        await oProductosService.UpdateImagenEnProducto(imaganApi.ProductosID, imaganApi.ImagenChica);
                    }
                    catch (Exception ex)
                    {


                        oParametrosServices.MensajeErrores = ex.Message;
                    }
                }
                else {

                    await oProductosService.UpdateImagenEnProducto(item.ProductosID, imgItem.ImagenChica);
                }
                    await JS.InvokeVoidAsync("replaceSyncMessage", "Fotos Faltantes" + avance + " / " + totcount + "\n");


            }
            return avance;

        }


        public async Task<int> BorrarTodo()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var res = await _dbConnection.DeleteAllAsync<ImagenAppItem>();

            return res;
        }

        public async Task<int> ActualizarImagenFromApi(IJSRuntime JS, List<int> lsIMGNuevas)

        {

            ParametrosServices oParam = new ParametrosServices();
            //comente el progreso de la barra para que no vuelva a empezar
            //await JS.InvokeVoidAsync("updateProgressBar", 10);
            int totcount = 0;
            int avance = 0;

            if (!flagSincroenUsoImagenes)
            {
                flagSincroenUsoImagenes = true;

                if (_dbConnection == null)
                {
                    SetUpDb();
                }
                ProductoListaService oProductosService = new ProductoListaService();
                var lsProd = await oProductosService.GetAll();

                lsIMGNuevas = lsIMGNuevas.Where(p => lsProd.Any(c => c.ProductosIDMaestro == p)).ToList();

                totcount = lsIMGNuevas.Count;

                IHttpClientFactory? httpFactory = null;

                try
                {
                    httpFactory = App.Current?.Handler?.MauiContext?.Services?.GetService<IHttpClientFactory>();
                    if (httpFactory == null)
                        throw new Exception("No se pudo resolver IHttpClientFactory. Verificá AddHttpClient() en MauiProgram.");
                }
                catch (Exception ex)
                {
                    await App.Current.MainPage.DisplayAlert("Atención", "Error inicializando HTTP: " + ex.Message, "OK");
                    return avance;
                }

                var oFuncionesSincronizar = new AppVendedores2025.Services.FuncionesSincronizar(httpFactory);


                try
                {
                    foreach (var item in lsIMGNuevas)
                    {
                        if (_dbConnection == null)
                        {
                            SetUpDb();
                        }
                        avance++;
                        ImagenSqlItem imaganApi;
                        imaganApi = await oFuncionesSincronizar.SincronizarImagenFromApi(item, new DateTime(2010, 1, 1), JS);


                        var prodImagen = await GetByID(item);

                        if (prodImagen == null)
                        {
                            try
                            {

                                await Add(new ImagenAppItem(item, imaganApi.ImagenChica, DateTime.Now));

                                await oProductosService.UpdateImagenEnProducto(imaganApi.ProductosID, imaganApi.ImagenChica);

                            }
                            catch (Exception ex) { Debug.WriteLine($"[ImagenAppItemService] Error agregando imagen ProductoID={item}: {ex.Message}"); }


                        }
                        else
                        {
                            try
                            {
                                if (imaganApi.ImagenChica != "")
                                {
                                    prodImagen.FechaUltimaAct = DateTime.Now;
                                    prodImagen.ImagenChica = imaganApi.ImagenChica;
                                    await Update(prodImagen);
                                    await oProductosService.UpdateImagenEnProducto(imaganApi.ProductosID, imaganApi.ImagenChica);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[ImagenAppItemService] Error actualizando imagen: {ex.Message}");
                            }


                        }

                        await JS.InvokeVoidAsync("replaceSyncMessage", " Fotos\n" + avance + " / " + totcount);


                    }



                }
                catch
                {
                    await App.Current.MainPage.DisplayAlert("Atencion", "error  GetImagenFromApi  123", "OK");
                    flagSincroenUsoImagenes = false;
                }

                oParam.FechaUltimaSincronizacionImagen = DateTime.Now;


                flagSincroenUsoImagenes = false;
                return 1;//ok

            }
            else
            {
                flagSincroenUsoImagenes = false;
                return 3;//se salteo
            }

        }



        //public async Task<int> Sincronizar(IEnumerable<EntidadesAppVendedores.ImagenAppItem> lsProductos)
        //{
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();
        //    }
        //    var resBorr = await BorrarTodo();

        //    foreach (var item in lsProductos)
        //    {
        //        await Add(item);


        //    }


        //    return resBorr;

        //}
    }
}

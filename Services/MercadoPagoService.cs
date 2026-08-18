using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using AppVendedores2025.Services;
using AppVendedores2025.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Components;
using MercadoPago.Error;
using System.Net.Http;


namespace AppVendedores2025.Services;

public class MercadoPagoService
{


    public async Task<string> Pago(double montoApagarMP, string KeyPagoID)
    {
        if (Parametros.ModoDemo)
        {
            // Modo demo: no se procesan pagos reales contra MercadoPago
            return $"https://empresademo.example/pago-demo/{KeyPagoID}";
        }

        ParametrosServices oParam = new ParametrosServices();
        string urlApi = oParam.urlApi;
        //Borrar para publicar
        //urlApi = "https://localhost:5101/";
        //double montoApagarMP = (double)montoApagar;
        //var accessToken = "TEST-REDACTED-DEMO";
        //var publicKey = "TEST-REDACTED-DEMO";

        //agregado 26-12 parametrizado
        var accessTokenMPdeApi = oParam.accessTokenMP;
        var publicKeyMPdeAPi = oParam.publicKeyMP;

        var accessToken = accessTokenMPdeApi;
        var publicKey = publicKeyMPdeAPi;

        //funciono hasta 26-12, ahora tomamos parametro API
        //var accessToken = "APP_USR-REDACTED-DEMO";
        //var publicKey = "APP_USR-REDACTED-DEMO";

        //(tokens de ejemplo removidos del repo demo)

        //*Mercado pago configuration
        MercadoPagoConfig.AccessToken = accessToken;
        var productDetails = new ProductsMP()
        {
            Id = 1,
            Name = "AppVendedores2025 Demo",
            Unit = 1,
            UnitPrice = montoApagarMP,
            CategoryId = 8,
            //Description = "The new era of the intelligence",
            Description = "AppVendedores2025 Demo ¡Gracias por su compra!",
        };

        //*Crear reques hacia mercado pago
        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Id = productDetails.Id.ToString(),
                        Title = productDetails.Name,
                        CurrencyId = "ARS",
                        //PictureUrl = "https://www.mercadopago.com/org-img/MP3/home/logomp3.gif",
                        PictureUrl = "https://cdn.empresademo.example/images/favicon-96x96.png",
                        Description = productDetails.Description,
                        CategoryId = productDetails.CategoryId.ToString(),
                        Quantity = productDetails.Unit,
                        UnitPrice = Convert.ToDecimal(productDetails.UnitPrice)
                    }
                },
            Payer = new PreferencePayerRequest
            {
                
            },
            BackUrls = new PreferenceBackUrlsRequest
            {
                // es android es 1 si es IOS es != 1
                Success = urlApi + "MercadoPago/" + KeyPagoID + "/1/Success",
                Failure = urlApi + "MercadoPago/" + KeyPagoID + "/Failure",
            },
            AutoReturn = "approved",
            PaymentMethods = new PreferencePaymentMethodsRequest
            {
                ExcludedPaymentMethods = [],
                ExcludedPaymentTypes = [],
                Installments = 1
            },
            StatementDescriptor = "Mercado Pago App Ventas AppVendedores2025 Demo",
            ExternalReference = $"Referencia_{Guid.NewGuid().ToString()}",
            Expires = true,
            ExpirationDateFrom = DateTime.Now,
            ExpirationDateTo = DateTime.Now.AddMinutes(4)
        };
        //var client = new PreferenceClient();
        //Preference preference = await client.CreateAsync(request);
        //return Redirect(preference.SandboxInitPoint);
        //return Redirect(preference.InitPoint);


        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(request);

        // Obtener la URL de pago (SandboxInitPoint o InitPoint según el caso)
        //string urlDePago = preference.SandboxInitPoint;
        string urlDePago = preference.InitPoint;  
        // Abrir la URL de pago en el navegador del dispositivo
        //await Launcher.OpenAsync(urlDePago);  // Esto abrirá la URL en el navegador predeterminado

        return urlDePago;  // También puedes devolver la URL para usarla en el frontend si lo necesitas

    }

    //public async Task<Preference> PagoPreference(double montoApagarMP, string KeyPagoID)
    //{
    //    ParametrosServices oParam = new ParametrosServices();
    //    string urlApi = oParam.urlApi;
    //    //Borrar para publicar
    //    //urlApi = "https://localhost:5101/";


    //    //var accessTokenMPdeApi = await oParam.GetParametroAppSeting("accessTokenMP");
    //    //var publicKeyMPdeAPi = await oParam.GetParametroAppSeting("publicKeyMP");

    //    //agregado 26-12 parametrizado
    //    var accessTokenMPdeApi = oParam.accessTokenMP;
    //    var publicKeyMPdeAPi = oParam.publicKeyMP;

    //    var accessToken = accessTokenMPdeApi;
    //    var publicKey = publicKeyMPdeAPi;


    //    //funciono hasta 26-12, ahora tomamos parametro API
    //    //var accessToken = "APP_USR-REDACTED-DEMO";
    //    //var publicKey = "APP_USR-REDACTED-DEMO";

    //    //(tokens de ejemplo removidos del repo demo)

    //    //*Mercado pago configuration
    //    MercadoPagoConfig.AccessToken = accessToken;
    //    var productDetails = new ProductsMP()
    //    {
    //        Id = 1,
    //        Name = "AppVendedores2025 Demo",
    //        Unit = 1,
    //        UnitPrice = montoApagarMP,
    //        CategoryId = 8,
    //        //Description = "The new era of the intelligence",
    //        Description = "AppVendedores2025 Demo ¡Gracias por su compra!",
    //    };

    //    //*Crear reques hacia mercado pago
    //    var request = new PreferenceRequest
    //    {
    //        Items = new List<PreferenceItemRequest>
    //            {
    //                new PreferenceItemRequest
    //                {
    //                    Id = productDetails.Id.ToString(),
    //                    Title = productDetails.Name,
    //                    CurrencyId = "ARS",
    //                    //PictureUrl = "https://www.mercadopago.com/org-img/MP3/home/logomp3.gif",
    //                    PictureUrl = "https://cdn.empresademo.example/images/favicon-96x96.png",
    //                    Description = productDetails.Description,
    //                    CategoryId = productDetails.CategoryId.ToString(),
    //                    Quantity = productDetails.Unit,
    //                    UnitPrice = Convert.ToDecimal(productDetails.UnitPrice)
    //                }
    //            },
    //        Payer = new PreferencePayerRequest
    //        {

    //        },
    //        BackUrls = new PreferenceBackUrlsRequest
    //        {  

    //            // es android es 1 si es IOS es != 1
    //            Success = urlApi + "MercadoPago/" + KeyPagoID + "/1/Success",
    //            Failure = urlApi + "MercadoPago/" + KeyPagoID + "/Failure",
    //        },
    //        AutoReturn = "approved",
    //        PaymentMethods = new PreferencePaymentMethodsRequest
    //        {
    //            ExcludedPaymentMethods = [],
    //            ExcludedPaymentTypes = [],
    //            Installments = 1
    //        },
    //        StatementDescriptor = "Mercado Pago App Ventas AppVendedores2025 Demo",
    //        ExternalReference = $"Referencia_{Guid.NewGuid().ToString()}",
    //        Expires = true,
    //        ExpirationDateFrom = DateTime.Now,
    //        ExpirationDateTo = DateTime.Now.AddMinutes(4)
    //    };
    //    //var client = new PreferenceClient();
    //    //Preference preference = await client.CreateAsync(request);
    //    //return Redirect(preference.SandboxInitPoint);
    //    //return Redirect(preference.InitPoint);


    //    var client = new PreferenceClient();
    //    Preference preference = await client.CreateAsync(request);

    //    return preference;  // retorna el objeto entero de preferencia

    //}

    public async Task<Preference> PagoPreference(double montoApagarMP, string KeyPagoID)
    {
        if (Parametros.ModoDemo)
        {
            // Modo demo: no se procesan pagos reales contra MercadoPago
            return null;
        }

        ParametrosServices oParam = new ParametrosServices();
        string urlApi = oParam.urlApi;

        var accessToken = oParam.accessTokenMP;
        var publicKey = oParam.publicKeyMP;

        MercadoPagoConfig.AccessToken = accessToken;

        var productDetails = new ProductsMP()
        {
            Id = 1,
            Name = "AppVendedores2025 Demo",
            Unit = 1,
            UnitPrice = montoApagarMP,
            CategoryId = 8,
            Description = "AppVendedores2025 Demo ¡Gracias por su compra!",
        };

        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
        {
            new PreferenceItemRequest
            {
                Id = productDetails.Id.ToString(),
                Title = productDetails.Name,
                CurrencyId = "ARS",
                PictureUrl = "https://cdn.empresademo.example/images/favicon-96x96.png",
                Description = productDetails.Description,
                CategoryId = productDetails.CategoryId.ToString(),
                Quantity = productDetails.Unit,
                UnitPrice = Convert.ToDecimal(productDetails.UnitPrice)
            }
        },
            Payer = new PreferencePayerRequest(),
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = urlApi + "MercadoPago/" + KeyPagoID + "/1/Success",
                Failure = urlApi + "MercadoPago/" + KeyPagoID + "/Failure",
                Pending = urlApi + "MercadoPago/" + KeyPagoID + "/Pending"
            },
            AutoReturn = "approved",
            PaymentMethods = new PreferencePaymentMethodsRequest
            {
                ExcludedPaymentMethods = [],
                ExcludedPaymentTypes = [],
                Installments = 1
            },
            StatementDescriptor = "Mercado Pago App Ventas AppVendedores2025 Demo",
            ExternalReference = $"Referencia_{Guid.NewGuid()}",
            Expires = true,
            ExpirationDateFrom = DateTime.Now,
            ExpirationDateTo = DateTime.Now.AddMinutes(4)
        };

        try
        {
            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);
            return preference;
        }
        catch (MercadoPagoApiException ex)
        {
            Console.WriteLine("🚨 [MercadoPagoApiException]");
            Console.WriteLine($"Status Code: {ex.StatusCode}");
            Console.WriteLine($"Message: {ex.ApiError.Message}");
            if (ex.ApiError.Cause != null && ex.ApiError.Cause.Any())
            {
                Console.WriteLine("Causes:");
                foreach (var cause in ex.ApiError.Cause)
                {
                    Console.WriteLine($"- Code: {cause.Code}, Description: {cause.Description}");
                }
            }
            throw new Exception($"MercadoPago API error: {ex.ApiError.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("🚨 [General Exception]");
            Console.WriteLine(ex.Message);
            throw;
        }
    }


    // Método para consultar el Pago por fecha y PagoID
    public async Task<(string PagoID, string StatusPago, string PaymentID)> ConsultarPagoPorFechaYPagoID(DateTime fechaPago, string pagoID)
    {

        ParametrosServices oParam = new ParametrosServices();
        string urlApiBase = oParam.urlApi;
        //Borrar para publicar
        //urlApiBase = "http://localhost:5101/";
        static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
        try
        {
            using (var httpClient = HttpHelper.CreateClient())  // Usamos HttpHelper en lugar de new HttpClient()
            {
                //Armamos el Header
                httpClient.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));
                // Llamar al endpoint de tu API que consulta el pago en la base de datos
                string url = $"{urlApiBase}MercadoPago/ConsultarPago?fechaPago={fechaPago:yyyy-MM-dd}&pagoID={pagoID}";

                // Realizar la solicitud GET
                var response = await httpClient.GetAsync(url);

                // Verificar si la respuesta es exitosa
                if (response.IsSuccessStatusCode)
                {
                    // Leer la respuesta y devolver los valores de PagoID y StatusPago directamente
                    var resultado = await response.Content.ReadAsStringAsync();

                    // En este caso, asumimos que la respuesta contiene los datos en un formato JSON como:
                    // { "PagoID": "A43812_2_20241202110632", "StatusPago": "Aprobado" }

                    var data = JsonConvert.DeserializeObject<dynamic>(resultado);
                    string pagoIDResult = data?.PagoID ?? null;
                    string statusPago = data?.StatusPago ?? null;
                    string paymentID = data?.PaymentID ?? null;
                    // Si encontramos los datos, los retornamos junto con el código de estado
                    if (pagoIDResult != null && statusPago != null)
                    {
                        return (pagoIDResult, statusPago, paymentID);  // Devolver el StatusCode junto con los datos
                    }
                    else
                    {
                        return (null, "No se encontró el pago o datos inválidos", "");
                    }
                }
                else
                {
                    // Si la respuesta no es exitosa, retornar un mensaje de error
                    return (null, "Error al consultar el pago", "");
                }
            }
        }
        catch (Exception ex)
        {
            // Manejo de excepciones y retorno de mensaje de error
            return (null, $"Error al realizar la consulta: {ex.Message}", "");
        }

    }

    public async Task<(string PagoID, string StatusPago, string Mensaje, string PaymentID)> ConsultarPagoConIntervaloAsync(string keyPagoID)
    {
        DateTime inicio = DateTime.Now; // Guardamos la hora de inicio
        TimeSpan intervalo = TimeSpan.FromSeconds(3); // Intervalo de 10 segundos entre consultas
        TimeSpan tiempoMaximo = TimeSpan.FromMinutes(4); // Tiempo máximo de 5 minutos
        
        while (DateTime.Now - inicio < tiempoMaximo)
        {
            // Realizar la consulta usando la función ConsultarPagoPorFechaYPagoID
            var respuestaPagoRealizado = await ConsultarPagoPorFechaYPagoID(DateTime.Now, keyPagoID);
           
            // Comprobar si el estado del pago es "approved"
            if (respuestaPagoRealizado.StatusPago == "approved")
            {
                // Verificar si el PagoID devuelto es igual al que tenemos en la app (keyPagoID)
                if (respuestaPagoRealizado.PagoID == keyPagoID)
                {
                    // Si el PagoID es el mismo, retornamos la respuesta inmediatamente
                    return (respuestaPagoRealizado.PagoID, respuestaPagoRealizado.StatusPago, "Pago confirmado", respuestaPagoRealizado.PaymentID);
                }
                else
                {
                    // Si el PagoID no coincide, retornamos un mensaje de error
                    return (null, "Pago no confirmado", "El PagoID no coincide.", "");
                }
            }

            // Esperar 15 segundos antes de hacer la siguiente consulta
            await Task.Delay(intervalo);
        }
        // Si después de 15 minutos no se obtiene "approved", devolver la última respuesta recibida (aunque no sea aprobada)
        var ultimaRespuesta = await ConsultarPagoPorFechaYPagoID(DateTime.Now, keyPagoID);
        return (ultimaRespuesta.PagoID, ultimaRespuesta.StatusPago, "Tiempo de espera agotado.", "");
    }

    public async Task<(string PagoID, string StatusPago, string Mensaje, string PaymentID)> ConsultarPagoConIntervaloIngresoAsync(string keyPagoID)
    {
        DateTime inicio = DateTime.Now; // Guardamos la hora de inicio
        TimeSpan intervalo = TimeSpan.FromSeconds(10); // Intervalo de 10 segundos entre consultas
        TimeSpan tiempoMaximo = TimeSpan.FromMinutes(5); // Tiempo máximo de 5 minutos

        while ((DateTime.Now - inicio) < tiempoMaximo)
        {
            // Realizar la consulta usando la función ConsultarPagoPorFechaYPagoID
            var respuestaPagoRealizado = await ConsultarPagoPorFechaYPagoID(DateTime.Now, keyPagoID);

            // Comprobar si el estado del pago es "approved"
            if (respuestaPagoRealizado.StatusPago == "approved")
            {
                // Verificar si el PagoID devuelto es igual al que tenemos en la app (keyPagoID)
                if (respuestaPagoRealizado.PagoID == keyPagoID)
                {
                    // Si el PagoID es el mismo, retornamos la respuesta inmediatamente
                    return (respuestaPagoRealizado.PagoID, respuestaPagoRealizado.StatusPago, "Pago confirmado", respuestaPagoRealizado.PaymentID);
                }
                else
                {
                    // Si el PagoID no coincide, retornamos un mensaje de error
                    return (null, "Pago no confirmado", "El PagoID no coincide.", "");
                }
            }
            // Esperar 15 segundos antes de hacer la siguiente consulta
            await Task.Delay(intervalo);
        }
        // Si después de 15 minutos no se obtiene "approved", devolver la última respuesta recibida (aunque no sea aprobada)
        var ultimaRespuesta = await ConsultarPagoPorFechaYPagoID(DateTime.Now, keyPagoID);
        return (ultimaRespuesta.PagoID, ultimaRespuesta.StatusPago, "Tiempo de espera agotado.", "");
    }

    public async Task<decimal> ObtenerMontoVentaMinima()
    {
        if (Parametros.ModoDemo)
        {
            // Modo demo: no se llama a la API de parámetros real
            return 0;
        }

        using (HttpClient client = HttpHelper.CreateClient())
        {
            try
            {
                ParametrosServices oParam = new ParametrosServices();
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                //Armamos el Header
                client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));

                // Realizar la solicitud GET
                HttpResponseMessage response = await client.GetAsync("https://api.empresademo.example/Ecom/parametros/VTAMINIMA");

                // Verificar si la solicitud fue exitosa
                if (response.IsSuccessStatusCode)
                {
                    // Leer la respuesta como una cadena de texto
                    string responseData = await response.Content.ReadAsStringAsync();

                    // Parsear la respuesta como un objeto JSON
                    JObject jsonResponse = JObject.Parse(responseData);

                    // Acceder directamente al monto de "VentaMinima"
                    var montoVentaMinima = jsonResponse["descuentos"]
                                                .FirstOrDefault(d => (string)d["descripcion"] == "VentaMinima")?
                                                ["monto"];

                    // Devolver el monto encontrado o 0 si no se encuentra
                    return montoVentaMinima?.Value<decimal>() ?? 0;
                }
                else
                {
                    // Si la respuesta no es exitosa, lanzar una excepción o manejar el error
                    throw new Exception("Error al obtener el valor: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Manejar errores de red o cualquier otro error
                Console.WriteLine("Error: " + ex.Message);
                return 0; // o devolver un valor por defecto
            }
        }
    }

    public async Task<List<dynamic>> ConsultarPercepcionesPorCUIT(string cuit)
    {
        if (Parametros.ModoDemo)
        {
            // Modo demo: no se llama a la API de parámetros real
            return new List<dynamic>();
        }

        using (HttpClient client = HttpHelper.CreateClient())
        {
            try
            {
                ParametrosServices oParam = new ParametrosServices();
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                //Armamos el Header
                client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));

                // Realizar la solicitud GET
                HttpResponseMessage responsea = await client.GetAsync($"https://api.empresademo.example/Ecom/parametros/CUIT|{cuit}");

                // Verificar si la respuesta fue exitosa
                if (responsea.IsSuccessStatusCode)
                {
                    // Leer el contenido de la respuesta como un string
                    var response = await responsea.Content.ReadAsStringAsync();

                    // Deserializar la respuesta JSON
                    var jsonResponse = JObject.Parse(response);

                    // Obtener la lista de descuentos
                    var descuentos = jsonResponse["descuentos"];

                    // Crear una lista de objetos anónimos para almacenar los descuentos relevantes
                    var listaDescuentos = new List<dynamic>();

                    // Iterar sobre los descuentos y agregar los datos relevantes
                    foreach (var descuento in descuentos)
                    {
                        // Crear un objeto anónimo con los datos que nos interesan
                        var descuentoObj = new
                        {
                            Descripcion = descuento["descripcion"]?.ToString(),
                            Monto = descuento["monto"]?.Value<decimal>() ?? 0,
                            Porcentaje = descuento["porcentaje"]?.Value<decimal>() ?? 0
                        };

                        // Agregar el descuento a la lista
                        listaDescuentos.Add(descuentoObj);
                    }

                    // Retornar la lista de descuentos
                    return listaDescuentos;
                }
                else
                {
                    // Manejo de error en la respuesta HTTP
                    Console.WriteLine($"Error: {responsea.StatusCode}");
                    return new List<dynamic>();  // Retorna una lista vacía en caso de error
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                Console.WriteLine($"Error al consultar la API: {ex.Message}");
                return new List<dynamic>();  // Retorna una lista vacía en caso de error
            }
        }
    }
}

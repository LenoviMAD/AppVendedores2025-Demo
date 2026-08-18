using EntidadesAppVendedores;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class ParametrosServices
    {
        private SQLiteAsyncConnection _dbConnection;

        public ParametrosServices()
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
        public async Task<int> AddorUpdateAsync(string nombre, string valString, decimal valDecimal, DateTime valDate)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var item = new ParametrosItem(nombre, valString, valDecimal, valDate);

            var resultado = await _dbConnection.QueryAsync<ParametrosItem>(
                $"Select * From {nameof(ParametrosItem)} where ParamNombre=?", nombre);

            if (resultado.Count == 0)
            {
                return await _dbConnection.InsertAsync(item);
            }
            else
            {
                var resultado2 = await _dbConnection.ExecuteAsync(
                    $"UPDATE {nameof(ParametrosItem)} SET ParamString=?, ParamDecimal=?, ParamDateTime=? WHERE ParamNombre=?",
                    item.ParamString, item.ParamDecimal, item.ParamDateTime.ToString(), item.ParamNombre);
                return resultado2;
            }
        }


        public int AddorUpdate(string nombre, string valString, decimal valDecimal, DateTime valDate)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var item = new ParametrosItem(nombre, valString, valDecimal, valDate);

            var resultado = _dbConnection.QueryAsync<ParametrosItem>(
                $"Select * From {nameof(ParametrosItem)} where ParamNombre=?", nombre).GetAwaiter().GetResult();

            if (resultado.Count == 0)
            {
                return _dbConnection.InsertAsync(item).GetAwaiter().GetResult();
            }
            else
            {
                var resultado2 = _dbConnection.ExecuteAsync(
                    $"UPDATE {nameof(ParametrosItem)} SET ParamString=?, ParamDecimal=?, ParamDateTime=? WHERE ParamNombre=?",
                    item.ParamString, item.ParamDecimal, item.ParamDateTime.ToString(), item.ParamNombre).GetAwaiter().GetResult();
                return resultado2;
            }
        }


        public async Task<int> BorrarTodo()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            var res = await _dbConnection.DeleteAllAsync<ParametrosItem>();

            return res;
        }

        public string VersionApp
        {
            get
            {
                var res = GetByNombre("versionapp");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("versionapp", value, 0, new DateTime());
            }
        }
        public string PagePadre
        {
            get
            {
                var res = GetByNombre("PagePadre");

                var defa = res.ParamString;
                if (defa.Trim() == "")
                {
                    defa = "/";
                }
                return defa;
            }

            set
            {
                AddorUpdate("PagePadre", value, 0, new DateTime());
            }
        }

        public bool EsModoTest
        {
            get
            {
                var res = GetByNombre("modoTest");

                return res.ParamString == "1" ? true : false;

            }
            set
            {
                string valor = value ? "1" : "0";
                AddorUpdate("modoTest", valor, 0, new DateTime());
            }
        }
        public bool botonEliminar
        {
            get
            {
                var res = GetByNombre("botonEliminar");

                return res.ParamString == "1" ? true : false;

            }
            set
            {
                string valor = value ? "1" : "0";
                AddorUpdate("botonEliminar", valor, 0, new DateTime());
            }
        }

        public bool Autogestion
        {
            get
            {
                var res = GetByNombre("autoGestion");

                return res.ParamString == "1" ? true : false;

            }
            set
            {
                string valor = value ? "1" : "0";
                AddorUpdate("autoGestion", valor, 0, new DateTime());
            }
        }
        public bool DescargarImagen
        {
            get
            {
                var res = GetByNombre("descargarImagen");

                return res.ParamString == "1" ? true : false;

            }
            set
            {
                string valor = value ? "1" : "0";
                AddorUpdate("descargarImagen", valor, 0, new DateTime());
            }
        }
        
        public string NombreDelVendedor
        {
            get
            {
                var res = GetByNombre("nombreDelVendedor");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("nombreDelVendedor", value, 0, new DateTime());
            }
        }
        public string mensajeVendedorHeadCache = "**";
        public string MensajeVendedorHead
        {
            get
            {
                // if (mensajeVendedorHeadCache == "**" || mensajeVendedorHeadCache=="")
                {
                    var res = GetByNombre("MensajeVendedorH");
                    return res.ParamString;
                    // mensajeVendedorHeadCache = res.ParamString;
                }
                //return mensajeVendedorHeadCache;

            }
            set
            {
                AddorUpdate("MensajeVendedorH", value, 0, new DateTime());
                mensajeVendedorHeadCache = value;
            }
        }

        public string MensajeErrores
        {
            get
            {
                // if (mensajeVendedorHeadCache == "**" || mensajeVendedorHeadCache=="")
                {
                    var res = GetByNombre("MensajeErrores");
                    return res.ParamString;
                    // mensajeVendedorHeadCache = res.ParamString;
                }
                //return mensajeVendedorHeadCache;

            }
            set
            {
                AddorUpdate("MensajeErrores", value, 0, new DateTime());
                mensajeVendedorHeadCache = value;
            }
        }


        public string puntosSinTx = "**";
        public string MensajeVPuntosSinTx
        {
            get
            {
                // if (puntosSinTx == "**" || puntosSinTx == "")
                {
                    var res = GetByNombre("MensajeTx");
                    return res.ParamString;
                    //puntosSinTx = res.ParamString;
                }
                //return puntosSinTx;

            }
            set
            {
                AddorUpdate("MensajeTx", value, 0, new DateTime());
                puntosSinTx = value;
            }
        }

        public string TestApp
        {
            get
            {
                var res = GetByNombre("testApp");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("testApp", value, 0, new DateTime());
            }
        }


        public string VendedorID
        {
            get
            {
                var res = GetByNombre("vendedorID");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("vendedorID", value, 0, new DateTime());
            }
        }
        public string GoolgeApiKeyV2
        {
            get
            {
                var res = GetByNombre("GoolgeApiKey");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("GoolgeApiKey", value, 0, new DateTime());
            }
        }

        public int EmpresaID
        {
            get
            {
                var res = GetByNombre("empresaID");
                int valor = (int)res.ParamDecimal;
                return valor > 0 ? valor : 0;
            }
            set
            {
                AddorUpdate("empresaID", "", value, new DateTime());
            }
        }
        public string Vendedor
        {
            get
            {
                var res = GetByNombre("vendedor");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("vendedor", value, 0, new DateTime());
            }
        }

        /// <summary>
        ///Para Guardar las Estrellitas en Parametros y obtener 
        ///</summary>
        public int EstrellitasVendedor
        {
            get
            {
                var res = GetByNombre("EstrellitasVendedor");

                // Verifica si res es nulo o si res.ParamDecimal es nulo
                if (res == null || res.ParamDecimal == null)
                {
                    //TODO:MInimo una estrella
                    return 0; // Si no hay valor, retorna 0
                }

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("EstrellitasVendedor", "", value, new DateTime());
            }
        }


        public int segundosActivarServicioGps
        {
            get
            {
                var res = GetByNombre("segundosActivarServicioGps");

                // Verifica si res es nulo o si res.ParamDecimal es nulo
                if (res == null || res.ParamDecimal == null)
                {
                    //TODO:MInimo una estrella
                    return 0; // Si no hay valor, retorna 0
                }

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("segundosActivarServicioGps", "", value, new DateTime());
            }
        }
        //public decimal CoheficienteComisionVendedor
        //{
        //    get
        //    {
        //        var res = GetByNombre("CoheficienteComisionVendedor");

        //        //Verifica si res es nulo o si res.ParamDecimal es nulo
        //        if (res == null || res.ParamDecimal == null)
        //        {
        //            return 0; // Si no hay valor, retorna 0
        //        }

        //        return res.ParamDecimal;

        //    }


        //    set
        //    {
        //        AddorUpdate("CoheficienteComisionVendedor", "", value, new DateTime());
        //    }
        //}

        public decimal CoheficienteComisionVendedor
        {
            get
            {
                var res = GetByNombre("CoheficienteComisionVendedor");

                if (res == null)
                {
                    return 0;
                }

                try
                {
                    // Forzamos a parsear manualmente por si el valor está mal interpretado
                    var valorTexto = res.ParamDecimal.ToString().Replace(',', '.');
                    decimal valorParseado = 0;

                    if (decimal.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out valorParseado))
                    {
                        return valorParseado;
                    }
                }
                catch
                {
                    // Log o manejo de error si querés
                }

                return 0; // Valor por defecto si falla
            }

            set
            {
                AddorUpdate("CoheficienteComisionVendedor", "", value, new DateTime());
            }
        }


        public int IrDirectoPostSincro
        {
            get
            {
                var res = GetByNombre("PrimeraSincronizacion");

                // Verifica si res es nulo o si res.ParamDecimal es nulo
                if (res == null || res.ParamDecimal == null)
                {
                    return 0; // Si no hay valor, retorna 0
                }

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("PrimeraSincronizacion", "", value, new DateTime());
            }
        }
        public List<Object> listEstrellas
        {
            get
            {
                try
                {
                    var res = GetByNombre("listEstrellas");

                    // Supongamos que ParamString es un JSON con una lista de objetos
                    if (string.IsNullOrWhiteSpace(res.ParamString))
                        return new List<Object>();

                    // Deserializamos desde JSON → List<object>
                    var lista = System.Text.Json.JsonSerializer.Deserialize<List<Object>>(res.ParamString);

                    return lista ?? new List<Object>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en GET listEstrellas: {ex.Message}");
                    return new List<Object>();
                }
            }

            set
            {
                try
                {
                    // Guardamos como JSON
                    var json = System.Text.Json.JsonSerializer.Serialize(value);
                    AddorUpdate("listEstrellas", json, 0, DateTime.Now);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en SET listEstrellas: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// Agregado para saber que tipo de usuario esta ingresando, modificacion para cliente 5-8 // 999 es cliente, resto vendedor
        /// </summary>
        public bool TipoDeUsuario
        {
            get
            {
                var res = GetByNombre("TipoDeUsuario");

                return res.ParamString == "1";

            }
            set
            {
                AddorUpdate("TipoDeUsuario", value ? "1" : "0", 0, new DateTime());
            }
        }
        public string TextoVD
        {
            get
            {
                var res = GetByNombre("TextoVD");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("TextoVD", value, 0, new DateTime());
            }
        }
        public string VersionDatosApp
        {
            get
            {
                var res = GetByNombre("VersionDatosApp");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("VersionDatosApp", value, 0, new DateTime());
            }
        }

        public string urlApi
        {
            get
            {
                var res = GetByNombre("urlApi");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("urlApi", value, 0, new DateTime());
            }
        }
        public string urlDisconsultas
        {
            get
            {
                var res = GetByNombre("urlDisconsultas");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("urlDisconsultas", value, 0, new DateTime());
            }
        }

        public string urlGPSpoint
        {
            get
            {
                var res = GetByNombre("urlGPSpoint");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("urlGPSpoint", value, 0, new DateTime());
            }
        }


        public int ListadePrecios
        {
            get
            {
                var res = GetByNombre("listadePrecios");

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("listadePrecios", "", value, new DateTime());
            }
        }
        //public int Cli_MCE
        //{
        //    get
        //    {
        //        var res = GetByNombre("Cli_MCE");

        //        return (int)res.ParamDecimal;

        //    }
        //    set
        //    {
        //        AddorUpdate("Cli_MCE", "", value, new DateTime());
        //    }
        //}
        public string listadePreciosNombre
        {
            get
            {
                var res = GetByNombre("listadePreciosNombre");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("listadePreciosNombre", value, 0, new DateTime());
            }
        }


        public int ClienteID
        {
            get
            {
                var res = GetByNombre("clienteID");

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("clienteID", "", value, new DateTime());
            }
        }

        public int tipoVD
        {
            get
            {
                var res = GetByNombre("tipoVD");

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("tipoVD", "", value, new DateTime());
            }
        }



        public string Pwd
        {
            get
            {
                var res = GetByNombre("pwd");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("pwd", value, 0, new DateTime());
            }
        }
        public string cliente
        {
            get
            {
                var res = GetByNombre("cliente");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("cliente", value, 0, new DateTime());
            }
        }
        public string Color
        {
            get
            {
                var res = GetByNombre("color");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("color", value, 0, new DateTime());
            }
        }
        public string DeviceID
        {
            get
            {
                var res = GetByNombre("DeviceID");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("DeviceID", value, 0, new DateTime());
            }
        }

        public DateTime FechaUltimaSincronizacion
        {
            get
            {
                var res = GetByNombre("ultimasync");

                return new DateTime(Convert.ToInt64(res.ParamDecimal));

            }
            set
            {
                AddorUpdate("ultimasync", "", value.Ticks, new DateTime());
            }
        }
        public DateTime FechaUltimaSincronizacionImagen
        {
            get
            {
                var res = GetByNombre("ultimasyncimg");

                return new DateTime(Convert.ToInt64(res.ParamDecimal));

            }
            set
            {
                AddorUpdate("ultimasyncimg", "", value.Ticks, new DateTime());
            }
        }
        public async Task<ParametrosItem> GetByNombreAsync(string nombre)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var res = new ParametrosItem("", "", 0, DateTime.Now);

            try
            {
                var resultado = await _dbConnection.QueryAsync<ParametrosItem>(
                    $"Select * From {nameof(ParametrosItem)} where ParamNombre=?", nombre);
                var res1 = resultado.FirstOrDefault();
                if (res1 != null)
                {
                    res = res1;
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[ParametrosServices] Error GetByNombreAsync('{nombre}'): {ex.Message}"); }

            return res;
        }

        public ParametrosItem GetByNombre(string nombre)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            string sp;
            var res = new ParametrosItem("", "", 0, DateTime.Now);
            if (nombre == "CoheficienteComisionVendedor")
            {
                sp = $"Select ParamNombre, ParamString,  CAST(REPLACE(ParamDecimal, ',', '.') AS REAL) as ParamDecimal, ParamDateTime From {nameof(ParametrosItem)} where ParamNombre=?";
            }
            else
            {
                sp = $"Select * From {nameof(ParametrosItem)} where ParamNombre=?";
            }

            try
            {
                var resultado = _dbConnection.QueryAsync<ParametrosItem>(sp, nombre).GetAwaiter().GetResult();
                var res1 = resultado.FirstOrDefault();
                if (res1 != null)
                {
                    res = res1;
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[ParametrosServices] Error GetByNombre('{nombre}'): {ex.Message}"); }

            return res;
        }




        //public ParametrosItem GetByNombre(string nombre)
        //{
        //    string sp;
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();
        //    }



        //    var res = new ParametrosItem(nombre, "", 100, DateTime.Now);
        //    if(nombre == "CoheficienteComisionVendedor")
        //    {
        //        sp = $"Select ParamNombre, ParamString,  CAST(REPLACE(ParamDecimal, ',', '.') AS REAL) as ParamDecimal, ParamDateTime From {nameof(ParametrosItem)} where ParamNombre='{nombre}'";
        //    }
        //    else
        //    {
        //        sp = $"Select * From {nameof(ParametrosItem)} where ParamNombre='{nombre}'";
        //    }

        //        //sp = $"Select * From {nameof(ParametrosItem)} ";


        //        try
        //        {
        //            var resultado = _dbConnection.QueryAsync<ParametrosItem>(sp);
        //            //		var res = await _dbConnection.Table<ClienteItem>().ToListAsync(); ;
        //            var res1 = resultado.Result.FirstOrDefault();
        //            if (res1 != null)
        //            {
        //                res = res1;
        //            }
        //        }
        //        catch { }

        //    return res;
        //}

        // Método para guardar los descuentos como un JSON
        public async Task<int> GuardarPercepcionesAsync(List<dynamic> descuentos)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            // Serializamos la lista de descuentos en formato JSON
            string jsonDescuentos = JsonConvert.SerializeObject(descuentos);

            // Guardamos el JSON en la tabla Parametros usando el nombre "descuentos"
            return await AddorUpdateAsync("Percepciones", jsonDescuentos, 0, DateTime.Now);
        }

        // Método para obtener los descuentos desde la base de datos
        //public List<dynamic> ObtenerPercepciones()
        //{
        //    if (_dbConnection == null)
        //    {
        //        SetUpDb();
        //    }

        //    // Recuperamos el JSON de la base de datos usando el nombre "descuentos"
        //    var res = GetByNombre("Percepciones");

        //    // Si encontramos el JSON, lo deserializamos
        //    if (!string.IsNullOrEmpty(res.ParamString))
        //    {
        //        // Deserializamos el JSON a la lista de descuentos
        //        return JsonConvert.DeserializeObject<List<dynamic>>(res.ParamString);
        //    }

        //    return new List<dynamic>(); // En caso de que no exista o haya un error
        //}

        public List<dynamic> ObtenerPercepciones()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            // Recuperamos el JSON de la base de datos usando el nombre "Percepciones"
            var res = GetByNombre("Percepciones");

            // Si encontramos el JSON, lo deserializamos
            if (!string.IsNullOrEmpty(res.ParamString))
            {
                // Deserializamos el JSON, asegurándonos de que es un array
                var percepcionesArray = JArray.Parse(res.ParamString);

                // Crear una lista para almacenar los objetos de percepciones relevantes
                var listaPercepciones = new List<dynamic>();

                // Iterar sobre las percepciones y agregar los datos relevantes
                foreach (var percepcion in percepcionesArray)
                {
                    // Crear un objeto anónimo con los datos que nos interesan
                    var percepcionObj = new
                    {
                        Descripcion = percepcion["Descripcion"]?.ToString(),
                        Monto = percepcion["Monto"]?.Value<decimal>() ?? 0, // Aseguramos que el monto sea decimal
                        Porcentaje = percepcion["Porcentaje"]?.Value<decimal>() ?? 0 // Aseguramos que el porcentaje sea decimal
                    };

                    // Agregar la percepción a la lista
                    listaPercepciones.Add(percepcionObj);
                }

                // Retornar la lista de percepciones deserializadas
                return listaPercepciones;
            }

            // Si no encontramos datos o hay un error, retornamos una lista vacía
            return new List<dynamic>();
        }

        public decimal montoMinimo
        {
            get
            {
                var res = GetByNombre("montoMinimo");

                return (decimal)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("montoMinimo", "", value, new DateTime());
            }
        }

        public int CabeceraCarritoID
        {
            get
            {
                var res = GetByNombre("PedidoCabeceraID");

                return (int)res.ParamDecimal;

            }
            set
            {
                AddorUpdate("PedidoCabeceraID", "", value, new DateTime());
            }
        }
        public bool ReactivarCarrito
        {
            get
            {
                var res = GetByNombre("ReactivarCarrito");

                return res.ParamString == "1";

            }
            set
            {
                AddorUpdate("ReactivarCarrito", value ? "1" : "0", 0, new DateTime());
            }
        }
        public bool ModoTestGps
        {
            get
            {
                var res = GetByNombre("montoMinimo");

                return res.ParamString == "1";

            }
            set
            {
                AddorUpdate("montoMinimo", value ? "1" : "0", 0, new DateTime());
            }
        }
        public async Task<string> GetParametroAppSeting(string key)
        {
            try
            {
                var urlApiBase = urlApi;

                var httpClient = HttpHelper.CreateClient();

                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                httpClient.DefaultRequestHeaders.Add(Clean(HeaderApiKey), Clean(ApiKey));
                httpClient.BaseAddress = new Uri(urlApiBase);

                var urlGetParametro = httpClient.BaseAddress + "Parametros/" + key;

                // Realizar la solicitud GET
                HttpResponseMessage response = await httpClient.GetAsync(urlGetParametro);

                // Verificar si la respuesta fue exitosa
                if (response.IsSuccessStatusCode)
                {
                    // Leer el contenido de la respuesta como cadena
                    var parametro = await response.Content.ReadAsStringAsync();
                    return parametro;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParametrosServices] Error GetParametroAppSeting('{key}'): {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetParametroAppSetingAsync(string key)
        {
            try
            {
                var urlApiBase = urlApi;

                var httpClient = HttpHelper.CreateClient();
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                //Armamos el Header
                httpClient.DefaultRequestHeaders.Add(Clean(HeaderApiKey), Clean(ApiKey));
                httpClient.BaseAddress = new Uri(urlApiBase);

                var urlGetParametro = httpClient.BaseAddress + "Parametros/" + key;

                HttpResponseMessage response = await httpClient.GetAsync(urlGetParametro);

                if (response.IsSuccessStatusCode)
                {
                    var parametro = await response.Content.ReadAsStringAsync();
                    return parametro;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParametrosServices] Error GetParametroAppSetingAsync('{key}'): {ex.Message}");
                return "";
            }
        }

        public string accessTokenMP
        {
            get
            {
                var res = GetByNombre("accessTokenMP");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("accessTokenMP", value, 0, new DateTime());
            }
        }

        public string publicKeyMP
        {
            get
            {
                var res = GetByNombre("publicKeyMP");

                return res.ParamString;

            }
            set
            {
                AddorUpdate("publicKeyMP", value, 0, new DateTime());
            }
        }

        public string MP_Test
        {
            get
            {
                // 1 es prendido 0 apagado
                var res = GetByNombre("MP_Test");
                return res.ParamString;
            }
            set
            {
                AddorUpdate("MP_Test", value, 0, new DateTime());
            }
        }


        // Helpers de ofuscación (NO es cifrado real)
        private static class Obf
        {
            private const string Prefix = "obf:";

            public static string ToStored(string plain)
            {
                if (plain is null) return Prefix; // vacío ofuscado
                var bytes = Encoding.UTF8.GetBytes(plain);
                using var ms = new MemoryStream();
                using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
                    gz.Write(bytes, 0, bytes.Length);
                var base64 = Convert.ToBase64String(ms.ToArray());
                return Prefix + base64;
            }

            public static string FromStored(string stored)
            {
                if (string.IsNullOrEmpty(stored)) return "";
                if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
                    return stored; // compatibilidad: estaba en claro

                var base64 = stored.Substring(Prefix.Length);
                var comp = Convert.FromBase64String(base64);
                using var msIn = new MemoryStream(comp);
                using var gz = new GZipStream(msIn, CompressionMode.Decompress);
                using var msOut = new MemoryStream();
                gz.CopyTo(msOut);
                return Encoding.UTF8.GetString(msOut.ToArray());
            }
        }
        public string HeaderApiKey
        {
            get
            {
                var res = GetByNombre("HeaderApiKey");

                var defa = res.ParamString;
                if (defa.Trim() == "")
                {
                    defa = "/";
                }
                return defa;
            }

            set
            {
                AddorUpdate("HeaderApiKey", value, 0, new DateTime());
            }
        }
        // JWT emitido por el login (header X-Auth-Token de VendedorItem). Se manda como
        // Authorization: Bearer en las llamadas protegidas con [Authorize] del backend.
        public string JwtToken
        {
            get
            {
                var res = GetByNombre("jwtToken");
                return res?.ParamString ?? "";
            }
            set
            {
                AddorUpdate("jwtToken", value ?? "", 0, new DateTime());
            }
        }

        public string ApiKey2
        {
            get
            {
                return Migajas.CalcularMigajas() + Percepciones.CalucloPrececiones();
            }
        }
        public string ApiKey
        {
            get
            {
                try
                {
                    var res = GetByNombre("ApiKey");
                    var stored = res?.ParamString ?? "";
                    var plain = Obf.FromStored(stored);

                    // Tu lógica original de default "/"
                    if (string.IsNullOrWhiteSpace(plain)) return "/";

                    return plain;
                }
                catch (FormatException ex)
                {
                    // Si hubo un base64 inválido, devolvemos fallback
                    System.Diagnostics.Debug.WriteLine($"ApiKey get formato inválido: {ex}");
                    return "/";
                }
                catch (InvalidDataException ex)
                {
                    // Si falló el GZip, asumimos que estaba en claro:
                    System.Diagnostics.Debug.WriteLine($"ApiKey get gzip inválido: {ex}");
                    try
                    {
                        var res = GetByNombre("ApiKey");
                        var stored = res?.ParamString ?? "";
                        return string.IsNullOrWhiteSpace(stored) ? "/" : stored;
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"ApiKey get fallback error: {ex2}");
                        return "/";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApiKey get error: {ex}");
                    return "/";
                }
            }
            set
            {
                try
                {
                    var v = value?.Trim() ?? "";
                    // Guardamos ofuscado sólo si hay valor; si querés, permití vacío:
                    var toStore = string.IsNullOrEmpty(v) ? "" : Obf.ToStored(v);
                    AddorUpdate("ApiKey", toStore, 0, new DateTime());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApiKey set error: {ex}");
                    throw; // que la UI decida
                }
            }
        }

        // ------- API KEY helpers (con ofuscación) -------

        public async Task SaveApiKeySettingsAsync(string headerApiKey, string apiKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(headerApiKey)) throw new ArgumentException("HeaderApiKey vacío.");
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("ApiKey vacía.");

            try
            {
                //await SetAsync("HeaderApiKey", headerApiKey.Trim(), ct);
                //await SetAsync("ApiKey", Obfuscate(apiKey.Trim()), ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("No se pudieron guardar las credenciales de API.", ex);
            }
        }

        //public async Task<(string? HeaderName, string? ApiKey)> LoadApiKeySettingsAsync(CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var m = await GetManyAsync(ct, "HeaderApiKey", "ApiKey");
        //        var header = m["HeaderApiKey"];
        //        var obf = m["ApiKey"];
        //        string? apiKey = string.IsNullOrWhiteSpace(obf) ? null : Deobfuscate(obf!);
        //        return (header, apiKey);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new InvalidOperationException("No se pudieron leer las credenciales de API.", ex);
        //    }
        //}

        // ------- Ofuscación simple (NO es cifrado) -------
        private static string Obfuscate(string plain)
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
                gz.Write(bytes, 0, bytes.Length);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static string Deobfuscate(string base64)
        {
            var comp = Convert.FromBase64String(base64);
            using var msIn = new MemoryStream(comp);
            using var gz = new GZipStream(msIn, CompressionMode.Decompress);
            using var msOut = new MemoryStream();
            gz.CopyTo(msOut);
            return Encoding.UTF8.GetString(msOut.ToArray());
        }

    }
}
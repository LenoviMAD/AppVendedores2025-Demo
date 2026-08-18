using EntidadesAppVendedores;
using System.Text;
using SQLite;
using System.Text.Json;
using System.Net.Http;

namespace AppVendedores2025.Services
{
    public class ClienteAltaService
    {
        private SQLiteAsyncConnection _dbConnection;
        private readonly HttpClient _httpClient;
        public ClienteAltaService()
        {
            SetUpDb();
            _httpClient = HttpHelper.CreateClient();
        }   

        private void SetUpDb()
        {
            if (_dbConnection == null)
            {
                _dbConnection =  DABSqlLite.SetUpDb();
                _dbConnection.CreateTableAsync<AltaDeClientesItem>().GetAwaiter().GetResult();
                
            }
        }

        public async Task<bool> AddClienteAlta(AltaDeClientesItem altaCliente)
        {
            try
            {
                if (_dbConnection == null)
                {
                    SetUpDb();
                }

                int result = await _dbConnection.InsertAsync(altaCliente);
                return result == 1; // Devuelve true si se insertó una fila
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                Console.WriteLine($"Error al insertar cliente: {ex.Message}");
                return false; // Indica que la inserción no fue exitosa
            }
        }

        public async Task<List<AltaDeClientesItem>> GetAllClienteAlta()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.Table<AltaDeClientesItem>().ToListAsync();
        }



        public async Task<AltaDeClientesItem> GetByClientePorTelefono(string telefono)
        {
            
            string sp = $"SELECT * FROM {nameof(AltaDeClientesItem)} WHERE Telefono={telefono}";
            
            var resultado = await _dbConnection.QueryAsync<AltaDeClientesItem>(sp);

            // Devuelve el primer item encontrado o null si no hay coincidencias
            return resultado.FirstOrDefault();
            
        }


        public async Task<int> UpdateEstatus(int id, int nuevoEstatus)
        {
            var cliente = await _dbConnection.Table<AltaDeClientesItem>().FirstOrDefaultAsync(c => c.Id == id);
            if (cliente != null)
            {
                cliente.Estatus = nuevoEstatus;
                return await _dbConnection.UpdateAsync(cliente);
            }
            return 0; // No se encontró el cliente
        }

        public Task<int> DeleteClienteAlta(AltaDeClientesItem altaCliente)
        {
            throw new NotImplementedException();
        }
      
        public async Task<bool> EnviarAltaCliente(AltaDeClientesItem cliente)
        {
            ParametrosServices oParam = new ParametrosServices();
            var url = oParam.urlApi; // Asegúrate de que `urlApi` tenga el esquema (http/https) y el dominio.

            string body = JsonSerializer.Serialize(cliente);

            using (var client = HttpHelper.CreateClient())
            {
                // Configura el contenido de la solicitud
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                
                static string Clean(string? value) => new((value ?? "").Where(c => !char.IsControl(c)).ToArray());
                client.DefaultRequestHeaders.Add(Clean(oParam.HeaderApiKey), Clean(oParam.ApiKey));

                try
                {   // Realiza la solicitud POST
                    var response = await client.PostAsync($"{url}ClienteItem/", content); // Concatenar la URL del endpoint

                    if (response.IsSuccessStatusCode)
                    {
                        await UpdateEstatus(cliente.Id, 1);
                    }
                    return response.IsSuccessStatusCode; // Devuelve true si la respuesta es exitosa
                }
                catch (Exception ex) 
                {
                    return false;
                }
            }
        }

        public async Task<bool> ProcesarAltasClientes()
        {
            ClienteAltaService clienteAltaService = new ClienteAltaService();

            // Obtiene todas las altas de clientes con estatus 0
            var altasClientes = await clienteAltaService.GetAllClienteAlta();

            // Filtra las altas con estatus 0
            var pendientes = altasClientes.Where(c => c.Estatus == 0).ToList();

            foreach (var cliente in pendientes)
            {
                await EnviarAltaCliente(cliente);
            }

            return true; // Devuelve true si se procesaron todas las solicitudes
        }

    }
}

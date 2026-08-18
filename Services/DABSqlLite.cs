using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;
using EntidadesAppVendedores;
using System.Diagnostics;

namespace AppVendedores2025.Services
{
	public class DABSqlLite
    {
        private static SQLiteAsyncConnection _dbConnection;
		private static SQLiteConnection _dbConnectionSinc;
		private static SQLiteAsyncConnection _dbConnectiontemp;
        public static SQLiteAsyncConnection SetUpDb()
        {
            if (_dbConnection == null)
            {



                string dbPath = GetPAthBase();
                _dbConnection = new SQLiteAsyncConnection(dbPath);
                //		await _dbConnection.CreateTableAsync<EntidadesAppVendedores.ProductoItemJson>();
                //	await _dbConnection.CreateTableAsync<ListadePrecioItem>();
                _dbConnection.CreateTableAsync<ClienteItem>();
                _dbConnection.CreateTableAsync<ImagenAppItem>();
                _dbConnection.CreateTableAsync<PedidoCabeceraItem>();
                _dbConnection.CreateTableAsync<PedidosDetalleItem>();
                //await _dbConnection.CreateTableAsync<CombosItemJson>();
                _dbConnection.CreateTableAsync<CombosHeadItem>();
                _dbConnection.CreateTableAsync<CombosProductosItem>();
                _dbConnection.CreateTableAsync<ProductoListaItem>();
                _dbConnection.CreateTableAsync<ProductosSubCategoriasItem>();
                _dbConnection.CreateTableAsync<CombosXClientesItem>();
                _dbConnection.CreateTableAsync<LoQueMasTeGustaItem>();
                _dbConnection.CreateTableAsync<GPSLocationPointItem>();
                _dbConnection.CreateTableAsync<ParametrosItem>();
                _dbConnection.CreateTableAsync<ProductosBloqueadosxCliente>();


            }
            //creacion de indices
            CreateIndexesProductosAsync();
            CreateIndexesCombosAsync();
            CreateIndexesGPSLocationPointItemAsync();
            CreateIndexesClienteItemAsync();
            CreateIndexesProductosSubCategoriasItemAsync();
            ReindexImagenAppItemAsync();

            return _dbConnection;
        }





        private static string GetPAthBase()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppVendedores2025Demo.db3");
          //  var pathDb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppVendedores2025Demo.db3");
          //return pathDb;
        }

		public static SQLiteConnection GetSQLiteConnectionSinc()
		{
			if (_dbConnectionSinc == null)
			{
				string dbPath = GetPAthBase();

				_dbConnectionSinc = new SQLiteConnection(dbPath);
			 
			}
			return _dbConnectionSinc;
		}

        // Método para crear índices de manera asíncrona
        private static async Task CreateIndexesProductosAsync()
        {
            try
            {
                var query1 = "CREATE INDEX IF NOT EXISTS IX_ProductosID ON ProductoListaItem(ProductosID ASC);";
                await _dbConnection.ExecuteAsync(query1);

                var query2 = "CREATE INDEX IF NOT EXISTS IX_ProductosIDPadre ON ProductoListaItem(ProductosIDPadre ASC);";
                await _dbConnection.ExecuteAsync(query2);

                var query = "CREATE INDEX IF NOT EXISTS IX_Nombre ON ProductoListaItem(Nombre ASC);";
                await _dbConnection.ExecuteAsync(query);
            }
            catch (Exception ex) { Debug.WriteLine($"[DABSqlLite] Error creando índices Productos: {ex.Message}"); }
        }

        private static async Task CreateIndexesClienteItemAsync()
        {
            try
            {
                var query1 = "CREATE INDEX IF NOT EXISTS IX_cli_clienteID ON ClienteItem(cli_clienteID ASC);";
                await _dbConnection.ExecuteAsync(query1);
            }
            catch (Exception ex) { Debug.WriteLine($"[DABSqlLite] Error creando índices ClienteItem: {ex.Message}"); }
        }

        private static async Task CreateIndexesProductosSubCategoriasItemAsync()
        {
            try
            {
                var query1 = "CREATE INDEX IF NOT EXISTS IX_ProductosID ON ProductosSubCategoriasItem(ProductosID ASC);";
                await _dbConnection.ExecuteAsync(query1);
            }
            catch (Exception ex) { Debug.WriteLine($"[DABSqlLite] Error creando índices SubCategorias: {ex.Message}"); }
        }

        private static async Task CreateIndexesGPSLocationPointItemAsync()
        {
            try
            {
                var query1 = "CREATE INDEX IF NOT EXISTS IX_Tramsmitido ON GPSLocationPointItem(Tramsmitido ASC);";
                await _dbConnection.ExecuteAsync(query1);
            }
            catch (Exception ex) { Debug.WriteLine($"[DABSqlLite] Error creando índices GPS: {ex.Message}"); }
        }



        public async Task ReindexTableAsync(string tableName)
        {

            // Crear la consulta SQL para reindexar la tabla
            var reindexQuery = $"REINDEX {tableName};";

       
            try
            {
                // Ejecutar la consulta de reindexado
                await _dbConnection.ExecuteAsync(reindexQuery);
               
            }
            catch (Exception ex)
            {
                // Manejo de excepciones en caso de error
               
            }
        }
        private static async Task CreateIndexesCombosAsync()
        {
            try
            {
                var query1 = "CREATE INDEX IF NOT EXISTS IX_CombosID ON CombosHeadItem(CombosID ASC);";
                await _dbConnection.ExecuteAsync(query1);

                var query4 = "CREATE INDEX IF NOT EXISTS IX_CombosNombre ON CombosHeadItem(NombreCombo ASC);";
                await _dbConnection.ExecuteAsync(query4);

                var query2 = "CREATE INDEX IF NOT EXISTS IX_CombosIDProductos ON CombosProductosItem(ComboId ASC);";
                await _dbConnection.ExecuteAsync(query2);

                var query3 = "CREATE INDEX IF NOT EXISTS IX_CombosProductosID ON CombosProductosItem(ComboProductoID ASC);";
                await _dbConnection.ExecuteAsync(query3);
            }
            catch (Exception ex) { Debug.WriteLine($"[DABSqlLite] Error creando índices Combos: {ex.Message}"); }
        }

        public static async Task ReindexImagenAppItemAsync()
        {
            try
            {
                var query = "REINDEX ImagenAppItem;";
                await _dbConnection.ExecuteAsync(query);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al reindexar: {ex.Message}");
            }
        }



        public static bool TruncateDatabase(int elegirQueBorrar)
        {
            try
            {
                var connection = GetSQLiteConnectionSinc();
                var tables = GetDatabaseTables();

                // 1. Deshabilitar claves foráneas
                connection.Execute("PRAGMA foreign_keys = OFF");
                if (elegirQueBorrar==1)//Todo menos la tabla parametros
                {
                    connection.RunInTransaction(() =>
                    {
                        foreach (var table in tables)
                        {

                            if (table != "ParametrosItem" && table!= "PedidosDetalleItem" && table!="PedidoCabeceraItem" && table != "GPSLocationPointItem")
                            {
                                connection.Execute($"DELETE FROM {table};");
                                //// Resetear autoincrementales
                                //connection.Execute($"DELETE FROM sqlite_sequence WHERE name = '{table}'");

                                // Resetear autoincrementales
                                // connection.Execute($"DELETE FROM sqlite_sequence WHERE name = '{table}'");
                            }

                        }

                    });
                }
                else if(elegirQueBorrar==2)
                {
                    // 2. Vaciar cada tabla
                    connection.RunInTransaction(() =>
                    {
                        foreach (var table in tables)
                        {

                            if (table != "ParametrosItem" && table != "ClienteItem" && table != "PedidosDetalleItem" && table != "PedidoCabeceraItem" && table != "ImagenAppItem" && table != "GPSLocationPointItem")
                            {
                                connection.Execute($"DELETE FROM {table};");
                                //// Resetear autoincrementales
                                //connection.Execute($"DELETE FROM sqlite_sequence WHERE name = '{table}'");

                                // Resetear autoincrementales
                                // connection.Execute($"DELETE FROM sqlite_sequence WHERE name = '{table}'");
                            }

                        }

                    });

                }
                else if (elegirQueBorrar == 3)
                {
                    // 2. Vaciar cada tabla
                    connection.RunInTransaction(() =>
                    {
                        foreach (var table in tables)
                        {

                            if (table != "ParametrosItem" && table != "ImagenAppItem" && table != "GPSLocationPointItem")
                            {
                                connection.Execute($"DELETE FROM {table};");
                                //// Resetear autoincrementales
                                //connection.Execute($"DELETE FROM sqlite_sequence WHERE name = '{table}'");

                                // Resetear autoincrementales
                                // connection.Execute($"DELETE FROM sqlite_sequence WHERE name = '{table}'");
                            }

                        }

                    });

                }
                   

               
                
                // 3. Habilita claves foráneas
                connection.Execute("PRAGMA foreign_keys = ON");
                connection.Execute("VACUUM;");//libera espacio en la tabla

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al truncar la base de datos: {ex.Message}");
                return false;
            }
        }

        




        public static List<string> GetDatabaseTables()
        {
            var connection = GetSQLiteConnectionSinc();

            var tables = connection.Query<TableInfo>(
                "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;");

            return tables.Select(t => t.name).ToList();
        }

        private class TableInfo
        {
            public string name { get; set; }
        }

    }





    public interface IListasdePrecioItemService
    {
        Task<List<ListadePrecioItem>> GetLista();
        Task<ListadePrecioItem> GetItem(int id);

        Task<bool> Add(IEnumerable<ListadePrecioItem> Item);
    }

    public interface IClientesItemService
    {
        Task<List<ClienteItem>> GetLista();
        Task<ClienteItem> GetItem(int id);

        Task<bool> Add(IEnumerable<ClienteItem> Item);
    }


    public interface IListadePrecioItemService
    {
        Task<List<ListadePrecioItem>> GetAll();
        Task<ListadePrecioItem> GetByID(int id);
        Task<int> Add(ListadePrecioItem item);
        Task<int> Update(ListadePrecioItem item);
        Task<int> DeleteProspecto(ListadePrecioItem item);

    }

       


    }

using System;

namespace AppVendedores2025.Shared
{
    public static class Parametros
    {
        // MODO DEMO: cuando está en true, las integraciones que requieren credenciales
        // reales de terceros (MercadoPago, WhatsApp Business API, etc.) quedan deshabilitadas
        // y muestran un aviso en vez de ejecutar el flujo real.
        public const bool ModoDemo = true;

        // URLS
        // NOTA (DEMO): repointeadas al backend real de esta app (VendedoresApi-Demo),
        // que corre en http://localhost:5101/ con endpoints /VendedorItem/{vd}/{pwd}/{version}/{empresaID}
        // (login), /AppVersion, /ParametrosApp/Soporte. Antes apuntaban por error a
        // IntegradorArchivosApi-Demo (backend de SincroApp, puerto 5080) — eso daba 404 en el
        // login porque esa API no tiene el endpoint VendedorItem. Originalmente (antes de
        // cualquier fork demo) apuntaban al backend real de producción (no incluido en este demo).
        public const string ApiURL_Primaria = "http://localhost:5101/";

        public const string ApiURL_Primaria_LocalHost = "http://localhost:5101/";

        public const string ApiURL_Secundaria = "http://localhost:5101/";

        public const string URL_Primaria_Fleteros = "http://localhost:5101/";


        // TELEFONOS
        public const string TELEFONO_TRAFICO = "5491100000000";


        //public const string ApiURL_Primaria = "https://localhost:4102/";
        //public const string ApiURL_Secundaria = "https://localhost:4103/";

        // Endpoints
        public static readonly string ApiURL_Fleteros_POST_PuntosGPS = $"{ApiURL_Primaria}Fleteros/PuntosGPS/";

        //************* https://localhost:5101/Fleteros/PaginaPrincipal/lola *******************
        public static readonly string ApiURL_Fleteros_GET_PaginaPrincipal = $"{ApiURL_Primaria}Fleteros/PaginaPrincipal/";

        //https://localhost:5101/Fleteros/ValidarFletero/cd22/lola
        public static readonly string ApiURL_Fleteros_GET_ValidarFletero = $"{ApiURL_Primaria}Fleteros/ValidarFletero/";

        //https://localhost:5101/Fleteros/ControlarCarga/lola
        public static readonly string ApiURL_Fleteros_GET_ControlarCarga = $"{ApiURL_Primaria}Fleteros/ControlarCarga/";

        //https://localhost:5101/Fleteros/CuentaCorriente/lola
        public static readonly string ApiURL_Fleteros_GET_CuentaCorriente = $"{ApiURL_Primaria}Fleteros/CuentaCorriente/";

        //https://localhost:5101/Fleteros/CuentaCorriente/lola
        public static readonly string ApiURL_Fleteros_GET_ProductosControlCarga = $"{ApiURL_Primaria}Fleteros/ProductosControlarCarga/";

        //https://localhost:5101/Fleteros/RepartosControlarCarga/lola
        public static readonly string ApiURL_Fleteros_GET_RepartosControlarCarga = $"{ApiURL_Primaria}Fleteros/RepartosControlarCarga/";

        //https://localhost:5101/Fleteros/ControlarProductos/repartoDetalleID
        public static readonly string ApiURL_Fleteros_POST_ControlarProductos = $"{ApiURL_Primaria}Fleteros/ControlarProductos/";

        //https://localhost:5101/Fleteros/ProductosFaltantes/{ClaveFletero}/{RepartoDetalleID:int}
        public static readonly string ApiURL_Fleteros_POST_ProductosFaltantes = $"{ApiURL_Primaria}Fleteros/ProductosFaltantes/";   
        
        
        //https://localhost:5101/Fleteros/ProductosFaltantes/{ClaveFletero}/{RepartoDetalleID:int}
        public static readonly string ApiURL_Fleteros_POST_AnularNotasDeCredito = $"{ApiURL_Primaria}Fleteros/AnularNotasDeCredito/";

        //Fleteros/ProductosDevoluciones/{repartoDetalleID}/{clienteID}
        public static readonly string ApiURL_Fleteros_GET_ProductosDelCliente = $"{ApiURL_Primaria}Fleteros/ProductosDevoluciones/";


        //Fleteros/SolicitarValeVendedor/{RepartoDetalleID:int}/{ClienteID:int}/{ProveedorIDVD:int}/{ClaveFletero}/{ImporteVale:int}/{Motivo}
        public static readonly string ApiURL_Fleteros_POST_SolicitarValeVendedor = $"{ApiURL_Primaria}Fleteros/SolicitarValeVendedor/";

        //https://localhost:5101/Fleteros/ProductosFaltantes/{ClaveFletero}/{RepartoDetalleID:int}
        public static readonly string ApiURL_Fleteros_POST_DevolucionDeProductos = $"{ApiURL_Primaria}Fleteros/DevolucionDeProductos/";

        //Fleteros/ProductosDevoluciones/{RepartoDetalleID}
        public static readonly string ApiURL_Fleteros_GET_Clientes = $"{ApiURL_Primaria}Fleteros/Clientes/";

        //Fleteros/DatosVendedor/{RepartoDetalleID}/{ClienteID}
        public static readonly string ApiURL_Fleteros_GET_DatosVendedor = $"{ApiURL_Primaria}Fleteros/DatosVendedor/";

        //Fleteros/ParametrosMP
        public static readonly string ApiURL_Fleteros_GET_ParametrosMP = $"{ApiURL_Primaria}Fleteros/ParametrosMP";

        //Fleteros/PagoMP/{RepartoDetalleID:int}/{clientesID:int}/{ClaveFletero}
        public static readonly string ApiURL_Fleteros_GET_PagoMP = $"{ApiURL_Primaria}Fleteros/PagoMP/";

        //Fleteros/PagoMP/{RepartoDetalleID:int}/{clientesID:int}/{ClaveFletero}
        public static readonly string ApiURL_Fleteros_GET_TransferenciaBancaria = $"{ApiURL_Primaria}Fleteros/TransferenciaBancaria/";


        //Fleteros/DatosRepartosXFleteroClave/{ClaveFletero}
        public static readonly string ApiURL_Fleteros_GET_RepartosDetalles = $"{ApiURL_Primaria}Fleteros/GetRepartosDetalle/";


        // Loginold.aspx?U=CD33&F=FELIPE
        public static readonly string URL_Fleteros_Pagina_Mapa = $"{URL_Primaria_Fleteros}Loginold.aspx?";

        //https://localhost:5101/PagoClienteMp.aspx?ClientesCodNc=
        public static readonly string URL_Fleteros_Pagina_ClienteMPago = $"{URL_Primaria_Fleteros}PagoClienteMp.aspx?ClientesCodNc=";


        public static readonly string URL_Fleteros_Pagina_EnviarFactura = $"{URL_Primaria_Fleteros}dwdoc.aspx?ClientesID=";


        public static readonly string URL_Fleteros_Pagina_DetalleFacturasVtas = $"{URL_Primaria_Fleteros}DetalleFacturasVtas.aspx?RepartosDetalleID=";        
        
        public static readonly string URL_Fleteros_Pagina_ValesFletero = $"{URL_Primaria_Fleteros}valesfletero.aspx?RepartosDetalleID=";

        public static readonly string URL_Fleteros_Pagina_NCFleterosList = $"{URL_Primaria_Fleteros}ncfleterosList.aspx?RepartosDetalleID=";       
        
        public static readonly string URL_Fleteros_Pagina_CierreRepartos = $"{URL_Primaria_Fleteros}ncdel.aspx?RepartosDetalleID=";


        public static string ObtenerApiPrimariaURL(string endpoint)
        {
            return $"{ApiURL_Primaria}{endpoint}";
        }       
        
        public static string ObtenerApiSecundariaURL(string endpoint)
        {
            return $"{ApiURL_Secundaria}{endpoint}";
        }

    }
}

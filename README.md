# AppVendedores2025

App para vendedores/repartidores en ruta: catálogo de productos, carga de pedidos, cuenta corriente de clientes, combos y seguimiento por GPS. La construí con **.NET MAUI multi-target (Blazor Hybrid)** para practicar un desarrollo móvil real, no solo una demo de escritorio.

## Modo demo

Uso un flag (`Parametros.ModoDemo = true`, en `Shared/Parametros.cs`) para deshabilitar las integraciones que requieren credenciales reales de terceros, mostrando un aviso en vez de ejecutar el flujo real:

- **MercadoPago** (checkout de pedidos en `VistaProductos.razor`, `CatalogoProductos.razor` y `SubCategorias.razor`, todos usan `MercadoPagoService`): al confirmar el pago se corta el flujo con un `DisplayAlert` — *"Pago no disponible en modo demo"* — antes de crear la preferencia de pago. El código de integración con MercadoPago (incluida la lógica de polling de estado de pago) sigue intacto, solo queda gateado por el flag.
- **WhatsApp** (`Components/Pages/MensajesARespnderWhatss.razor`): la página, a la que se navega cuando el backend reporta mensajes pendientes de WhatsApp Business, muestra *"No disponible en modo demo"* en vez del flujo real.

El flag vive en un único lugar y el código de cada integración no está borrado — solo tiene el desvío (`if (Parametros.ModoDemo) { ...; return; }`) al principio.

## GPS

El GPS no está mockeado: la app pide permiso de ubicación al sistema operativo y usa `Geolocation.Default.GetLocationAsync` (ver `Services/GPSService.cs`, `Services/LocationService.cs`, `Services/GpsTrackingService.cs` y las implementaciones específicas por plataforma en `Platforms/iOS/LocationService.cs` y `Platforms/Android/ForegroundServiceFlete.cs`). Al correr la app en un dispositivo/emulador real vas a ver el prompt de permisos nativo del sistema operativo.

## Requisitos para compilar

- Visual Studio 2022 (17.8+) con el workload **.NET MAUI** instalado, o bien:
  ```
  dotnet workload install maui
  ```
- El proyecto fija el SDK de .NET en `global.json` (`9.0.312`, `rollForward: latestFeature`).
- Targets definidos en el `.csproj`: `net9.0-android`, `net9.0-maccatalyst`, `net9.0-ios`, y (solo en Windows) `net9.0-windows10.0.19041.0`.

Para compilar el target Windows:
```
dotnet build -f net9.0-windows10.0.19041.0
```

El target Android puede depender de la versión de JDK instalada en la máquina de build — si tenés problemas ahí, priorizá el target Windows para probar que la app funciona.

## API a la que se conecta

Esta app no tiene base de datos propia en el servidor — usa SQLite localmente solo para cachear lo sincronizado. Todo el login, catálogo, clientes y pedidos vienen de:

- **`VendedoresApi-Demo`** → `http://localhost:5101` (ver [`../VendedoresApi-Demo`](../VendedoresApi-Demo), configurado en `Shared/Parametros.cs` vía `ApiURL_Primaria`). Hay que levantarla **antes** de abrir la app, o el login va a fallar.

## Credenciales

| Vendedor (VD) | Clave | EmpresaID |
|---|---|---|
| `V001` | `Demo123!` | `1` (se completa solo en el login) |

## Cómo ejecutar

1. Levantar primero **`VendedoresApi-Demo`**:
   ```bash
   cd ../VendedoresApi-Demo
   dotnet run --urls http://localhost:5101
   ```
   Dejarla corriendo (crea y siembra la base LocalDB sola, sin pasos manuales).
2. En otra terminal, restaurar y compilar el target Windows de la app:
   ```bash
   dotnet restore
   dotnet build -f net9.0-windows10.0.19041.0
   dotnet run --project AppVendedores2025.csproj -f net9.0-windows10.0.19041.0
   ```
   (o abrir la solución en Visual Studio 2022 con el workload **.NET MAUI** instalado y correr con F5 sobre el target Windows).
3. Loguearse con `V001` / `Demo123!` (el `EmpresaID` se completa solo). Tocar **Sincronizar** en la pantalla principal para bajar clientes, catálogo y combos — es un paso manual a propósito, no automático post-login.
4. Tocar un cliente para entrar a su catálogo — trae 8 clientes de prueba ubicados en lugares públicos de CABA (Obelisco, Plaza de Mayo, Recoleta, Puerto Madero, Caminito, Teatro Colón, Congreso, Planetario), 12 productos de almacén/bebidas/golosinas y 1 combo ("Combo Desayuno"). El checkout con MercadoPago y la pantalla de WhatsApp muestran el aviso de modo demo en vez de ejecutar la integración real.

Flujo verificado de punta a punta: login (JWT), sincronización manual, catálogo de clientes con GPS, catálogo de productos, pestaña de combos, agregar producto al carrito.

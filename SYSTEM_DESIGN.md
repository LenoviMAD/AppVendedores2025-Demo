# System Design & UX/UI — AppVendedores 2025

> Plataforma: .NET MAUI Blazor Hybrid · Frontend: Razor Components · Estado: `ParametrosServices` global + LocalStorage  
> Fecha análisis: 2026-05-21

---

## 1. Visión General

**AppVendedores 2025** es una aplicación móvil híbrida (MAUI Blazor) con dos perfiles de usuario:

| Perfil | Descripción |
|---|---|
| **Vendedor** | Representante comercial en campo. Gestiona clientes, toma pedidos en nombre de ellos y monitorea su performance (estrellas, comisiones). |
| **Cliente / Autogestión** | Cliente final con acceso a e-commerce propio: navega catálogo, arma carrito y paga vía Mercado Pago. |

La app funciona principalmente **offline-first**: sincroniza los datos al iniciar sesión y los almacena localmente. El envío de pedidos (transmisión) requiere conectividad puntual.

---

## 2. Arquitectura del Sistema

### 2.1 Capas

```
┌──────────────────────────────────────────────────────────┐
│                    UI Layer (Razor Components)            │
│  Pages · Modals · Sub-Components · Layout                │
├──────────────────────────────────────────────────────────┤
│                  Application Services Layer               │
│  ProductoListaService · PedidoCabeceraService            │
│  ClienteItemService · CombosItemService                  │
│  GPSService · MercadoPagoService · FuncionesSincronizar  │
├──────────────────────────────────────────────────────────┤
│                    Domain / Entities                      │
│  EntidadesAppVendedores (ClienteItem, ProductoListaItem, │
│  PedidoCabeceraItem, CombosHeadItem, etc.)               │
├──────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                     │
│  SQLite local DB · LocalStorage (JS Interop)             │
│  HTTP API (sync, transmisión) · GPS MAUI Plugin          │
│  Mercado Pago API · WhatsApp deep links                  │
└──────────────────────────────────────────────────────────┘
```

### 2.2 Flujo de Datos Principal

```
Inicio de sesión
     │
     ▼
ValidarVD() ──► API Server
     │
     ▼
FuncionesSincronizar ──► Descarga masiva (clientes, productos, combos, pedidos)
     │                    └► Guarda en SQLite local
     ▼
ParametrosServices (sesión global)
     │
     ├──► Vendedor: Dashboard → Clientes → VistaProductos → Carrito → Transmitir
     │
     └──► Cliente: VistaProductos / CatalogoProductos → Carrito → Mercado Pago
```

### 2.3 Estado Global (`ParametrosServices`)

Objeto singleton inyectado por DI que persiste la sesión activa:

| Campo | Propósito |
|---|---|
| `VendedorID` | ID del vendedor logueado |
| `ClienteID` | ID del cliente actualmente seleccionado |
| `ListadePrecios` | Lista activa (1=Distribución, 3=Autoservicio, 4=Salón, 7=E-commerce) |
| `TextoVD` | Nombre/código visible del vendedor |
| `TipoVD` | Tipo de vendedor (determina flujos habilitados) |

### 2.4 Persistencia Local

| Mecanismo | Usado para |
|---|---|
| **SQLite** | Clientes, productos, combos, pedidos, categorías (sync masivo) |
| **LocalStorage** | Plantillas de texto libre, mensajes leídos, configuración liviana |

---

## 3. Mapa de Páginas y Rutas

```
/                           → Index (Login → Dashboard)
├── /clientes               → Lista de clientes (solo Vendedor)
├── /pedidos                → Historial de pedidos
├── /catalogoProductos      → Grid de categorías
│   └── /subCategorias/{id} → Subcategorías de una categoría
│       └── /vistaProductos/{subcatID}/{filtro?} → Catálogo de productos
├── /mensajes               → Bandeja de entrada (mensajes de back-office)
├── /permisos               → Gestión de permiso GPS
├── /configApiKey           → Configuración API
├── /solicitudAlta          → Registro de vendedor
└── /solicitudAltaEcom      → Registro de cliente e-commerce
```

### Flujos de Navegación

```
Vendedor:
Login ──► Dashboard ──► Clientes ──► VistaProductos ──► (carrito inline) ──► Transmisión

Cliente:
Login ──► VistaProductos / CatalogoProductos ──► (carrito inline) ──► Mercado Pago
```

---

## 4. Componentes — Inventario Detallado

### 4.1 Layout & Routing

| Componente | Rol |
|---|---|
| `MainLayout.razor` | Shell vacío — solo renderiza `@Body` |
| `Routes.razor` | Router principal, aplica `MainLayout` por defecto |
| `_Imports.razor` | Usings globales (servicios, entities, JSInterop, i18n) |
| `LanguageSelect.razor` | Selector de idioma (i18n) |

### 4.2 Autenticación & Dashboard

| Componente | Rol | Líneas aprox. |
|---|---|---|
| `Index.razor` + `Index.razor.cs` | Controlador principal: login + dashboard | ~1,565 |
| `IndexLoginView.razor` | Vista del formulario de login | Subcomponente |
| `IndexDashboardView.razor` | Vista del dashboard post-login | Subcomponente |
| `ModalSoporte.razor` | Modal de soporte vía WhatsApp | Modal |
| `ModalActualizacionObligatoria.razor` | Bloqueo por versión desactualizada | Modal |
| `ModalPermisoGPS.razor` | Consentimiento de ubicación GPS | Modal |

### 4.3 Gestión de Clientes

| Componente | Rol |
|---|---|
| `Clientes.razor` | Lista filtrable de clientes con búsqueda debounce (250ms) |

### 4.4 Pedidos

| Componente | Rol |
|---|---|
| `Pedidos.razor` | Historial con collapse, estados con colores, reactivación |

### 4.5 Catálogo de Productos

| Componente | Rol | Líneas aprox. |
|---|---|---|
| `VistaProductos.razor` | Catálogo completo + carrito inline | ~1,900 |
| `VistaProductosHeader.razor` | Barra superior (búsqueda, badge carrito) | Subcomponente |
| `ModalCarrito.razor` | Carrito de compras con totales y checkout | Modal |
| `ModalProductoDetalle.razor` | Detalle de producto + qty + combos | Modal |
| `ModalComboDetalle.razor` | Constructor de combos con grupos | Modal |
| `ProcessingModal.razor` | Barra de progreso de pago (5 min timeout) | Modal |
| `CatalogoProductos.razor` | Grid de categorías (entry point alternativo) | Página |
| `MenuCategorias.razor` | Carrusel de categorías (Bootstrap, auto-slide 2.6s) | Componente |
| `SubCategorias.razor` | Grid 2 columnas de subcategorías | Página |

### 4.6 Mensajería y Configuración

| Componente | Rol |
|---|---|
| `Mensajes.razor` | Bandeja de mensajes de back-office (LocalStorage) |
| `Permisos.razor` | Solicitud de permiso GPS "Siempre" |
| `ConfigApiKey.razor` | Configuración de API key |
| `SolicitudAlta.razor` | Formulario de alta vendedor |
| `SolicitudAltaEcom.razor` | Formulario de alta cliente e-commerce |

---

## 5. Modelo de Dominio

### Entidades Core

```
ClienteItem
├── cli_clientesid       (PK)
├── cli_nombre
├── cli_codigo
├── cli_direccion
├── cli_porcentajepercepcionib
├── DiasNoVenta          (alertas >30 días)
├── Distancia            (calculada por GPS)
├── CUITValido           (modo CF/FD)
├── cli_MCE              (estado de cuenta)
├── cli_reba             (estado de transmisión)
└── Visistado

ProductoListaItem
├── ProductosID          (PK)
├── Nombre, Marca, CodigoDeProducto
├── ImagenWeb
├── PrecioUnitarioFinal
├── StockEnUnidades, DiasDeStockDisponible
├── CantidadEnCarrito    (estado UI)
├── UnidadesPorBulto
├── PrecioNoCalculado    (indica precio modificado)
├── IngresosRecientes, NuevaIncorporacoin
├── DiasDePrecioModificado
├── LstComboCliente      (combos asociados)
└── ComisionDiferencial

PedidoCabeceraItem
├── PedidoCabeceraID     (PK)
├── ClienteID → ClienteItem
├── FechaCarga, FechaDeEntrega
├── TotalPedido
├── ResultadoTx          (Pendiente/Enviado/Error)
├── PedidoCerrado        (bool)
├── KeyPagoID, PaymentID (Mercado Pago)
└── ClienteCodigo, ClienteDireccion (denormalizados)

PedidosDetalleItem
├── ProductoID → ProductoListaItem
├── CombosID → CombosHeadItem (opcional)
├── Cantidad, UnidadesPorBulto
├── PrecioUnitarioFinal
├── TotalLinea, TotalLineaNeto
└── NombreProducto, ImagenWeb (denormalizados)

CombosHeadItem
├── CombosID             (PK)
├── Codigo, Cantidad
├── CantidadDinamica     (grupos variables)
├── CantSCargo           (cant. sin cargo)
├── CantidadMaximaPorCliente, CantidadMaximaPorFactura
├── CantidadConCargo
└── GetCantidadesGrupo   (reglas de grupos)

CombosProductosItem
├── ComboProductoID      (PK)
├── ComboTipo            (ConCargo / SinCargo)
├── NrGrupoDinamico
├── Descuento1, Descuento2
├── TextoPrecioCalculado1-7 (por lista de precios)
└── ProductoListaItem    (producto asociado)
```

### Relaciones

```
ClienteItem ──< PedidoCabeceraItem ──< PedidosDetalleItem >── ProductoListaItem
                                                                     │
CombosHeadItem ──< CombosProductosItem >── ProductoListaItem         │
CombosXClientesItem >── CombosHeadItem                               │
CategoriaItem ──< SubCategoriasApiEcomItem ──< ProductoListaItem ────┘
```

---

## 6. Integraciones Externas

| Sistema | Uso | Componente |
|---|---|---|
| **API REST (back-office)** | Sync masivo, validación login, transmisión de pedidos | `FuncionesSincronizar`, `PedidoCabeceraService` |
| **Mercado Pago** | Pago e-commerce (preferencia + polling resultado) | `MercadoPagoService`, `ProcessingModal` |
| **GPS (MAUI Plugin)** | Ordenar clientes por distancia, ubicación de entrega | `GPSService` |
| **WhatsApp** | Soporte técnico + comunicación con clientes (tipo 5) | Deep links `wa.me/` |
| **Google Maps** | Cómo llegar al cliente (tipo 5) | Deep links `maps.google.com` |
| **Google Play / App Store** | Actualización obligatoria | `ModalActualizacionObligatoria` |

---

## 7. Listas de Precios

| ID | Nombre | Perfil |
|---|---|---|
| 1 | Distribución | Vendedor |
| 3 | Autoservicio | Vendedor |
| 4 | Salón | Vendedor |
| 7 | E-commerce | Cliente autogestión |

El vendedor puede cambiar la lista activa desde el dashboard. El precio mostrado y los cálculos de carrito se adaptan dinámicamente.

---

## 8. Sistema de Combos

Los combos tienen dos tipos de ítems:

- **Con Cargo (CC):** Productos que se pagan (con descuento aplicado).
- **Sin Cargo (SC):** Productos bonificados que se "ganan" al completar el combo.

Validación en `ModalComboDetalle`:
1. Verificar que cada grupo CC esté completamente seleccionado.
2. Verificar que los grupos SC cumplan la cantidad requerida.
3. Respetar `CantidadMaximaPorCliente` y `CantidadMaximaPorFactura`.

---

## 9. UX/UI — Análisis y Patrones

### 9.1 Principios Observados

| Principio | Implementación |
|---|---|
| **Mobile-first** | Grid 6 columnas en producto, navegación inferior fija, modales bottom-sheet |
| **Offline-first** | Sync al inicio, operación local, transmisión diferida |
| **Progressive disclosure** | Categoría → Subcategoría → Producto → Detalle → Carrito |
| **Feedback inmediato** | Badges de carrito, progress bars, colores de estado |
| **Dual-persona** | Misma codebase, flujos completamente diferentes por tipo de usuario |

### 9.2 Flujo Principal — Vendedor

```
┌─────────────┐
│   LOGIN     │  Usuario + contraseña
└──────┬──────┘
       │ ValidarVD() + Sincronización
       ▼
┌─────────────────────────────────────────────────────┐
│                    DASHBOARD                         │
│  ⭐⭐⭐⭐⭐⭐  Estrellas de performance             │
│  Coeficiente de comisión (color verde/rojo)         │
│  [X] Clientes sin pedido   [Y] Pedidos sin TX       │
│  [Clientes] [Pedidos] [Sincronizar] [Soporte]       │
└──────┬──────────────────────────────────────────────┘
       │
       ▼
┌─────────────┐    búsqueda     ┌─────────────────┐
│  CLIENTES   │ ──────────────► │ VISTA PRODUCTOS │
│  (lista)    │  selecciona     │ (catálogo)      │
└─────────────┘  cliente        └────────┬────────┘
                                         │ agrega al carrito
                                         ▼
                                ┌─────────────────┐
                                │  MODAL CARRITO  │
                                │  Fecha entrega  │
                                │  [Transmitir]   │
                                └─────────────────┘
```

### 9.3 Flujo Principal — Cliente E-commerce

```
┌─────────────┐
│   LOGIN     │
└──────┬──────┘
       ▼
┌─────────────────────────────────────────┐
│   CATÁLOGO DE PRODUCTOS / CATEGORÍAS    │
│   Barra inferior: ♡ Más vendido         │
│                   📁 Catálogo           │
│                   ✨ Nuevos             │
│                   👤 Mi cuenta          │
└──────────┬──────────────────────────────┘
           │ navega
           ▼
┌─────────────────────┐    ┌──────────────────────┐
│  SUBCATEGORÍAS      │───►│  VISTA PRODUCTOS     │
│  (grid 2 cols)      │    │  (grid 6 cols)       │
└─────────────────────┘    └──────────┬───────────┘
                                      │ agrega
                                      ▼
                           ┌──────────────────────┐
                           │   MODAL CARRITO      │
                           │   Percepciones (imp) │
                           │   [Realizar Pago]    │
                           └──────────┬───────────┘
                                      │
                                      ▼
                           ┌──────────────────────┐
                           │  MERCADO PAGO        │
                           │  ProcessingModal     │
                           │  (polling 5 min)     │
                           └──────────────────────┘
```

### 9.4 Componentes de UI — Patrones Visuales

#### Tarjeta de Cliente (Clientes.razor)
```
┌──────────────────────────────────────────────────┐
│ 🔒 [CLIENTE NOMBRE]           Cód: 00123         │
│    Av. Corrientes 1234, CABA                      │
│    IIBB: 2.5%    📍 3.2 km    ⏱ 45 días         │
│    [🗺 Maps]  [💬 WhatsApp]                       │
└──────────────────────────────────────────────────┘
  Color borde: amarillo/naranja/verde/blanco (stock)
```

#### Tarjeta de Pedido (Pedidos.razor)
```
┌──────────────────────────────────────────────────┐
│ Cód: 00123 · Av. Corrientes 1234      [$12,500]  │
│ Cargado: 20/05/2026         [● PENDIENTE]        │
│                                        [Ver Info]│
│ ▼ (expandido)                                    │
│   • 3x Producto A                                │
│   • 2x Producto B                                │
│   [Modificar] [Reactivar] [Borrar]               │
└──────────────────────────────────────────────────┘
  Estados: blanco (ingresado) · amarillo (pendiente)
           verde (enviado OK) · rojo (error)
```

#### Tarjeta de Producto (VistaProductos.razor)
```
┌────────────────┐
│   [imagen]     │ ← Badge: "Últimas Unidades" / "Nuevo" / "Precio Mod."
│                │          "Ingreso Reciente" / "Combo" / "Destacado"
│ $1,250         │
│ ~~$1,500~~ CC  │ ← Precio tachado si hay combo
│ [Comprar]      │
└────────────────┘
```

#### Modal de Carrito
```
┌────────────────────────────────────────────────────┐
│ CARRITO                                      [✕]  │
├────────────────────────────────────────────────────┤
│ Producto A     3 uds    [-][3][+]    $3,750  [🗑] │
│ Producto B     1 bto    [-][1][+]    $2,500  [🗑] │
│ COMBO XYZ      2 x      [-][2][+]    $5,000  [🗑] │
├────────────────────────────────────────────────────┤
│ Subtotal neto:                           $11,250   │
│ Percepción IIBB 2.5%:                      $281   │
│ TOTAL:                                   $11,531   │
├────────────────────────────────────────────────────┤
│ Entrega: [Lun 25/05] ▼                            │
│                              [TRANSMITIR PEDIDO]  │
└────────────────────────────────────────────────────┘
```

### 9.5 Navegación Inferior (Bottom Nav) — Catálogo

```
┌──────────────────────────────────────────────────┐
│  [♡ Me gusta] [🔥 Más vendido] [📁 Catálogo]    │
│  [✨ Nuevos]  [👤 Mi cuenta]                     │
└──────────────────────────────────────────────────┘
```

Cada tab mapea a una ruta de `vistaProductos` con subcategorías especiales (IDs 100002, 100003, 100004).

### 9.6 Dashboard — Indicadores de Performance (Vendedor)

```
┌───────────────────────────────────────────────────┐
│  ¡Bienvenido, Juan!                               │
│                                                   │
│  Desempeño:  ⭐ ⭐ ⭐ ⭐ ☆ ☆  (4/6 estrellas)    │
│  Comisión:   +2.3%  ▲  (color verde)             │
│                                                   │
│  Clientes sin pedido:  [  5  ]                   │
│  Pedidos sin TX:       [  2  ]                   │
│                                                   │
│  Lista de precios: [Distribución ▼]              │
│                                                   │
│  [Clientes]  [Pedidos]  [Ingresar]               │
│  [Sincronizar]  [Soporte]  [Salir]               │
└───────────────────────────────────────────────────┘
```

---

## 10. Sistema de Filtros y Ordenamiento (VistaProductos)

### Filtros de ordenamiento disponibles:

| Opción | Descripción |
|---|---|
| Más Vendido (default) | Ordenamiento por volumen histórico |
| Precio Modificado | Productos con precio cambiado recientemente |
| Últimas Unidades | Stock bajo |
| Ingreso Reciente | Lotes ingresados recientemente |
| Nuevos Productos | Nuevas incorporaciones al catálogo |
| Combos Destacados | Combos activos |
| Precio Menor a Mayor | Ascendente por precio |
| Precio Mayor a Menor | Descendente por precio |
| A-Z | Alfabético |

### Filtros adicionales:
- **Marca**: dropdown con todas las marcas disponibles
- **Texto libre**: búsqueda debounce en tiempo real (con race condition prevention)
- **Subcategoría**: filtrado por subcategoría seleccionada en navegación

---

## 11. Mensajería y Comunicación

### 11.1 Mensajes de Back-Office (Mensajes.razor)
- Se sincronizan como `MensajeAVendedorItem` desde el servidor
- Se marcan como leídos en LocalStorage (`MensajesLeidos[]`)
- Polling periódico (`ConsultarMensajesNuevos`) con contador en dashboard

### 11.2 Comunicación con Clientes (Vendedor tipo 5)
- Generación de links WhatsApp deep link (`wa.me/`)
- Plantillas de texto libre guardadas en LocalStorage
- Link de Google Maps para navegación al cliente

### 11.3 Soporte Técnico
- `ModalSoporte` genera link WhatsApp pre-armado con:
  - Código de vendedor
  - Versión de la app
  - Fecha de última sincronización
  - Email alternativo de soporte

---

## 12. Seguridad y Permisos

| Aspecto | Implementación |
|---|---|
| Autenticación | `ValidarVD()` contra API, sin tokens explícitos visibles |
| Sesión | `ParametrosServices` en memoria (no persiste entre reinicios) |
| GPS | Consentimiento explícito via `ModalPermisoGPS`, solicita permiso "Siempre" |
| Versión | `AppVersionService` bloquea login si versión < requerida |
| Eliminación de cuenta | Flujo dedicado con confirmación en `IndexDashboardView` |

---

## 13. Localización (i18n)

- Sistema de recursos `AppResources` vía `IStringLocalizer`
- Namespaces: `AppVendedores2025.Localization`, `AppVendedores2025.Resources.Strings`
- Componente `LanguageSelect.razor` para cambio de idioma en runtime

---

## 14. Deuda Técnica y Observaciones

| Área | Observación |
|---|---|
| `Index.razor` | ~1,565 líneas — candidato a mayor descomposición (ya parcialmente refactorizado con `IndexComponents/`) |
| `VistaProductos.razor` | ~1,900 líneas — componente más complejo del sistema |
| `MainLayout.razor` | Shell mínimo, sin navigation drawer ni toolbar global |
| `Home.razor` | Placeholder vacío, ruta `/xxxx` no productiva |
| `ConfigApiKey.razor` | Sin contenido visible en análisis, posiblemente en desarrollo |
| Estado global | `ParametrosServices` singleton sin manejo formal de errores de sesión expirada |
| Pago MP | Polling de 5 minutos en `ProcessingModal` — sensible a conectividad intermitente |

---

## 15. Diagrama de Componentes

```
App
└── Routes.razor
    └── MainLayout.razor
        ├── Index.razor (/)
        │   ├── ModalPermisoGPS.razor
        │   ├── ModalActualizacionObligatoria.razor
        │   ├── IndexComponents/
        │   │   ├── IndexLoginView.razor
        │   │   ├── IndexDashboardView.razor
        │   │   └── ModalSoporte.razor
        │
        ├── Clientes.razor (/clientes)
        │
        ├── Pedidos.razor (/pedidos)
        │
        ├── CatalogoProductos.razor (/catalogoProductos)
        │   └── MenuCategorias.razor
        │
        ├── SubCategorias.razor (/subCategorias/{id})
        │   └── MenuCategorias.razor
        │   └── [Modals compartidos de VistaProductos]
        │
        ├── VistaProductos.razor (/vistaProductos/...)
        │   ├── VistaProductosComponents/
        │   │   ├── VistaProductosHeader.razor
        │   │   ├── ModalCarrito.razor
        │   │   ├── ModalProductoDetalle.razor
        │   │   ├── ModalComboDetalle.razor
        │   │   └── ProcessingModal.razor
        │   └── MenuCategorias.razor
        │
        ├── Mensajes.razor (/mensajes)
        ├── Permisos.razor (/permisos)
        ├── ConfigApiKey.razor (/configApiKey)
        ├── SolicitudAlta.razor
        └── SolicitudAltaEcom.razor
```

---

---

## 16. Design System — Colores, Tipografía y Estilos Visuales

### 16.1 Paleta de Colores Primarios

La identidad visual se basa en una gama de **azules corporativos** como color principal, con semántica clara por estado (rojo=error, verde=ok, amarillo=advertencia).

| Rol | Color | Hex | Uso principal |
|---|---|---|---|
| **Brand Primary** | ![#09388B](https://via.placeholder.com/12/09388B/09388B.png) Azul oscuro | `#09388B` | Botones principales, headers, logo |
| **Brand Secondary** | ![#165DC9](https://via.placeholder.com/12/165DC9/165DC9.png) Azul medio | `#165DC9` | Acentos, links activos, gradientes |
| **Brand Deep** | ![#002b62](https://via.placeholder.com/12/002b62/002b62.png) Navy | `#002b62` | Dashboard cards, fondos de panel |
| **Success** | ![#22c55e](https://via.placeholder.com/12/22c55e/22c55e.png) Verde | `#22c55e` | Pedidos enviados, estados OK |
| **Success Alt** | ![#27ae60](https://via.placeholder.com/12/27ae60/27ae60.png) Verde oscuro | `#27ae60` | Badges de éxito, gradientes |
| **Danger** | ![#ef4444](https://via.placeholder.com/12/ef4444/ef4444.png) Rojo | `#ef4444` | Errores de pedido, alertas críticas |
| **Danger Alt** | ![#c0392b](https://via.placeholder.com/12/c0392b/c0392b.png) Rojo oscuro | `#c0392b` | Mensajes de error, modales de alerta |
| **Warning** | ![#f59e0b](https://via.placeholder.com/12/f59e0b/f59e0b.png) Ámbar | `#f59e0b` | Pedidos pendientes, advertencias |
| **Warning Alt** | ![#ffc107](https://via.placeholder.com/12/ffc107/ffc107.png) Amarillo | `#ffc107` | Badges de advertencia (Bootstrap) |
| **Price Strike** | ![#f81404](https://via.placeholder.com/12/f81404/f81404.png) Rojo vivo | `#f81404` | Precio tachado en combos |

### 16.2 Colores Neutros

| Rol | Hex | Uso |
|---|---|---|
| Fondo claro 1 | `#f8fafc` | Fondo general de páginas |
| Fondo claro 2 | `#f0f4f8` | Grid de productos, secciones |
| Fondo claro 3 | `#e8eef8` | Fondos de tarjetas, chips |
| Borde estándar | `#d1d5db` | Inputs, separadores |
| Borde suave | `#e8eef8` | Cards, panels |
| Texto principal | `#1f2937` | Cuerpo de texto |
| Texto secundario | `#374151` | Labels, sublabels |
| Texto muted | `#6b7280` | Metadata, descripciones |
| Texto deshabilitado | `#9aabca` | Placeholders, disabled |
| Placeholder | `#9ca3af` | Inputs vacíos |
| Blanco | `#ffffff` | Tarjetas, modales |

### 16.3 Gradientes del Sistema

```css
/* Gradiente principal (botones, barras de carga) */
linear-gradient(90deg, #09388B 0%, #165DC9 100%)

/* Gradiente dashboard (cards principales) */
linear-gradient(135deg, #002b62 0%, #09388B 100%)

/* Gradiente card peligro (cuentas bloqueadas, errores) */
linear-gradient(135deg, #7b1a1a 0%, #c0392b 100%)

/* Gradiente combo/descuento */
linear-gradient(90deg, #b45309 0%, #f59e0b 60%, #fbbf24 100%)

/* Gradiente categorías */
linear-gradient(135deg, #1758c4 0%, #0a3d99 100%)

/* Gradiente modal actualización obligatoria */
linear-gradient(90deg, #002b62, #165DC9, #09388B)

/* Gradiente product detail hero */
rgba(9,30,80,0.88) → transparent
```

### 16.4 Colores Semánticos de Estado — Pedidos

Los estados de pedido usan pares bg/text con bajo contraste para chips legibles:

| Estado | Fondo | Texto | Clase CSS |
|---|---|---|---|
| Ingresado | `#dbeafe` | `#1d4ed8` | `.ped_ingresado` |
| Pendiente | `#fef3c7` | `#b45309` | `.ped_pendiente` |
| Enviado OK | `#dcfce7` | `#15803d` | `.ped_enviado` |
| Error | `#fee2e2` | `#b91c1c` | `.ped_error` |

### 16.5 Colores de Grupos de Combo (ModalComboDetalle)

Los 10 grupos visuales distinguen ítems dentro de un combo:

| Grupo | Color de fondo | Hex |
|---|---|---|
| grupo0 | Blanco (sin grupo) | `white` |
| grupo1 | Cyan claro | `#8fe3ff` |
| grupo2 | Verde claro | `#9bfab0` |
| grupo3 | Durazno suave | `#FAE5D3` |
| grupo4 | Gris claro | `#F2F3F4` |
| grupo5 | Lavanda | `#E8DAEF` |
| grupo6-9 | Marrón | `chocolate` |
| grupoSC (sin cargo) | Rosa claro | `#FADBD8` |

### 16.6 Colores de Navegación Inferior

| Tab | Ícono | Color |
|---|---|---|
| Me gusta | ♥ Corazón | `#e53e3e` (rojo) |
| Más vendido | 🔥 Fuego | `#dd6b20` (naranja) |
| Catálogo | 📁 Catálogo | `#165DC9` (azul) |
| Nuevos | ✨ Estrella | `#d69e2e` (dorado) |
| Mi cuenta | 👤 Usuario | `#805ad5` (violeta) |

### 16.7 Badges de Producto

| Badge | Fondo | Texto | Significado |
|---|---|---|---|
| Últimas Unidades | `#ffc107` | `#7b4a00` | Stock crítico |
| Precio Modificado | `#165DC9` | `white` | Precio cambiado |
| Nuevo | `#38a169` | `white` | Nueva incorporación |
| Ingreso Reciente | `#805ad5` | `white` | Ingreso reciente |
| Destacado | `#e53e3e` | `white` | Producto destacado |
| Combo | `#dd6b20` | `white` | Tiene combo activo |

### 16.8 Sombras

```css
/* Sombra suave (cards, inputs) */
box-shadow: 0 2px 8px rgba(9, 56, 139, 0.08);

/* Sombra media (modales, dropdowns) */
box-shadow: 0 4px 12px rgba(0, 43, 98, 0.35);

/* Sombra fuerte (overlays, popups) */
box-shadow: 0 12px 35px rgba(0, 0, 0, 0.15);
```

---

### 16.9 Tipografía

#### Font Stack

| Nivel | Familia | Fallback |
|---|---|---|
| **Primaria (UI general)** | `Inter` | `Segoe UI`, `system-ui`, `sans-serif` |
| **Secundaria (formularios)** | `Source Sans Pro` | `sans-serif` |
| **Alertas de stock** | `Century Gothic` | `sans-serif` |
| **Mensajes de vendedor** | `Noto Serif` | `serif` |
| **Código / técnico** | `SFMono-Regular`, `Menlo` | `Consolas`, `monospace` |
| **Fallback universal** | `Helvetica Neue`, `Helvetica`, `Arial` | `sans-serif` |

#### Escala Tipográfica

| Rol | Tamaño | Peso | Uso |
|---|---|---|---|
| Título hero | `2rem – 2.5rem` | 700 | Pantalla login, títulos principales |
| Título de página | `1.25rem – 1.5rem` | 600 | Headers de sección |
| Subtítulo | `1rem – 1.1rem` | 600 | Nombre de cliente, producto |
| Navegación | `18px – 22px` | 500 | Íconos de nav inferior |
| Cuerpo | `13px – 15px` | 400 | Descripciones, listas |
| Label / chip | `11px – 14px` | 500 | Badges, chips de estado |
| Badge / micro | `9px – 12px` | 600 | Contadores de carrito, micro-labels |

---

### 16.10 Componentes Visuales — Referencia Rápida

#### Spinner / Loading

```
Fondo:    linear-gradient(135deg, #09388B → #165DC9)
Spinner:  border rgba(255,255,255,0.25) + top #ffffff
Texto:    white (rgba distintos para jerarquía)
```

#### Modal de Actualización Obligatoria

```
Overlay:  rgba(0, 20, 55, 0.93)
Barra:    linear-gradient(90deg, #002b62, #165DC9, #09388B)
Botón:    linear-gradient(135deg, #09388B → #165DC9 → #2563eb)
Badge:    rgba(123,26,26,0.08) fondo · #c0392b texto
```

#### Tarjetas de Clientes

```
Código:          bg #e8eef8  · text #09388B
IIBB:            bg #fff3cd  · text #7b4a00
Visita oblig.:   bg rgba(255,193,7,0.9) · text #7a5000
Alerta ventas:   bg rgba(250,50,68,0.85) · text white
Bloqueado:       text #c0392b · ícono 🔒
```

#### Inputs / Forms

```
Borde:        #d1d5db (default) → #0d47a1 (focus)
Sombra focus: rgba(13, 71, 161, 0.08)
Fondo:        #ffffff (activo) · #f1f5f9 (disabled)
Texto:        #1f2937
Placeholder:  #9ca3af
```

---

*Documento generado por análisis estático de componentes — AppVendedores 2025 Antigraviti*

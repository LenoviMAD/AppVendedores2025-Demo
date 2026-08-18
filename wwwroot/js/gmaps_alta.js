let gmaps_autocomplete_sai = null;
let gmaps_alta_map = null;
let gmaps_alta_marker = null;

window.initAltaGoogleMaps = function (dotNetHelper, inputId, mapId) {
    var map_id_elem = document.getElementById(mapId);
    if (!map_id_elem) return;

    // Inicializar mapa vacío (Buenos Aires centro/sur como fallback)
    gmaps_alta_map = new google.maps.Map(map_id_elem, {
        center: { lat: -34.603722, lng: -58.381592 },
        zoom: 12,
        disableDefaultUI: true,
    });

    const input_sai = document.getElementById(inputId);
    if (!input_sai) return;

    gmaps_autocomplete_sai = new google.maps.places.Autocomplete(input_sai);
    gmaps_autocomplete_sai.bindTo("bounds", gmaps_alta_map);

    gmaps_alta_marker = new google.maps.Marker({
        map: gmaps_alta_map,
        anchorPoint: new google.maps.Point(0, -29),
        draggable: false // No editable por el usuario
    });
    gmaps_alta_marker.setVisible(false);

    // Evitar que al presionar Enter en el Autocomplete se submita el formulario Blazor
    input_sai.addEventListener('keydown', function(event) {
        if (event.key === 'Enter') {
            event.preventDefault();
        }
    });

    gmaps_autocomplete_sai.addListener("place_changed", () => {

        // ─── Referencia de propiedades útiles del objeto `place` (Google Place) ───────
        //
        // place.formatted_address     → Dirección completa formateada como string legible.
        //                               Ej: "Av. Corrientes 1234, Buenos Aires, Argentina"
        //
        // place.address_components    → Array con las partes individuales de la dirección.
        //                               Cada componente tiene: long_name, short_name, types[].
        //                               Types útiles: "route" (calle), "street_number" (número),
        //                               "locality" (ciudad/localidad), "administrative_area_level_1" (provincia),
        //                               "country" (país), "postal_code" (código postal).
        //
        // place.geometry.location     → Objeto LatLng. Usar .lat() y .lng() para obtener coordenadas.
        // place.geometry.viewport     → Bounding box recomendado para hacer fitBounds().
        //
        // place.place_id              → Identificador único del lugar en Google Maps.
        //                               Útil para consultas posteriores a la Places API.
        //
        // place.name                  → Nombre del lugar tal como lo muestra Google.
        //                               Para direcciones suele ser la calle + número.
        //
        // place.types                 → Array de tipos del lugar. Ej: ["street_address"], ["establishment"].
        //                               Permite distinguir si es un domicilio, negocio, ruta, etc.
        //
        // place.plus_code             → Código global de ubicación (Open Location Code).
        //                               Útil en zonas sin dirección postal estándar.
        //
        // place.utc_offset_minutes    → Diferencia horaria del lugar respecto a UTC.
        //
        // place.business_status       → Estado comercial. Valores: "OPERATIONAL", "CLOSED_TEMPORARILY",
        //                               "CLOSED_PERMANENTLY". Solo disponible para establecimientos.
        //
        // place.formatted_phone_number → Teléfono del lugar formateado localmente.
        //                               Solo disponible si el lugar es un establecimiento con datos enriquecidos.
        //
        // place.website               → URL del sitio web oficial del lugar (si existe).
        //
        // place.opening_hours         → Objeto con horarios de apertura/cierre.
        //                               Propiedades: open_now (bool), periods[], weekday_text[].
        //
        // place.photos                → Array de fotos asociadas al lugar.
        //                               Cada foto tiene: getUrl(), width, height, html_attributions.
        //
        // place.icon                  → URL del ícono representativo del tipo de lugar.
        //
        // ──────────────────────────────────────────────────────────────────────────────

        const place = gmaps_autocomplete_sai.getPlace();

        // Validación: place vacío o sin datos de geometría
        if (!place || !place.geometry || !place.geometry.location) {
            console.warn("gmaps_alta: place_changed sin geometría válida.", place);
            return;
        }

        // Dirección completa para mostrar en el diálogo y enviar al modelo
        const exactAddress = place.formatted_address || place.name || input_sai.value;

        // ─── Extraer Código Postal desde address_components ──────────────────────────
        let codigoPostal = "";

        if (place.address_components && Array.isArray(place.address_components)) {
            // Buscar el componente de tipo "postal_code"
            const postalComponent = place.address_components.find(
                (comp) => comp.types && comp.types.includes("postal_code")
            );
            if (postalComponent) {
                // Preferimos long_name; short_name es equivalente para códigos postales
                codigoPostal = postalComponent.long_name || postalComponent.short_name || "";
            }
        }
        // Si Google no devuelve código postal para la dirección, codigoPostal queda ""
        // y el backend lo recibirá vacío sin romper el flujo.
        // ─────────────────────────────────────────────────────────────────────────────

        Swal.fire({
            title: '¿Confirmar dirección?',
            text: exactAddress,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, es correcta',
            cancelButtonText: 'No, corregir',
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#d33',
            backdrop: `rgba(0,0,0,0.4)`
        }).then((result) => {
            if (result.isConfirmed) {
                gmaps_alta_marker.setVisible(false);

                // Ajustar el mapa al viewport del lugar o centrar con zoom fijo
                if (place.geometry.viewport) {
                    gmaps_alta_map.fitBounds(place.geometry.viewport);
                } else {
                    gmaps_alta_map.setCenter(place.geometry.location);
                    gmaps_alta_map.setZoom(15);
                }

                gmaps_alta_marker.setPosition(place.geometry.location);
                gmaps_alta_marker.setVisible(true);

                // Obtener coordenadas como strings para el modelo Blazor
                const lat = place.geometry.location.lat();
                const lng = place.geometry.location.lng();

                // Invocar el método Blazor que incluye el código postal.
                // SetCoordenadasDireccionConPostal recibe: lat, lng, dirección, código postal.
                // Si el método nuevo no existiera en el componente, caer en el método original.
                const hasPostalMethod = dotNetHelper._invokeMethodAsync !== undefined
                    || typeof dotNetHelper.invokeMethodAsync === 'function';

                dotNetHelper.invokeMethodAsync(
                    'SetCoordenadasDireccionConPostal',
                    lat.toString(),
                    lng.toString(),
                    exactAddress,
                    codigoPostal
                ).catch(() => {
                    // Fallback: si el método con postal no existe, usamos el original de 3 params
                    dotNetHelper.invokeMethodAsync(
                        'SetCoordenadasDireccion',
                        lat.toString(),
                        lng.toString(),
                        exactAddress
                    );
                });

            } else {
                // Si no confirma, limpiamos el input para obligar a seleccionar de nuevo
                input_sai.value = "";
                gmaps_alta_marker.setVisible(false);
            }
        });
    });
};

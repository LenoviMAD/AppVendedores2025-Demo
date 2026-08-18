//var latCli = -34.738271087511215;
//var lonCli = -34.738271087511215;
//var latVend = -34.74000093724406;
//var lonVend = -58.34078363148795;
var linkGMapsCliente = "";
var linkGmapVendedor = "";
//var latCli = 0;
//var lonCli = 0;
//var latVend = 0;
//var lonVend = 0;
var currentInfoWindow = null;

//function getLocation() {

//	//alert("getLocation");
//	if (navigator.geolocation) {
//		navigator.geolocation.getCurrentPosition(showPosition, error);

//	} else {
//		alert("Geolocation no esta soportada por su telefono.");
//		document.getElementById("Label1").value = "Geolocation is not supported by this browser.";

//	}
//}

//function showPosition(position) {

//	latVend = position.coords.latitude;
//	lonVend = position.coords.longitude;

//	latCli = 0;// document.getElementById('<%=txtLatiudCliente.ClientID %>').value.replace(',', '.');
//	lonCli = 0; document.getElementById('<%=txtLongitidCliente.ClientID %>').value.replace(',', '.');

//	//----armar links
//	linkGMapsCliente = "https://maps.google.com/?q=" + latCli + "," + lonCli;
//	linkGmapVendedor = 'https://maps.google.com/?q=' + position.coords.latitude + ',' + position.coords.longitude;


//}

//function showMapparaVendedor(latCli, lonCli, latVend, lonVend) {
//	posVendedor = [lonVend, latVend];
//	posCliente = [lonCli, latCli];

//	const mapPadding = 80;

//	var fitBounds = function () {
//		let bounds = new mapboxgl.LngLatBounds();
//		bounds.extend(posVendedor);
//		bounds.extend(posCliente);
//		map1.fitBounds(bounds, { padding: mapPadding });
//	}

//	var view = function () {
//		fitBounds();
//		mark2.setLngLat(posVendedor);
//	}


//	{
//		mapboxgl.accessToken = 'pk.eyJ1IjoiZGFuaWVsd3d3IiwiYSI6ImNsZnRwajhvcjAzM3gzcmxkMDVkanFodG0ifQ.NZ98Do4EsBYHg1tCnEmuUw'

//		var map1 = new mapboxgl.Map({
//			container: 'map1', // container id
//			style: 'mapbox://styles/mapbox/streets-v11', // style URL
//		});


//		map1.on('load', function () {
//			//if (tipo == "foto")
//			{
//				//agregar recorrido
//				var directions1 = new MapboxDirections({
//					accessToken: mapboxgl.accessToken,
//					unit: 'metric',
//					profile: 'mapbox/driving',
//					interactive: false,
//					controls: {
//						inputs: false,
//						instructions: false,
//						profileSwitcher: false
//					}
//				});

//				map1.addControl(directions1);

//				directions1.setOrigin(posVendedor);

//				directions1.setDestination(posCliente);
//			}
//			showmarkers();
//		});


//		function showmarkers() {
//			fitBounds();

//			var elCliente = document.createElement('div');
//			elCliente.className = 'markerCliente';
//			new mapboxgl.Marker(elCliente)
//				.setLngLat(posCliente)
//				.addTo(map1);

//			var elVendedor = document.createElement('div');
//			elVendedor.className = 'markerVendedor';
//			mark2 = new mapboxgl.Marker(elVendedor)
//				.setLngLat(posVendedor)
//				.addTo(map1);
//			contRefresh = 0;

//		}
//	}

//}

function AbrirModalMapa(latCli, lonCli, latVend, lonVend) {

	showMapparaVendedor(latCli, lonCli, latVend, lonVend);

	$('#gMapsModal').modal('show');


	$(".btn-close").click(function () {
		$("#gMapsModal").modal('hide');
		var gMapsModal = $("#map1");
		gMapsModal.empty();


	});
	$('#gMapsModal').on('hidden.bs.modal', function () {
		var gMapsModal = $("#map1");
		gMapsModal.empty();
	})
}



function getLocation() {
	if (navigator.geolocation) {
		navigator.geolocation.getCurrentPosition(showPosition);
	} else {
		alert("Geolocalización no es soportada por este navegador.");
	}
}

function showPosition(position) {
	alert("Tu ubicación actual es: " + position.coords.latitude + ", " + position.coords.longitude);
}






//getLocation();

function ObtenerUbicacion() {
	return new Promise(function (resolve, reject) {
		if (navigator.geolocation) {
			navigator.geolocation.getCurrentPosition(function (position) {
				// Convertir la posición a un objeto JSON
				var ubicacion = {
					latitud: position.coords.latitude,
					longitud: position.coords.longitude
				};
				console.log("VD: "+position.coords.latitude + "," + position.coords.longitude)
				resolve(JSON.stringify(ubicacion));
			}, function (error) {
				reject(error.message);
			});
		} else {
			reject("Geolocalización no es soportada por este navegador.");
		}
	});
}



function initMap(lsClientes, posVD) {

	$('#gMapsModal').modal('show');


	$(".btn-close").click(function () {
		$("#gMapsModal").modal('hide');
		var gMapsModal = $("#map1");
		gMapsModal.empty();


	});









	// Crea un objeto de mapa y establece la ubicación y el zoom
	var map = new google.maps.Map(document.getElementById('mapgoogle'), {
		center: { lat: -lsClientes[0].cli_latitud, lng: -lsClientes[0].cli_longitud },
		zoom: 12
	});

	var customIcon = {
		url: 'https://api.empresademo.example/mpagoapp/css/img/yo.png',
		scaledSize: new google.maps.Size(64, 64)
	};

	//var markers = [];

	// Tu código foreach aquí
	lsClientes.forEach(function (element) {
		var poscliente = { lat: -element.cli_latitud, lng: -element.cli_longitud };

		var marker = new google.maps.Marker({
			position: poscliente,
			map: map,
		
			title: element.cli_codigo + "\n" + element.cli_nombre
		});
		var infowindow = new google.maps.InfoWindow({
			content: marker.getTitle()
		});

		marker.addListener('click', function () {
			if (currentInfoWindow != null) {
				currentInfoWindow.close();
			}
			infowindow.open(map, marker);
			currentInfoWindow = infowindow;
		});

	});

	try {
		posVD.latitud = posVD.latitud.tostring().replace(",", ".");
		posVD.longitud = posVD.longitud.tostring().replace(",", ".");
	} catch { }
	var posvd = { lat: posVD.latitud, lng: posVD.longitud };
	var marker = new google.maps.Marker({
		position: posvd,
		icon: customIcon,
		map: map
		
	});


}
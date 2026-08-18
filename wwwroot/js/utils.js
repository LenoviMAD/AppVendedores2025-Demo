window.initInfiniteScroll = function (dotNetRef) {
    var sentinel = document.getElementById('infinite-scroll-sentinel');
    if (!sentinel || !('IntersectionObserver' in window)) return;

    if (window._infiniteScrollObserver) {
        window._infiniteScrollObserver.disconnect();
    }

    window._infiniteScrollObserver = new IntersectionObserver(function (entries) {
        if (entries[0].isIntersecting) {
            dotNetRef.invokeMethodAsync('CargarMasProductos');
        }
    }, { rootMargin: '300px' });

    window._infiniteScrollObserver.observe(sentinel);
};

window.destroyInfiniteScroll = function () {
    if (window._infiniteScrollObserver) {
        window._infiniteScrollObserver.disconnect();
        window._infiniteScrollObserver = null;
    }
};

function callCSharpMethod() {
    DotNet.invokeMethodAsync('AppVendedores', 'HandleBackButton')
        .then(() => {
            console.log('Método C# llamado con éxito');
        })
        .catch(error => {
            console.error('Error al llamar al método C#: ', error);
        });
}

// Función para resetear el primer select a la opción "Mayor Venta" (value="0")
function resetSelect() {
    document.getElementById('selectOrder').value = "0";
}

function OcultarLupa() {
    document.getElementById("searchInput").addEventListener("input", function () {
        if (this.value.trim() !== '') {
            this.classList.add('hide-background');
        } else {
            this.classList.remove('hide-background');
        }
    });
}

function cerrarModal() {
    $('#modalcarrito').modal('hide'); // Cierra el modal
}


function setCookie(name, value, days) {
    console.log('setCookie ' + value);
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}




function getCookie(name) {
    try {
        // Log rápido para confirmar valores en iPhone
        const st = getComputedStyle(document.documentElement).getPropertyValue('--safe-top').trim();
        const sb = getComputedStyle(document.documentElement).getPropertyValue('--safe-bottom').trim();
        console.log('[SAFE] top:', st || '0', 'bottom:', sb || '0');

        // (Opcional) exponé valores como data-attr para debug visual
        document.documentElement.setAttribute('data-safe-top', st || '0');
    } catch (err) {
        console.warn('[SAFE] No se pudieron leer insets:', err);
    }

    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}
function deleteAllCookies() {
    var cookies = document.cookie.split(";");

    var ultsinfroimagen = getCookie("ultimasyncimg");


    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i];
        var eqPos = cookie.indexOf("=");
        var name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
        document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 GMT";
    }

    setCookie("ultimasyncimg", ultsinfroimagen, 30);
}

function openURL(url) {

}


(function () {
    try {
        // Log rápido para confirmar valores en iPhone
        const st = getComputedStyle(document.documentElement).getPropertyValue('--safe-top').trim();
        const sb = getComputedStyle(document.documentElement).getPropertyValue('--safe-bottom').trim();
        console.log('[SAFE] top:', st || '0', 'bottom:', sb || '0');

        // (Opcional) exponé valores como data-attr para debug visual
        document.documentElement.setAttribute('data-safe-top', st || '0');
    } catch (err) {
        console.warn('[SAFE] No se pudieron leer insets:', err);
    }
})();
//function CambiarStatus(status) {

//    //1 ok
//    //2 spin
//    //3 no sinc
//    //4 error conexion
//    if (status==1) {
//        document.getElementById("spinner").className = "spinnerOK";
//    }
//    if (status == 2) {
//        document.getElementById("spinner").className = "spinner";
//    }
//    if (status == 3) {
//        document.getElementById("spinner").className = "spinnerERR";
//    }
//    if (status == 4) {
//        document.getElementById("spinner").className = "spinnerERRCon";
//    }


//}

function openmodalCombo() {
    console.log("abrir modal combo");

    $('#myModalcombo').modal('show');
    //// Close modal on button click
    $(".btn2").click(function () {
        $("#myModalcombo").modal('hide');



    });
    $('#myModalcombo').on('hidden.bs.modal', function () {
    })
}

function cerrarTodosModal() {
    $("#modalcarrito").modal('hide');
    $("#myModalcombo").modal('hide');
    $("#myModal").modal('hide');

}



function openCategoriasModal() {
    console.log("abrir modal combo");

    $('#modalcategorias').modal('show');
    //// Close modal on button click
    $(".btn2").click(function () {
        $("#modalcategorias").modal('hide');


    });
    $('#modalcategorias').on('hidden.bs.modal', function () {
    })
}
function openCarritoModal() {
    console.log("abrir modal combo");

    $('#modalcarrito').modal('show');
    //// Close modal on button click
    $(".btn2").click(function () {
        $("#modalcarrito").modal('hide');


    });
    $('#modalcarrito').on('hidden.bs.modal', function () {
    })
}

function openModalSoporte() {
    console.log("abrir modal soporte");

    $('#modalsoporte').modal('show');
    //// Close modal on button click
    $(".btn2").click(function () {
        $("#modalsoporte").modal('hide');
    });
    $('#modalsoporte').on('hidden.bs.modal', function () {
    })
}
function openModalTexto() {
    console.log("abrir modal texto");

    $('#modalTexto').modal('show');
    //// Close modal on button click
    $(".btn2").click(function () {
        $("#modalTexto").modal('hide');
    });
    $('#modalTexto').on('hidden.bs.modal', function () {
    })
}
function openSubCategoriasModal() {
    console.log("abrir modal openSubCategoriasModal");
    $("#modalcategorias").modal('hide');
    $('#openSubCategoriasModal').modal('show');
    //// Close modal on button click
    $(".btn2").click(function () {
        $("#openSubCategoriasModal").modal('hide');


    });
    $('#openSubCategoriasModal').on('hidden.bs.modal', function () {
    })
}

function closeSubcategoriasModal() {
    $("#openSubCategoriasModal").modal('hide');
}
function addtocartModal() {


    console.log("valor en addtocartModal");
    $('#myModal').modal('show');

    //// Close modal on button click
    $(".btnclose1").click(function () {
        $("#myModal").modal('hide');
    });
    $(".btnclose2").click(function () {
        $("#myModal").modal('hide');
        $("#modalcarrito").modal('hide');

        //setTimeout(function () {
        //    $("#modalcarrito").modal('show');
        //}, 500); 

    });
    $(".btn2").click(function () {
        $("#myModal").modal('hide');
    });
    $('#myModal').on('hidden.bs.modal', function () {
    })

}



function ActualizarCantidadCarrito1(nodeDiv, prodId, cantIncrement, cantActualizar) {
    //(this ,@it.ProductosID,0,this.value)"


    var cant = 0;
    if (cantIncrement == 0) {
        //actualizar cant, no es incrementador
        cant = parseInt(cantActualizar);
    }
    else {
        cant = parseInt(nodeDiv.parentElement.childNodes[3].value);
        cant += parseInt(cantIncrement);

        nodeDiv.parentElement.childNodes[3].value = cant;
    }

    if (cant < 1) {
        cant = 0;
        nodeDiv.parentElement.childNodes[3].value = 0;
    }
    //var clientesID = 11;
    //$.ajax({
    //    url: '/UpdateCart',
    //    success: function (data) {
    //        alert(data);
    //    },
    //    error: function (error) {
    //        alert("Error: " + error);
    //    }
    //})


    //var stop = 0;

}


function ToogleBotonCerrar() {

    //$('#botonCerrar').
}

function ocultarnav() {

    $("#navbarRazor").hide();
    //  make_sticky('#sticky-elem-id');
}

function mostrarrnav() {

    $("#navbarRazor").show();
    //  make_sticky('#sticky-elem-id');
}



//window.onpopstate = function (event) {
//    // Agrega aquí el código que deseas ejecutar cuando se hace clic en el botón de retroceso

//    //let instance = new MyApp.MyCSharpClass();
//    //instance.invokeMethodAsync('BotonAtrasMigajas');
//    DotNet.invokeMethodAsync('AppVendedores', 'BotonAtrasMigajas')
//        .then(() => {
//            console.log('Método HandleBackButton llamado con éxito');
//        })
//        .catch(error => {
//            console.error('Error al llamar al método HandleBackButton: ', error);
//        });

//};



//window.addEventListener('popstate', function (event) {

//    DotNet.invokeMethodAsync('AppVendedores', 'HandleBackButton')
//        .then(() => {
//            console.log('Método HandleBackButton llamado con éxito');
//        })
//        .catch(error => {
//            console.error('Error al llamar al método HandleBackButton: ', error);
//        });
//    // código a ejecutar cuando se presione el botón "Back"
//});










if (window.history && history.pushState) { // check for history api support
    window.addEventListener('load', function () {
        // create history states
        history.pushState(-1, null); // back state
        history.pushState(0, null); // main state
        history.pushState(1, null); // forward state
        history.go(-1); // start in main state

        this.addEventListener('popstate', function (event, state) {
            // check history state and fire custom events
            cerrarTodosModal();
            if (state = event.state) {

                event = document.createEvent('Event');
                event.initEvent(state > 0 ? 'next' : 'previous', true, true);
                this.dispatchEvent(event);

                // reset state
                history.go(-state);

                window.addEventListener("popstate", function (e) {
                    e.preventDefault();
                    Swal.fire({
                        title: '¿Estás seguro que quieres salir del Cliente?',
                        showCancelButton: true,
                        confirmButtonText: 'Sí, salir',
                        cancelButtonText: 'No, quedarme',
                        reverseButtons: true
                    }).then((result) => {
                        if (result.isConfirmed) {
                            window.location.replace("/");
                        } else {
                            history.pushState({}, "");
                        }
                    });
                });

            }
        }, false);
    }, false);
}






$('.owl-carousel').owlCarousel({
    items: 1,
    loop: true,
    margin: 10,
    autoplay: true,
    autoplayTimeout: 1000,
    autoplayHoverPause: true
})


function esCf() {


    return $("#fccf").is(':checked');

}



//-----------------------------------------

const delayP = 3000; //ms

var slidesP = 0;
var slidesCountP = 0;


let current = 0;
setTimeout(() => {
    slidesP = document.querySelector(".slidesP");
    slidesCountP = 3// slidesP.childElementCount;
    const maxLeft = (slidesCountP - 1) * 100 * -1;
    function changeSlide(next = true) {
        if (next) {
            current += current > maxLeft ? -100 : current * -1;
        } else {
            current = current < 0 ? current + 100 : maxLeft;
        }
        try {
            slidesP.style.left = current + "%";
        } catch {

        }
    }

    let autoChange = setInterval(changeSlide, delayP);
    const restart = function () {
        clearInterval(autoChange);
        autoChange = setInterval(changeSlide, delayP);
    };

}, "1000")

//---------------------------------------------------------------------------------------------------------------------------------------------

//var containerCarrusel = 0;
//var slider =0;
//var slides =0;
//var currentPosition = 1;
//var currentMargin = 0;
//var slidesPerPage = 0;

//function cargarCarousel() {

//     containerCarrusel = document.getElementById('containerCarrusel');
//     slider = document.getElementById('slider');

//        containerWidth = containerCarrusel.offsetWidth;

//        window.addEventListener("resize", checkWidth);

//        function checkWidth() {
//            containerWidth = containerCarrusel.offsetWidth;
//            setParams(containerWidth);
//        }

//        function setParams(w) {

//            slidesCount = slides - slidesPerPage;// - 1;
//            if (currentPosition > slidesCount) {
//                currentPosition -= slidesPerPage;
//            };
//            currentMargin = - currentPosition * (100 / slidesPerPage);
//            slider.style.marginLeft = currentMargin + '%';


//        }

//        setParams();





//}
//var delay = 2000;
//let autoChange = setInterval(slideLeft, delay);
//const restart = function () {
//    clearInterval(autoChange);
//    autoChange = setInterval(slideRight, delay);
//};

//function slideRight() {
//    if (currentPosition <= 1) {
//        try {
//            slider.style.marginLeft = -(100 * (document.getElementsByClassName('slide').length - 1) / 2)+100 + '%';
//            currentMargin = -(100 *( (document.getElementsByClassName('slide').length - 1) / 2 )-100);
//            currentPosition = (document.getElementsByClassName('slide').length - 1)/2;// -;
//        }
//          catch { console.log(" ayyyyyyy") }

//    }
//    else {

//            try {
//                slider.style.marginLeft = currentMargin + (100) + '%';
//                currentMargin = currentMargin + 100;
//                currentPosition--;// = slidesCount - 1;
//            }
//            catch { console.log(" aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaahhhhhhhhhhhhhhhhhhhhhhhhjjjjjjjjjjjjjjjj") }

//    }


//};

//function slideLeft() {

//    if (currentPosition < ((document.getElementsByClassName('slide').length - 1)/2)) {
//        try {
//            slider.style.marginLeft = currentMargin - (100 ) + '%';
//            currentMargin = currentMargin - (100 );
//            currentPosition ++;
//        }
//        catch { console.log(" eror  slideLeft") }

//    }
//    else {

//            try {
//                slider.style.marginLeft = 0 + '%';
//                currentMargin =0;
//                currentPosition= 1;
//            }
//            catch {
//            console.log(" aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaahhhhhhhhhhhhhhhhhhhhhhhhjjjjjjjjjjjjjjjj")}

//    }

//};

//---------------------------------------------------------------------------------------------------------------------------------------------


document.addEventListener("DOMContentLoaded", () => {
    cargarCarousel(); // Inicializa el carrusel
    slideLeft(); // Mueve el carrusel automáticamente al inicio
    startAutoSlide(); // Inicia el autoplay para que siga moviéndose
});



var containerCarrusel = 0;
var slider = 0;
var slides = 0;
var currentPosition = 1;
var currentMargin = 0;
var slidesPerPage = 1;
var autoChange;
var userInteracted = false;
var timeoutRestart;

function cargarCarousel() {
    containerCarrusel = document.getElementById('containerCarrusel');
    slider = document.getElementById('slider');
    slides = document.getElementsByClassName('slide').length;

    if (!containerCarrusel || !slider || slides === 0) {
        console.error("No se encontró el carrusel o no hay slides.");
        return;
    }

    containerWidth = containerCarrusel.offsetWidth;

    // Usar ResizeObserver en lugar de "resize" para iOS
    const observer = new ResizeObserver(() => {
        containerWidth = containerCarrusel.offsetWidth;
        setParams(containerWidth);
    });
    observer.observe(containerCarrusel);

    function setParams(w) {
        slidesCount = slides - slidesPerPage;
        if (currentPosition > slidesCount) {
            currentPosition -= slidesPerPage;
        }
        currentMargin = -currentPosition * (100 / slidesPerPage);
        slider.style.marginLeft = currentMargin + '%';
    }

    setParams();
}


// Función para iniciar el autoplay cada 6 segundos
function startAutoSlide() {
    if (!userInteracted) {
        clearInterval(autoChange);
        autoChange = setInterval(slideLeft, 2000); // Se mueve cada 6 segundos
    }
}

// Función para detener el autoplay temporalmente cuando el usuario usa una flecha
function stopAutoSlide() {
    clearInterval(autoChange); // Detiene el autoplay
    userInteracted = true;

    clearTimeout(timeoutRestart); // Cancela cualquier reinicio anterior
    timeoutRestart = setTimeout(() => {
        userInteracted = false;
        startAutoSlide(); // Reactiva el autoplay después de 6 segundos
    }, 2000);
}


// Función para mover el carrusel hacia la derecha
function slideRight() {
    stopAutoSlide(); // Pausa el autoplay

    if (currentPosition <= 1) {
        try {
            slider.style.marginLeft = -(100 * (slides - 1) / 2) + 100 + '%';
            currentMargin = -(100 * ((slides - 1) / 2) - 100);
            currentPosition = (slides - 1) / 2;
        } catch (error) {
            console.log("Error en slideRight: ", error);
        }
    } else {
        try {
            slider.style.marginLeft = currentMargin + 100 + '%';
            currentMargin += 100;
            currentPosition--;
        } catch (error) {
            console.log("Error en slideRight: ", error);
        }
    }
}

// Función para mover el carrusel hacia la izquierda
function slideLeft() {
    stopAutoSlide(); // Pausa el autoplay

    if (currentPosition < (slides - 1) / 2) {
        try {
            slider.style.marginLeft = currentMargin - 100 + '%';
            currentMargin -= 100;
            currentPosition++;
        } catch (error) {
            console.log("Error en slideLeft: ", error);
        }
    } else {
        try {
            slider.style.marginLeft = '0%';
            currentMargin = 0;
            currentPosition = 1;
        } catch (error) {
            console.log("Error en slideLeft: ", error);
        }
    }
}

// Iniciar autoplay cuando cargue la página
document.addEventListener("DOMContentLoaded", () => {
    startAutoSlide();
});


//---------------------------------------------------------------------------------------------------------------------------------------------
function showLoader() {
    const loader = document.getElementById("loader");
    loader.style.display = "block";
}

function hideLoader() {
    const loader = document.getElementById("loader");
    loader.style.display = "none";
}

function cerrarComboConSlide() {
    var content = document.querySelector('#myModalcombo .modal-content');
    if (!content) { $('#myModalcombo').modal('hide'); return; }
    content.style.transition = 'transform 0.32s ease-in, opacity 0.28s ease-in';
    content.style.transform = 'translateY(100%)';
    content.style.opacity = '0';
    setTimeout(function () {
        $('#myModalcombo').one('hidden.bs.modal', function () {
            content.style.transition = '';
            content.style.transform = '';
            content.style.opacity = '';
        });
        $('#myModalcombo').modal('hide');
    }, 330);
}

function cerrarCarritoConSlide() {
    var content = document.querySelector('#modalcarrito .modal-content');
    if (!content) { $('#modalcarrito').modal('hide'); return; }
    content.style.transition = 'transform 0.32s ease-in, opacity 0.28s ease-in';
    content.style.transform = 'translateY(100%)';
    content.style.opacity = '0';
    setTimeout(function () {
        $('#modalcarrito').one('hidden.bs.modal', function () {
            content.style.transition = '';
            content.style.transform = '';
            content.style.opacity = '';
        });
        $('#modalcarrito').modal('hide');
    }, 330);
}

function cerrarProductoConSlide() {
    var content = document.querySelector('#myModal .modal-content');
    if (!content) { $('#myModal').modal('hide'); return; }
    content.style.transition = 'transform 0.32s ease-in, opacity 0.28s ease-in';
    content.style.transform = 'translateY(100%)';
    content.style.opacity = '0';
    setTimeout(function () {
        $('#myModal').one('hidden.bs.modal', function () {
            content.style.transition = '';
            content.style.transform = '';
            content.style.opacity = '';
        });
        $('#myModal').modal('hide');
    }, 330);
}

function showMSG() {
    const loader = document.getElementById("progressBarcontainer");
    loader.style.display = "block";
    hideLanguaje();
}

function hideMSG() {
    const loader = document.getElementById("progressBarcontainer");
    loader.style.display = "none";
    hideLanguaje();
}


function hideLanguaje() {
    const loader = document.getElementById("progressBarcontainer");
    const language = document.getElementById("selectLenguaje");


    language.style.display = loader.style.display == "none" ? "block" : "none";
}


//function addSyncMessage(message) {
//    const syncMessages = document.getElementById("syncmessages");
//    const p = document.createElement("p");
//    p.classList.add("pmensaje");
//    p.innerText = message;
//    syncMessages.appendChild(p);






//}

function replaceSyncMessage(message) {
    //const syncMessages = document.getElementById("syncmessages");
    //const lastP = syncMessages.lastElementChild;
    //if (lastP !== null && lastP.tagName === "P") {
    //    lastP.innerText = message;
    //} else {
    //    const p = document.createElement("p");
    //    p.classList.add("pmensaje");
    //    p.innerText = message;
    //    syncMessages.appendChild(p);
    //}



    const progressText = document.getElementById("progressText");
    const lastP1 = progressText.lastElementChild;
    if (lastP1 !== null && lastP1.tagName === "P") {
        lastP1.innerText = message;
    } else {
        const p = document.createElement("p");
        //   p.classList.add("pmensaje");
        p.innerText = message;
        progressText.appendChild(p);
    }


}
window.showCustomAlert = function (message) {
    var modalHtml = '<div class="custom-modal">' +
        '<div class="custom-content">' +
        message +
        '</div>' +
        '</div>';

    var bodyElement = document.getElementsByTagName('body')[0];
    bodyElement.insertAdjacentHTML('beforeend', modalHtml);
};



// Actualizar el valor de la barra de progreso
var value = 0;

function resetProgressBar() {
    value = 0;
}
function updateProgressBar(newvalue) {
    var progressBar = document.getElementById('progressBar');
    //value += newvalue;
    progressBar.value = newvalue;
}

function openURL(url) {
    window.open(url);
}



//setInterval(function () {
//    DotNet.invokeMethodAsync('AppVendedores', 'ConsultarMensajesNuevos')
//        .then(data => {
//            // Manejar la respuesta del método C# si es necesario
//            console.log('Método C# invocado con éxito:', data);
//        })
//        .catch(error => {
//            console.error('Error al invocar el método C#:', error);
//        });
//}, 600);
document.addEventListener("DOMContentLoaded", function () {
    const bodyElement = document.body;

    const successMessage = bodyElement.dataset.successMessage;
    const errorMessage = bodyElement.dataset.errorMessage;

    if (successMessage) {
        Toastify({
            text: successMessage,
            duration: 4000,
            close: true,
            gravity: "bottom",
            position: "right",
            stopOnFocus: true,
            style: {
                background: "#28a745"
            }
        }).showToast();
    }

    if (errorMessage) {
        Toastify({
            text: errorMessage,
            duration: 4000,
            close: true,
            gravity: "bottom",
            position: "right",
            stopOnFocus: true,
            style: {
                background: "#dc3545"
            }
        }).showToast();
    }

});
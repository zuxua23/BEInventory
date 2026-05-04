
window.Alert = {

    success(msg) {
        return Swal.fire({
            icon: 'success',
            title: 'Success',
            text: msg,
            timer: 1500,
            showConfirmButton: false
        });
    },

    error(msg) {
        return Swal.fire({
            icon: 'error',
            title: 'Error',
            text: msg
        });
    },

    confirm(msg) {
        return Swal.fire({
            title: 'Are you sure?',
            text: msg,
            icon: 'warning',
            showCancelButton: true
        });
    }
};
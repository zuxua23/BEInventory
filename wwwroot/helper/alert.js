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

    warning(msg) {
        return Swal.fire({
            icon: 'warning',
            title: 'Warning',
            text: msg
        });
    },

    confirm(options) {

        if (typeof options === "string") {
            options = { text: options };
        }

        return Swal.fire({
            title: options.title || "Are you sure?",
            text: options.text || "",
            icon: options.icon || "warning",
            showCancelButton: true,

            confirmButtonText: options.confirmText || "Yes",
            cancelButtonText: options.cancelText || "Cancel",

            confirmButtonColor: options.confirmColor || "#d33",
            cancelButtonColor: options.cancelColor || "#6c757d"
        });
    },

    deleteConfirm(entity = "Data", customText = null) {
        return this.confirm({
            text: customText || `${entity} will be deleted!`,
            confirmText: "Yes, delete!",
            confirmColor: "#d33"
        });
    }
};
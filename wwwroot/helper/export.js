window.Export = {

    async download(url, defaultFileName = "export") {

        try {

            Swal.fire({
                title: "Exporting...",
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading()
            });

            const res = await fetch(url, {
                credentials: "include"
            });

            if (!res.ok)
                throw new Error("Export failed");

            const blob = await res.blob();

            let fileName = defaultFileName;

            const disposition = res.headers.get("Content-Disposition");

            if (disposition) {

                let match = disposition.match(/filename\*=UTF-8''([^;]+)/);

                if (match) {
                    fileName = decodeURIComponent(match[1]);
                }
            }

            const blobUrl = window.URL.createObjectURL(blob);

            const a = document.createElement("a");
            a.href = blobUrl;
            a.download = fileName;

            document.body.appendChild(a);

            a.click();

            a.remove();

            window.URL.revokeObjectURL(blobUrl);

            Swal.close();

            await Alert.success("Export complete");

        } catch (err) {

            Swal.close();

            Alert.error(err.message);
        }
    },

    bind(selector, config) {

        $(selector).on("click", async function () {

            const btn = $(this);

            try {
                if (config.beforeExport) {

                    const valid = await config.beforeExport();

                    if (!valid)
                        return;
                }

                btn.prop("disabled", true);

                const url =
                    typeof config.url === "function"
                        ? config.url()
                        : config.url;

                await Export.download(
                    url,
                    config.fileName || "export"
                );

            } finally {

                btn.prop("disabled", false);
            }
        });
    }
};
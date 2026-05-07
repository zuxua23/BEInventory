window.Api = {

    async request(url, options = {}) {

        try {

            const res = await fetch(url, {
                credentials: "include",
                headers: {
                    "Content-Type": "application/json"
                },
                ...options
            });

            if (res.status === 401) {
                window.location.href = "/login";
                return null;
            }

            if (res.status === 403) {

                Alert.warning("You do not have access");

                return null;
            }

            if (!res.ok) {

                let errorData = {
                    message: "Request failed",
                    type: "error"
                };

                try {

                    const text = await res.text();

                    if (text) {
                        errorData = JSON.parse(text);
                    }

                } catch { }

                switch (errorData.type) {

                    case "warning":
                        Alert.warning(errorData.message);
                        break;

                    default:
                        Alert.error(errorData.message);
                        break;
                }
                return {
                    ok: false,
                    status: res.status,
                    errorData
                };
            }

            return res;

        } catch (err) {

            console.error(err);

            Alert.error("Network error");

            return null;
        }
    },

    async get(url) {

        const res = await this.request(url);

        if (!res || !res.ok)
            return null;

        try {

            const text = await res.text();

            return text ? JSON.parse(text) : null;

        } catch (err) {

            console.error("GET parse error:", err);

            return null;
        }
    },

    async post(url, data) {

        return this.request(url, {
            method: "POST",
            body: JSON.stringify(data)
        });
    },

    async put(url, data) {

        return this.request(url, {
            method: "PUT",
            body: JSON.stringify(data)
        });
    },

    async delete(url) {

        return this.request(url, {
            method: "DELETE"
        });
    }
};
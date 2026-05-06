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

                let message = "Request failed";

                try {

                    const text = await res.text();

                    if (text) {

                        const err = JSON.parse(text);

                        if (err.message)
                            message = err.message;
                    }

                } catch { }

                Alert.warning(message);

                return res;
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
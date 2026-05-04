
window.Api = {

    async request(url, options = {}) {
        const res = await fetch(url, {
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            ...options
        });

        if (res.status === 401) {
            window.location.href = "/login";
            return null;
        }

        if (res.status === 403) {
            alert("No access");
            return null;
        }

        return res;
    },

    async get(url) {
        const res = await this.request(url);
        return res ? res.json() : null;
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
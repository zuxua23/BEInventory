window.PermissionComponent = (function () {

    let MODULES = [];
    let containerId = null;

    async function loadModules(apiUrl = "/api/permission/modules") {

        const data = await Api.get(apiUrl);

        if (!data) return [];

        MODULES = data;

        return MODULES;
    }

    function render(container, existing = {}) {

        containerId = container;

        const el = document.getElementById(container);

        if (!el) return;

        el.innerHTML = "";

        MODULES.forEach(mod => {

            const col = document.createElement("div");

            col.className = "col-xl-4 col-lg-6 col-md-6";

            const opsHtml = mod.permissions.map(op => {

                const id = `perm_${mod.moduleKey}_${op}`;

                const checked =
                    existing[mod.moduleKey]?.includes(op)
                        ? "checked"
                        : "";

                return `
                    <label class="permission-item">

                        <input type="checkbox"
                            class="op-check"
                            id="${id}"
                            data-module="${mod.moduleKey}"
                            data-op="${op}"
                            ${checked}
                            onchange="
                                PermissionComponent.syncModule('${mod.moduleKey}');
                                PermissionComponent.syncSelectAll();
                            ">

                        <span class="permission-pill">
                            ${formatPermission(op)}
                        </span>

                    </label>
                `;
            }).join("");

            const allChecked = mod.permissions.every(op =>
                existing[mod.moduleKey]?.includes(op)
            );

            col.innerHTML = `
            <div class="permission-module-card shadow-sm">
                    <div class="permission-module-header">

                        <div class="custom-control custom-checkbox mb-0">

                            <input type="checkbox"
                                   class="custom-control-input module-header-check"
                                   id="mod_${mod.moduleKey}"
                                   ${allChecked ? "checked" : ""}
                                   onchange="
                                        PermissionComponent.toggleModule(
                                            '${mod.moduleKey}',
                                            this.checked
                                        )
                                   ">

                            <label class="custom-control-label"
                                   for="mod_${mod.moduleKey}">
                                ${mod.moduleName}
                            </label>

                        </div>

                    </div>

                    <div class="permission-ops">
                        ${opsHtml}
                    </div>

                </div >
    `;

            el.appendChild(col);
        });

        syncSelectAll();
    }

    function toggleModule(moduleKey, checked) {

        document
            .querySelectorAll(`[data-module="${moduleKey}"]`)
            .forEach(cb => cb.checked = checked);

        syncModule(moduleKey);
        syncSelectAll();
    }

    function syncModule(moduleKey) {

        const ops =
            document.querySelectorAll(
                `[data-module="${moduleKey}"]`            );

        const header =
            document.getElementById(`mod_${moduleKey}`);
        if (!header) return;

        header.checked =
            [...ops].every(c => c.checked);
    }

    function syncSelectAll() {

        const allOps =
            document.querySelectorAll(".op-check");

        const selectAll =
            document.getElementById("selectAllCheck");

        if (!selectAll) return;

        selectAll.checked =
            [...allOps].length > 0 &&
            [...allOps].every(c => c.checked);
    }

    function toggleSelectAll(checked) {

        document
            .querySelectorAll(".op-check")
            .forEach(cb => cb.checked = checked);

        document
            .querySelectorAll(".module-header-check")
            .forEach(cb => cb.checked = checked);

        syncSelectAll();
    }


    function getSelected() {

        const result = {};

        MODULES.forEach(mod => {

            const checked = [
                ...document.querySelectorAll(
                    `[data-module="${mod.moduleKey}"]`
                )
            ]
                .filter(cb => cb.checked)
                .map(cb => cb.dataset.op);

            if (checked.length > 0) {
                result[mod.moduleKey] = checked;
            }
        });

        return result;
    }

    function countSelected() {

        return document.querySelectorAll(
            ".op-check:checked"
        ).length;
    }
    function formatPermission(text) {

        return text
            .replaceAll("_", " ")
            .toLowerCase()
            .replace(/\b\w/g, l => l.toUpperCase());
    }

    return {

        loadModules,
        render,

        toggleModule,
        syncModule,

        toggleSelectAll,
        syncSelectAll,

        getSelected,
        countSelected
    };

})();

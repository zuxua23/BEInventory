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
        el.innerHTML = "";

        MODULES.forEach(mod => {

            const col = document.createElement("div");
            col.className = "col-xl-3 col-lg-4 col-md-6";

            const opsHtml = mod.permissions.map(op => {

                const id = `perm_${mod.moduleKey}_${op}`;
                const checked = existing[mod.moduleKey]?.includes(op) ? "checked" : "";

                return `
                    <div class="custom-control custom-checkbox">
                        <input type="checkbox"
                            class="custom-control-input op-check"
                            id="${id}"
                            data-module="${mod.moduleKey}"
                            data-op="${op}"
                            ${checked}
                            onchange="PermissionComponent.syncModule('${mod.moduleKey}')">
                        <label class="custom-control-label" for="${id}">${op}</label>
                    </div>
                `;
            }).join("");

            const allChecked = mod.permissions.every(op =>
                existing[mod.moduleKey]?.includes(op)
            );

            col.innerHTML = `
                <div class="permission-module-card">
                    <div class="permission-module-header">
                        <div class="custom-control custom-checkbox mb-0">
                            <input type="checkbox"
                                   class="custom-control-input module-header-check"
                                   id="mod_${mod.moduleKey}"
                                   ${allChecked ? "checked" : ""}
                                   onchange="PermissionComponent.toggleModule('${mod.moduleKey}', this.checked)">
                            <label class="custom-control-label">${mod.moduleName}</label>
                        </div>
                    </div>
                    <div class="permission-ops">${opsHtml}</div>
                </div>
            `;

            el.appendChild(col);
        });
    }

    function toggleModule(moduleKey, checked) {
        document.querySelectorAll(`[data-module="${moduleKey}"]`)
            .forEach(cb => cb.checked = checked);
    }

    function syncModule(moduleKey) {
        const ops = document.querySelectorAll(`[data-module="${moduleKey}"]`);
        document.getElementById(`mod_${moduleKey}`).checked =
            [...ops].every(c => c.checked);
    }

    function getSelected() {

        const result = {};

        MODULES.forEach(mod => {
            const checked = [...document.querySelectorAll(`[data-module="${mod.moduleKey}"]`)]
                .filter(cb => cb.checked)
                .map(cb => cb.dataset.op);

            if (checked.length > 0) {
                result[mod.moduleKey] = checked;
            }
        });

        return result;
    }

    function countSelected() {
        return document.querySelectorAll(".op-check:checked").length;
    }

    return {
        loadModules,
        render,
        toggleModule,
        syncModule,
        getSelected,
        countSelected
    };

})();
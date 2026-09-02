/**
 * device-department.js
 * When the department dropdown changes, loads the matching networks via AJAX
 * and repopulates the network dropdown.
 *
 * Place in: wwwroot/js/device-department.js
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        var deptSelect    = document.getElementById('DepartmentId');
        var networkSelect = document.getElementById('NetworkId');
        var apiUrl        = document.getElementById('networkApiUrl')?.value;
        var currentNetId  = document.getElementById('currentNetworkId')?.value;

        if (!deptSelect || !networkSelect || !apiUrl) return;

        function loadNetworks(departmentId, selectValue) {
            if (!departmentId) {
                networkSelect.innerHTML = '<option value="">No network</option>';
                return;
            }

            fetch(apiUrl + '?departmentId=' + encodeURIComponent(departmentId))
                .then(function (r) { return r.json(); })
                .then(function (networks) {
                    networkSelect.innerHTML = '<option value="">No network</option>';
                    networks.forEach(function (n) {
                        var opt = document.createElement('option');
                        opt.value       = n.value;
                        opt.textContent = n.text;
                        if (selectValue && String(n.value) === String(selectValue))
                            opt.selected = true;
                        networkSelect.appendChild(opt);
                    });
                })
                .catch(function (e) { console.error('device-department error:', e); });
        }

        // On department change: reload networks, clear current selection
        deptSelect.addEventListener('change', function () {
            loadNetworks(this.value, null);
        });

        // On page load: load networks for the already-selected department
        // and pre-select the current network (edit form)
        if (deptSelect.value) {
            loadNetworks(deptSelect.value, currentNetId);
        }
    });

})();

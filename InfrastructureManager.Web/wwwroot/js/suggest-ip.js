/**
 * suggest-ip.js
 * "Suggest IP"-knop op Devices Create/Edit: vraagt een vrij IP-adres op voor
 * het geselecteerde netwerk en vult dat automatisch in het IP-veld van het
 * huidige device type in, via DeviceFields.getIpFieldInputs().
 *
 * De knop wordt uitgeschakeld zolang DeviceFields nog velden aan het laden
 * is (na het wisselen van device type), zodat er nooit op een lege of
 * verouderde veldenlijst gezocht wordt.
 *
 * Place in: wwwroot/js/suggest-ip.js
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var btn        = document.getElementById('btnSuggestIp');
        var networkSel = document.getElementById('NetworkId');
        var resultEl   = document.getElementById('suggestIpResult');
        var suggestUrl = document.getElementById('suggestIpUrl')?.value;

        if (!btn || !networkSel || !resultEl || !suggestUrl) return;

        function fieldsAreLoading() {
            return !!(window.DeviceFields && typeof window.DeviceFields.isLoading === 'function' && window.DeviceFields.isLoading());
        }

        function updateButtonState() {
            btn.disabled = !networkSel.value || fieldsAreLoading();
            if (!networkSel.value) resultEl.textContent = '';
        }

        networkSel.addEventListener('change', updateButtonState);

        document.addEventListener('devicefields:loadstart', function () {
            btn.disabled = true;
        });
        document.addEventListener('devicefields:loadend', updateButtonState);

        updateButtonState();

        btn.addEventListener('click', async function () {
            if (!networkSel.value) return;

            if (fieldsAreLoading()) {
                resultEl.textContent = 'Still loading device type fields — try again in a moment.';
                return;
            }

            btn.disabled = true;
            resultEl.textContent = 'Looking up a free address...';

            try {
                var res  = await fetch(suggestUrl + '?id=' + encodeURIComponent(networkSel.value));
                var data = await res.json();

                if (!data.ip) {
                    resultEl.textContent = 'No free address found (or the subnet is too large to scan automatically).';
                    return;
                }

                var inputs = (window.DeviceFields && typeof window.DeviceFields.getIpFieldInputs === 'function')
                    ? window.DeviceFields.getIpFieldInputs()
                    : [];

                if (inputs.length === 0) {
                    resultEl.textContent = 'Suggested: ' + data.ip + ' — this device type has no IP field, copy it manually if needed.';
                } else {
                    inputs.forEach(function (input) { input.value = data.ip; });
                    resultEl.textContent = 'Filled in: ' + data.ip;
                }
            } catch (e) {
                resultEl.textContent = 'Could not determine a free address.';
            } finally {
                updateButtonState();
            }
        });
    });
})();
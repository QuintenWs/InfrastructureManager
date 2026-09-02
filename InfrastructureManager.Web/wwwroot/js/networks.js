/**
 * networks.js
 * Handles the DHCP range fields toggle on the Create and Edit Network forms.
 * Place in: wwwroot/js/networks.js
 */

(function () {
    'use strict';

    function initDhcpToggle() {
        const toggle = document.getElementById('dhcpToggle');
        const fields = document.getElementById('dhcpFields');

        if (!toggle || !fields) return;

        function setDhcpVisibility() {
            if (toggle.checked) {
                fields.style.display = '';
            } else {
                fields.style.display = 'none';

                // Clear values so empty strings don't get validated or saved
                fields.querySelectorAll('input[type="text"], input:not([type])').forEach(function (input) {
                    input.value = '';
                });
            }
        }

        // Run immediately on page load to match the current model state
        // (important when the form is returned after a failed POST)
        setDhcpVisibility();

        toggle.addEventListener('change', setDhcpVisibility);
    }

    // Wait for the DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initDhcpToggle);
    } else {
        initDhcpToggle();
    }
})();

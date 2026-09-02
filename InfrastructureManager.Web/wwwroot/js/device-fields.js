/**
 * device-fields.js
 * Dynamically loads and renders device-type-specific fields.
 *
 * Supported field types:
 *   text, number, date, select, textarea, checkbox, url,
 *   ipv4, ipv6, mac
 *
 * Place in: wwwroot/js/device-fields.js
 */

var DeviceFields = (function () {
    'use strict';

    var _config = {};

    function init(config) {
        _config = config;

        var select = document.getElementById(config.selectId);
        if (!select) return;

        loadFields(select.value);

        select.addEventListener('change', function () {
            loadFields(this.value);
        });
    }

    function loadFields(deviceTypeValue) {
        if (!deviceTypeValue) return;

        var url = _config.apiUrl + '?deviceType=' + encodeURIComponent(deviceTypeValue);
        if (_config.deviceId) {
            url += '&deviceId=' + encodeURIComponent(_config.deviceId);
        }

        fetch(url)
            .then(function (res) { return res.json(); })
            .then(function (fields) { renderFields(fields); })
            .catch(function (err) { console.error('device-fields error:', err); });
    }

    function renderFields(fields) {
        var container = document.getElementById(_config.containerId);
        var section   = document.getElementById(_config.sectionId);

        container.innerHTML = '';

        if (!fields || fields.length === 0) {
            if (section) section.style.display = 'none';
            return;
        }

        if (section) section.style.display = '';

        fields.forEach(function (field) {
            var isFullWidth = field.fieldType === 'textarea';
            var div = document.createElement('div');
            div.className = isFullWidth ? 'form-group col-span-full' : 'form-group';

            // Checkbox renders its own label inside the wrapper
            if (field.fieldType !== 'checkbox') {
                var label = document.createElement('label');
                label.className   = 'form-label';
                label.textContent = field.label + (field.isRequired ? ' *' : '');
                div.appendChild(label);
            }

            var input = buildInput(field);
            div.appendChild(input);

            var hint = getHint(field.fieldType);
            if (hint) {
                var small = document.createElement('div');
                small.className   = 'text-secondary small mt-1';
                small.textContent = hint;
                div.appendChild(small);
            }

            container.appendChild(div);
        });
    }

    function buildInput(field) {
        var name = 'FieldValues[' + field.id + ']';

        // ── select ────────────────────────────────────────────────────────────
        if (field.fieldType === 'select' && field.selectOptions) {
            var sel = document.createElement('select');
            sel.name = name;
            sel.className = 'form-select custom-input';
            sel.required  = field.isRequired;

            var blank = document.createElement('option');
            blank.value = ''; blank.textContent = '— select —';
            sel.appendChild(blank);

            field.selectOptions.split(',').forEach(function (opt) {
                var o = document.createElement('option');
                o.value = opt.trim(); o.textContent = opt.trim();
                if (o.value === field.currentValue) o.selected = true;
                sel.appendChild(o);
            });
            return sel;
        }

        // ── textarea ──────────────────────────────────────────────────────────
        if (field.fieldType === 'textarea') {
            var ta = document.createElement('textarea');
            ta.name      = name;
            ta.className = 'form-control custom-input';
            ta.rows      = 3;
            ta.required  = field.isRequired;
            ta.value     = field.currentValue || '';
            return ta;
        }

        // ── checkbox ──────────────────────────────────────────────────────────
        if (field.fieldType === 'checkbox') {
            var wrapper = document.createElement('div');
            wrapper.className = 'form-check mt-1';

            // Hidden false so unchecked submits "false"
            var hidden = document.createElement('input');
            hidden.type  = 'hidden';
            hidden.name  = name;
            hidden.value = 'false';
            wrapper.appendChild(hidden);

            var cb = document.createElement('input');
            cb.type      = 'checkbox';
            cb.name      = name;
            cb.value     = 'true';
            cb.className = 'form-check-input';
            cb.id        = 'field_' + field.id;
            cb.checked   = field.currentValue === 'true';
            wrapper.appendChild(cb);

            var lbl = document.createElement('label');
            lbl.className   = 'form-check-label';
            lbl.htmlFor     = 'field_' + field.id;
            lbl.textContent = field.label + (field.isRequired ? ' *' : '');
            wrapper.appendChild(lbl);

            return wrapper;
        }

        // ── all other input types ─────────────────────────────────────────────
        var input = document.createElement('input');
        input.name      = name;
        input.className = 'form-control custom-input';
        input.value     = field.currentValue || '';
        input.required  = field.isRequired;

        switch (field.fieldType) {

            case 'number':
                input.type = 'number';
                input.min  = '0';
                break;

            case 'date':
                input.type = 'date';
                break;

            case 'ipv4':
                input.type        = 'text';
                input.placeholder = '192.168.1.1';
                input.pattern     = '^(\\d{1,3}\\.){3}\\d{1,3}$';
                input.title       = 'Enter a valid IPv4 address (e.g. 192.168.1.1)';
                input.addEventListener('blur', validateIpv4);
                break;

            case 'ipv6':
                input.type        = 'text';
                input.placeholder = '2001:db8::1';
                // IPv6 is complex — we do a loose structural check client-side
                // and rely on server-side for strict validation
                input.title       = 'Enter a valid IPv6 address (e.g. 2001:db8::1)';
                input.addEventListener('blur', validateIpv6);
                break;

            case 'mac':
                input.type        = 'text';
                input.placeholder = 'AA:BB:CC:DD:EE:FF';
                input.pattern     = '^([0-9A-Fa-f]{2}[:\\-]){5}[0-9A-Fa-f]{2}$';
                input.title       = 'Enter a valid MAC address (AA:BB:CC:DD:EE:FF)';
                input.addEventListener('blur', function () {
                    this.value = this.value.toUpperCase()
                        // normalise dashes to colons
                        .replace(/-/g, ':');
                });
                break;

            case 'url':
                input.type        = 'url';
                input.placeholder = 'https://';
                break;

            default:
                input.type = 'text';
        }

        return input;
    }

    // ── Validators ────────────────────────────────────────────────────────────

    function validateIpv4() {
        var val  = this.value.trim();
        if (!val) return;

        var parts = val.split('.');
        var valid = parts.length === 4 &&
            parts.every(function (p) {
                var n = parseInt(p, 10);
                return /^\d+$/.test(p) && n >= 0 && n <= 255;
            });

        setInputState(this, valid, 'Invalid IPv4 address');
    }

    function validateIpv6() {
        var val = this.value.trim();
        if (!val) return;

        // Loose check: contains colons, only hex digits and colons
        // Full IPv6 validation is complex — this catches obvious mistakes
        var valid = /^[0-9a-fA-F:]+$/.test(val) &&
                    val.indexOf(':') !== -1 &&
                    val.length >= 2 &&
                    val.length <= 39;

        setInputState(this, valid, 'Invalid IPv6 address');
    }

    function setInputState(el, valid, message) {
        if (valid) {
            el.classList.remove('is-invalid');
            el.classList.add('is-valid');
            el.setCustomValidity('');
        } else {
            el.classList.remove('is-valid');
            el.classList.add('is-invalid');
            el.setCustomValidity(message);
        }
    }

    // ── Hints ─────────────────────────────────────────────────────────────────

    function getHint(fieldType) {
        switch (fieldType) {
            case 'ipv4':     return 'Format: 192.168.x.x';
            case 'ipv6':     return 'Format: 2001:db8::1  (full or compressed notation)';
            case 'mac':      return 'Format: AA:BB:CC:DD:EE:FF — auto-uppercased';
            case 'url':      return 'Include https://';
            default:         return null;
        }
    }

    return { init: init };
})();
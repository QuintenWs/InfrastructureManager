/**
 * visits.js
 * Powers the "Nieuw bezoek" form: dynamically add/remove "new item" rows
 * (with proper re-indexing so ASP.NET Core model binding keeps working),
 * and show/hide the resolution-notes box when an open item is checked off.
 *
 * Place in: wwwroot/js/visits.js
 */

(function () {
    'use strict';

    function initNewItemRows() {
        var container = document.getElementById('newItemsContainer');
        var addBtn    = document.getElementById('addNewItemBtn');
        if (!container || !addBtn) return;

        function reindex() {
            var rows = container.querySelectorAll('.new-item-row');
            rows.forEach(function (row, i) {
                row.querySelectorAll('[name]').forEach(function (el) {
                    el.name = el.name.replace(/NewItems\[\d+\]/, 'NewItems[' + i + ']');
                });
            });
        }

        function attachRemove(row) {
            var btn = row.querySelector('.remove-item-row');
            if (!btn) return;
            btn.addEventListener('click', function () {
                var rows = container.querySelectorAll('.new-item-row');
                if (rows.length <= 1) {
                    // Keep at least one row — just clear it instead of removing
                    var textarea = row.querySelector('textarea');
                    if (textarea) textarea.value = '';
                    var select = row.querySelector('select');
                    if (select) select.value = 'Normal';
                    return;
                }
                row.remove();
                reindex();
            });
        }

        addBtn.addEventListener('click', function () {
            var rows     = container.querySelectorAll('.new-item-row');
            var template = rows[rows.length - 1].cloneNode(true);

            template.querySelectorAll('textarea').forEach(function (el) { el.value = ''; });
            var select = template.querySelector('select');
            if (select) select.value = 'Normal';

            container.appendChild(template);
            attachRemove(template);
            reindex();
        });

        container.querySelectorAll('.new-item-row').forEach(attachRemove);
    }

    function initResolveToggles() {
        document.querySelectorAll('.resolve-toggle').forEach(function (checkbox) {
            var notesEl = document.getElementById(checkbox.dataset.notesTarget);
            if (!notesEl) return;

            function sync() {
                notesEl.style.display = checkbox.checked ? '' : 'none';
            }

            checkbox.addEventListener('change', sync);
            sync();
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initNewItemRows();
        initResolveToggles();
    });
})();

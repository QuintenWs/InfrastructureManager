/**
 * photo-upload.js
 * Drag & drop multi-file upload with previews.
 * Place in: wwwroot/js/photo-upload.js
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        var zone     = document.getElementById('dropZone');
        var input    = document.getElementById('photoFileInput');
        var preview  = document.getElementById('photoPreviewGrid');
        var form     = document.getElementById('photoUploadForm');
        var counter  = document.getElementById('fileCounter');

        if (!zone || !input || !preview || !form) return;

        var selectedFiles = new DataTransfer();

        // ── Click zone to open file picker ────────────────────────────────────
        zone.addEventListener('click', function () {
            input.click();
        });

        // ── Drag events ───────────────────────────────────────────────────────
        zone.addEventListener('dragover', function (e) {
            e.preventDefault();
            zone.classList.add('drag-over');
        });

        zone.addEventListener('dragleave', function (e) {
            if (!zone.contains(e.relatedTarget))
                zone.classList.remove('drag-over');
        });

        zone.addEventListener('drop', function (e) {
            e.preventDefault();
            zone.classList.remove('drag-over');
            addFiles(e.dataTransfer.files);
        });

        // ── File input change ─────────────────────────────────────────────────
        input.addEventListener('change', function () {
            addFiles(this.files);
            // Reset so the same file can be re-added after removal
            this.value = '';
        });

        // ── Add files to selection ────────────────────────────────────────────
        function addFiles(fileList) {
            for (var i = 0; i < fileList.length; i++) {
                var file = fileList[i];

                if (!isAllowed(file)) {
                    alert(file.name + ' is not allowed. Only JPEG, PNG, GIF and WebP images up to 10 MB.');
                    continue;
                }

                // Avoid duplicates by name + size
                var duplicate = false;
                for (var j = 0; j < selectedFiles.files.length; j++) {
                    if (selectedFiles.files[j].name === file.name &&
                        selectedFiles.files[j].size === file.size) {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate) {
                    selectedFiles.items.add(file);
                    addPreview(file);
                }
            }

            // Keep the hidden input in sync so the form submits the files
            input.files = selectedFiles.files;
            updateCounter();
        }

        // ── Render a preview card ─────────────────────────────────────────────
        function addPreview(file) {
            var reader  = new FileReader();
            var card    = document.createElement('div');
            card.className = 'photo-preview-card';
            card.dataset.name = file.name;
            card.dataset.size = file.size;

            reader.onload = function (e) {
                card.innerHTML =
                    '<img src="' + e.target.result + '" alt="' + escapeHtml(file.name) + '" />' +
                    '<div class="photo-preview-name">' + escapeHtml(file.name) + '</div>' +
                    '<button type="button" class="photo-preview-remove" title="Remove">' +
                    '  <i class="bi bi-x-circle-fill"></i>' +
                    '</button>';

                card.querySelector('.photo-preview-remove')
                    .addEventListener('click', function (ev) {
                        ev.stopPropagation();
                        removeFile(file.name, file.size);
                        card.remove();
                        updateCounter();
                    });

                preview.appendChild(card);
            };

            reader.readAsDataURL(file);
        }

        // ── Remove a file from the DataTransfer ───────────────────────────────
        function removeFile(name, size) {
            var next = new DataTransfer();
            for (var i = 0; i < selectedFiles.files.length; i++) {
                var f = selectedFiles.files[i];
                if (f.name !== name || f.size !== size)
                    next.items.add(f);
            }
            selectedFiles = next;
            input.files   = selectedFiles.files;
        }

        // ── Update counter label ──────────────────────────────────────────────
        function updateCounter() {
            var n = selectedFiles.files.length;
            if (counter)
                counter.textContent = n === 0
                    ? 'No files selected'
                    : n + ' file' + (n > 1 ? 's' : '') + ' ready to upload';
        }

        // ── Validation ────────────────────────────────────────────────────────
        function isAllowed(file) {
            var allowed = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
            return allowed.includes(file.type) && file.size <= 10 * 1024 * 1024;
        }

        function escapeHtml(str) {
            return str
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;');
        }

    });

})();
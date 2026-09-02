/**
 * photo-lightbox.js
 * Opens a fullscreen overlay when a .photo-thumb is clicked.
 * Close by clicking outside the image, pressing ESC, or clicking the X button.
 *
 * Place in: wwwroot/js/photo-lightbox.js
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        // ── Build overlay once ────────────────────────────────────────────────
        var overlay = document.createElement('div');
        overlay.id  = 'lightboxOverlay';
        overlay.innerHTML =
            '<button id="lightboxClose" aria-label="Close">' +
            '  <i class="bi bi-x-lg"></i>' +
            '</button>' +
            '<img id="lightboxImg" src="" alt="" />' +
            '<div id="lightboxCaption"></div>';
        document.body.appendChild(overlay);

        var img     = document.getElementById('lightboxImg');
        var caption = document.getElementById('lightboxCaption');

        // ── Open on .photo-thumb click ────────────────────────────────────────
        document.addEventListener('click', function (e) {
            var thumb = e.target.closest('.photo-thumb');
            if (!thumb) return;

            e.preventDefault();

            // src is the same URL we already loaded for the thumbnail
            img.src = thumb.src;
            img.alt = thumb.alt;
            caption.textContent = thumb.alt || '';

            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        });

        // ── Close on overlay background click ─────────────────────────────────
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) close();
        });

        // ── Close button ──────────────────────────────────────────────────────
        document.getElementById('lightboxClose')
            .addEventListener('click', close);

        // ── ESC key ───────────────────────────────────────────────────────────
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') close();
        });

        function close() {
            overlay.classList.remove('active');
            document.body.style.overflow = '';
            img.src = '';
        }
    });

})();

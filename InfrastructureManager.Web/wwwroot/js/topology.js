/**
 * topology.js — Cytoscape.js + cytoscape-edgehandles
 */

(function () {
    'use strict';

    var dataEl = document.getElementById('topologyData');
    if (!dataEl) return;
    if (typeof cytoscape === 'undefined') { console.error('Cytoscape not loaded'); return; }

    var data     = JSON.parse(dataEl.textContent);
    var isAdmin  = (document.getElementById('topoIsAdmin') || {}).value === 'true';
    var saveUrl  = (document.getElementById('topoSaveUrl') || {}).value || '';
    var statusEl = document.getElementById('topoStatus');

    // ── Badges ────────────────────────────────────────────────────────────────
    var BADGES = {
        Switch:'SW', RouterRed:'RTR', RouterBlack:'RTR', Firewall:'FW',
        Crypto:'CR', NAS:'NAS', Server:'SRV', WPC:'WPC', BPC:'BPC',
        Laptop:'LPT', Desktop:'PC', Printer:'PRT', UPS:'UPS',
        VTC:'VTC', RAP:'AP', Phone:'TEL', MediaConverter:'MC',
        SAR:'SAR', Armadillo:'ARM', SFP:'SFP', Other:'DEV'
    };
    function badge(t) { return BADGES[t] || 'DEV'; }

    // ── Elements ──────────────────────────────────────────────────────────────
    var elements = [];
    var savedPos = data.savedPositions || {};
    var hasInternet = (data.networks || []).some(function (n) { return n.isInternetAccessible; });

    if (hasInternet) {
        elements.push({
            data: { id: 'cloud', label: 'Internet', nodeType: 'cloud' },
            position: savedPos['cloud'] || { x: 400, y: 60 }
        });
    }

    (data.networks || []).forEach(function (net, i) {
        var nid   = 'net_' + net.id;
        var autoX = 140 + i * 300;
        elements.push({
            data: { id: nid, label: net.name,
                    sublabel: net.networkAddress + '/' + net.cidr,
                    isInternet: net.isInternetAccessible, nodeType: 'network' },
            position: savedPos[nid] || { x: autoX, y: 220 }
        });
        if (hasInternet && net.isInternetAccessible)
            elements.push({ data: { id: 'e_cloud_' + nid, source: 'cloud', target: nid, edgeType: 'internet' } });

        (net.devices || []).forEach(function (dev, j) {
            var did  = 'dev_' + dev.id;
            var cols = Math.max(1, Math.ceil(Math.sqrt((net.devices || []).length)));
            elements.push({
                data: { id: did, label: dev.name,
                        sublabel: badge(dev.deviceType) + (dev.ipAddress ? '  ' + dev.ipAddress : ''),
                        deviceType: dev.deviceType, status: dev.status,
                        devId: dev.id, nodeType: 'device' },
                position: savedPos[did] || {
                    x: autoX + ((j % cols) - (cols - 1) / 2) * 215,
                    y: 430 + Math.floor(j / cols) * 130
                }
            });
            elements.push({ data: { id: 'e_' + nid + '_' + did, source: nid, target: did, edgeType: 'default' } });
        });
    });

    (data.unassignedDevices || []).forEach(function (dev, i) {
        var did = 'dev_' + dev.id;
        elements.push({
            data: { id: did, label: dev.name,
                    sublabel: badge(dev.deviceType) + (dev.ipAddress ? '  ' + dev.ipAddress : ''),
                    deviceType: dev.deviceType, status: dev.status,
                    devId: dev.id, nodeType: 'device' },
            position: savedPos[did] || { x: 90 + i * 215, y: 600 }
        });
    });

    if (data.customEdges && data.customEdges.length) {
        elements = elements.filter(function (el) {
            return !(el.data && el.data.edgeType === 'default');
        });
        data.customEdges.forEach(function (ce, i) {
            elements.push({ data: { id: 'ce_' + i, source: ce.from, target: ce.to, edgeType: 'custom' } });
        });
    }

    // ── Stylesheet ────────────────────────────────────────────────────────────
    var SS = [
        {
            selector: 'node[nodeType="cloud"]',
            style: {
                shape: 'round-rectangle', width: 144, height: 62,
                'background-color': '#eef2ff', 'border-color': '#818cf8', 'border-width': 2.5,
                label: 'Internet', 'font-size': 14, 'font-weight': 700,
                color: '#3730a3', 'font-family': 'Inter,Arial,sans-serif',
                'text-valign': 'center', 'text-halign': 'center',
            }
        },
        {
            selector: 'node[nodeType="network"]',
            style: {
                shape: 'round-rectangle',
                width: 210, height: 74,
                'min-width': 210, 'min-height': 74,
                'background-color': '#1e40af', 'border-width': 0,
                label: function (e) { return e.data('label') + '\n' + e.data('sublabel'); },
                'text-wrap': 'wrap', 'text-max-width': 190, 'line-height': 1.6,
                'font-size': 12, 'font-weight': 700,
                color: '#bfdbfe', 'font-family': 'Inter,Arial,sans-serif',
                'text-valign': 'center', 'text-halign': 'center',
            }
        },
        {
            selector: 'node[nodeType="device"]',
            style: {
                shape: 'round-rectangle',
                width: 188, height: 74,
                'min-width': 188, 'min-height': 74,
                'background-color': '#fff', 'border-color': '#e2e8f0', 'border-width': 2,
                label: function (e) { return e.data('label') + '\n' + e.data('sublabel'); },
                'text-wrap': 'wrap', 'text-max-width': 172, 'line-height': 1.6,
                'font-size': 11.5, 'font-weight': 600,
                color: '#1e293b', 'font-family': 'Inter,Arial,sans-serif',
                'text-valign': 'center', 'text-halign': 'center',
            }
        },
        { selector: 'node[status="Active"]',      style: { 'border-color': '#16a34a', 'border-width': 2.5 } },
        { selector: 'node[status="Offline"]',     style: { 'border-color': '#dc2626', 'border-width': 2.5 } },
        { selector: 'node[status="Maintenance"]', style: { 'border-color': '#d97706', 'border-width': 2.5 } },
        {
            selector: 'edge',
            style: {
                width: 2, 'line-color': '#94a3b8',
                'target-arrow-color': '#94a3b8', 'target-arrow-shape': 'triangle',
                'curve-style': 'bezier', 'arrow-scale': 0.85,
            }
        },
        {
            selector: 'edge[edgeType="internet"]',
            style: { width: 2.5, 'line-color': '#4f46e5', 'target-arrow-color': '#4f46e5' }
        },
        { selector: 'node:selected', style: { 'border-color': '#f59e0b', 'border-width': 3, 'overlay-opacity': 0.1, 'overlay-color': '#f59e0b' } },
        { selector: 'edge:selected', style: { 'line-color': '#f59e0b', 'target-arrow-color': '#f59e0b', width: 3 } },
        { selector: '.eh-handle',   style: { 'background-color': '#2563eb', width: 16, height: 16, shape: 'ellipse', 'overlay-opacity': 0, 'border-width': 2, 'border-color': '#fff' } },
        { selector: '.eh-hover',    style: { 'background-color': '#2563eb', 'border-color': '#2563eb', 'border-width': 3, 'overlay-opacity': 0.12 } },
        { selector: '.eh-source',   style: { 'border-color': '#2563eb', 'border-width': 3 } },
        { selector: '.eh-target',   style: { 'border-color': '#16a34a', 'border-width': 3 } },
        { selector: '.eh-preview, .eh-ghost-edge', style: { 'line-color': '#2563eb', 'target-arrow-color': '#2563eb', 'target-arrow-shape': 'triangle', width: 2, 'line-style': 'dashed' } },
    ];

    // ── Init Cytoscape ────────────────────────────────────────────────────────
    var cy = cytoscape({
        container:           document.getElementById('cy'),
        elements:            elements,
        style:               SS,
        layout:              { name: 'preset' },
        userZoomingEnabled:  true,
        wheelSensitivity:    0.3,
        userPanningEnabled:  true,
        boxSelectionEnabled: true,
        autoungrabify:       !isAdmin,
    });

    cy.ready(function () { cy.fit(50); });

    // ── Edgehandles ───────────────────────────────────────────────────────────
    if (isAdmin) {
        try {
            var eh = cy.edgehandles({
                canConnect: function (src, tgt) { return src.id() !== tgt.id(); },
                edgeParams: function ()         { return { data: { edgeType: 'custom' } }; },
                hoverDelay:    150,
                snap:          true,
                snapThreshold: 50,
                snapFrequency: 15,
                disableBrowserGestures: true,
            });

            cy.on('ehcomplete', function () {
                setStatus('Verbinding toegevoegd ✓ — opslaan...');
                scheduleSave();
                // Zet draw mode uit na elke verbinding
                drawMode = false;
                eh.disableDrawMode();
                updateDrawBtn();
            });

            // Draw mode toggle
            var drawMode = false;

            function updateDrawBtn() {
                var btn = q('btnDraw');
                if (!btn) return;
                if (drawMode) {
                    btn.textContent = '✕ Stop drawing';
                    btn.classList.add('active');
                    btn.style.background = '#f59e0b';
                    btn.style.borderColor = '#f59e0b';
                    btn.style.color = '#fff';
                    document.getElementById('cy').style.cursor = 'crosshair';
                    setStatus('Draw mode: klik op een node en sleep naar de target om te verbinden.');
                } else {
                    btn.textContent = '✏️ Draw connection';
                    btn.classList.remove('active');
                    btn.style.background = '';
                    btn.style.borderColor = '';
                    btn.style.color = '';
                    document.getElementById('cy').style.cursor = '';
                    setStatus('');
                }
            }

            q('btnDraw') && q('btnDraw').addEventListener('click', function () {
                drawMode = !drawMode;
                if (drawMode) {
                    eh.enableDrawMode();
                } else {
                    eh.disableDrawMode();
                }
                updateDrawBtn();
            });

            // Escape cancelt draw mode
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && drawMode) {
                    drawMode = false;
                    eh.disableDrawMode();
                    updateDrawBtn();
                }
            });

        } catch (err) {
            console.warn('cytoscape-edgehandles failed:', err);
            setStatus('Edge drawing niet beschikbaar.');
        }
    }

    // ── Delete key ────────────────────────────────────────────────────────────
    if (isAdmin) {
        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Delete' && e.key !== 'Backspace') return;
            var tag = document.activeElement ? document.activeElement.tagName : '';
            if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
            var sel = cy.$(':selected');
            if (!sel.length) return;
            sel.remove();
            scheduleSave();
            setStatus('Verwijderd.');
        });
    }

    // ── Click device → details ────────────────────────────────────────────────
    cy.on('tap', 'node[nodeType="device"]', function (evt) {
        var devId = evt.target.data('devId');
        if (devId) window.location.href = '/Devices/Details/' + devId;
    });

    // ── Toolbar ───────────────────────────────────────────────────────────────
    function q(id) { return document.getElementById(id); }
    q('btnZoomIn')  && q('btnZoomIn').addEventListener('click',  function () { cy.zoom({ level: cy.zoom() * 1.25, renderedPosition: { x: cy.width()/2, y: cy.height()/2 } }); });
    q('btnZoomOut') && q('btnZoomOut').addEventListener('click', function () { cy.zoom({ level: cy.zoom() * 0.8,  renderedPosition: { x: cy.width()/2, y: cy.height()/2 } }); });
    q('btnFit')     && q('btnFit').addEventListener('click',     function () { cy.fit(50); });
    q('btnSave')    && q('btnSave').addEventListener('click',    doSave);

    // ── Save ──────────────────────────────────────────────────────────────────
    var saveTimer = null;
    function scheduleSave() { clearTimeout(saveTimer); saveTimer = setTimeout(doSave, 700); }
    function setStatus(m)   { if (statusEl) statusEl.textContent = m; }

    cy.on('dragfree', 'node', function () { if (isAdmin) scheduleSave(); });

    function doSave() {
        if (!isAdmin || !saveUrl) return;
        var positions = {};
        cy.nodes().forEach(function (n) {
            var p = n.position();
            positions[n.id()] = { x: Math.round(p.x), y: Math.round(p.y) };
        });
        var edges = [];
        cy.edges().forEach(function (e) {
            edges.push({ from: e.data('source'), to: e.data('target') });
        });
        var tok = document.querySelector('input[name="__RequestVerificationToken"]');
        fetch(saveUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tok ? tok.value : '' },
            body: JSON.stringify({ departmentId: data.departmentId, positions: positions, edges: edges })
        }).then(function () {
            setStatus('Layout opgeslagen ✓');
            setTimeout(function () { setStatus(''); }, 2500);
        }).catch(function () { setStatus('Opslaan mislukt.'); });
    }

    // ── Hints ─────────────────────────────────────────────────────────────────
    cy.on('mouseover', 'node', function () {
        if (!statusEl || statusEl.textContent) return;
        setStatus('Klik → details  |  Sleep → verplaatsen  |  "Draw connection" → verbinding leggen  |  Selecteer + Delete → verwijderen');
    });
    cy.on('mouseout', 'node', function () {
        if (statusEl && statusEl.textContent && !statusEl.textContent.includes('Draw mode')) setStatus('');
    });

})();
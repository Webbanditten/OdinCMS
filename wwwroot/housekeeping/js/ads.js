// Housekeeping > Ads (Areas/Housekeeping/Views/Ads/Index): create and edit ads for every ad place.
// Talks JSON to AdsController: Ad?id= (one ad), Save, Toggle, Delete; Preview?image=&format=&x=&y=&zoom= draws a slanted
// billboard (the list's thumbnails; the form draws its previews itself, see drawSlant, so they can be dragged);
// Rooms?ids= names the room loading ad's rooms. The room picker searches with the editors' /housekeeping/editor/rooms?query=.
(function ($) {
    'use strict';

    var BASE = '/housekeeping/ads/';
    var editingId = 0;
    var frames = null;     // wwwroot/housekeeping/ads/frames/frames.json: format -> { room, point }
    var imageSize = null;  // natural size of the chosen picture
    var formats = window.billboardFormats || {};  // BillboardFormats: id -> { width, height, faceHeight, left }
    // A flat billboard's crop (Ad.CropX/CropY/CropZoom): the picture point the screens are centred on, and the zoom.
    var crop = { x: 0.5, y: 0.5, zoom: 1 };
    var source = null;     // the chosen picture as an <img>, for drawing the slanted previews

    function api(method, action, body) {
        return fetch(BASE + action, {
            method: method,
            credentials: 'same-origin',
            headers: body ? { 'Content-Type': 'application/json' } : {},
            body: body ? JSON.stringify(body) : undefined
        }).then(function (response) {
            return response.text().then(function (text) {
                if (!response.ok) throw new Error(text || response.statusText);
                return text ? JSON.parse(text) : null;
            });
        });
    }

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        if (text) node.textContent = text;
        return node;
    }

    function placement() { return $('#ad-placement').val(); }
    function option() { return $('#ad-placement option:selected'); }
    function isBillboard() { return placement() === 'room_billboard'; }

    function loadFrames() {
        if (frames) return Promise.resolve(frames);
        return fetch('/housekeeping/ads/frames/frames.json').then(function (r) { return r.json(); }).then(function (f) { frames = f; return f; });
    }

    // ---- Form state ----

    function syncCrop() {
        $('.ad-crop').toggle(isBillboard() && $('#ad-fit').val() === 'auto');
        $('#ad-zoom').val(crop.zoom);
        $('#ad-zoom-value').text(Math.round(crop.zoom * 100) + ' %');
    }

    function syncPlacement() {
        $('.ad-billboard').toggle(isBillboard());
        syncCrop();
        $('.ad-loading').toggle(placement() === 'room_loading');
        $('.ad-pool').toggle(placement() === 'pool_screen');
        $('#ad-click').closest('div').prev('label').addBack().toggle(placement() !== 'pool_screen');
        var o = option();
        $('#ad-placement-hint').text(o.data('size') + '. ' + o.data('where'));
        syncRooms();
        preview();
    }

    function syncRooms() {
        var fit = $('#ad-fit').val();
        $('#ad-rooms .ad-room').each(function () {
            var ok = fit === 'auto' || this.dataset.format === fit;
            this.classList.toggle('is-disabled', !ok);
            var box = this.querySelector('input');
            box.disabled = !ok;
            if (!ok) box.checked = false;
        });
    }

    function imageInfo() {
        var src = $('#ad-image').val().trim();
        imageSize = null;
        $('#ad-image-info').text('');
        if (!src) { preview(); return; }
        var probe = new Image();
        probe.onload = function () {
            imageSize = { w: probe.naturalWidth, h: probe.naturalHeight };
            var o = option(), text = imageSize.w + '×' + imageSize.h;
            if (!isBillboard()) {
                var w = +o.data('width'), h = +o.data('height');
                if (placement() === 'room_loading') {
                    if (imageSize.w > w || imageSize.h > h) text += ' — bigger than ' + w + '×' + h + ', the loading window cuts it off';
                } else if (imageSize.w !== w || imageSize.h !== h) {
                    text += ' — the place is ' + w + '×' + h + ', so it is ' + (placement().indexOf('website') === 0 ? 'stretched' : 'cut or padded');
                }
            }
            $('#ad-image-info').text(text);
            preview();
        };
        probe.onerror = function () { $('#ad-image-info').text('The picture could not be loaded.'); preview(); };
        probe.src = src;
    }

    // ---- Room picker (room loading ad) ----
    // Public rooms are listed up front (rendered by the page); typing searches every room by name, owner or ID.
    // A click adds a room, a second click (or the chip's ×) takes it off again.

    var picked = [];       // [{ id, name, owner }] in the order they were added; owner null = public room
    var searchTimer = null, searchId = 0;

    function isPicked(id) { return picked.some(function (r) { return r.id === id; }); }

    function renderPicked() {
        var box = document.getElementById('ad-picked');
        box.innerHTML = '';
        picked.forEach(function (room) {
            var chip = el('span', { 'class': 'ad-chip' });
            chip.appendChild(document.createTextNode(room.name || 'No room ' + room.id));
            chip.appendChild(el('small', {}, room.name ? (room.owner ? room.owner + ', ' : '') + room.id : 'deleted'));
            var x = el('button', { type: 'button', title: 'Take it off', 'data-unpick': room.id }, '×');
            chip.appendChild(x);
            box.appendChild(chip);
        });
        $('.ad-room-pick').each(function () { this.classList.toggle('is-picked', isPicked(+this.dataset.id)); });
    }

    function toggleRoom(room) {
        if (isPicked(room.id)) picked = picked.filter(function (r) { return r.id !== room.id; });
        else picked.push(room);
        renderPicked();
    }

    function pickButton(room) {
        var label = room.name + (room.owner ? ' · ' + room.owner : '');
        var button = el('button', { type: 'button', 'class': 'ad-room-pick', 'data-id': room.id, 'data-name': room.name, 'data-owner': room.owner || '', title: label + ' (' + room.id + ')' }, label + ' ');
        button.appendChild(el('small', {}, room.owner ? String(room.id) : 'public, ' + room.id));
        return button;
    }

    function searchRooms() {
        var query = $('#ad-room-search').val().trim();
        var mine = ++searchId;
        var showPublic = function (heading) {
            $('#ad-room-public').prop('hidden', false);
            $('#ad-room-results').prop('hidden', true);
            $('#ad-room-heading').text(heading);
        };
        if (!query) { showPublic('Public rooms. Search to find private rooms.'); return Promise.resolve(); }
        if (query.length < 2 && !/^\d+$/.test(query)) { showPublic('Type one more letter.'); return Promise.resolve(); }
        return fetch('/housekeeping/editor/rooms?query=' + encodeURIComponent(query), { credentials: 'same-origin' })
            .then(function (r) { if (!r.ok) throw new Error(r.statusText); return r.json(); })
            .then(function (rooms) {
                if (mine !== searchId) return;
                var list = document.getElementById('ad-room-results');
                list.innerHTML = '';
                rooms.forEach(function (room) { list.appendChild(pickButton(room)); });
                list.hidden = false;
                $('#ad-room-public').prop('hidden', true);
                $('#ad-room-heading').text(rooms.length ? (rooms.length >= 40 ? 'The first 40 rooms. Type more to narrow it down.' : rooms.length + (rooms.length === 1 ? ' room.' : ' rooms.')) : 'No room matches.');
                renderPicked();
            })
            .catch(function () { if (mine === searchId) $('#ad-room-heading').text('The search failed.'); });
    }

    function loadPicked(ids) {
        picked = [];
        renderPicked();
        if (!ids || !ids.length) return;
        api('GET', 'Rooms?ids=' + ids.join(',')).then(function (rooms) {
            picked = rooms.map(function (r) { return { id: r.id, name: r.name, owner: r.owner }; });
            renderPicked();
        }).catch(function () {
            picked = ids.map(function (id) { return { id: id, name: '#' + id, owner: null }; });
            renderPicked();
        });
    }

    // ---- Preview ----

    function preview() {
        var box = document.getElementById('ad-preview');
        var src = $('#ad-image').val().trim();
        box.innerHTML = '';
        if (!src) { box.appendChild(el('span', { 'class': 'ad-hint' }, 'Pick a picture.')); return; }
        if (isBillboard()) {
            var fit = $('#ad-fit').val();
            Promise.all([loadFrames(), fit === 'auto' ? loadSource(src) : Promise.resolve(null)]).then(function (loaded) {
                var f = loaded[0];
                box.innerHTML = '';
                Object.keys(f).filter(function (format) { return fit === 'auto' || fit === format; }).forEach(function (format) {
                    var frame = f[format];
                    var figure = el('figure');
                    var wrap = el('div', { 'class': 'ad-frame' });
                    var bg = el('img', { 'class': 'ad-frame-bg', src: '/housekeeping/ads/frames/' + format + '.png', alt: '' });
                    wrap.appendChild(bg);
                    if (fit === 'auto' && loaded[1] && formats[format]) {
                        // Drawn here like AdService.Slant, so dragging is instant; the hotel gets the server's version.
                        var fmt = formats[format];
                        var canvas = el('canvas', { 'class': 'ad-slant', width: fmt.width, height: fmt.height, tabindex: '0', 'data-format': format,
                            title: 'Drag to move the picture (arrow keys: 1 px, Shift: 10 px)' });
                        canvas.style.left = (frame.point[0] - Math.floor(fmt.width / 2)) + 'px';
                        canvas.style.top = (frame.point[1] - Math.floor(fmt.height / 2)) + 'px';
                        wrap.appendChild(canvas);
                    } else {
                        var ad = el('img', { alt: '' });
                        ad.onload = function () {
                            ad.style.left = (frame.point[0] - Math.floor(ad.naturalWidth / 2)) + 'px';
                            ad.style.top = (frame.point[1] - Math.floor(ad.naturalHeight / 2)) + 'px';
                            // The client draws it with matte ink: white around the picture's edge is see-through.
                            ad.style.mixBlendMode = 'multiply';
                        };
                        ad.src = src;
                        wrap.appendChild(ad);
                    }
                    figure.appendChild(wrap);
                    figure.appendChild(el('figcaption', {}, format.replace('_', ', facing ') + ' (' + frame.room + ')'));
                    box.appendChild(figure);
                });
                drawSlants();
            });
            return;
        }
        var o = option(), w = +o.data('width'), h = +o.data('height');
        var slot = el('div', { 'class': 'ad-slot' });
        slot.style.width = w + 'px';
        slot.style.height = h + 'px';
        var img = el('img', { src: src, alt: '' });
        if (placement().indexOf('website') === 0) { img.style.width = w + 'px'; img.style.height = h + 'px'; }
        slot.appendChild(img);
        var figure = el('figure');
        figure.appendChild(slot);
        figure.appendChild(el('figcaption', {}, o.text() + ', ' + w + '×' + h));
        box.appendChild(figure);
    }

    // ---- Billboard crop (flat pictures) ----

    function loadSource(src) {
        if (source && source.dataset.src === src && source.complete) return Promise.resolve(source);
        return new Promise(function (resolve) {
            var img = new Image();
            img.dataset.src = src;
            img.onload = function () { source = img; resolve(img); };
            img.onerror = function () { resolve(null); };
            img.src = src;
        });
    }

    // AdService.CropFor: the screen's shape scaled to just cover the picture, zoomed, centred on the crop point, kept inside.
    function cropFor(W, H, fw, fh) {
        var zoom = Math.min(4, Math.max(1, crop.zoom));
        var scale = Math.max(fw / W, fh / H) * zoom;
        var cw = Math.min(W, Math.max(1, Math.round(fw / scale)));
        var ch = Math.min(H, Math.max(1, Math.round(fh / scale)));
        var cx = Math.round(Math.min(Math.max(crop.x * W - cw / 2, 0), W - cw));
        var cy = Math.round(Math.min(Math.max(crop.y * H - ch / 2, 0), H - ch));
        return { x: cx, y: cy, w: cw, h: ch, scale: scale };
    }

    // AdService.Slant: the cut picture sheared like the frames' screens (stepping on odd columns), with a dark outline.
    function drawSlant(canvas) {
        var fmt = formats[canvas.dataset.format];
        if (!fmt || !source) return;
        var w = fmt.width, faceH = fmt.faceHeight, half = w / 2;
        var c = canvas.getContext('2d');
        c.fillStyle = '#fff';
        c.fillRect(0, 0, canvas.width, canvas.height);
        var flat = document.createElement('canvas');
        flat.width = w;
        flat.height = faceH;
        var f = flat.getContext('2d');
        f.fillStyle = '#fff';
        f.fillRect(0, 0, w, faceH);
        f.imageSmoothingQuality = 'high';
        var r = cropFor(source.naturalWidth, source.naturalHeight, w, faceH);
        f.drawImage(source, r.x, r.y, r.w, r.h, 0, 0, w, faceH);
        c.fillStyle = 'rgb(30,30,30)';
        for (var x = 0; x < w; x++) {
            var top = fmt.left ? half - Math.floor((x + 1) / 2) : Math.floor((x + 1) / 2);
            c.drawImage(flat, x, 0, 1, faceH, x, top, 1, faceH);
            if (x === 0 || x === w - 1) c.fillRect(x, top, 1, faceH);
            else { c.fillRect(x, top, 1, 1); c.fillRect(x, top + faceH - 1, 1, 1); }
        }
    }

    function drawSlants() {
        document.querySelectorAll('#ad-preview canvas.ad-slant').forEach(drawSlant);
    }

    // Moves the crop point so the picture follows the mouse by (dx, dy) screen pixels in the given format's frame.
    function moveCrop(format, start, dx, dy) {
        var fmt = formats[format];
        if (!fmt || !source) return;
        var W = source.naturalWidth, H = source.naturalHeight;
        var r = cropFor(W, H, fmt.width, fmt.faceHeight);
        var minX = r.w / 2 / W, minY = r.h / 2 / H;
        crop.x = Math.min(Math.max(start.x - dx / (r.scale * W), minX), 1 - minX);
        crop.y = Math.min(Math.max(start.y - dy / (r.scale * H), minY), 1 - minY);
        drawSlants();
    }

    // ---- Open, save ----

    function open(ad, rooms) {
        editingId = ad ? ad.id : 0;
        $('#ad-form-title').text(ad ? 'Edit ' + ad.name : 'New ad');
        $('#ad-name').val(ad ? ad.name : '');
        $('#ad-placement').val(ad ? ad.placement : 'room_billboard');
        $('#ad-image').val(ad ? ad.image : '');
        $('#ad-fit').val(ad && ad.placement === 'room_billboard' ? ad.fit : 'auto');
        crop = { x: ad ? ad.cropX : 0.5, y: ad ? ad.cropY : 0.5, zoom: ad ? ad.cropZoom : 1 };
        $('#ad-click').val(ad ? ad.clickUrl || '' : '');
        $('#ad-starts').val(ad && ad.startsAt ? ad.startsAt.replace('T', ' ').substring(0, 16) : '');
        $('#ad-ends').val(ad && ad.endsAt ? ad.endsAt.replace('T', ' ').substring(0, 16) : '');
        $('#ad-weight').val(ad ? ad.weight : 1);
        $('#ad-enabled').prop('checked', ad ? ad.enabled : true);
        $('#ad-rooms input, .ad-pool input').each(function () { this.checked = !!rooms && rooms.indexOf(+this.value) >= 0; });
        loadPicked(ad && ad.placement === 'room_loading' ? rooms : null);
        $('#ad-room-search').val('');
        searchRooms();
        $('#ad-error').text('');
        $('#ad-form').prop('hidden', false);
        syncPlacement();
        imageInfo();
        document.getElementById('ad-form').scrollIntoView({ behavior: 'smooth' });
    }

    function date(value) {
        value = (value || '').trim();
        return value ? value.replace(' ', 'T') + ':00' : null;
    }

    function save() {
        var body = {
            id: editingId,
            name: $('#ad-name').val(),
            placement: placement(),
            image: $('#ad-image').val(),
            fit: $('#ad-fit').val(),
            cropX: crop.x,
            cropY: crop.y,
            cropZoom: crop.zoom,
            clickUrl: placement() === 'pool_screen' ? '' : $('#ad-click').val(),
            startsAt: date($('#ad-starts').val()),
            endsAt: date($('#ad-ends').val()),
            enabled: $('#ad-enabled').prop('checked'),
            weight: parseInt($('#ad-weight').val(), 10) || 1,
            rooms: isBillboard() ? $('#ad-rooms input:checked').map(function () { return +this.value; }).get()
                : placement() === 'pool_screen' ? $('.ad-pool input:checked').map(function () { return +this.value; }).get()
                : placement() === 'room_loading' ? picked.map(function (r) { return r.id; }) : []
        };
        $('#ad-save').prop('disabled', true);
        $('#ad-error').text('');
        api('POST', 'Save', body).then(function () { location.reload(); })
            .catch(function (error) { $('#ad-error').text(error.message); })
            .then(function () { $('#ad-save').prop('disabled', false); });
    }

    function remove(id, name) {
        Swal.fire({
            title: 'Delete ' + name + '?',
            text: 'Its views and clicks go with it. Turning it off keeps them.',
            icon: 'warning', showCancelButton: true, confirmButtonText: 'Delete'
        }).then(function (answer) {
            if (!answer.value && !answer.isConfirmed) return;
            api('POST', 'Delete', { id: id }).then(function () { location.reload(); })
                .catch(function (error) { Swal.fire('Ad', error.message, 'error'); });
        });
    }

    $(function () {
        $('.datetimepicker').datetimepicker({ format: 'Y-m-d H:i', step: 15 });
        $('#ad-dates-clear').on('click', function (e) { e.preventDefault(); $('#ad-starts, #ad-ends').val(''); });
        $('#ad-rooms-none').on('click', function (e) { e.preventDefault(); $('#ad-rooms input').prop('checked', false); });
        $('#ad-placement').on('change', function () { syncPlacement(); imageInfo(); });
        $('#ad-fit').on('change', function () { syncRooms(); syncCrop(); preview(); });
        $('#ad-zoom').on('input', function () {
            crop.zoom = +this.value;
            $('#ad-zoom-value').text(Math.round(crop.zoom * 100) + ' %');
            // Keep the point inside what the new zoom can reach.
            var first = document.querySelector('#ad-preview canvas.ad-slant');
            if (first) moveCrop(first.dataset.format, { x: crop.x, y: crop.y }, 0, 0);
        });
        $('#ad-crop-reset').on('click', function (e) {
            e.preventDefault();
            crop = { x: 0.5, y: 0.5, zoom: 1 };
            syncCrop();
            drawSlants();
        });
        $(document).on('pointerdown', '#ad-preview canvas.ad-slant', function (e) {
            var canvas = this, start = { x: crop.x, y: crop.y }, x0 = e.clientX, y0 = e.clientY;
            canvas.setPointerCapture(e.pointerId);
            canvas.focus();
            var move = function (ev) { moveCrop(canvas.dataset.format, start, ev.clientX - x0, ev.clientY - y0); };
            var up = function () {
                canvas.removeEventListener('pointermove', move);
                canvas.removeEventListener('pointerup', up);
                canvas.removeEventListener('pointercancel', up);
            };
            canvas.addEventListener('pointermove', move);
            canvas.addEventListener('pointerup', up);
            canvas.addEventListener('pointercancel', up);
            e.preventDefault();
        });
        $(document).on('keydown', '#ad-preview canvas.ad-slant', function (e) {
            var step = e.shiftKey ? 10 : 1;
            var d = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] }[e.key];
            if (!d) return;
            e.preventDefault();
            moveCrop(this.dataset.format, { x: crop.x, y: crop.y }, d[0], d[1]);
        });
        $('#ad-image').on('change', imageInfo);
        $('#ad-room-search').on('input', function () { clearTimeout(searchTimer); searchTimer = setTimeout(searchRooms, 250); });
        $('#ad-room-search').on('keydown', function (e) {
            // Enter takes the first room found, so "1018 ⏎" adds a room by its ID.
            if (e.key !== 'Enter') return;
            e.preventDefault();
            clearTimeout(searchTimer);
            searchRooms().then(function () {
                var first = $('#ad-room-results:not([hidden]) .ad-room-pick').first();
                if (first.length && !first.hasClass('is-picked')) first.trigger('click');
            });
        });
        $(document).on('click', '.ad-room-pick', function (e) {
            e.preventDefault();
            toggleRoom({ id: +this.dataset.id, name: this.dataset.name, owner: this.dataset.owner || null });
        });
        $(document).on('click', '[data-unpick]', function (e) {
            e.preventDefault();
            var id = +this.dataset.unpick;
            picked = picked.filter(function (r) { return r.id !== id; });
            renderPicked();
        });
        $('#ad-new').on('click', function () { open(null, null); });
        $('#ad-cancel').on('click', function () { $('#ad-form').prop('hidden', true); });
        $('#ad-save').on('click', save);
        $(document).on('click', '[data-edit]', function (e) {
            e.preventDefault();
            api('GET', 'Ad?id=' + this.dataset.edit).then(function (r) { open(r.ad, r.rooms); })
                .catch(function (error) { Swal.fire('Ad', error.message, 'error'); });
        });
        $(document).on('click', '[data-toggle]', function (e) {
            e.preventDefault();
            api('POST', 'Toggle', { id: +this.dataset.toggle, enabled: this.dataset.enabled === '1' }).then(function () { location.reload(); })
                .catch(function (error) { Swal.fire('Ad', error.message, 'error'); });
        });
        $(document).on('click', '[data-delete]', function (e) {
            e.preventDefault();
            remove(+this.dataset.delete, this.dataset.name);
        });
    });
})(jQuery);

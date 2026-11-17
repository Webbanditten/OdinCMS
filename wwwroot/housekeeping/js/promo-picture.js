// Promo picture builder (Areas/Housekeeping/Views/PromoPicture/Index.cshtml): crops a room photo to the promo size,
// draws a banner on top and saves the PNG with its settings (PromoPictureController.Save). Rooms come from the editors'
// room search (/housekeeping/editor/rooms). Everything is drawn without smoothing, so the pixels stay sharp.
(function () {
    var root = document.getElementById('promo-builder');
    if (!root) return;
    var cfg = JSON.parse(root.dataset.config);
    var W = cfg.width, H = cfg.height;
    var canvas = document.getElementById('promo-canvas');
    var ctx = canvas.getContext('2d');
    var small = document.getElementById('promo-small');
    var smallCtx = small.getContext('2d');
    var hint = document.getElementById('promo-canvas-hint');
    var status = document.getElementById('promo-status');
    var saveButton = document.getElementById('promo-save');
    var bannerSelect = document.getElementById('promo-banner');
    var backgroundInput = document.getElementById('promo-background');

    var state = {
        id: cfg.picture ? cfg.picture.id : null,
        room: null,
        photo: null,
        zoom: cfg.picture ? cfg.picture.zoom : 1,
        x: cfg.picture ? cfg.picture.offsetX : 0,
        y: cfg.picture ? cfg.picture.offsetY : 0,
        dirty: false
    };
    var banners = {};
    cfg.banners.forEach(function (b) {
        var img = new Image();
        img.onload = draw;
        img.src = b.file;
        banners[b.id] = img;
    });
    if (cfg.picture) {
        bannerSelect.value = cfg.picture.banner || '';
        backgroundInput.value = cfg.picture.background || '#000000';
    }

    function fetchJson(url) {
        return fetch(url, { credentials: 'same-origin' }).then(function (response) {
            if (!response.ok) throw new Error(response.statusText);
            return response.json();
        });
    }

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function render(target) {
        var c = target.getContext('2d');
        c.imageSmoothingEnabled = false;
        c.fillStyle = backgroundInput.value;
        c.fillRect(0, 0, W, H);
        if (state.photo) c.drawImage(state.photo, state.x, state.y, state.photo.naturalWidth * state.zoom, state.photo.naturalHeight * state.zoom);
        var banner = banners[bannerSelect.value];
        if (banner && banner.complete && banner.naturalWidth) c.drawImage(banner, 0, 0);
    }

    function draw() {
        render(canvas);
        smallCtx.imageSmoothingEnabled = false;
        smallCtx.drawImage(canvas, 0, 0);
        document.querySelectorAll('.promo-zoom').forEach(function (b) { b.classList.toggle('is-active', +b.dataset.zoom === state.zoom); });
        saveButton.disabled = !state.photo;
    }

    function changed() {
        state.dirty = true;
        status.textContent = '';
        draw();
    }

    function centre() {
        if (!state.photo) return;
        state.x = Math.round((W - state.photo.naturalWidth * state.zoom) / 2);
        state.y = Math.round((H - state.photo.naturalHeight * state.zoom) / 2);
    }

    function setRoom(room, keepPosition) {
        state.room = room;
        var current = document.getElementById('promo-room-current');
        current.textContent = '';
        current.appendChild(el('strong', null, room.name));
        current.appendChild(document.createTextNode((room.owner ? ' af ' + room.owner : ' (public room)') + ' · id ' + room.id + ' '));
        var link = el('a', null, room.photo ? 'retake photo' : 'take a photo');
        link.href = '/housekeeping/rooms/view/' + room.id;
        link.target = '_blank';
        current.appendChild(link);
        if (!room.photo) {
            state.photo = null;
            hint.textContent = 'This room has no photo yet. Take one on the room page, then pick the room again.';
            hint.style.display = '';
            draw();
            return;
        }
        var img = new Image();
        img.onload = function () {
            state.photo = img;
            if (!keepPosition) centre();
            hint.style.display = 'none';
            draw();
        };
        img.src = room.photo;
    }

    // Room search
    var search = document.getElementById('promo-room-search');
    var results = document.getElementById('promo-room-results');
    var timer = null, searchId = 0;
    search.addEventListener('input', function () {
        clearTimeout(timer);
        var query = search.value.trim();
        if (query.length < 2 && !/^\d+$/.test(query)) { results.textContent = ''; return; }
        timer = setTimeout(function () {
            var id = ++searchId;
            fetchJson('/housekeeping/editor/rooms?query=' + encodeURIComponent(query)).then(function (rooms) {
                if (id !== searchId) return;
                results.textContent = '';
                if (!rooms.length) results.appendChild(el('em', null, 'No room matches.'));
                rooms.forEach(function (room) {
                    var b = el('button', 'promo-room-result' + (room.photo ? '' : ' is-without-photo'));
                    b.type = 'button';
                    b.title = room.name + (room.owner ? ' af ' + room.owner : '') + ' · id ' + room.id + (room.photo ? '' : '\nNo photo yet');
                    if (room.photo) { var t = el('img'); t.src = room.photo; t.alt = ''; t.loading = 'lazy'; b.appendChild(t); }
                    else b.appendChild(el('span', 'promo-room-nophoto', 'no photo'));
                    b.appendChild(el('span', null, room.name));
                    b.addEventListener('click', function () {
                        results.textContent = '';
                        search.value = '';
                        setRoom(room, false);
                        changed();
                    });
                    results.appendChild(b);
                });
            }).catch(function () { results.textContent = 'The search failed.'; });
        }, 250);
    });

    // Moving the room: drag (in picture pixels, whatever size the canvas is shown at) or arrow keys.
    var drag = null;
    canvas.addEventListener('pointerdown', function (e) {
        if (!state.photo) return;
        canvas.setPointerCapture(e.pointerId);
        drag = { px: e.clientX, py: e.clientY, x: state.x, y: state.y, scale: canvas.getBoundingClientRect().width / W };
        canvas.focus();
    });
    canvas.addEventListener('pointermove', function (e) {
        if (!drag) return;
        state.x = Math.round(drag.x + (e.clientX - drag.px) / drag.scale);
        state.y = Math.round(drag.y + (e.clientY - drag.py) / drag.scale);
        changed();
    });
    canvas.addEventListener('pointerup', function () { drag = null; });
    canvas.addEventListener('pointercancel', function () { drag = null; });
    canvas.addEventListener('keydown', function (e) {
        var step = e.shiftKey ? 10 : 1;
        var moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
        if (!moves[e.key] || !state.photo) return;
        e.preventDefault();
        state.x += moves[e.key][0];
        state.y += moves[e.key][1];
        changed();
    });

    // Zoom around the middle of the picture.
    document.querySelectorAll('.promo-zoom').forEach(function (b) {
        b.addEventListener('click', function () {
            var zoom = +b.dataset.zoom;
            if (zoom === state.zoom) return;
            state.x = Math.round(W / 2 - (W / 2 - state.x) * zoom / state.zoom);
            state.y = Math.round(H / 2 - (H / 2 - state.y) * zoom / state.zoom);
            state.zoom = zoom;
            changed();
        });
    });
    document.getElementById('promo-centre').addEventListener('click', function () { centre(); changed(); });
    bannerSelect.addEventListener('change', changed);
    backgroundInput.addEventListener('input', changed);

    // Saving
    saveButton.addEventListener('click', function () {
        if (!state.photo || !state.room) return;
        saveButton.disabled = true;
        status.textContent = 'Saving...';
        var out = document.createElement('canvas');
        out.width = W;
        out.height = H;
        render(out);
        var png = out.toDataURL('image/png').split(',')[1];
        fetch('/housekeeping/promopicture/save', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                id: state.id, roomId: state.room.id, zoom: state.zoom, offsetX: state.x, offsetY: state.y,
                background: backgroundInput.value, banner: bannerSelect.value, promoId: cfg.promoId, png: png
            })
        }).then(function (response) {
            return response.json().then(function (data) {
                if (!response.ok) throw new Error(data.error || response.statusText);
                return data;
            });
        }).then(function (data) {
            state.id = data.id;
            state.dirty = false;
            saveButton.textContent = 'Save changes';
            saveButton.disabled = false;
            status.textContent = 'Saved.' + (data.promos.length ? ' ' + data.promos.length + ' promo(s) updated.' : '');
            history.replaceState(null, '', '/housekeeping/promopicture?id=' + data.id + (cfg.promoId ? '&promoId=' + cfg.promoId : ''));
            document.getElementById('promo-url').value = data.url;
            document.getElementById('promo-saved').style.display = '';
            var create = document.getElementById('promo-create');
            if (create) {
                var page = document.getElementById('promo-page');
                var update = function () {
                    var teaser = 'Rum-spot: ' + state.room.name + (state.room.owner ? ' af ' + state.room.owner : '');
                    create.href = '/housekeeping/promo/create?pageId=' + page.value + '&img=' + encodeURIComponent(data.url) +
                        '&teaser=' + encodeURIComponent(teaser) + '&link1Text=' + encodeURIComponent('Tag et kig') +
                        '&link1Href=' + encodeURIComponent('/community/featured_rooms');
                };
                page.onchange = update;
                update();
            }
        }).catch(function (error) {
            saveButton.disabled = false;
            status.textContent = 'Not saved: ' + error.message;
        });
    });

    window.addEventListener('beforeunload', function (e) {
        if (state.dirty) { e.preventDefault(); e.returnValue = ''; }
    });

    draw();
    if (cfg.roomId) {
        fetchJson('/housekeeping/editor/rooms/' + cfg.roomId).then(function (room) { setRoom(room, !!cfg.picture); }).catch(function () {
            hint.textContent = 'Room ' + cfg.roomId + ' was not found.';
        });
    }
})();

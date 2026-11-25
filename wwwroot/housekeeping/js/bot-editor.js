// The bot editor (Areas/Housekeeping/Views/Bots/Edit.cshtml): four tabs next to a live preview of the bot as the hotel
// draws it in its room (BotsController.Picture -> the avatar renderer with the room's casts, at the room's zoom).
//   Look:   an avatar maker built from what the room can dress a bot in (BotsController.Parts): pictures of every
//           option, the room's own clothes first, the hotel's colour swatches; start from a random look, a Habbo's look
//           (LookOf) or another bot's.
//   Place:  the room's floor plan (Map: rooms_models + public_items + the other bots): click where it stands, paint
//           where it walks.
//   Talk:   its lines as chat bubbles (say / shout / whisper), answers optionally left to the AI (_AI_), its drinks.
//   Details: mission, and a summary.
// Everything ends in the form's hidden fields: figure (part=model/colour&...), StartLook ("head,body"), X, Y, Walkspace
// ("x,y x,y"), Speech / Response / UnrecognisedResponse ("|" between lines, "#SHOUT" / "#WHISPER" after one), HandItems.
(function () {
    'use strict';
    var cfg = window.botEditor;
    var $ = function (id) { return document.getElementById(id); };
    var esc = function (s) { return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); };
    var isNew = $('bot-form').getAttribute('data-new') === '1';

    // ===== the figure: [part, model, colour] in its own order (parts not shown here are kept as they are) =====
    var figure = [];
    function parseFigure(text) {
        return String(text || '').split('&').map(function (item) {
            var eq = item.indexOf('=');
            if (eq <= 0) return null;
            var rest = item.slice(eq + 1).split('/');
            return [item.slice(0, eq).trim().toLowerCase(), (rest[0] || '').trim(), (rest[1] || '').trim()];
        }).filter(Boolean);
    }
    function textOf(list) { return list.map(function (p) { return p[0] + '=' + p[1] + (p[2] ? '/' + p[2] : ''); }).join('&'); }
    function get(part, list) { return (list || figure).find(function (p) { return p[0] === part; }); }
    function set(part, model, colour, list) {
        list = list || figure;
        var p = get(part, list);
        if (!p) list.push(p = [part, '001', '']);
        if (model !== undefined) p[1] = model;
        if (colour !== undefined) p[2] = colour;
    }
    function copy(list) { return list.map(function (p) { return p.slice(); }); }
    function toHex(c) {
        var m = /^(\d+),(\d+),(\d+)$/.exec(c || '');
        return m ? '#' + [m[1], m[2], m[3]].map(function (v) { return ('0' + Math.min(255, +v).toString(16)).slice(-2); }).join('').toUpperCase() : '';
    }
    function fromHex(h) { var v = parseInt(h.replace('#', ''), 16); return [(v >> 16) & 255, (v >> 8) & 255, v & 255].join(','); }

    // ===== room, direction, pose =====
    function room() { var o = $('bot-room').selectedOptions[0]; return { id: +$('bot-room').value, name: o ? o.textContent.trim() : '', casts: o ? o.getAttribute('data-casts') : '' }; }
    var look = ($('bot-startlook').value || '2,2').split(',');
    var head = +look[0] || 0, body = look.length > 1 ? +look[1] || 0 : head;
    var pose = '', drink = 0;
    var turn = function (d) { return (d + 8) % 8; };
    document.querySelectorAll('[data-turn]').forEach(function (b) {
        b.addEventListener('click', function () {
            var by = +b.getAttribute('data-by');
            if (b.getAttribute('data-turn') === 'body') { body = turn(body + by); head = body; }
            else {
                var next = turn(head + by), diff = (next - body + 8) % 8; // a head turns at most one step from the body
                if (diff === 0 || diff === 1 || diff === 7) head = next;
            }
            changed();
        });
    });
    $('bot-poses').addEventListener('click', function (e) {
        var b = e.target.closest('button[data-pose]');
        if (!b) return;
        pose = b.getAttribute('data-pose');
        document.querySelectorAll('#bot-poses button').forEach(function (x) { x.classList.toggle('on', x === b); });
        preview();
    });

    // ===== tabs =====
    var TABS = ['look', 'place', 'talk', 'details'];
    var NAMES = { look: 'Look', place: 'Place', talk: 'Talk', details: 'Details' };
    var tab = 'look';
    function showTab(name) {
        tab = name;
        document.querySelectorAll('.be-panel').forEach(function (p) { p.hidden = p.getAttribute('data-panel') !== name; });
        document.querySelectorAll('#be-tabs button').forEach(function (b) { b.classList.toggle('on', b.getAttribute('data-tab') === name); });
        var i = TABS.indexOf(name);
        $('be-prev').hidden = i === 0;
        $('be-next').hidden = i === TABS.length - 1;
        if (i < TABS.length - 1) $('be-next').textContent = 'Next: ' + NAMES[TABS[i + 1]] + ' →';
        if (name === 'place') drawMap();
        if (name === 'details') summary();
    }
    $('be-tabs').addEventListener('click', function (e) { var b = e.target.closest('button[data-tab]'); if (b) showTab(b.getAttribute('data-tab')); });
    $('be-next').addEventListener('click', function () { showTab(TABS[Math.min(TABS.length - 1, TABS.indexOf(tab) + 1)]); });
    $('be-prev').addEventListener('click', function () { showTab(TABS[Math.max(0, TABS.indexOf(tab) - 1)]); });

    // ===== Look =====
    var CATS = [
        { key: 'hr', label: 'Hair / hat', parts: [{ part: 'hr', title: '' }], colour: ['hr'], head: true },
        { key: 'ch', label: 'Top', parts: [{ part: 'ch', title: 'Top' }, { part: 'ls', also: ['rs'], title: 'Sleeves' }], colour: ['ch'], sleeves: true },
        { key: 'lg', label: 'Bottoms', parts: [{ part: 'lg', title: '' }], colour: ['lg'] },
        { key: 'sh', label: 'Shoes', parts: [{ part: 'sh', title: '' }], colour: ['sh'] },
        { key: 'skin', label: 'Skin & head', parts: [{ part: 'hd', title: 'Head' }], colour: ['hd', 'fc', 'bd', 'lh', 'rh'], head: true },
        { key: 'face', label: 'Face', parts: [{ part: 'ey', title: 'Eyes' }, { part: 'fc', title: 'Face' }], head: true },
    ];
    var cat = CATS[0];
    var choices = null; // { size, zoom, parts: { hr: [{ model, room, players }] }, colours: { hr: ['#RRGGBB'] } }
    var sleevesFollow = true;

    $('bot-cats').innerHTML = CATS.map(function (c) { return '<button type="button" data-cat="' + c.key + '">' + esc(c.label) + '</button>'; }).join('');
    $('bot-cats').addEventListener('click', function (e) {
        var b = e.target.closest('button[data-cat]');
        if (!b) return;
        cat = CATS.find(function (c) { return c.key === b.getAttribute('data-cat'); });
        renderLook();
    });

    function thumb(list, isHead) {
        return cfg.picture + '?figure=' + encodeURIComponent(textOf(list)) + '&roomId=' + room().id + '&direction=2&headDirection=2' + (isHead ? '&head=true' : '');
    }

    function renderLook() {
        document.querySelectorAll('#bot-cats button').forEach(function (b) { b.classList.toggle('on', b.getAttribute('data-cat') === cat.key); });
        var box = $('bot-options');
        if (!choices) { box.innerHTML = '<p class="be-hint">Loading the room\'s clothes…</p>'; $('bot-colours').innerHTML = ''; return; }
        var html = '';
        cat.parts.forEach(function (row) {
            var list = choices.parts[row.part] || [];
            var current = (get(row.part) || [row.part, '001'])[1];
            if (row.title) html += '<div class="be-opt-title">' + esc(row.title) + '</div>';
            if (!list.length) {
                html += '<p class="be-hint">' + (row.part === 'ey' ? 'People drawn at 32 px have no separate eyes: the head has them.' : 'This room has none of these at its size.') + '</p>';
                return;
            }
            html += '<div class="be-grid">';
            list.forEach(function (c) {
                var variant = copy(figure);
                set(row.part, c.model, undefined, variant);
                (row.also || []).forEach(function (p) { set(p, c.model, undefined, variant); });
                var tag = c.room ? '<span class="be-tag room">this room</span>' : !c.players ? '<span class="be-tag staff">staff</span>' : '';
                html += '<button type="button" class="be-opt' + (c.model === current ? ' on' : '') + (cat.head ? ' head' : '') + '" data-part="' + row.part + '" data-also="' + (row.also || []).join(',') + '" data-model="' + esc(c.model) + '" title="' + esc(row.part + ' ' + c.model + (c.room ? ': only in this room' : !c.players ? ': not for players' : '')) + '">' +
                    '<img loading="lazy" src="' + esc(thumb(variant, cat.head)) + '" alt="" onload="this.style.width=(this.naturalWidth*' + (choices.size === 's' ? 3 : 2) + ')+\'px\'">' + tag + '</button>';
            });
            if (!list.some(function (c) { return c.model === current; })) html += '<span class="be-missing" title="This room has no such clothes at its size: the hotel draws nothing there">' + esc(current) + ' (not in this room)</span>';
            html += '</div>';
        });
        box.innerHTML = html;
        renderColours();
    }
    $('bot-options').addEventListener('click', function (e) {
        var b = e.target.closest('.be-opt');
        if (!b) return;
        var model = b.getAttribute('data-model');
        set(b.getAttribute('data-part'), model);
        (b.getAttribute('data-also') || '').split(',').filter(Boolean).forEach(function (p) { set(p, model); });
        changed();
        renderLook();
    });

    function renderColours() {
        var box = $('bot-colours');
        if (!cat.colour || !choices) { box.innerHTML = ''; return; }
        var part = cat.colour[0];
        var current = toHex((get(part) || [])[2]);
        // the colours players get for this very piece (as the hotel's own editor shows them); a room's or staff piece
        // has none of its own: every colour the hotel uses for that part
        var model = (get(cat.parts[0].part) || [])[1];
        var mine = ((choices.parts[cat.parts[0].part] || []).find(function (c) { return c.model === model; }) || {}).colours || [];
        var swatches = mine.length > 1 ? mine : (choices.colours && choices.colours[part]) || [];
        var html = '<div class="be-opt-title">Colour' + (mine.length > 1 ? '' : ' <span class="be-hint">(every colour the hotel uses for it)</span>') + '</div><div class="be-swatches">' +
            swatches.map(function (h) { return '<button type="button" class="be-swatch' + (h === current ? ' on' : '') + '" data-colour="' + h + '" style="background:' + h + '" title="' + h + '"></button>'; }).join('') +
            '<label class="be-custom" title="Any colour"><input type="color" id="bot-colour-any" value="' + (current || '#EEEEEE').toLowerCase() + '"> any</label>' +
            '<button type="button" class="be-nocolour" data-colour="" title="No colour: the hotel draws it light grey">none</button></div>';
        if (cat.sleeves) html += '<label class="be-check"><input type="checkbox" id="bot-sleeves-follow"' + (sleevesFollow ? ' checked' : '') + '> sleeves in the same colour</label>';
        box.innerHTML = html;
        var follow = $('bot-sleeves-follow');
        if (follow) follow.addEventListener('change', function () { sleevesFollow = follow.checked; applyColour(toHex((get('ch') || [])[2])); });
        $('bot-colour-any').addEventListener('input', function () { applyColour(this.value); });
    }
    $('bot-colours').addEventListener('click', function (e) {
        var b = e.target.closest('[data-colour]');
        if (b) applyColour(b.getAttribute('data-colour'));
    });
    var thumbTimer = 0;
    function applyColour(hex) {
        var value = hex ? fromHex(hex) : '';
        var parts = cat.colour.slice();
        if (cat.sleeves && sleevesFollow) parts.push('ls', 'rs');
        parts.forEach(function (p) { set(p, undefined, value); });
        changed();
        document.querySelectorAll('.be-swatch').forEach(function (s) { s.classList.toggle('on', s.getAttribute('data-colour') === (hex || '').toUpperCase()); });
        clearTimeout(thumbTimer);
        thumbTimer = setTimeout(renderLook, 400); // the pictures in the new colour
    }

    function loadParts() {
        choices = null;
        renderLook();
        fetch(cfg.parts + '?roomId=' + room().id, { credentials: 'same-origin' })
            .then(function (r) { if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'HTTP ' + r.status); }); return r.json(); })
            .then(function (json) { choices = json; renderLook(); changed(); drawMap(); })
            .catch(function (err) { $('bot-options').innerHTML = '<p class="bot-error">Couldn\'t load the room\'s clothes: ' + esc(err.message) + '</p>'; changed(); });
    }

    // start from: random, a Habbo, another bot
    function pick(list) { return list[Math.floor(Math.random() * list.length)]; }
    $('bot-random').addEventListener('click', function () {
        if (!choices) return;
        var models = function (part) {
            var all = choices.parts[part] || [];
            var good = all.filter(function (c) { return c.players || c.room; });
            return (good.length ? good : all).map(function (c) { return c.model; });
        };
        var colours = function (part) { return (choices.colours && choices.colours[part]) || ['#EEEEEE']; };
        ['hr', 'ch', 'lg', 'sh', 'hd'].forEach(function (part) { var m = models(part); if (m.length) set(part, pick(m)); });
        var skin = fromHex(pick(colours('hd')));
        ['hd', 'fc', 'bd', 'lh', 'rh'].forEach(function (p) { set(p, undefined, skin); });
        ['hr', 'ch', 'lg', 'sh'].forEach(function (p) { set(p, undefined, fromHex(pick(colours(p)))); });
        var sleeves = models('ls');
        if (sleeves.length) { var s = pick(sleeves); set('ls', s, get('ch')[2]); set('rs', s, get('ch')[2]); }
        sleevesFollow = true;
        changed();
        renderLook();
    });
    function useFigure(text) {
        var next = parseFigure(text);
        if (!next.length) return;
        figure = next;
        sleevesFollow = (get('ls') || [])[2] === (get('ch') || [])[2];
        changed();
        renderLook();
    }
    $('bot-habbo-use').addEventListener('click', function () {
        var name = $('bot-habbo').value.trim();
        if (!name) return $('bot-habbo').focus();
        fetch(cfg.lookOf + '?username=' + encodeURIComponent(name), { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : r.text().then(function (t) { throw new Error(t); }); })
            .then(function (json) { useFigure(json.figure); flash('Now wearing ' + name + '\'s look'); })
            .catch(function (err) { flash(err.message || 'No such Habbo', true); });
    });
    $('bot-habbo').addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); $('bot-habbo-use').click(); } });
    $('bot-copy').addEventListener('change', function () { if (this.value) { useFigure(this.value); flash('Now wearing ' + this.selectedOptions[0].textContent + '\'s look'); } this.value = ''; });
    $('bot-figure-text').addEventListener('input', function () { figure = parseFigure(this.value); changed(); });

    function flash(text, bad) {
        var el = $('bot-zoom-note');
        el.textContent = text;
        el.classList.toggle('bad', !!bad);
        clearTimeout(flash.t);
        flash.t = setTimeout(function () { el.classList.remove('bad'); zoomNote(); }, 2500);
    }

    // ===== preview, bubble, warnings =====
    var previewTimer = 0, shownUrl = '', botImage = new Image();
    function pictureUrl() {
        return cfg.picture + '?figure=' + encodeURIComponent(textOf(figure)) + '&roomId=' + room().id + '&direction=' + body + '&headDirection=' + head +
            (pose ? '&action=' + pose : '') + (drink ? '&drink=' + drink : '');
    }
    function preview() {
        clearTimeout(previewTimer);
        previewTimer = setTimeout(function () {
            var url = pictureUrl();
            if (url === shownUrl) return;
            shownUrl = url;
            var img = $('bot-preview');
            img.onload = function () { img.style.width = (img.naturalWidth * (img.naturalWidth <= 40 ? 5 : 3)) + 'px'; $('bot-stage').classList.remove('failed'); };
            img.onerror = function () { $('bot-stage').classList.add('failed'); };
            img.src = url;
            // the map draws the bot at its tile, standing
            var still = cfg.picture + '?figure=' + encodeURIComponent(textOf(figure)) + '&roomId=' + room().id + '&direction=' + body + '&headDirection=' + head;
            if (botImage.src.indexOf(still) < 0) { botImage = new Image(); botImage.onload = drawMap; botImage.src = still; }
        }, 150);
    }
    function bubble(text, mode) {
        var b = $('bot-bubble');
        if (!text) { b.hidden = true; return; }
        b.textContent = ($('bot-name').value.trim() || 'Bot') + ': ' + text;
        b.className = 'be-bubble' + (mode ? ' ' + mode.toLowerCase() : '');
        b.hidden = false;
        clearTimeout(bubble.t);
        bubble.t = setTimeout(function () { b.hidden = true; }, 3500);
    }
    var playing = 0;
    $('bot-play').addEventListener('click', function () {
        if (playing) { clearInterval(playing); playing = 0; this.textContent = '▶ Play its lines'; bubble(''); return; }
        var lines = chats.flatMap(function (c) { return c.lines(); });
        if (!lines.length) return flash('It has no lines yet: add some in Talk', true);
        var i = 0;
        var self = this;
        var next = function () { var l = lines[i++ % lines.length]; bubble(l.text, l.mode); pose = 'spk'; preview(); };
        next();
        playing = setInterval(next, 2600);
        self.textContent = '■ Stop';
    });

    function zoomNote() {
        $('bot-zoom-note').textContent = choices ? 'As the hotel draws it in ' + room().name + (choices.size === 's' ? ' (people there are 32 px)' : '') + ', enlarged' : '';
    }
    function warnings() {
        var out = [];
        if (choices) {
            CATS.forEach(function (c) {
                c.parts.forEach(function (row) {
                    var p = get(row.part), list = choices.parts[row.part] || [];
                    if (p && list.length && !list.some(function (x) { return x.model === p[1]; }))
                        out.push((row.title || c.label) + ' ' + p[1] + ': this room has no such clothes at its size, so the hotel draws nothing there');
                });
            });
        }
        if (map && !tileFree(+$('bot-x').value, +$('bot-y').value)) out.push('It stands on a tile with no floor or on furniture: pick another in Place');
        var html = out.map(function (w) { return '<li>' + esc(w) + '</li>'; }).join('');
        if (map && tileFree(+$('bot-x').value, +$('bot-y').value)) {
            // walk tiles Odin's pathfinder can't take it to: it stands still when it picks one
            var lost = unreachableWalk(), bare = lost.filter(function (k) { var c = k.split(','); return !isFloor(+c[0], +c[1]); }).length;
            if (lost.length) html += '<li>' + esc(
                (bare ? bare + ' tile' + (bare === 1 ? '' : 's') + ' where it walks ha' + (bare === 1 ? 's' : 've') + ' no floor in this room' : '') +
                (bare && lost.length > bare ? ', and ' : '') +
                (lost.length > bare ? (lost.length - bare) + ' can\'t be reached from where it stands (furniture, or a step higher than 1.5)' : '') +
                '. It stands still when it picks one.') + ' <button type="button" data-fix="walk">Remove them</button></li>';
        }
        $('bot-warnings').innerHTML = html;
    }

    $('bot-warnings').addEventListener('click', function (e) {
        if (!e.target.closest('[data-fix=walk]')) return;
        unreachableWalk().forEach(function (k) { walk.delete(k); });
        saveWalk();
        drawMap();
        warnings();
        placeNote();
    });

    function changed() {
        $('bot-figure').value = textOf(figure);
        if (document.activeElement !== $('bot-figure-text')) $('bot-figure-text').value = textOf(figure);
        $('bot-startlook').value = head + ',' + body;
        zoomNote();
        warnings();
        preview();
        placeNote();
    }

    // ===== Place: the room's floor plan =====
    // As Odin reads it (RoomModel): a digit is floor at that height, anything else no floor; the door tile is floor at
    // door_z. Drawn like the client's Room Geometry: a tile is lifted by its height * #factorh of the room's layout.
    // Habbos (and bots) can't step up or down more than 1.5 (Pathfinder.MAX_LIFT_HEIGHT).
    var map = null; // { rows, door, bots, solid: Set, seats: Set, names, tiles: [{ x, y, h }] in drawing order, minH, maxH }
    var walk = new Set(($('bot-walkspace').value || '').trim().split(/\s+/).filter(function (t) { return /^\d+,\d+$/.test(t); }));
    var tool = 'stand';
    var T = 32, HW = 16, HH = 8, PAD = 24, TOP = 70, MAX_STEP = 1.5;
    var canvas = $('bot-map'), ctx = canvas.getContext('2d');
    var key = function (x, y) { return x + ',' + y; };
    var STEPS = [[1, 0], [-1, 0], [0, 1], [0, -1]];

    function loadMap() {
        map = null;
        drawMap();
        fetch(cfg.map + '?roomId=' + room().id + '&botId=' + cfg.botId, { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : r.text().then(function (t) { throw new Error(t || 'HTTP ' + r.status); }); })
            .then(function (json) {
                var solid = new Set(), seats = new Set(), names = {};
                json.furni.forEach(function (f) {
                    var w = Math.max(1, f.width || 1), l = Math.max(1, f.length || 1);
                    if (f.rotation === 2 || f.rotation === 6) { var t = w; w = l; l = t; }
                    for (var i = 0; i < w; i++) for (var j = 0; j < l; j++) {
                        var k = key(f.x + i, f.y + j);
                        names[k] = f.sprite;
                        if (/can_sit_on_top|can_lay_on_top/.test(f.behaviour || '')) seats.add(k);
                        else if (/solid/.test(f.behaviour || '')) solid.add(k);
                    }
                });
                map = { rows: json.rows, door: json.door, bots: json.bots, solid: solid, seats: seats, names: names };
                var tiles = [];
                var cols = Math.max.apply(null, json.rows.map(function (r) { return r.length; }).concat([json.door.x + 1]));
                var rows = Math.max(json.rows.length, json.door.y + 1);
                for (var y = 0; y < rows; y++) for (var x = 0; x < cols; x++) { var h = height(x, y); if (h !== null) tiles.push({ x: x, y: y, h: h }); }
                tiles.sort(function (a, b) { return (a.x + a.y) - (b.x + b.y) || a.x - b.x; }); // back to front, as the client's locZ
                map.tiles = tiles;
                map.cols = cols; map.rowCount = rows;
                map.minH = Math.min.apply(null, tiles.map(function (t) { return t.h; }).concat([0]));
                map.maxH = Math.max.apply(null, tiles.map(function (t) { return t.h; }).concat([0]));
                // a new bot, or one moved to another room, starts on the nearest free tile inside the door
                if ((isNew || roomMoved) && !tileFree(+$('bot-x').value, +$('bot-y').value)) nearDoor();
                drawMap();
                changed();
            })
            .catch(function (err) { map = null; $('bot-place-note').textContent = 'Couldn\'t load the room\'s floor plan: ' + err.message; });
    }
    function height(x, y) {
        if (!map) return null;
        if (map.door && map.door.x === x && map.door.y === y) return +map.door.z || 0;
        var row = map.rows[y], c = row ? row.charAt(x) : '';
        return /^[0-9]$/.test(c) ? +c : null;
    }
    function isFloor(x, y) { return height(x, y) !== null; }
    function tileFree(x, y) { return isFloor(x, y) && !map.solid.has(key(x, y)); }
    function canStep(x1, y1, x2, y2) { return Math.abs(height(x1, y1) - height(x2, y2)) <= MAX_STEP; }
    function isDoor(x, y) { return !!map.door && map.door.x === x && map.door.y === y; }
    // where a Habbo can walk from (x, y): free floor, no seats in the way, no step over 1.5
    function reachable(x, y) {
        var seen = new Set([key(x, y)]), queue = [[x, y]];
        while (queue.length) {
            var q = queue.shift();
            STEPS.forEach(function (d) {
                var nx = q[0] + d[0], ny = q[1] + d[1], k = key(nx, ny);
                if (seen.has(k) || !tileFree(nx, ny) || !canStep(q[0], q[1], nx, ny)) return;
                seen.add(k);
                if (!map.seats.has(k)) queue.push([nx, ny]); // a seat can be walked to, not through
            });
        }
        return seen;
    }
    function unreachableWalk() {
        if (!map) return [];
        var r = reachable(+$('bot-x').value, +$('bot-y').value);
        return Array.from(walk).filter(function (k) { return !r.has(k); });
    }
    var roomMoved = false;
    function nearDoor() {
        var d = map.door || { x: 0, y: 0 }, seen = new Set([key(d.x, d.y)]), queue = [[d.x, d.y]];
        while (queue.length) {
            var q = queue.shift();
            if (!isDoor(q[0], q[1]) && tileFree(q[0], q[1]) && !map.seats.has(key(q[0], q[1])) && !map.bots.some(function (b) { return b.x === q[0] && b.y === q[1]; })) {
                $('bot-x').value = q[0]; $('bot-y').value = q[1];
                walk = new Set([key(q[0], q[1])]);
                saveWalk();
                return;
            }
            STEPS.forEach(function (dxy) {
                var nx = q[0] + dxy[0], ny = q[1] + dxy[1], k = key(nx, ny);
                if (seen.has(k) || !tileFree(nx, ny) || !canStep(q[0], q[1], nx, ny)) return;
                seen.add(k);
                queue.push([nx, ny]);
            });
        }
    }
    // pixels one height level lifts a tile at the map's tile width (the layout's #factorh at its #factorx)
    function lift() { return choices && choices.heightFactor ? choices.heightFactor * T / (choices.zoom || 32) : 2 * HH; }
    function size() {
        var L = lift(), rise = (map.maxH - map.minH) * L;
        return { L: L, ox: PAD + map.rowCount * HW, oy: PAD + TOP + rise, w: (map.cols + map.rowCount) * HW + PAD * 2, h: (map.cols + map.rowCount) * HH + PAD * 2 + TOP + rise };
    }
    // a tile's left corner on the canvas, lifted by its height
    function screen(x, y, s) { var h = height(x, y); return { x: s.ox + (x - y) * HW, y: s.oy + (x + y) * HH - ((h === null ? map.minH : h) - map.minH) * s.L }; }
    function path(points) { ctx.beginPath(); ctx.moveTo(points[0], points[1]); for (var i = 2; i < points.length; i += 2) ctx.lineTo(points[i], points[i + 1]); ctx.closePath(); }
    function diamond(p, fill, stroke, width) {
        path([p.x, p.y, p.x + HW, p.y - HH, p.x + T, p.y, p.x + HW, p.y + HH]);
        if (fill) { ctx.fillStyle = fill; ctx.fill(); }
        if (stroke) { ctx.lineWidth = width || 1; ctx.strokeStyle = stroke; ctx.stroke(); ctx.lineWidth = 1; }
    }
    function drawMap() {
        if (tab !== 'place') return;
        if (!map) { canvas.width = 300; canvas.height = 60; ctx.fillStyle = '#888'; ctx.fillText('Loading the floor plan…', 10, 30); return; }
        var s = size();
        canvas.width = s.w; canvas.height = s.h;
        // shown as big as the panel allows (at most twice), pixel for pixel
        var avail = canvas.parentElement.clientWidth - 2;
        canvas.style.width = Math.min(avail, s.w * 2) + 'px';
        ctx.clearRect(0, 0, s.w, s.h);
        ctx.lineWidth = 1;
        var sx = +$('bot-x').value, sy = +$('bot-y').value;
        var lost = new Set(unreachableWalk());
        var others = {};
        map.bots.forEach(function (b) { (b.walk || '').split(/\s+/).forEach(function (k) { if (k) others[k] = true; }); });
        map.tiles.forEach(function (t) {
            var p = screen(t.x, t.y, s), k = key(t.x, t.y), drop = (t.h - map.minH) * s.L;
            if (drop > 0) { // the block's two visible sides, down to the room's lowest floor (tiles in front paint over them)
                path([p.x, p.y, p.x + HW, p.y + HH, p.x + HW, p.y + HH + drop, p.x, p.y + drop]);
                ctx.fillStyle = '#b9ae8b'; ctx.fill();
                path([p.x + HW, p.y + HH, p.x + T, p.y, p.x + T, p.y + drop, p.x + HW, p.y + HH + drop]);
                ctx.fillStyle = '#a69c7b'; ctx.fill();
            }
            diamond(p, 'hsl(45, 22%, ' + Math.min(90, 80 + (t.h - map.minH) * 2) + '%)', '#c4bb9e');
            if (map.solid.has(k)) diamond(p, 'rgba(120, 82, 45, 0.55)');
            else if (map.seats.has(k)) diamond(p, 'rgba(60, 110, 190, 0.45)');
            if (isDoor(t.x, t.y)) diamond(p, 'rgba(40, 160, 80, 0.7)');
            if (others[k]) diamond(p, 'rgba(140, 140, 160, 0.25)');
            if (walk.has(k)) diamond(p, lost.has(k) ? 'rgba(220, 40, 30, 0.45)' : 'rgba(245, 150, 20, 0.55)', lost.has(k) ? '#b3261e' : '#d27a00');
            if (t.x === sx && t.y === sy) diamond(p, null, '#000', 2);
        });
        // the other bots, and this one at its tile
        ctx.font = '11px Verdana, sans-serif';
        map.bots.forEach(function (b) {
            var p = screen(b.x, b.y, s);
            ctx.fillStyle = '#556';
            ctx.beginPath(); ctx.arc(p.x + HW, p.y - 4, 4, 0, Math.PI * 2); ctx.fill();
            ctx.fillText(b.name, p.x + HW + 6, p.y - 2);
        });
        if (botImage.complete && botImage.naturalWidth && isFloor(sx, sy)) {
            var p = screen(sx, sy, s);
            var w = botImage.naturalWidth, hgt = botImage.naturalHeight, scale = w > 40 ? 0.5 : 1;
            ctx.drawImage(botImage, p.x + HW - (w * scale) / 2, p.y + 4 - hgt * scale, w * scale, hgt * scale);
        }
        placeNote();
    }
    // the tile under the mouse: the front-most block (its top or its sides) that holds the point, as it is drawn
    function inside(px, py, pts) {
        var hit = false;
        for (var i = 0, j = pts.length - 2; i < pts.length; j = i, i += 2) {
            if ((pts[i + 1] > py) !== (pts[j + 1] > py) && px < (pts[j] - pts[i]) * (py - pts[i + 1]) / (pts[j + 1] - pts[i + 1]) + pts[i]) hit = !hit;
        }
        return hit;
    }
    function tileAt(ev) {
        if (!map) return null;
        var s = size(), r = canvas.getBoundingClientRect();
        var px = (ev.clientX - r.left) * (canvas.width / r.width), py = (ev.clientY - r.top) * (canvas.height / r.height);
        for (var i = map.tiles.length - 1; i >= 0; i--) {
            var t = map.tiles[i], p = screen(t.x, t.y, s), drop = (t.h - map.minH) * s.L;
            if (inside(px, py, [p.x, p.y, p.x + HW, p.y - HH, p.x + T, p.y, p.x + T, p.y + drop, p.x + HW, p.y + HH + drop, p.x, p.y + drop])) return t;
        }
        return null;
    }
    var painting = false;
    function apply(t) {
        if (!map || !t) return;
        var k = key(t.x, t.y);
        if (tool === 'stand') {
            if (!tileFree(t.x, t.y)) return flash('It can\'t stand on furniture', true);
            $('bot-x').value = t.x; $('bot-y').value = t.y;
            walk.add(k);
            preview();
        } else if (tool === 'paint') { if (tileFree(t.x, t.y)) walk.add(k); }
        else walk.delete(k);
        saveWalk();
        drawMap();
        warnings();
    }
    canvas.addEventListener('mousedown', function (ev) { painting = tool !== 'stand'; apply(tileAt(ev)); });
    window.addEventListener('mouseup', function () { painting = false; });
    var lastTip = '';
    canvas.addEventListener('mousemove', function (ev) {
        if (!map) return;
        var t = tileAt(ev), tip = $('bot-map-tip');
        if (painting) apply(t);
        if (!t) { tip.hidden = true; return; }
        var k = key(t.x, t.y);
        var text = t.x + ',' + t.y + ' · height ' + t.h + (map.names[k] ? ' · ' + map.names[k] : '') + (isDoor(t.x, t.y) ? ' · door' : '') +
            map.bots.filter(function (b) { return b.x === t.x && b.y === t.y; }).map(function (b) { return ' · ' + b.name; }).join('');
        if (text !== lastTip) { tip.textContent = text; lastTip = text; }
        var wr = canvas.parentElement.getBoundingClientRect();
        tip.style.left = (ev.clientX - wr.left + canvas.parentElement.scrollLeft + 12) + 'px';
        tip.style.top = (ev.clientY - wr.top + canvas.parentElement.scrollTop + 12) + 'px';
        tip.hidden = false;
    });
    canvas.addEventListener('mouseleave', function () { $('bot-map-tip').hidden = true; });
    $('bot-tools').addEventListener('click', function (e) {
        var b = e.target.closest('button');
        if (!b) return;
        if (b.hasAttribute('data-tool')) {
            tool = b.getAttribute('data-tool');
            document.querySelectorAll('#bot-tools [data-tool]').forEach(function (x) { x.classList.toggle('on', x === b); });
        } else if (b.hasAttribute('data-radius')) {
            if (!map) return;
            // every free tile within n steps of where it stands, walking round furniture and over steps of at most 1.5
            var n = +b.getAttribute('data-radius'), sx = +$('bot-x').value, sy = +$('bot-y').value;
            var seen = new Set([key(sx, sy)]), queue = [[sx, sy, 0]];
            while (queue.length) {
                var q = queue.shift();
                STEPS.forEach(function (dxy) {
                    var nx = q[0] + dxy[0], ny = q[1] + dxy[1], k = key(nx, ny);
                    if (q[2] >= n || seen.has(k) || !tileFree(nx, ny) || map.seats.has(k) || isDoor(nx, ny) || !canStep(q[0], q[1], nx, ny)) return;
                    seen.add(k);
                    queue.push([nx, ny, q[2] + 1]);
                });
            }
            walk = seen;
            saveWalk();
            drawMap();
            warnings();
        }
    });
    $('bot-walk-clear').addEventListener('click', function () { walk = new Set([key($('bot-x').value, $('bot-y').value)]); saveWalk(); drawMap(); warnings(); });
    function saveWalk() { $('bot-walkspace').value = Array.from(walk).join(' '); }
    function placeNote() {
        var h = map ? height(+$('bot-x').value, +$('bot-y').value) : null;
        $('bot-place-note').textContent = 'Stands on ' + $('bot-x').value + ',' + $('bot-y').value + (h !== null ? ' (height ' + h + ')' : '') + ', facing ' + body + (head !== body ? ' (looking ' + head + ')' : '') +
            ' · walks on ' + walk.size + ' tile' + (walk.size === 1 ? '' : 's') + ' · turn it with the buttons under the preview';
    }

    // ===== Talk: chat bubbles =====
    var MODES = ['', 'SHOUT', 'WHISPER'];
    var MODE_LABEL = { '': 'says', SHOUT: 'shouts', WHISPER: 'whispers' };
    var chats = [];
    // %drink% in a serving line, as the hotel fills it in (the drink held in the preview, else its first drink)
    function filled(text) {
        var d = holding || drinks()[0] || 'Vand';
        return text.replace(/%lowercaseDrink%/g, d.toLowerCase()).replace(/%drink%/g, d);
    }
    document.querySelectorAll('.be-chat[data-for]').forEach(function (box) {
        var hidden = $(box.getAttribute('data-for'));
        var raw = hidden.value || '';
        var ai = raw.indexOf('_AI_') >= 0;
        var lines = raw.replace('_AI_', '').split('|').map(function (l) {
            // "text#SHOUT" / "#WHISPER" / "#CHAT" (Odin's BotSpeech); any other # is part of the text
            var hash = l.lastIndexOf('#'), tag = hash > 0 ? l.slice(hash + 1).trim().toUpperCase() : '';
            var known = tag === 'SHOUT' || tag === 'WHISPER' || tag === 'CHAT';
            return { text: (known ? l.slice(0, hash) : l).trim(), mode: tag === 'SHOUT' || tag === 'WHISPER' ? tag : '' };
        }).filter(function (l) { return l.text; });
        box.innerHTML = '<div class="be-chat-head"><span class="be-label">' + esc(box.getAttribute('data-title')) + '</span>' +
            (box.getAttribute('data-ai') ? '<label class="be-check" title="With this on the hotel says none of these lines itself (_AI_): the AI answers instead. The lines are kept."><input type="checkbox" class="be-ai"' + (ai ? ' checked' : '') + '> the AI answers instead</label>' : '') +
            '</div><p class="be-hint">' + esc(box.getAttribute('data-hint')) + '</p><div class="be-bubbles"></div>' +
            '<div class="be-say"><input type="text" maxlength="200" placeholder="Type a line and press Enter"><select>' + MODES.map(function (m) { return '<option value="' + m + '">' + MODE_LABEL[m] + '</option>'; }).join('') + '</select>' +
            (box.getAttribute('data-drink') ? '<button type="button" class="be-insert" title="Where the drink\'s name goes">+ %drink%</button>' : '') + '<button type="button" class="be-add">Add</button></div>';
        var list = box.querySelector('.be-bubbles'), input = box.querySelector('.be-say input'), modeSel = box.querySelector('.be-say select');
        var aiBox = box.querySelector('.be-ai');
        function save() {
            hidden.value = (aiBox && aiBox.checked ? '_AI_' : '') + lines.map(function (l) { return l.text.replace(/[|#]/g, '') + (l.mode ? '#' + l.mode : ''); }).join('|');
        }
        function render() {
            list.innerHTML = lines.map(function (l, i) {
                return '<div class="be-line ' + (l.mode || 'say').toLowerCase() + '" data-i="' + i + '"><span class="be-line-text" contenteditable="true" spellcheck="true">' + esc(l.text) + '</span>' +
                    '<span class="be-line-tools"><button type="button" data-act="mode" title="Say, shout or whisper">' + MODE_LABEL[l.mode] + '</button><button type="button" data-act="show" title="Show it above the bot">▶</button><button type="button" data-act="del" title="Remove">×</button></span></div>';
            }).join('') || '<p class="be-hint be-empty">No lines yet.</p>';
        }
        function add() {
            var text = input.value.replace(/[|#]/g, '').trim();
            if (!text) return;
            lines.push({ text: text, mode: modeSel.value });
            input.value = '';
            save(); render();
            sayIt(text, modeSel.value);
        }
        box.querySelector('.be-say .be-add').addEventListener('click', add);
        var insert = box.querySelector('.be-insert');
        if (insert) insert.addEventListener('click', function () {
            var at = input.selectionStart == null ? input.value.length : input.selectionStart;
            input.value = input.value.slice(0, at) + '%drink%' + input.value.slice(input.selectionEnd == null ? at : input.selectionEnd);
            input.focus();
            input.selectionStart = input.selectionEnd = at + 7;
        });
        function sayIt(text, mode) { bubble(box.getAttribute('data-drink') ? filled(text) : text, mode); }
        input.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); add(); } });
        list.addEventListener('click', function (e) {
            var b = e.target.closest('button[data-act]');
            if (!b) return;
            var i = +b.closest('.be-line').getAttribute('data-i'), act = b.getAttribute('data-act');
            if (act === 'del') lines.splice(i, 1);
            else if (act === 'mode') lines[i].mode = MODES[(MODES.indexOf(lines[i].mode) + 1) % MODES.length];
            if (act === 'show') return sayIt(lines[i].text, lines[i].mode);
            save(); render();
        });
        list.addEventListener('input', function (e) {
            var el = e.target.closest('.be-line');
            if (!el) return;
            lines[+el.getAttribute('data-i')].text = el.querySelector('.be-line-text').textContent.replace(/[|#]/g, '').trim();
            save();
        });
        list.addEventListener('keydown', function (e) { if (e.key === 'Enter' && e.target.classList.contains('be-line-text')) { e.preventDefault(); e.target.blur(); } });
        if (aiBox) aiBox.addEventListener('change', save);
        render();
        chats.push({ lines: function () { return box.getAttribute('data-drink') ? lines.map(function (l) { return { text: filled(l.text), mode: l.mode }; }) : lines; }, title: box.getAttribute('data-title'), ai: function () { return !!(aiBox && aiBox.checked); } });
    });

    // drinks: names. One the hotel has a hand item for (handitemN) is drawn as that item.
    var HAND = cfg.handItems || {};
    function handItemOf(name) {
        var n = String(name || '').trim().toLowerCase();
        for (var k in HAND) if (HAND[k].toLowerCase() === n) return +k;
        return 0;
    }
    function drinks() { return ($('bot-handitems').value || '').split(',').map(function (d) { return d.trim(); }).filter(Boolean); }
    var holding = '';
    function showDrinks() {
        var list = drinks();
        $('bot-drink-list').innerHTML = list.map(function (d, i) {
            var item = handItemOf(d);
            return '<span class="be-drink' + (d === holding ? ' on' : '') + '" title="' + (item ? 'Hand item ' + item : 'Not one of the hotel\'s hand items: Habbos get a drink called ' + esc(d) + ', which the hotel has no picture of') + '">' +
                esc(d) + (item ? '' : ' <small>(no picture)</small>') +
                '<button type="button" data-hold="' + i + '" title="Hold it in the preview">✋</button><button type="button" data-rm="' + i + '" title="Remove">×</button></span>';
        }).join('') || '<span class="be-hint">None: it serves nothing.</span>';
        $('bot-carry').innerHTML = '<option value="">no drink</option>' + list.map(function (d) { return '<option' + (d === holding ? ' selected' : '') + '>' + esc(d) + '</option>'; }).join('');
        $('bot-handitem-chips').innerHTML = Object.keys(HAND).length ? '<span class="be-hint">The hotel\'s hand items:</span> ' + Object.keys(HAND).map(function (k) {
            var has = list.some(function (d) { return d.toLowerCase() === HAND[k].toLowerCase(); });
            return '<button type="button" class="be-chip' + (has ? ' on' : '') + '" data-add="' + esc(HAND[k]) + '" title="Hand item ' + k + '">' + esc(HAND[k]) + '</button>';
        }).join('') : '';
    }
    function hold(name) { holding = name || ''; drink = handItemOf(holding); showDrinks(); preview(); }
    $('bot-carry').addEventListener('change', function () { hold(this.value); });
    $('bot-drink-list').addEventListener('click', function (e) {
        var b = e.target.closest('button');
        if (!b) return;
        var list = drinks();
        if (b.hasAttribute('data-hold')) return hold(list[+b.getAttribute('data-hold')]);
        var gone = list.splice(+b.getAttribute('data-rm'), 1)[0];
        $('bot-handitems').value = list.join(',');
        if (gone === holding) hold(''); else showDrinks();
    });
    function addDrink(name) {
        name = String(name || '').replace(/[,|#]/g, '').replace(/\s+/g, ' ').trim();
        if (!name) return $('bot-drink-new').focus();
        var list = drinks();
        if (list.some(function (d) { return d.toLowerCase() === name.toLowerCase(); })) return hold(name);
        if ((list.concat(name)).join(',').length > 50) return flash('The drinks are at most 50 characters together', true);
        list.push(name);
        $('bot-handitems').value = list.join(',');
        $('bot-drink-new').value = '';
        hold(name);
    }
    $('bot-drink-add').addEventListener('click', function () { addDrink($('bot-drink-new').value); });
    $('bot-drink-new').addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); addDrink(this.value); } });
    $('bot-handitem-chips').addEventListener('click', function (e) {
        var b = e.target.closest('button[data-add]');
        if (!b) return;
        var name = b.getAttribute('data-add'), list = drinks();
        var at = list.findIndex(function (d) { return d.toLowerCase() === name.toLowerCase(); });
        if (at >= 0) { list.splice(at, 1); $('bot-handitems').value = list.join(','); if (holding.toLowerCase() === name.toLowerCase()) hold(''); else showDrinks(); }
        else addDrink(name);
    });

    // ===== Details: what it all adds up to =====
    function summary() {
        var parts = [
            ['Name', $('bot-name').value.trim() || '(no name yet)'],
            ['Room', room().name],
            ['Stands on', $('bot-x').value + ',' + $('bot-y').value + ', facing ' + body + ', walks on ' + walk.size + ' tiles'],
        ];
        chats.forEach(function (c) { parts.push([c.title, c.ai() ? 'the AI answers (' + c.lines().length + ' lines to start from)' : c.lines().length + ' line' + (c.lines().length === 1 ? '' : 's')]); });
        parts.push(['Drinks', drinks().join(', ') || 'none']);
        $('bot-summary').innerHTML = '<table>' + parts.map(function (p) { return '<tr><th>' + esc(p[0]) + '</th><td>' + esc(p[1]) + '</td></tr>'; }).join('') + '</table>';
    }

    // ===== save =====
    $('bot-form').addEventListener('submit', function (e) {
        saveWalk();
        if (!$('bot-name').value.trim()) { e.preventDefault(); showTab('details'); $('bot-name').focus(); return flash('Give it a name first', true); }
        if (map && !tileFree(+$('bot-x').value, +$('bot-y').value)) { e.preventDefault(); showTab('place'); return flash('Pick where it stands first', true); }
    });
    $('bot-room').addEventListener('change', function () { roomMoved = true; loadParts(); loadMap(); });

    // ===== start =====
    figure = parseFigure($('bot-figure').value);
    sleevesFollow = (get('ls') || [])[2] === (get('ch') || [])[2];
    showDrinks();
    showTab('look');
    changed();
    loadParts();
    loadMap();
    if (isNew) $('bot-name').focus();
})();

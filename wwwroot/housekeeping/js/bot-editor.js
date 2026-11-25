// The bot edit page (Areas/Housekeeping/Views/Bots/Edit.cshtml): a live preview of the bot as the hotel draws it in its
// room (BotsController.Picture -> the avatar renderer with the room's casts), a look editor built from what the room can
// draw (BotsController.Parts), and list editors for what it says and the drinks it serves. Everything ends up in the
// form's hidden fields: figure (part=model/colour&...), StartLook ("head,body"), Speech / Response /
// UnrecognisedResponse ("|" between lines, "#SHOUT" / "#WHISPER" after one), HandItems ("1,2").
(function () {
    'use strict';
    var cfg = window.botEditor;
    var $ = function (id) { return document.getElementById(id); };
    var esc = function (s) { return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); };

    // --- the figure: [part, model, colour] in its own order (parts the editor doesn't show are kept as they are) ---
    var figure = [];
    function parseFigure(text) {
        return String(text || '').split('&').map(function (item) {
            var eq = item.indexOf('=');
            if (eq <= 0) return null;
            var rest = item.slice(eq + 1).split('/');
            return [item.slice(0, eq).trim().toLowerCase(), (rest[0] || '').trim(), (rest[1] || '').trim()];
        }).filter(Boolean);
    }
    function figureText() {
        return figure.map(function (p) { return p[0] + '=' + p[1] + (p[2] ? '/' + p[2] : ''); }).join('&');
    }
    function get(part) { return figure.find(function (p) { return p[0] === part; }); }
    function set(part, model, colour) {
        var p = get(part);
        if (!p) figure.push(p = [part, '001', '']);
        if (model !== undefined) p[1] = model;
        if (colour !== undefined) p[2] = colour;
    }
    // colours: "r,g,b" (the client's), a palette number, or nothing
    function toHex(c) {
        var m = /^(\d+),(\d+),(\d+)$/.exec(c || '');
        if (!m) return '';
        return '#' + [m[1], m[2], m[3]].map(function (v) { return ('0' + Math.min(255, +v).toString(16)).slice(-2); }).join('');
    }
    function fromHex(h) {
        var v = parseInt(h.slice(1), 16);
        return [(v >> 16) & 255, (v >> 8) & 255, v & 255].join(',');
    }

    // --- direction, pose, drink ---
    var look = ($('bot-startlook').value || '2,2').split(',');
    var head = +look[0] || 0, body = look.length > 1 ? +look[1] || 0 : head;
    var pose = '', drink = 0;
    var turn = function (d) { return (d + 8) % 8; };
    document.querySelectorAll('[data-turn]').forEach(function (b) {
        b.addEventListener('click', function () {
            var by = +b.getAttribute('data-by');
            if (b.getAttribute('data-turn') === 'body') { body = turn(body + by); head = body; }
            else {
                // the client turns a head at most one step away from the body
                var next = turn(head + by), diff = (next - body + 8) % 8;
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
    $('bot-carry').addEventListener('change', function () { drink = +this.value || 0; preview(); });

    // --- the room's clothes ---
    var ROWS = [
        { label: 'Hair / hat', parts: ['hr'], colour: true },
        { label: 'Head', parts: ['hd'] },
        { label: 'Skin', parts: [], skin: ['hd', 'fc', 'bd', 'lh', 'rh'] },
        { label: 'Eyes', parts: ['ey'] },
        { label: 'Face', parts: ['fc'] },
        { label: 'Shirt', parts: ['ch'], colour: true },
        { label: 'Sleeves', parts: ['ls', 'rs'], colour: true },
        { label: 'Trousers', parts: ['lg'], colour: true },
        { label: 'Shoes', parts: ['sh'], colour: true },
    ];
    var choices = null; // { size, zoom, parts: { hr: [{ model, room, players }] } }
    function room() { var o = $('bot-room').selectedOptions[0]; return { id: +$('bot-room').value, name: o ? o.textContent : '', casts: o ? o.getAttribute('data-casts') : '' }; }

    function loadParts() {
        $('bot-parts').innerHTML = '<p class="bot-hint">Loading the room\'s clothes…</p>';
        fetch(cfg.parts + '?roomId=' + room().id, { credentials: 'same-origin' })
            .then(function (r) { if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'HTTP ' + r.status); }); return r.json(); })
            .then(function (json) { choices = json; buildRows(); changed(); })
            .catch(function (err) { $('bot-parts').innerHTML = '<p class="bot-error">Couldn\'t load the room\'s clothes: ' + esc(err.message) + '</p>'; choices = null; changed(); });
    }

    function buildRows() {
        var box = $('bot-parts');
        box.innerHTML = '';
        ROWS.forEach(function (row) {
            var el = document.createElement('div');
            el.className = 'bot-part';
            var html = '<span class="bot-label">' + esc(row.label) + '</span>';
            if (row.parts.length) html += '<select data-row-model></select>';
            else html += '<span></span>';
            if (row.colour || row.skin) html += '<span class="bot-colour"><input type="color" data-row-colour><button type="button" data-row-nocolour title="No colour: the client draws it light grey (EEEEEE)">×</button><small data-row-colour-note></small></span>';
            el.innerHTML = html;
            box.appendChild(el);
            row.el = el;
            var select = el.querySelector('[data-row-model]');
            if (select) select.addEventListener('change', function () { row.parts.forEach(function (p) { set(p, select.value); }); changed(); });
            var colour = el.querySelector('[data-row-colour]');
            if (colour) {
                colour.addEventListener('input', function () { (row.skin || row.parts).forEach(function (p) { set(p, undefined, fromHex(colour.value)); }); changed(); });
                el.querySelector('[data-row-nocolour]').addEventListener('click', function () { (row.skin || row.parts).forEach(function (p) { set(p, undefined, ''); }); changed(); });
            }
        });
        fillRows();
    }

    function fillRows() {
        ROWS.forEach(function (row) {
            if (!row.el) return;
            var select = row.el.querySelector('[data-row-model]');
            if (select) {
                var part = row.parts[0];
                var current = (get(part) || [part, '001'])[1];
                var list = (choices && choices.parts[part]) || [];
                // 32 px Habbos have no eyes of their own (the client draws none): nothing to pick there
                var none = choices && !list.length;
                select.disabled = !!none;
                if (none) { select.innerHTML = '<option>' + esc(current) + ' · not drawn at ' + (choices.size === 's' ? '32' : '64') + ' px</option>'; return fillColour(row); }
                var opts = list.map(function (c) {
                    var tag = c.room ? ' · only in this room' : !c.players ? ' · not for players' : '';
                    return '<option value="' + esc(c.model) + '"' + (c.model === current ? ' selected' : '') + '>' + esc(c.model + tag) + '</option>';
                });
                if (!list.some(function (c) { return c.model === current; })) opts.unshift('<option value="' + esc(current) + '" selected>' + esc(current) + ' · not in this room</option>');
                select.innerHTML = opts.join('');
            }
            fillColour(row);
        });
    }

    function fillColour(row) {
        {
            var colour = row.el.querySelector('[data-row-colour]');
            if (colour) {
                var c = (get((row.skin || row.parts)[0]) || [])[2] || '';
                var hex = toHex(c);
                colour.value = hex || '#eeeeee';
                row.el.querySelector('[data-row-colour-note]').textContent = hex ? '' : /^\d+$/.test(c) && +c ? 'palette ' + c : 'none';
            }
        }
    }

    // --- preview and warnings ---
    var timer = 0, shownUrl = '';
    function preview() {
        clearTimeout(timer);
        timer = setTimeout(function () {
            var q = 'figure=' + encodeURIComponent(figureText()) + '&roomId=' + room().id + '&direction=' + body + '&headDirection=' + head +
                (pose ? '&action=' + pose : '') + (drink ? '&drink=' + drink : '');
            var url = cfg.picture + '?' + q;
            if (url === shownUrl) return;
            shownUrl = url;
            var img = $('bot-preview');
            img.onload = function () { img.style.width = (img.naturalWidth * (img.naturalWidth <= 40 ? 4 : 2)) + 'px'; $('bot-stage').classList.remove('failed'); };
            img.onerror = function () { $('bot-stage').classList.add('failed'); };
            img.src = url;
        }, 150);
    }

    function warnings() {
        var out = [];
        if (choices) {
            ROWS.forEach(function (row) {
                row.parts.forEach(function (part) {
                    var p = get(part);
                    var list = choices.parts[part] || [];
                    if (p && list.length && !list.some(function (c) { return c.model === p[1]; }))
                        out.push(row.label + ' ' + p[1] + ' (' + part + '): this room has no such clothes at ' + (choices.size === 's' ? '32' : '64') + ' px, so the hotel draws nothing there');
                });
            });
        }
        $('bot-warnings').innerHTML = out.map(function (w) { return '<li>' + esc(w) + '</li>'; }).join('');
        $('bot-zoom-note').textContent = choices
            ? 'Drawn ' + (choices.size === 's' ? 'at 32 px' : 'at 64 px') + (choices.zoom ? ', as people are in ' : ' in ') + room().name.replace(/ \(\d+\)$/, '') + (room().casts ? ' (' + room().casts + ')' : '') + ', shown enlarged'
            : '';
    }

    function changed() {
        $('bot-figure').value = figureText();
        if (document.activeElement !== $('bot-figure-text')) $('bot-figure-text').value = figureText();
        $('bot-startlook').value = head + ',' + body;
        $('bot-body-dir').textContent = body;
        $('bot-head-dir').textContent = head;
        $('bot-dir').textContent = 'body ' + body + ', head ' + head;
        fillRows();
        warnings();
        preview();
    }

    $('bot-figure-text').addEventListener('input', function () { figure = parseFigure(this.value); changed(); });
    $('bot-room').addEventListener('change', loadParts);

    // --- walk area ---
    function walkCount() {
        var tiles = $('bot-walkspace').value.trim().split(/\s+/).filter(Boolean);
        var bad = tiles.filter(function (t) { return !/^\d+,\d+$/.test(t); });
        var x = document.querySelector('[name=X]').value, y = document.querySelector('[name=Y]').value;
        $('bot-walk-count').textContent = bad.length ? 'Not tiles: ' + bad.slice(0, 3).join(' ') + ' (write x,y with spaces between)'
            : tiles.length + ' tiles' + (tiles.indexOf(x + ',' + y) < 0 ? '; the start tile ' + x + ',' + y + ' is added by the hotel' : '');
    }
    ['input', 'change'].forEach(function (ev) {
        $('bot-walkspace').addEventListener(ev, walkCount);
        document.querySelector('[name=X]').addEventListener(ev, walkCount);
        document.querySelector('[name=Y]').addEventListener(ev, walkCount);
    });

    // --- what it says: lines with a chat type, or the AI's text ---
    var MODES = [['', 'says'], ['SHOUT', 'shouts'], ['WHISPER', 'whispers']];
    document.querySelectorAll('.bot-lines').forEach(function (box) {
        var hidden = $(box.getAttribute('data-for'));
        var raw = hidden.value || '';
        box.innerHTML = '<span class="bot-label">' + esc(box.getAttribute('data-title')) + '</span>';
        if (raw.indexOf('_AI_') >= 0) {
            box.insertAdjacentHTML('beforeend', '<p class="bot-hint">Answered by the AI (<code>_AI_</code>): the hotel passes this text on and doesn\'t pick lines itself.</p><textarea rows="3" class="bot-ai"></textarea>');
            var ta = box.querySelector('.bot-ai');
            ta.value = raw;
            ta.addEventListener('input', function () { hidden.value = ta.value; });
            return;
        }
        var list = document.createElement('div');
        list.className = 'bot-line-list';
        box.appendChild(list);
        var add = document.createElement('button');
        add.type = 'button';
        add.textContent = '+ line';
        box.appendChild(add);
        function addLine(text, mode) {
            var row = document.createElement('div');
            row.className = 'bot-line';
            row.innerHTML = '<input type="text" maxlength="200"><select>' + MODES.map(function (m) { return '<option value="' + m[0] + '">' + m[1] + '</option>'; }).join('') + '</select><button type="button" title="Remove">×</button>';
            row.querySelector('input').value = text;
            row.querySelector('select').value = mode;
            row.querySelector('button').addEventListener('click', function () { row.remove(); save(); });
            row.addEventListener('input', save);
            row.addEventListener('change', save);
            list.appendChild(row);
            return row;
        }
        function save() {
            hidden.value = Array.from(list.querySelectorAll('.bot-line')).map(function (row) {
                var text = row.querySelector('input').value.replace(/[|#]/g, '').trim();
                var mode = row.querySelector('select').value;
                return text ? text + (mode ? '#' + mode : '') : '';
            }).filter(Boolean).join('|');
        }
        raw.split('|').forEach(function (line) {
            if (!line.trim()) return;
            var hash = line.lastIndexOf('#');
            var mode = hash > 0 ? line.slice(hash + 1).trim().toUpperCase() : '';
            if (mode && mode !== 'SHOUT' && mode !== 'WHISPER' && mode !== 'CHAT') mode = '';
            addLine(hash > 0 && mode ? line.slice(0, hash) : line, mode === 'CHAT' ? '' : mode);
        });
        add.addEventListener('click', function () { addLine('', '').querySelector('input').focus(); });
    });

    // --- drinks ---
    function drinks() { return ($('bot-handitems').value || '').split(',').map(function (d) { return d.trim(); }).filter(function (d) { return /^\d+$/.test(d); }); }
    function showDrinks() {
        var list = drinks();
        $('bot-drink-list').innerHTML = list.map(function (d, i) { return '<span class="bot-drink"><input type="number" min="1" max="999" value="' + esc(d) + '" data-i="' + i + '"><button type="button" data-rm="' + i + '">×</button></span>'; }).join('');
        $('bot-carry').innerHTML = '<option value="0">no drink</option>' + list.map(function (d) { return '<option value="' + esc(d) + '"' + (+d === drink ? ' selected' : '') + '>holds ' + esc(d) + '</option>'; }).join('');
    }
    $('bot-drink-list').addEventListener('input', function (e) {
        var i = e.target.getAttribute('data-i');
        if (i == null) return;
        var list = drinks();
        list[+i] = String(Math.max(1, +e.target.value || 1));
        $('bot-handitems').value = list.join(',');
        $('bot-carry').innerHTML = '<option value="0">no drink</option>' + list.map(function (d) { return '<option value="' + esc(d) + '">holds ' + esc(d) + '</option>'; }).join('');
    });
    $('bot-drink-list').addEventListener('click', function (e) {
        var i = e.target.getAttribute('data-rm');
        if (i == null) return;
        var list = drinks();
        list.splice(+i, 1);
        $('bot-handitems').value = list.join(',');
        showDrinks();
    });
    $('bot-drink-add').addEventListener('click', function () {
        var list = drinks();
        list.push('1');
        $('bot-handitems').value = list.join(',');
        showDrinks();
    });

    // --- start ---
    figure = parseFigure($('bot-figure').value);
    showDrinks();
    walkCount();
    changed();
    loadParts();
})();

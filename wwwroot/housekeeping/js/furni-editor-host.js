// The furni editor page in Housekeeping (Areas/Housekeeping/Views/FurniEditor, docs/furni-editor-housekeeping.md): the
// editor runs in an iframe from keplercms:furniEditor:url and talks to this page with postMessage. Only messages from
// that origin and that frame count. Privileged requests (validate, publish, roll back) are made here, with this page's
// login and antiforgery token, after a click in this panel; the editor only proposes a furni.
//
// Messages to the editor: open { large: {name, bytes}, small?: {name, bytes}, colors, sprite, revision, madeHere,
// height? (its definition's seat height, for the Habbo in the editor's room) }, published { ok, text }, status { text }.
// From the editor: ready, publish { sprite, large, small?, colour, states, stateNames?, wall, kind?, height? } (states:
// how many the furni's .data has, stateNames: what they are called; kind and height: what the Habbo in its room was set
// to: 'seat' or 'bed' and the seat's height), state { sprite, hotel, wall } (what is open now: the steps box shows
// where you are).
//
// The editor can also run in a window of its own ("Open the editor in its own window"): the same page, opened by this
// one, talking to its opener instead of its parent. What is open is carried from one to the other: handover (asks an
// editor for its casts as they are now; it answers with handover { sprite, large?, small?, colors, shown, hotel, dirty,
// height }), then open { ...that, handover: true } to the other one once it said ready, and close to the one it left
// (nothing is lost there any more: no warning). Messages from the window say popup: true.
(function () {
    'use strict';
    var cfg = window.furniEditorHost;
    var frame = document.getElementById('fe-frame');
    // The editor answers only the origin named in `parent`: this page's, as the browser sees it (see Index.cshtml).
    function editorUrl() { return cfg.frameBase + encodeURIComponent(window.location.origin); }
    frame.src = editorUrl();
    var popup = null;        // the editor's own window, while it is out of the page
    var frameLoaded = true;  // the frame holds an editor (false: emptied after its furni went to the window)
    var frameReady = false;  // ... and it said ready
    var move = null;         // carrying the open furni between the two: { from, to, toReady, furni, timer, done }
    var watch = null;
    var token = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
    var $ = function (id) { return document.getElementById(id); };
    var upload = null; // the last checked furni: { uploadId, sprite, report }
    var shown = null;  // sprite whose versions are listed
    var made = {};     // sprite -> live revision, of the furni made here
    var current = null; // what the editor has open: { sprite, hotel, wall }
    var lastPublished = null; // sprite just published here (its next step is the catalogue)

    // --- where you are ---
    function step(n, text) {
        document.querySelectorAll('#fe-steps li').forEach(function (li) {
            var k = +li.getAttribute('data-step');
            li.classList.toggle('is-done', k < n);
            li.classList.toggle('is-current', k === n);
        });
        if (text !== undefined) $('fe-now').innerHTML = text;
    }
    function showState() {
        showAfter();
        if (!current || !current.sprite) return step(1, 'Nothing open yet.');
        var kind = current.wall ? 'wall item' : 'furni';
        var name = '<code>' + esc(current.sprite) + '</code>';
        if (lastPublished === current.sprite && made[current.sprite])
            return step(5, name + ' r' + made[current.sprite] + ' is in the hotel. Next: <i>Give me one</i> to try it (no catalogue needed), and <i>Put it in the catalogue</i> (below) when players should get it.');
        if (current.hotel) return step(2, 'Open: ' + name + ', the hotel\'s own ' + kind + ' (read-only). <i>Make a copy…</i> in the editor to make it yours.');
        if (made[current.sprite]) return step(3, 'Open: ' + name + ', your ' + kind + ', live as r' + made[current.sprite] + '. <i>Publish…</i> again makes r' + (made[current.sprite] + 1) + '.');
        step(3, 'Open: ' + name + ', your new ' + kind + ': not in the hotel yet. <i>Publish…</i> in the editor when it\'s ready.');
    }

    /** The editor in use: its own window while that is open, else the frame. */
    function editorWindow() { return popup && !popup.closed ? popup : frame.contentWindow; }
    function send(message, transfer) {
        var w = editorWindow();
        if (w) w.postMessage(message, cfg.editorOrigin, transfer || []);
    }
    function api(path, opts) {
        opts = opts || {};
        opts.credentials = 'same-origin';
        if (opts.method === 'POST') opts.headers = Object.assign({ RequestVerificationToken: token }, opts.headers || {});
        return fetch(cfg.base + '/' + path, opts).then(function (r) {
            var json = (r.headers.get('content-type') || '').indexOf('json') >= 0;
            return (json ? r.json() : r.text()).then(function (body) {
                if (!r.ok) throw new Error((body && body.error) || (typeof body === 'string' && body) || ('HTTP ' + r.status));
                return body;
            });
        });
    }
    function bytes(path) {
        return fetch(cfg.base + '/' + path, { credentials: 'same-origin' }).then(function (r) {
            if (r.status === 404) return null;
            if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'HTTP ' + r.status); });
            return r.arrayBuffer();
        });
    }
    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); }
    function note(text) { send({ type: 'status', text: text }); }
    function when(iso) { var d = new Date(iso); return isNaN(d) ? '' : d.toLocaleString('da-DK', { dateStyle: 'short', timeStyle: 'short' }); }

    // --- opening ---
    /** A furni's large and small cast (a version made here, or the live/hotel files) into the editor. */
    function openFurni(sprite, revision, madeHere) {
        var q = 'sprite=' + encodeURIComponent(sprite) + (revision ? '&revision=' + revision : '');
        return Promise.all([
            bytes('Cast?' + q),
            bytes('Cast?' + q + '&small=true'),
            api('Definitions?query=' + encodeURIComponent(sprite)),
        ]).then(function (res) {
            if (!res[0] && !res[1]) throw new Error('no cast hh_furni_xx_' + sprite + '.cct');
            var colors = res[2].filter(function (d) { return d.sprite.split('*')[0] === sprite; })
                .map(function (d) { return { label: d.sprite + ': ' + d.name, colour: d.colour }; });
            var msg = { type: 'open', sprite: sprite, revision: revision || 0, madeHere: !!madeHere, colors: colors };
            // the seat height of its definition: where the Habbo in the editor's room sits or lies
            var def = res[2].filter(function (d) { return d.sprite === sprite; })[0];
            if (def && typeof def.topHeight === 'number') msg.height = def.topHeight;
            var transfer = [];
            if (res[0]) { msg.large = { name: 'hh_furni_xx_' + sprite + '.cct', bytes: res[0] }; transfer.push(res[0]); }
            if (res[1]) { msg.small = { name: 'hh_furni_xx_s_' + sprite + '.cct', bytes: res[1] }; transfer.push(res[1]); }
            send(msg, transfer);
            if (popup && !popup.closed) popup.focus();
            showVersions(sprite);
        }).catch(function (err) { alertBox('Couldn\'t open ' + sprite + ': ' + err.message); });
    }

    var searchTimer = 0;
    $('fe-search').addEventListener('input', function () {
        clearTimeout(searchTimer);
        var q = this.value.trim();
        searchTimer = setTimeout(function () {
            if (!q) { $('fe-results').innerHTML = ''; return; }
            api('Definitions?query=' + encodeURIComponent(q)).then(function (list) {
                $('fe-results').innerHTML = list.map(function (d) {
                    return '<div class="fe-result" data-sprite="' + esc(d.sprite) + '" data-made="' + (d.madeHere ? 1 : 0) + '"><span>' + esc(d.sprite) +
                        (d.madeHere ? '<span class="fe-badge">made here</span>' : '') + '</span><small>' + esc(d.name) + '</small></div>';
                }).join('') || '<small>Nothing found</small>';
            });
        }, 200);
    });
    function pick(e) {
        var row = e.target.closest('.fe-result');
        if (!row) return;
        document.querySelectorAll('.fe-result').forEach(function (r) { r.classList.toggle('is-active', r === row); });
        openFurni(row.getAttribute('data-sprite').split('*')[0], 0, row.getAttribute('data-made') === '1');
    }
    $('fe-results').addEventListener('click', pick);
    $('fe-made').addEventListener('click', pick);

    function loadMade() {
        api('Made').then(function (list) {
            made = {};
            list.forEach(function (m) { made[m.sprite] = m.revision; });
            showState();
            $('fe-made-box').hidden = list.length === 0;
            $('fe-made').innerHTML = list.map(function (m) {
                return '<div class="fe-result" data-sprite="' + esc(m.sprite) + '" data-made="1"><span>' + esc(m.sprite) + '</span><small>r' + m.revision + ' · ' + esc(when(m.createdAt)) + '</small></div>';
            }).join('');
        }).catch(function () { /* the table may not exist yet */ });
    }

    function alertBox(text) {
        $('fe-report').innerHTML = '<p class="fe-report-error">' + esc(text) + '</p>';
        $('fe-def').hidden = true;
        $('fe-note-row').hidden = true;
        $('fe-confirm').disabled = true;
        $('fe-publish').hidden = false;
    }

    // --- publishing ---
// What a furni is in the hotel, as the attributes of its definition (items_definitions). `flags` (behaviour) and
    // `interactor` are what the game server goes by. Switching is a separate choice (below), so a seat or a table can
    // have states too; `rights` is whether a furni that doesn't switch still carries the rights flag, as the hotel's do.
    var RIGHTS = 'requires_rights_for_interaction';
    var SWITCH = ['custom_data_numeric_on_off', 'custom_data_numeric_state', 'custom_data_on_off'];
    var kinds = {
        furniture: { flags: ['solid'], interactor: 'default', height: 1, rights: true,
            hint: 'Habbos walk around it and nothing can be put on it: a plant, a statue, a machine, a lamp.',
            heightLabel: 'Height', heightHint: 'How tall it is; a chair\'s seat is 1. It hardly matters while nothing can stand on it.' },
        table: { flags: ['solid', 'can_stack_on_top'], interactor: 'default', height: 1, rights: true,
            hint: 'Habbos walk around it, and other furni can be put on top of it.',
            heightLabel: 'Height of its top', heightHint: 'Furni put on it stand this high; most of the hotel\'s tables are 1.' },
        seat: { flags: ['can_sit_on_top'], interactor: 'chair', height: 1, rights: false,
            hint: 'Habbos walk to it and sit down, facing the way it is turned. Nothing can be put on it.',
            heightLabel: 'Seat height', heightHint: 'How high a Habbo sits: 1 for most of the hotel\'s chairs, 1.2 for the higher ones. Set a Habbo on it in the editor\'s room to see it.' },
        bed: { flags: ['can_lay_on_top'], interactor: 'bed', height: 1.6, rights: false,
            hint: 'Habbos walk to it and lie down. Nothing can be put on it.',
            heightLabel: 'Bed height', heightHint: 'How high a Habbo lies: the hotel\'s beds are 1.6 to 1.8, a sleeping bag 0.8.' },
        rug: { flags: ['can_stand_on_top', 'can_stack_on_top'], interactor: 'default', height: 0, rights: true,
            hint: 'Habbos walk over it, and furni can be put on top of it: a carpet, a floor tile, anything flat.',
            heightLabel: 'Height', heightHint: 'How far it lifts what is on it: 0 for a carpet, 0.2 to 0.45 for the hotel\'s thicker tiles.' },
        wall: { flags: ['wall_item'], interactor: 'default', height: 0, rights: true,
            hint: 'It hangs on a wall. The cast decides this: it was made as a wall item.' },
    };
    // Two states are the hotel's on / off (its lamps); more need the attribute that takes any state number, or the
    // game server refuses the third state and the furni is stuck on its second.
    function switchFlag(states) { return states > 2 ? 'custom_data_numeric_state' : 'custom_data_numeric_on_off'; }

    // A definition the hotel already has, as this form's choices: { kind, switches, rights }. Null when its attributes
    // hold something the form doesn't set (a gate, a roller, a vending machine...): then they are kept as they are.
    function readDefinition(d) {
        var flags = String(d.behaviour || '').split(',').map(function (f) { return f.trim().toLowerCase(); }).filter(Boolean);
        var has = function (f) { return flags.indexOf(f) >= 0; };
        var key = has('wall_item') ? 'wall' : has('can_sit_on_top') ? 'seat' : has('can_lay_on_top') ? 'bed' : has('can_stand_on_top') ? 'rug'
            : has('solid') ? (has('can_stack_on_top') ? 'table' : 'furniture') : null;
        if (!key) return null;
        var kind = kinds[key], known = kind.flags.concat(SWITCH, [RIGHTS]);
        if (!kind.flags.every(has) || flags.some(function (f) { return known.indexOf(f) < 0; })) return null;
        if (String(d.interactor || 'default').toLowerCase() !== kind.interactor) return null;
        return { kind: key, switches: SWITCH.some(has), rights: has(RIGHTS) };
    }

    // The attributes the form's choices come to.
    function attributes() {
        var d = (upload && upload.existing) || {};
        var key = $('fe-preset').value, kind = kinds[key];
        if (!kind) return { behaviour: d.behaviour || '', interactor: d.interactor || 'default', maxStatus: d.maxStatus || '1', kept: true };
        var states = upload ? upload.states : 1;
        // the game server switches every wall item that has states; a floor furni only with the attribute
        var switches = states > 1 && (key === 'wall' || $('fe-switches').checked);
        var flags = kind.flags.slice();
        if (switches) flags.push(switchFlag(states));
        if (switches ? $('fe-switch-rights').checked : kind.rights) flags.push(RIGHTS);
        return { behaviour: flags.join(','), interactor: kind.interactor, maxStatus: String(states), switches: switches };
    }

    function refreshDefinition() {
        if (!upload) return;
        var key = $('fe-preset').value, kind = kinds[key], a = attributes();
        var states = upload.states, names = upload.stateNames, wall = key === 'wall';
        $('fe-def').querySelector('.fe-def-fields').classList.toggle('is-off', !$('fe-save-def').checked);
        $('fe-kind-hint').textContent = kind ? kind.hint
            : 'What it is stays as it is now (the attributes below). Its name, description, size, height and colours are still saved.';
        // its states
        var text;
        if (states < 2) text = 'One state: there is nothing to switch.';
        else {
            // the names the furni gives its states (the editor's States tab); "state 1, state 2" says nothing
            var named = names && names.length === states && !names.every(function (n) { return /^state \d+$/i.test(n); });
            text = states + ' states' + (named ? ': ' + names.join(' → ') : '') + '.';
            if (kind && wall) text += ' A wall item with states always switches on a double-click.';
            else if (kind && !a.switches) text += ' Not switched: it stays in its first state' + (named ? ' (' + names[0] + ')' : '') + '.';
            else if (kind && states > 2) text += ' Each double-click goes one further, and from the last back to the first.';
        }
        $('fe-states-text').textContent = text;
        $('fe-switches-row').hidden = !kind || states < 2;
        $('fe-switches').disabled = wall;
        if (wall && states > 1) $('fe-switches').checked = true;
        $('fe-switch-rights-row').hidden = !kind || !a.switches;
        // size and height: a wall item has neither
        $('fe-size-row').hidden = wall;
        $('fe-height-row').hidden = wall;
        $('fe-height-label').textContent = kind && kind.heightLabel ? kind.heightLabel : 'Height';
        $('fe-height-hint').textContent = kind && kind.heightHint ? kind.heightHint : 'A seat: how high a Habbo sits. Anything else: how high furni put on it stand.';
        $('fe-attrs').innerHTML = '<span>' + (a.kept ? 'Kept as' : 'Saved as') + '</span> <code>behaviour: ' + esc(a.behaviour || '(none)') + '</code> <code>interactor: ' +
            esc(a.interactor) + '</code> <code>states: ' + esc(a.maxStatus) + '</code>';
    }

    function reportHtml(title, r) {
        if (!r) return '';
        var html = '<p><b>' + esc(title) + '</b>: ' + r.bitmaps + ' pictures, ' + r.fields + ' fields, ' + r.palettes + ' palettes, ' + Math.round(r.bytes / 1024) + ' KB' +
            (r.classes.length ? ', classes ' + esc(r.classes.join(', ')) : '') + '</p>';
        if (r.errors.length) html += '<ul class="fe-report-error">' + r.errors.map(function (e) { return '<li>' + esc(e) + '</li>'; }).join('') + '</ul>';
        if (r.warnings.length) html += '<ul>' + r.warnings.map(function (w) { return '<li>' + esc(w) + '</li>'; }).join('') + '</ul>';
        return html;
    }

    function onPublish(m) {
        if (!cfg.canPublish) { alertBox('Publishing is off on this hotel (keplercms:furniEditor:castDir and historyDir).'); return; }
        var sprite = String(m.sprite || '');
        lastPublished = null;
        step(4, 'Publishing <code>' + esc(sprite) + '</code>: read the check below, fill in its definition and press <i>Publish to the hotel</i>.');
        $('fe-publish').hidden = false;
        $('fe-publish').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        $('fe-pub-sprite').textContent = sprite;
        $('fe-report').innerHTML = '<p>Checking…</p>';
        $('fe-def').hidden = true;
        $('fe-note-row').hidden = true;
        $('fe-confirm').disabled = true;
        upload = null;
        var form = new FormData();
        form.append('sprite', sprite);
        form.append('cast', new Blob([m.large], { type: 'application/octet-stream' }), 'hh_furni_xx_' + sprite + '.cct');
        if (m.small) form.append('small', new Blob([m.small], { type: 'application/octet-stream' }), 'hh_furni_xx_s_' + sprite + '.cct');
        api('Validate', { method: 'POST', body: form }).then(function (r) {
            var head;
            if (r.state === 'taken') head = '<p class="fe-report-error">' + esc(r.reason) + '</p>';
            else if (!r.ok) head = '<p class="fe-report-error">The hotel won\'t take these casts (below).</p>';
            else head = '<p class="fe-report-ok">' + (r.state === 'new' ? 'A new furni: it becomes revision 1.' : 'Made here: this becomes revision ' + r.revision + ' (the current one stays in the versions).') + '</p>';
            if (r.ok && !r.small) head += '<p>' + (r.keepsSmall ? 'No small cast sent: the current small one is kept.' : 'No small (32 px) cast: the small room zoom shows nothing. Make one with the editor\'s 32 px button.') + '</p>';
            $('fe-report').innerHTML = head + reportHtml('hh_furni_xx_' + sprite + '.cct', r.large) + reportHtml('hh_furni_xx_s_' + sprite + '.cct', r.small);
            if (!r.ok) return;
            var d = r.definition;
            var isWall = !!m.wall || r.large.classes.some(function (c) { return /^Item Object/.test(c); });
            upload = { uploadId: r.uploadId, sprite: sprite, isNew: r.state === 'new', existing: d, states: Math.max(1, +m.states || 1),
                stateNames: Array.isArray(m.stateNames) ? m.stateNames.map(String) : null, wall: isWall,
                givenHeight: typeof m.height === 'number' ? m.height : null };
            $('fe-save-def').checked = true;
            $('fe-save-def').disabled = upload.isNew; // a new furni needs its definition
            $('fe-def-state').textContent = d ? 'updates definition ' + d.id : 'a new definition';
            $('fe-name').value = d ? d.name : sprite;
            $('fe-desc').value = d ? d.description || '' : '';
            $('fe-length').value = d ? d.length : (r.large.length || 1);
            $('fe-width').value = d ? d.width : (r.large.width || 1);
            // The cast says whether it is a wall item, so only the kinds it can be are offered; "keep" needs a definition.
            Array.prototype.forEach.call($('fe-preset').options, function (o) {
                var off = o.value === 'keep' ? !d : (o.value === 'wall') !== isWall;
                o.hidden = o.disabled = off;
            });
            // A new furni: what the editor knows (a wall item, a Habbo sitting or lying on it), and it switches when it
            // has states. One the hotel has: its definition as the form's choices, or kept when the form can't say it.
            var read = d ? readDefinition(d) : null;
            if (read && (read.kind === 'wall') !== isWall) read = null;
            var defIsWall = d ? /(^|,)\s*wall_item\s*(,|$)/i.test(d.behaviour || '') : isWall;
            var kind = d && defIsWall === isWall ? (read ? read.kind : 'keep') : isWall ? 'wall' : m.kind === 'seat' ? 'seat' : m.kind === 'bed' ? 'bed' : 'furniture';
            // states it didn't have before switch by themselves, like a new furni's
            var gained = upload.states > 1 && (!read || (!read.switches && (parseInt(d.maxStatus, 10) || 1) < 2));
            $('fe-switches').checked = upload.states > 1 && (gained || read.switches);
            $('fe-switch-rights').checked = read && read.switches ? read.rights : true;
            $('fe-height').value = upload.givenHeight !== null ? upload.givenHeight : d ? d.topHeight : kinds[kind].height;
            $('fe-colour').value = (d && d.colour) || m.colour || '0,0,0';
            $('fe-tradable').checked = d ? d.isTradable : true;
            $('fe-recyclable').checked = d ? d.isRecyclable : true;
            $('fe-preset').value = kind;
            $('fe-note').value = '';
            $('fe-def').hidden = false;
            $('fe-note-row').hidden = false;
            $('fe-confirm').disabled = false;
            if (isWall) $('fe-length').value = $('fe-width').value = 1;
            refreshDefinition();
        }).catch(function (err) { alertBox('Check failed: ' + err.message); });
    }

    $('fe-preset').addEventListener('change', function () {
        var kind = kinds[this.value], d = upload && upload.existing;
        // the kind's usual height, unless the editor's Habbo already showed how high it sits or lies
        if (kind && !(upload && upload.givenHeight !== null && (this.value === 'seat' || this.value === 'bed'))) $('fe-height').value = kind.height;
        else if (!kind && d) $('fe-height').value = d.topHeight;
        refreshDefinition();
    });
    ['fe-switches', 'fe-switch-rights', 'fe-save-def'].forEach(function (id) { $(id).addEventListener('change', refreshDefinition); });

    $('fe-cancel').addEventListener('click', function () { $('fe-publish').hidden = true; upload = null; note('Publishing cancelled'); });

    $('fe-confirm').addEventListener('click', function () {
        if (!upload) return;
        var d = upload.existing || {};
        var attrs = attributes();
        var definition = {
            id: d.id || 0, sprite: upload.sprite, spriteId: d.spriteId || 0,
            name: $('fe-name').value.trim() || upload.sprite, description: $('fe-desc').value.trim(),
            colour: $('fe-colour').value.trim() || '0,0,0',
            length: +$('fe-length').value || 1, width: +$('fe-width').value || 1, topHeight: +$('fe-height').value || 0,
            // max_status: as many states as its .data has
            maxStatus: attrs.maxStatus, behaviour: attrs.behaviour, interactor: attrs.interactor,
            isTradable: $('fe-tradable').checked, isRecyclable: $('fe-recyclable').checked, drinkIds: d.drinkIds || '',
        };
        $('fe-confirm').disabled = true;
        var body = { uploadId: upload.uploadId, saveDefinition: $('fe-save-def').checked, definition: definition, note: $('fe-note').value.trim() };
        api('Publish', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
            .then(function (r) {
                var text = 'Published ' + r.sprite + ' r' + r.revision + (r.small ? ' (with its small cast)' : '') + (r.definitionId ? ', definition ' + r.definitionId : '') +
                    '. The hotel reloaded its catalogue; players get it on their next login.';
                $('fe-report').innerHTML = '<p class="fe-report-ok">' + esc(text) + '</p>' +
                    '<p><button type="button" class="fe-primary" id="fe-giveme-now">Give me one</button> to try it in a room (no catalogue needed), or sell it in the box below.</p>';
                $('fe-giveme-now').addEventListener('click', function () {
                    $('fe-after').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    $('fe-giveme').click();
                });
                made[r.sprite] = r.revision;
                lastPublished = r.sprite;
                showState();
                $('fe-def').hidden = true;
                $('fe-note-row').hidden = true;
                send({ type: 'published', ok: true, text: text, sprite: r.sprite, revision: r.revision });
                showVersions(upload.sprite);
                loadMade();
                upload = null;
            })
            .catch(function (err) {
                $('fe-confirm').disabled = false;
                $('fe-report').insertAdjacentHTML('beforeend', '<p class="fe-report-error">' + esc(err.message) + '</p>');
                send({ type: 'published', ok: false, text: err.message });
            });
    });

    // --- after publishing: into your hand, into the catalogue ---
    var pagesLoaded = false;
    function showAfter() {
        var sprite = current && made[current.sprite] ? current.sprite : null;
        $('fe-after').hidden = !sprite;
        if (!sprite) return;
        if ($('fe-after-sprite').textContent !== sprite) $('fe-after-result').innerHTML = '';
        $('fe-after-sprite').textContent = sprite;
        if (pagesLoaded) return;
        pagesLoaded = true;
        api('CataloguePages').then(function (list) {
            $('fe-page').innerHTML = '<option value="">Pick a catalogue page…</option>' + list.map(function (p) {
                return '<option value="' + p.id + '">' + esc(p.name || ('page ' + p.id)) + ' (' + p.products + ')' + (p.visible ? '' : ' · not in the index') + (p.fuse && p.fuse !== 'DEFAULT' ? ' · ' + esc(p.fuse) : '') + '</option>';
            }).join('');
        }).catch(function () { pagesLoaded = false; });
    }
    function afterResult(text, ok) {
        $('fe-after-result').innerHTML = '<p class="' + (ok ? 'fe-report-ok' : 'fe-report-error') + '">' + text + '</p>';
    }
    function after(path, body, button, done) {
        if (!current || !made[current.sprite]) return;
        body.sprite = current.sprite;
        button.disabled = true;
        api(path, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
            .then(function (r) { afterResult(done(r, body.sprite), true); })
            .catch(function (err) { afterResult(esc(err.message), false); })
            .then(function () { button.disabled = false; });
    }
    $('fe-giveme').addEventListener('click', function () {
        after('GiveMe', {}, this, function (r, sprite) { return 'One <code>' + esc(sprite) + '</code> is in your hand: open the hotel and place it in a room.'; });
    });
    $('fe-sell').addEventListener('click', function () {
        if (!+$('fe-page').value) { afterResult('Pick the catalogue page it goes on first.', false); return; }
        after('AddToCatalogue', { pageId: +$('fe-page').value, price: +$('fe-price').value || 0 }, this, function (r, sprite) {
            pagesLoaded = false;
            return '<code>' + esc(sprite) + '</code> is on <b>' + esc(r.page) + '</b> for ' + r.price + ' credits. The hotel reloaded its catalogue.';
        });
    });

    // --- versions ---
    function showVersions(sprite) {
        shown = sprite;
        api('Versions?sprite=' + encodeURIComponent(sprite)).then(function (list) {
            if (shown !== sprite) return;
            $('fe-history-box').hidden = list.length === 0;
            $('fe-hist-sprite').textContent = sprite;
            $('fe-history').innerHTML = list.map(function (v, i) {
                var what = v.source === 'rollback' ? 'rolled back to r' + v.rolledBackFrom : (v.note ? esc(v.note) : '');
                return '<div class="fe-history-row' + (i === 0 ? ' is-current' : '') + '"><span class="fe-hist-meta">r' + v.revision + ' · ' + esc(when(v.createdAt)) + ' · ' + esc(v.user) +
                    (what ? ' · ' + what : '') + (v.smallBytes ? '' : ' · no small') + (i === 0 ? ' (live)' : '') + '</span>' +
                    '<button type="button" data-open="' + v.revision + '">Open</button>' +
                    (i === 0 ? '' : '<button type="button" data-rollback="' + v.revision + '">Roll back</button>') + '</div>';
            }).join('');
        }).catch(function () { $('fe-history-box').hidden = true; });
    }
    $('fe-history').addEventListener('click', function (e) {
        var open = e.target.closest('button[data-open]');
        if (open) return openFurni(shown, +open.getAttribute('data-open'), true);
        var b = e.target.closest('button[data-rollback]');
        if (!b) return;
        var rev = +b.getAttribute('data-rollback');
        var withDef = $('fe-rollback-def').checked;
        if (!confirm('Publish r' + rev + ' of ' + shown + ' again as a new revision' + (withDef ? ', with its definition' : '') + '? The current one stays in the versions.')) return;
        api('Rollback', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sprite: shown, revision: rev, withDefinition: withDef }) })
            .then(function (r) { note('Rolled ' + r.sprite + ' back to r' + rev + ': now r' + r.revision); showVersions(shown); loadMade(); })
            .catch(function (err) { alert('Roll back failed: ' + err.message); });
    });

    // --- the editor in its own window ---
    function showPlace() {
        var out = !!popup;
        document.querySelector('.fe-host').classList.toggle('is-popped', out);
        $('fe-popped').hidden = !out;
        $('fe-bar').hidden = out;
        frame.hidden = out;
    }
    function reloadFrame() {
        frame.src = editorUrl();
        frameLoaded = true;
        frameReady = false;
    }
    /** Carry what the editor in `from` has open to the one in `to` (once that is ready), then done(carried). */
    function carry(from, to, toReady, done) {
        if (move) clearTimeout(move.timer);
        move = { from: from, to: to, toReady: toReady, furni: from ? undefined : null, answered: !!from, done: done };
        if (from) {
            from.postMessage({ type: 'handover' }, cfg.editorOrigin);
            // an editor that doesn't answer keeps what it has: nothing is carried, and it is not closed
            move.timer = setTimeout(function () {
                if (move && move.furni === undefined) { move.furni = null; move.answered = false; finishMove(); }
            }, 4000);
        }
        finishMove();
    }
    function finishMove() {
        if (!move || !move.toReady || move.furni === undefined) return;
        var m = move;
        move = null;
        clearTimeout(m.timer);
        var f = m.furni;
        if (f && (f.large || f.small)) {
            var transfer = [];
            if (f.large) transfer.push(f.large.bytes);
            if (f.small) transfer.push(f.small.bytes);
            m.to.postMessage({ type: 'open', handover: true, sprite: f.sprite, large: f.large, small: f.small, colors: f.colors, shown: f.shown, hotel: f.hotel, dirty: !!f.dirty, height: f.height }, cfg.editorOrigin, transfer);
        }
        // what it had open is in the other editor now: it may close without asking
        if (m.answered && m.from) m.from.postMessage({ type: 'close' }, cfg.editorOrigin);
        m.done(m.answered);
    }
    function watchPopup() {
        clearInterval(watch);
        watch = setInterval(function () {
            if (!popup || !popup.closed) return;
            // its window was closed: the editor is in the page again
            clearInterval(watch);
            popup = null;
            if (move) { clearTimeout(move.timer); move = null; }
            if (!frameLoaded) reloadFrame();
            current = null;
            showPlace();
            showState();
        }, 700);
    }
    $('fe-popout').addEventListener('click', function () {
        var w = window.open(editorUrl(), 'furniEditor', 'popup=yes,left=0,top=0,width=' + screen.availWidth + ',height=' + screen.availHeight);
        if (!w) { alertBox('The browser blocked the editor\'s window: allow pop-ups for this page and press the button again.'); return; }
        var from = frameLoaded && frameReady ? frame.contentWindow : null;
        popup = w;
        current = null;
        carry(from, w, false, function (carried) {
            // one editor at a time: the frame is emptied once its furni is in the window (else it keeps what it has)
            if (carried || !from) { frame.src = 'about:blank'; frameLoaded = false; frameReady = false; }
        });
        watchPopup();
        showPlace();
        showState();
    });
    $('fe-show').addEventListener('click', function () { if (popup && !popup.closed) popup.focus(); });
    $('fe-dock').addEventListener('click', function () {
        var w = popup;
        if (!w || w.closed) return;
        if (!frameLoaded) reloadFrame();
        popup = null;
        clearInterval(watch);
        current = null;
        carry(w, frame.contentWindow, frameReady, function (carried) {
            if (!carried) alertBox('The editor\'s window didn\'t answer, so what is open there stayed there: close it yourself when you are done with it.');
        });
        showPlace();
        showState();
    });

    // --- the editor ---
    window.addEventListener('message', function (e) {
        if (e.origin !== cfg.editorOrigin || !e.source) return;
        var m = e.data || {};
        var fromFrame = e.source === frame.contentWindow;
        var known = fromFrame || e.source === popup || (move && (e.source === move.to || e.source === move.from));
        if (!known) {
            // an editor window that outlived a reload of this page is still this page's (the frame keeps what it has)
            if (m.popup !== true || popup || move) return;
            popup = e.source;
            watchPopup();
            showPlace();
        }
        if (m.type === 'ready') {
            if (fromFrame) frameReady = true;
            if (move && e.source === move.to) { move.toReady = true; finishMove(); }
            return;
        }
        if (m.type === 'handover') {
            if (move && e.source === move.from && move.furni === undefined) { move.furni = m.large || m.small ? m : null; finishMove(); }
            return;
        }
        if (e.source !== editorWindow()) return; // only the editor in use
        if (m.type === 'publish' && m.large instanceof ArrayBuffer && (!m.small || m.small instanceof ArrayBuffer)) {
            onPublish(m);
            if (popup) { try { window.focus(); } catch (err) { /* the browser decides */ } }
        } else if (m.type === 'state') {
            current = { sprite: String(m.sprite || ''), hotel: !!m.hotel, wall: !!m.wall };
            if (lastPublished && lastPublished !== current.sprite) lastPublished = null;
            showState();
        }
    });

    loadMade();
})();

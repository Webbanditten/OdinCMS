// The furni editor page in Housekeeping (Areas/Housekeeping/Views/FurniEditor, docs/furni-editor-housekeeping.md): the
// editor runs in an iframe from keplercms:furniEditor:url and talks to this page with postMessage. Only messages from
// that origin and that frame count. Privileged requests (validate, publish, roll back) are made here, with this page's
// login and antiforgery token, after a click in this panel; the editor only proposes a furni.
//
// Messages to the editor: open { large: {name, bytes}, small?: {name, bytes}, colors, sprite, revision, madeHere,
// height? (its definition's seat height, for the Habbo in the editor's room) }, published { ok, text }, status { text }.
// From the editor: ready, publish { sprite, large, small?, colour, states, wall, kind?, height? } (kind and height: what
// the Habbo in its room was set to: 'seat' or 'bed' and the seat's height), state { sprite, hotel, wall } (what is open
// now: the steps box shows where you are).
(function () {
    'use strict';
    var cfg = window.furniEditorHost;
    var frame = document.getElementById('fe-frame');
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

    function send(message, transfer) {
        if (frame.contentWindow) frame.contentWindow.postMessage(message, cfg.editorOrigin, transfer || []);
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
    var presets = {
        furniture: { behaviour: 'solid,requires_rights_for_interaction', interactor: 'default', maxStatus: '1', height: 1 },
        table: { behaviour: 'solid,can_stack_on_top,requires_rights_for_interaction', interactor: 'default', maxStatus: '1', height: 1 },
        seat: { behaviour: 'can_sit_on_top', interactor: 'chair', maxStatus: '1', height: 1 },
        bed: { behaviour: 'can_lay_on_top', interactor: 'bed', maxStatus: '2', height: 1.6 },
        lamp: { behaviour: 'solid,custom_data_numeric_on_off,requires_rights_for_interaction', interactor: 'default', maxStatus: '2', height: 2 },
        rug: { behaviour: 'can_stand_on_top,can_stack_on_top,requires_rights_for_interaction', interactor: 'default', maxStatus: '1', height: 0 },
        wall: { behaviour: 'wall_item,requires_rights_for_interaction', interactor: 'default', maxStatus: '1', height: 0 },
        walllamp: { behaviour: 'wall_item,requires_rights_for_interaction,custom_data_on_off', interactor: 'default', maxStatus: '2', height: 0 },
    };

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
            upload = { uploadId: r.uploadId, sprite: sprite, isNew: r.state === 'new', existing: r.definition, states: +m.states || 1 };
            var d = r.definition;
            $('fe-save-def').checked = true;
            $('fe-save-def').disabled = upload.isNew; // a new furni needs its definition
            $('fe-def-state').textContent = d ? 'updates definition ' + d.id : 'a new definition';
            $('fe-name').value = d ? d.name : sprite;
            $('fe-desc').value = d ? d.description || '' : '';
            $('fe-length').value = d ? d.length : (r.large.length || 1);
            $('fe-width').value = d ? d.width : (r.large.width || 1);
            var isWall = !!m.wall || r.large.classes.some(function (c) { return /^Item Object/.test(c); });
            // What the editor knows: a wall item, more than one state (it switches), a Habbo sitting or lying on it.
            var kind = isWall ? (upload.states > 1 ? 'walllamp' : 'wall') : m.kind === 'seat' ? 'seat' : m.kind === 'bed' ? 'bed' : upload.states > 1 ? 'lamp' : 'furniture';
            $('fe-height').value = typeof m.height === 'number' ? m.height : d ? d.topHeight : presets[kind].height;
            $('fe-colour').value = (d && d.colour) || m.colour || '0,0,0';
            $('fe-tradable').checked = d ? d.isTradable : true;
            $('fe-recyclable').checked = d ? d.isRecyclable : true;
            $('fe-preset').value = d ? 'keep' : kind;
            $('fe-note').value = '';
            $('fe-def').hidden = false;
            $('fe-note-row').hidden = false;
            $('fe-confirm').disabled = false;
            if (isWall) $('fe-length').value = $('fe-width').value = 1;
        }).catch(function (err) { alertBox('Check failed: ' + err.message); });
    }

    $('fe-preset').addEventListener('change', function () {
        var p = presets[this.value];
        if (p) $('fe-height').value = p.height;
    });

    $('fe-cancel').addEventListener('click', function () { $('fe-publish').hidden = true; upload = null; note('Publishing cancelled'); });

    $('fe-confirm').addEventListener('click', function () {
        if (!upload) return;
        var d = upload.existing || {};
        var preset = presets[$('fe-preset').value];
        var definition = {
            id: d.id || 0, sprite: upload.sprite, spriteId: d.spriteId || 0,
            name: $('fe-name').value.trim() || upload.sprite, description: $('fe-desc').value.trim(),
            colour: $('fe-colour').value.trim() || '0,0,0',
            length: +$('fe-length').value || 1, width: +$('fe-width').value || 1, topHeight: +$('fe-height').value || 0,
            // a furni that switches has as many states as its .data
            maxStatus: preset ? (preset.maxStatus !== '1' && upload.states > 2 ? String(upload.states) : preset.maxStatus) : (d.maxStatus || '1'),
            behaviour: preset ? preset.behaviour : (d.behaviour || ''),
            interactor: preset ? preset.interactor : (d.interactor || 'default'),
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
        after('GiveMe', { present: $('fe-present').checked }, this, function (r, sprite) {
            return r.present
                ? 'A present with <code>' + esc(sprite) + '</code> is in your hand: place it in a room of yours and open it.'
                : 'One <code>' + esc(sprite) + '</code> is in your hand: open the hotel and place it in a room.';
        });
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

    // --- the editor ---
    window.addEventListener('message', function (e) {
        if (e.origin !== cfg.editorOrigin || e.source !== frame.contentWindow) return;
        var m = e.data || {};
        if (m.type === 'publish' && m.large instanceof ArrayBuffer && (!m.small || m.small instanceof ArrayBuffer)) onPublish(m);
        else if (m.type === 'state') {
            current = { sprite: String(m.sprite || ''), hotel: !!m.hotel, wall: !!m.wall };
            if (lastPublished && lastPublished !== current.sprite) lastPublished = null;
            showState();
        }
    });

    loadMade();
})();

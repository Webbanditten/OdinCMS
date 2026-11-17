// TinyMCE plugins for the housekeeping editors' Furni and Habbo buttons. Both open the same kind of dialog: search,
// pick from a grid of thumbnails, set the options with a live preview, then insert the image. They insert the images
// the way articles have always shown them:
//   furni:  <img src=".../habbo-imaging/furni?sprite=rare_dragonlamp&color=4&direction=2" style="float: right; ...">
//   habbos: <img src=".../habbo-imaging/avatarimage?habbo=Name&action=wav&direction=2&head_direction=2&gesture=sml">
// Select (or double-click) such an image in the text and the same dialog edits it.
// Searches: /api/hotel/furni/search and /housekeeping/editor/habbos. Image URLs: <meta name="habbo-furni-url"> and
// <meta name="habbo-avatar-url"> (keplercms:furniUrl / keplercms:figureUrl).
// Load after tinymce.min.js and before editor.js, which adds the buttons for the plugins registered here.
(function () {
    var MAX_RESULTS = 60;

    function meta(name, fallback) {
        var el = document.querySelector('meta[name="' + name + '"]');
        return (el && el.content) || fallback;
    }

    function text(tag, className, value) {
        var el = document.createElement(tag);
        el.className = className;
        el.textContent = value;
        return el;
    }

    function paramsOf(img) {
        var src = img.getAttribute('src') || '';
        return new URLSearchParams(src.slice(src.indexOf('?') + 1));
    }

    function isOn(params, key) {
        return params.get(key) === 'true' || params.get(key) === '1';
    }

    function oneOf(value, allowed, fallback) {
        return allowed.indexOf(value) !== -1 ? value : fallback;
    }

    function valuesOf(items) {
        return items.map(function (item) { return item.value; });
    }

    // Floated right or left (also the older articles' align="right"), or in the line.
    function alignOf(img) {
        var float = img.style.cssFloat || img.getAttribute('align') || '';
        return float === 'left' || float === 'right' ? float : 'none';
    }

    var ALIGN = [
        { value: 'right', text: 'Right, text around' },
        { value: 'left', text: 'Left, text around' },
        { value: 'none', text: 'In the line' }
    ];

    // Exact match on any field first, then items whose first field starts with the query, then the second...;
    // alphabetical (by the first field) within each group.
    function rank(items, query, fields) {
        var q = query.toLowerCase();
        var score = function (item) {
            var values = fields(item).map(function (v) { return (v || '').toLowerCase(); });
            if (values.indexOf(q) !== -1) return 0;
            for (var i = 0; i < values.length; i++) {
                if (values[i].indexOf(q) === 0) return i + 1;
            }
            return values.length + 1;
        };
        return items
            .map(function (item, index) { return { item: item, score: score(item), index: index }; })
            .sort(function (a, b) {
                return a.score - b.score || (fields(a.item)[0] || '').localeCompare(fields(b.item)[0] || '', 'da', { sensitivity: 'base' }) || a.index - b.index;
            })
            .map(function (entry) { return entry.item; });
    }

    function fetchJson(url) {
        return fetch(url, { credentials: 'same-origin' }).then(function (response) {
            if (!response.ok) throw new Error(response.status);
            return response.json();
        });
    }

    // ---- The dialog, shared by both buttons. `spec` says what is searched and how its image is built. ----

    function openPicker(editor, spec) {
        var selected = editor.selection.getNode();
        var editing = spec.isImage(selected) ? spec.readImage(selected) : null;
        var chosen = editing ? editing.item : null;
        var resultsEl = null;
        var previewEl = null;
        var previewOk = false;
        var closed = false;
        var searchTimer = null;
        var searchId = 0;
        var dialog;

        var options = function () {
            var data = dialog.getData();
            var result = {};
            spec.optionNames.concat(['align']).forEach(function (name) { result[name] = data[name]; });
            return result;
        };

        var setInsertEnabled = function () {
            if (!closed) dialog.setEnabled('insert', !!chosen && previewOk);
        };

        var insert = function () {
            if (closed || !chosen || !previewOk) return;
            var opts = options();
            var name = spec.altText(chosen);
            var attrs = { src: spec.imageUrl(chosen, opts), alt: name, title: name };
            if (opts.align === 'right') attrs.style = 'float: right; margin: 0 0 6px 8px;';
            if (opts.align === 'left') attrs.style = 'float: left; margin: 0 8px 6px 0;';
            dialog.close();
            if (editing) editor.selection.select(selected);
            editor.insertContent(editor.dom.createHTML('img', attrs));
            editor.focus();
        };

        var renderPreview = function () {
            if (!previewEl) return;
            previewEl.textContent = '';
            previewOk = false;
            setInsertEnabled();
            if (!chosen) {
                previewEl.appendChild(text('span', 'habbo-picker-hint', spec.pickHint));
                return;
            }
            var opts = options();
            var img = document.createElement('img');
            img.alt = spec.altText(chosen);
            img.onload = function () { previewOk = true; setInsertEnabled(); };
            img.onerror = function () {
                previewOk = false;
                setInsertEnabled();
                img.replaceWith(text('span', 'habbo-picker-hint', spec.noImage));
            };
            img.src = spec.imageUrl(chosen, opts);
            previewEl.appendChild(img);
            previewEl.appendChild(text('span', 'habbo-picker-caption', spec.caption(chosen, opts)));
        };

        var choose = function (item, button) {
            chosen = item;
            resultsEl.querySelectorAll('.habbo-picker-result').forEach(function (el) {
                el.classList.toggle('is-selected', el === button);
                el.setAttribute('aria-selected', el === button ? 'true' : 'false');
            });
            renderPreview();
        };

        var renderResults = function (items, query) {
            resultsEl.textContent = '';
            if (!items.length) {
                resultsEl.appendChild(text('span', 'habbo-picker-hint', spec.noResults(query)));
                return;
            }
            var refresh = false;
            items.slice(0, MAX_RESULTS).forEach(function (item) {
                var button = document.createElement('button');
                button.type = 'button';
                button.className = 'habbo-picker-result';
                button.setAttribute('role', 'option');
                button.title = spec.itemTitle(item);
                var img = document.createElement('img');
                img.loading = 'lazy';
                img.alt = '';
                img.src = spec.thumbnail(item);
                img.onerror = function () { img.replaceWith(text('span', 'habbo-picker-noimage', '?')); };
                button.appendChild(img);
                button.appendChild(text('span', 'habbo-picker-name', spec.itemName(item)));
                button.addEventListener('click', function () { choose(item, button); });
                button.addEventListener('dblclick', function () {
                    choose(item, button);
                    // Insert straight away once the preview has loaded.
                    var tries = 0;
                    var wait = setInterval(function () {
                        if (previewOk) { clearInterval(wait); insert(); }
                        else if (++tries > 40 || closed) clearInterval(wait);
                    }, 50);
                });
                if (chosen && spec.same(chosen, item)) {
                    button.classList.add('is-selected');
                    button.setAttribute('aria-selected', 'true');
                    // An image read from the text knows less than the search does (furni states, a Habbo's figure).
                    if (chosen.partial) {
                        chosen = item;
                        refresh = true;
                    }
                }
                resultsEl.appendChild(button);
            });
            if (items.length > MAX_RESULTS) {
                resultsEl.appendChild(text('span', 'habbo-picker-hint habbo-picker-more', 'Showing ' + MAX_RESULTS + ' of ' + items.length + '. Type more of the name to narrow it down.'));
            }
            if (refresh) renderPreview();
        };

        var search = function (query) {
            clearTimeout(searchTimer);
            query = (query || '').trim();
            var extra = spec.extraResults ? spec.extraResults(query) : [];
            if (query.length < 2) {
                resultsEl.textContent = '';
                resultsEl.appendChild(text('span', 'habbo-picker-hint', spec.searchHint));
                return;
            }
            if (extra.length) {
                renderResults(extra, query);
                return;
            }
            searchTimer = setTimeout(function () {
                var id = ++searchId;
                spec.search(query)
                    .then(function (items) {
                        if (id !== searchId || closed) return; // a newer search is on its way
                        renderResults(rank(items || [], query, spec.rankFields), query);
                    })
                    .catch(function () {
                        if (id !== searchId || closed) return;
                        resultsEl.textContent = '';
                        resultsEl.appendChild(text('span', 'habbo-picker-hint', 'The search failed. Try again.'));
                    });
            }, 250);
        };

        var initialData = { query: editing ? editing.query : '', align: 'right' };
        Object.keys(spec.defaults).forEach(function (name) { initialData[name] = spec.defaults[name]; });
        if (editing) Object.keys(editing.options).forEach(function (name) { initialData[name] = editing.options[name]; });

        dialog = editor.windowManager.open({
            title: (editing ? 'Edit ' : 'Insert ') + spec.noun,
            size: 'medium',
            body: {
                type: 'panel',
                items: [
                    { type: 'input', name: 'query', label: spec.searchLabel, placeholder: spec.placeholder },
                    { type: 'htmlpanel', html: '<div class="habbo-picker-results" role="listbox" aria-label="' + spec.noun + '"></div>', onInit: function (el) { resultsEl = el.querySelector('.habbo-picker-results') || el; } },
                    { type: 'grid', columns: 4, items: spec.fields.concat([{ type: 'selectbox', name: 'align', label: 'Position', items: ALIGN }]) },
                    { type: 'htmlpanel', html: '<div class="habbo-picker-preview" aria-live="polite"></div>', onInit: function (el) { previewEl = el.querySelector('.habbo-picker-preview') || el; } }
                ]
            },
            initialData: initialData,
            buttons: [
                { type: 'cancel', text: 'Cancel' },
                { type: 'submit', name: 'insert', text: editing ? 'Update' : 'Insert', buttonType: 'primary', enabled: false }
            ],
            onChange: function (api, details) {
                if (details.name === 'query') search(api.getData().query);
                else renderPreview();
            },
            onSubmit: insert,
            onClose: function () {
                closed = true;
                clearTimeout(searchTimer);
            }
        });

        renderPreview();
        search(initialData.query);
    }

    function register(name, spec) {
        tinymce.PluginManager.add(name, function (editor) {
            editor.ui.registry.addToggleButton(name, {
                text: spec.button,
                icon: spec.icon,
                tooltip: spec.tooltip,
                onAction: function () { openPicker(editor, spec); },
                onSetup: function (api) {
                    var update = function (event) { api.setActive(spec.isImage(event.element)); };
                    editor.on('NodeChange', update);
                    return function () { editor.off('NodeChange', update); };
                }
            });
            editor.on('dblclick', function (event) {
                if (spec.isImage(event.target)) {
                    editor.selection.select(event.target);
                    openPicker(editor, spec);
                }
            });
            return { getMetadata: function () { return { name: spec.tooltip }; } };
        });
    }

    // ---- Furni ----

    // "rare_dragonlamp*4" is the dragon lamp in colour 4.
    function splitSprite(sprite) {
        var parts = (sprite || '').split('*');
        return { sprite: parts[0], color: parseInt(parts[1], 10) || 0 };
    }

    function furniLabel(item) {
        return item.name ? item.name + ' (' + item.sprite + ')' : item.sprite;
    }

    function furniUrl(item, options) {
        var parts = splitSprite(item.sprite);
        var query = ['sprite=' + encodeURIComponent(parts.sprite)];
        if (parts.color) query.push('color=' + parts.color);
        if (options.size === 'icon') {
            query.push('icon=true');
        } else {
            if (options.size === 'small') query.push('s=true');
            query.push('direction=' + options.direction);
            var state = Math.max(0, parseInt(options.state, 10) || 0);
            if (state > 0) query.push('state=' + state);
        }
        return meta('habbo-furni-url', '/habbo-imaging/furni') + '?' + query.join('&');
    }

    var FURNI_ROTATION = [{ value: '2', text: '2 (default)' }, { value: '4', text: '4' }, { value: '0', text: '0' }, { value: '6', text: '6' }];
    var FURNI_SIZE = [{ value: 'room', text: 'As in the room' }, { value: 'small', text: 'Small' }, { value: 'icon', text: 'Catalogue icon' }];

    register('habbofurni', {
        noun: 'furni image',
        button: 'Furni',
        icon: 'image',
        tooltip: 'Insert a furni image (select one in the text to edit it)',
        searchLabel: 'Search furni (name or sprite)',
        placeholder: 'e.g. drage or rare_dragonlamp',
        searchHint: 'Type at least 2 letters of a furni name or sprite.',
        pickHint: 'Search for a furni and click it.',
        noImage: 'No image for this furni with these settings. Try another rotation, state or size.',
        noResults: function (query) { return 'No furni found for "' + query + '".'; },
        fields: [
            { type: 'selectbox', name: 'direction', label: 'Rotation', items: FURNI_ROTATION },
            { type: 'input', name: 'state', label: 'State', inputMode: 'numeric' },
            { type: 'selectbox', name: 'size', label: 'Size', items: FURNI_SIZE }
        ],
        optionNames: ['direction', 'state', 'size'],
        defaults: { direction: '2', state: '0', size: 'room' },
        isImage: function (node) {
            return !!node && node.nodeName === 'IMG' && /\/habbo-imaging\/furni\?/.test(node.getAttribute('src') || '');
        },
        readImage: function (img) {
            var params = paramsOf(img);
            var color = parseInt(params.get('color') || params.get('colour'), 10) || 0;
            var sprite = params.get('sprite') || '';
            return {
                query: sprite,
                item: { sprite: sprite + (color ? '*' + color : ''), name: img.getAttribute('alt') || img.getAttribute('title') || '', partial: true },
                options: {
                    direction: oneOf(params.get('direction') || params.get('rotation') || '2', valuesOf(FURNI_ROTATION), '2'),
                    state: params.get('state') || '0',
                    size: isOn(params, 'icon') ? 'icon' : (isOn(params, 's') || isOn(params, 'small') ? 'small' : 'room'),
                    align: alignOf(img)
                }
            };
        },
        search: function (query) { return fetchJson('/api/hotel/furni/search?query=' + encodeURIComponent(query)); },
        rankFields: function (item) { return [item.name, item.sprite]; },
        // The same furni and colour ("rare_dragonlamp*0" is "rare_dragonlamp" in its default colour).
        same: function (a, b) {
            var x = splitSprite(a.sprite);
            var y = splitSprite(b.sprite);
            return x.sprite === y.sprite && x.color === y.color;
        },
        itemName: function (item) { return item.name || item.sprite; },
        itemTitle: furniLabel,
        altText: function (item) { return item.name || splitSprite(item.sprite).sprite; },
        thumbnail: function (item) { return furniUrl(item, { size: 'icon' }); },
        imageUrl: furniUrl,
        caption: function (item) {
            var states = parseInt(item.maxStatus, 10);
            return furniLabel(item) + (states > 1 ? '. States 0 to ' + (states - 1) + '.' : '');
        }
    });

    // ---- Habbos ----

    var ACTIONS = [
        { value: 'std', text: 'Standing' }, { value: 'wav', text: 'Waving' }, { value: 'wlk', text: 'Walking' },
        { value: 'sit', text: 'Sitting' }, { value: 'lay', text: 'Lying down' }, { value: 'crr', text: 'Holding a drink' },
        { value: 'drk', text: 'Drinking' }
    ];
    var GESTURES = [
        { value: 'sml', text: 'Smiling' }, { value: 'std', text: 'Neutral' }, { value: 'srp', text: 'Surprised' },
        { value: 'sad', text: 'Sad' }, { value: 'agr', text: 'Angry' }, { value: 'spk', text: 'Talking' },
        { value: 'eyb', text: 'Eyes closed' }
    ];
    // Habbo directions, as the viewer sees them.
    var DIRECTIONS = [
        { value: '2', text: 'Front right (default)' }, { value: '3', text: 'Front' }, { value: '4', text: 'Front left' },
        { value: '1', text: 'Right' }, { value: '5', text: 'Left' }, { value: '0', text: 'Back right' },
        { value: '6', text: 'Back left' }, { value: '7', text: 'Back' }
    ];
    var HEAD = [{ value: 'same', text: 'Same as body' }].concat(DIRECTIONS.map(function (d) { return { value: d.value, text: d.text.replace(' (default)', '') }; }));
    var SIZES = [{ value: 'b', text: 'Normal' }, { value: 's', text: 'Small' }, { value: 'l', text: 'Large' }, { value: 'head', text: 'Head only' }];
    var LOOKS = [{ value: 'live', text: 'Always current' }, { value: 'frozen', text: 'Keep today\'s' }];

    function avatarUrl(item, options) {
        var query = [];
        if (options.look === 'live' && item.username) query.push('habbo=' + encodeURIComponent(item.username));
        else query.push('figure=' + encodeURIComponent(item.figure || ''));
        var action = options.action;
        var carry = 0;
        if (action === 'crr') { action = 'std'; carry = 1; }
        if (action === 'drk') carry = 1;
        query.push('action=' + action);
        query.push('direction=' + options.direction);
        query.push('head_direction=' + (options.head === 'same' ? options.direction : options.head));
        query.push('gesture=' + options.gesture);
        if (options.size === 'head') query.push('head=true');
        else if (options.size !== 'b') query.push('size=' + options.size);
        if (carry) query.push('crr=' + carry);
        return meta('habbo-avatar-url', '/habbo-imaging/avatarimage') + '?' + query.join('&');
    }

    function avatarHead(item) {
        var who = item.figure ? 'figure=' + encodeURIComponent(item.figure) : 'habbo=' + encodeURIComponent(item.username || '');
        return meta('habbo-avatar-url', '/habbo-imaging/avatarimage') + '?' + who + '&head=true&direction=3&head_direction=3&gesture=sml';
    }

    register('habboavatar', {
        noun: 'Habbo',
        button: 'Habbo',
        icon: 'user',
        tooltip: 'Insert a Habbo (select one in the text to edit it)',
        searchLabel: 'Search Habbo (name), or paste a figure code',
        placeholder: 'e.g. Patrick',
        searchHint: 'Type at least 2 letters of a Habbo name, or paste a figure code.',
        pickHint: 'Search for a Habbo and click them.',
        noImage: 'No image for this Habbo with these settings.',
        noResults: function (query) { return 'No Habbo found for "' + query + '".'; },
        fields: [
            { type: 'selectbox', name: 'action', label: 'Doing', items: ACTIONS },
            { type: 'selectbox', name: 'gesture', label: 'Expression', items: GESTURES },
            { type: 'selectbox', name: 'direction', label: 'Facing', items: DIRECTIONS },
            { type: 'selectbox', name: 'head', label: 'Head turned', items: HEAD },
            { type: 'selectbox', name: 'size', label: 'Size', items: SIZES },
            { type: 'selectbox', name: 'look', label: 'Look', items: LOOKS }
        ],
        optionNames: ['action', 'gesture', 'direction', 'head', 'size', 'look'],
        defaults: { action: 'std', gesture: 'sml', direction: '2', head: 'same', size: 'b', look: 'live' },
        isImage: function (node) {
            return !!node && node.nodeName === 'IMG' && /\/habbo-imaging\/avatarimage\?/.test(node.getAttribute('src') || '');
        },
        readImage: function (img) {
            var params = paramsOf(img);
            var username = params.get('habbo');
            var figure = params.get('figure');
            var alt = img.getAttribute('alt') || img.getAttribute('title') || '';
            var action = params.get('action') || 'std';
            var carry = parseInt(params.get('crr'), 10) > 0;
            if (isOn(params, 'drk')) action = 'drk';
            if (action === 'std' && carry) action = 'crr';
            var direction = oneOf(params.get('direction') || '2', valuesOf(DIRECTIONS), '2');
            var head = params.get('head_direction');
            var name = username || (alt && alt !== 'Habbo' ? alt : null);
            return {
                query: name || figure || '',
                // With habbo= the search fills in the figure; with figure= that look is kept unless another is picked.
                item: username ? { username: username, figure: null, partial: true } : { username: name, figure: figure },
                options: {
                    action: oneOf(action, valuesOf(ACTIONS), 'std'),
                    gesture: oneOf(params.get('gesture') || 'sml', valuesOf(GESTURES), 'sml'),
                    direction: direction,
                    head: head === null || head === direction ? 'same' : oneOf(head, valuesOf(DIRECTIONS), 'same'),
                    size: isOn(params, 'head') ? 'head' : oneOf(params.get('size') || 'b', ['b', 's', 'l'], 'b'),
                    look: username ? 'live' : 'frozen',
                    align: alignOf(img)
                }
            };
        },
        search: function (query) { return fetchJson('/housekeeping/editor/habbos?query=' + encodeURIComponent(query)); },
        // A figure code (2006 figures are all digits) needs no search.
        extraResults: function (query) { return /^\d{10,}$/.test(query) ? [{ username: null, figure: query }] : []; },
        rankFields: function (item) { return [item.username]; },
        same: function (a, b) {
            if (a.username && b.username) return a.username.toLowerCase() === b.username.toLowerCase();
            return !a.username && !b.username && a.figure === b.figure;
        },
        itemName: function (item) { return item.username || 'Figure code'; },
        itemTitle: function (item) { return item.username || item.figure; },
        altText: function (item) { return item.username || 'Habbo'; },
        thumbnail: avatarHead,
        imageUrl: avatarUrl,
        caption: function (item, options) {
            if (!item.username) return 'Figure code ' + item.figure + '.';
            return item.username + (options.look === 'live' ? ': always shows their current look.' : ': keeps the look they have today.');
        }
    });
})();

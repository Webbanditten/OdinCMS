// TinyMCE plugin "habbofurni": the editor's Furni button. Search furni by name or sprite, pick one, choose rotation,
// state, size and position with a live preview, and it inserts the image the way news articles have shown furni:
//   <img src=".../habbo-imaging/furni?sprite=rare_dragonlamp&color=4&direction=2" style="float: right; ...">
// Select (or double-click) a furni image in the text and the same dialog edits it.
// Search: /api/hotel/furni/search. Images: <meta name="habbo-furni-url"> (keplercms:furniUrl, default /habbo-imaging/furni).
// Load after tinymce.min.js and before initHabboEditor (editor.js adds the button when this plugin is registered).
(function () {
    var MAX_RESULTS = 60;

    function baseUrl() {
        var meta = document.querySelector('meta[name="habbo-furni-url"]');
        return (meta && meta.content) || '/habbo-imaging/furni';
    }

    // "rare_dragonlamp*4" is the dragon lamp in colour 4.
    function splitSprite(sprite) {
        var parts = (sprite || '').split('*');
        return { sprite: parts[0], color: parseInt(parts[1], 10) || 0 };
    }

    // The same furni and colour ("rare_dragonlamp*0" is "rare_dragonlamp" in its default colour).
    function sameFurni(a, b) {
        var x = splitSprite(a);
        var y = splitSprite(b);
        return x.sprite === y.sprite && x.color === y.color;
    }

    function label(item) {
        return item.name ? item.name + ' (' + item.sprite + ')' : item.sprite;
    }

    function imageUrl(sprite, color, options) {
        var query = ['sprite=' + encodeURIComponent(sprite)];
        if (color) query.push('color=' + color);
        if (options.size === 'icon') {
            query.push('icon=true');
        } else {
            if (options.size === 'small') query.push('s=true');
            query.push('direction=' + options.direction);
            if (options.state > 0) query.push('state=' + options.state);
        }
        return baseUrl() + '?' + query.join('&');
    }

    function thumbnailUrl(item) {
        var parts = splitSprite(item.sprite);
        return imageUrl(parts.sprite, parts.color, { size: 'icon' });
    }

    function isFurniImage(node) {
        return !!node && node.nodeName === 'IMG' && /\/habbo-imaging\/furni\?/.test(node.getAttribute('src') || '');
    }

    function alignOf(img) {
        var float = img.style.cssFloat || img.getAttribute('align') || '';
        return float === 'left' || float === 'right' ? float : 'none';
    }

    // The options of an image already in the text (also the older articles' rotation= and align="right").
    function readImage(img) {
        var src = img.getAttribute('src');
        var params = new URLSearchParams(src.slice(src.indexOf('?') + 1));
        var isOn = function (key) { return params.get(key) === 'true' || params.get(key) === '1'; };
        var color = parseInt(params.get('color') || params.get('colour'), 10) || 0;
        return {
            sprite: (params.get('sprite') || '') + (color ? '*' + color : ''),
            name: img.getAttribute('alt') || img.getAttribute('title') || params.get('sprite') || '',
            direction: params.get('direction') || params.get('rotation') || '2',
            state: params.get('state') || '0',
            size: isOn('icon') ? 'icon' : (isOn('s') || isOn('small') ? 'small' : 'room'),
            align: alignOf(img)
        };
    }

    // Exact sprite or name first, then names and sprites that start with the query, then the rest.
    function rank(items, query) {
        var q = query.toLowerCase();
        var score = function (item) {
            var name = (item.name || '').toLowerCase();
            var sprite = (item.sprite || '').toLowerCase();
            if (sprite === q || name === q) return 0;
            if (name.indexOf(q) === 0) return 1;
            if (sprite.indexOf(q) === 0) return 2;
            return 3;
        };
        return items
            .map(function (item, index) { return { item: item, score: score(item), index: index }; })
            .sort(function (a, b) { return a.score - b.score || a.index - b.index; })
            .map(function (entry) { return entry.item; });
    }

    function stateCount(item) {
        var count = parseInt(item && item.maxStatus, 10);
        return count > 1 ? count : 0;
    }

    function open(editor) {
        var selected = editor.selection.getNode();
        var editing = isFurniImage(selected) ? readImage(selected) : null;
        var chosen = editing ? { sprite: editing.sprite, name: editing.name } : null;
        var resultsEl = null;
        var previewEl = null;
        var previewOk = false;
        var searchTimer = null;
        var searchId = 0;
        var closed = false;
        var dialog;

        var options = function () {
            var data = dialog.getData();
            return {
                direction: data.direction,
                state: Math.max(0, parseInt(data.state, 10) || 0),
                size: data.size,
                align: data.align
            };
        };

        var setInsertEnabled = function () {
            if (!closed) dialog.setEnabled('insert', !!chosen && previewOk);
        };

        var insert = function () {
            if (closed || !chosen || !previewOk) return;
            var parts = splitSprite(chosen.sprite);
            var opts = options();
            var name = chosen.name || parts.sprite;
            var attrs = { src: imageUrl(parts.sprite, parts.color, opts), alt: name, title: name };
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
            if (!chosen) {
                previewEl.appendChild(text('span', 'habbo-furni-hint', 'Search for a furni and click it.'));
                previewOk = false;
                setInsertEnabled();
                return;
            }
            var parts = splitSprite(chosen.sprite);
            var img = document.createElement('img');
            img.alt = chosen.name;
            img.onload = function () { previewOk = true; setInsertEnabled(); };
            img.onerror = function () {
                previewOk = false;
                setInsertEnabled();
                img.replaceWith(text('span', 'habbo-furni-hint', 'No image for this furni with these settings. Try another rotation, state or size.'));
            };
            previewOk = false;
            setInsertEnabled();
            img.src = imageUrl(parts.sprite, parts.color, options());
            previewEl.appendChild(img);
            var caption = label(chosen);
            var states = stateCount(chosen);
            if (states) caption += '. States 0 to ' + (states - 1) + '.';
            previewEl.appendChild(text('span', 'habbo-furni-caption', caption));
        };

        var choose = function (item, button) {
            chosen = item;
            if (resultsEl) {
                resultsEl.querySelectorAll('.habbo-furni-result').forEach(function (el) {
                    el.classList.toggle('is-selected', el === button);
                    el.setAttribute('aria-selected', el === button ? 'true' : 'false');
                });
            }
            renderPreview();
        };

        var renderResults = function (items, query) {
            resultsEl.textContent = '';
            if (!items.length) {
                resultsEl.appendChild(text('span', 'habbo-furni-hint', 'No furni found for "' + query + '".'));
                return;
            }
            items.slice(0, MAX_RESULTS).forEach(function (item) {
                var button = document.createElement('button');
                button.type = 'button';
                button.className = 'habbo-furni-result';
                button.setAttribute('role', 'option');
                button.title = label(item);
                var img = document.createElement('img');
                img.loading = 'lazy';
                img.alt = '';
                img.src = thumbnailUrl(item);
                img.onerror = function () { img.replaceWith(text('span', 'habbo-furni-noimage', '?')); };
                button.appendChild(img);
                button.appendChild(text('span', 'habbo-furni-name', item.name || item.sprite));
                button.addEventListener('click', function () { choose(item, button); });
                button.addEventListener('dblclick', function () {
                    choose(item, button);
                    // Insert straight away once the preview has loaded.
                    var tries = 0;
                    var wait = setInterval(function () {
                        if (previewOk) { clearInterval(wait); insert(); }
                        else if (++tries > 20) clearInterval(wait);
                    }, 50);
                });
                if (chosen && sameFurni(chosen.sprite, item.sprite)) {
                    button.classList.add('is-selected');
                    chosen = item;
                }
                resultsEl.appendChild(button);
            });
            if (items.length > MAX_RESULTS) {
                resultsEl.appendChild(text('span', 'habbo-furni-hint habbo-furni-more', 'Showing ' + MAX_RESULTS + ' of ' + items.length + '. Type more of the name to narrow it down.'));
            }
        };

        var search = function (query) {
            clearTimeout(searchTimer);
            query = (query || '').trim();
            if (query.length < 2) {
                resultsEl.textContent = '';
                resultsEl.appendChild(text('span', 'habbo-furni-hint', 'Type at least 2 letters of a furni name or sprite.'));
                return;
            }
            searchTimer = setTimeout(function () {
                var id = ++searchId;
                fetch('/api/hotel/furni/search?query=' + encodeURIComponent(query), { credentials: 'same-origin' })
                    .then(function (response) { return response.ok ? response.json() : []; })
                    .then(function (items) {
                        if (id !== searchId) return; // a newer search is on its way
                        var hadStates = chosen && chosen.maxStatus !== undefined;
                        renderResults(rank(items || [], query), query);
                        if (chosen && !hadStates && chosen.maxStatus !== undefined) renderPreview();
                    })
                    .catch(function () {
                        if (id !== searchId) return;
                        resultsEl.textContent = '';
                        resultsEl.appendChild(text('span', 'habbo-furni-hint', 'The furni search failed. Try again.'));
                    });
            }, 250);
        };

        dialog = editor.windowManager.open({
            title: editing ? 'Edit furni image' : 'Insert furni image',
            size: 'medium',
            body: {
                type: 'panel',
                items: [
                    { type: 'input', name: 'query', label: 'Search furni (name or sprite)', placeholder: 'e.g. drage or rare_dragonlamp' },
                    { type: 'htmlpanel', html: '<div class="habbo-furni-results" role="listbox" aria-label="Furni"></div>', onInit: function (el) { resultsEl = el.querySelector('.habbo-furni-results') || el; } },
                    {
                        type: 'grid', columns: 4, items: [
                            { type: 'selectbox', name: 'direction', label: 'Rotation', items: [
                                { value: '2', text: '2 (default)' }, { value: '4', text: '4' }, { value: '0', text: '0' }, { value: '6', text: '6' }] },
                            { type: 'input', name: 'state', label: 'State', inputMode: 'numeric' },
                            { type: 'selectbox', name: 'size', label: 'Size', items: [
                                { value: 'room', text: 'As in the room' }, { value: 'small', text: 'Small' }, { value: 'icon', text: 'Catalogue icon' }] },
                            { type: 'selectbox', name: 'align', label: 'Position', items: [
                                { value: 'right', text: 'Right, text around' }, { value: 'left', text: 'Left, text around' }, { value: 'none', text: 'In the line' }] }
                        ]
                    },
                    { type: 'htmlpanel', html: '<div class="habbo-furni-preview" aria-live="polite"></div>', onInit: function (el) { previewEl = el.querySelector('.habbo-furni-preview') || el; } }
                ]
            },
            initialData: {
                query: editing ? splitSprite(editing.sprite).sprite : '',
                direction: editing ? editing.direction : '2',
                state: editing ? editing.state : '0',
                size: editing ? editing.size : 'room',
                align: editing ? editing.align : 'right'
            },
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
        if (editing) search(splitSprite(editing.sprite).sprite);
        else search('');
    }

    function text(tag, className, value) {
        var el = document.createElement(tag);
        el.className = className;
        el.textContent = value;
        return el;
    }

    tinymce.PluginManager.add('habbofurni', function (editor) {
        editor.ui.registry.addToggleButton('habbofurni', {
            text: 'Furni',
            icon: 'image',
            tooltip: 'Insert a furni image (select one in the text to edit it)',
            onAction: function () { open(editor); },
            onSetup: function (api) {
                var update = function (event) { api.setActive(isFurniImage(event.element)); };
                editor.on('NodeChange', update);
                return function () { editor.off('NodeChange', update); };
            }
        });
        editor.on('dblclick', function (event) {
            if (isFurniImage(event.target)) {
                editor.selection.select(event.target);
                open(editor);
            }
        });
        return { getMetadata: function () { return { name: 'Habbo furni images' }; } };
    });
})();

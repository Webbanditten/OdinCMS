// TinyMCE plugin "habboshop": the housekeeping editors' "Buy furni" button. It inserts {buy_furni_ID} (ID =
// catalogue_items.id), which the site renders as the furni, its price and a one-click buy button
// (Models/BuyFurniWidget.cs, Views/Shared/Containers/BuyFurni.cshtml). Only what any player can buy in the game's
// catalogue can be inserted; the rest is listed greyed out with the reason. Select a code in the text and the dialog
// edits it. Search: /housekeeping/editor/shopfurni?query=, one item: /housekeeping/editor/shopfurni/{id}.
// Load after tinymce.min.js and before editor.js.
(function () {
    var CODE = /\{buy_furni_(\d{1,9})\}/i;

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function furniImage(item, icon) {
        var meta = document.querySelector('meta[name="habbo-furni-url"]');
        var base = (meta && meta.content) || '/habbo-imaging/furni';
        var parts = (item.sprite || '').split('*');
        if (!parts[0]) return '';
        return base + '?sprite=' + encodeURIComponent(parts[0]) + (parts[1] ? '&color=' + parts[1] : '') + (icon ? '&icon=true' : '&direction=2');
    }

    function fetchJson(url) {
        return fetch(url, { credentials: 'same-origin' }).then(function (response) {
            if (!response.ok) throw new Error(response.statusText);
            return response.json();
        });
    }

    function open(editor) {
        var selected = editor.selection.getContent({ format: 'text' }) || '';
        var editingMatch = CODE.exec(selected);
        var chosen = null, resultsEl = null, previewEl = null, dialog = null, timer = null, searchId = 0, closed = false;

        var renderPreview = function () {
            if (!previewEl) return;
            previewEl.textContent = '';
            if (!chosen) {
                previewEl.appendChild(el('span', 'habbo-picker-hint', 'Pick a furni. The site shows it with its price and a buy button; one click buys it.'));
                if (dialog) dialog.setEnabled('insert', false);
                return;
            }
            var box = el('div', 'habbo-shop-preview');
            var src = furniImage(chosen, false);
            if (src) {
                var img = el('img');
                img.alt = '';
                img.src = src;
                img.onerror = function () { img.remove(); };
                box.appendChild(img);
            }
            var text = el('div', 'habbo-shop-text');
            text.appendChild(el('strong', null, chosen.name));
            if (chosen.description && chosen.description !== chosen.name) text.appendChild(el('div', null, chosen.description));
            if (chosen.notForSale) {
                text.appendChild(el('div', 'habbo-shop-reason', 'Can\'t be sold on the website: ' + chosen.notForSale + '.'));
            } else {
                text.appendChild(el('div', null, chosen.price + ' Habbo Mønter'));
                text.appendChild(el('span', 'habbo-shop-button', 'Køb'));
            }
            text.appendChild(el('div', 'habbo-picker-caption', '{buy_furni_' + chosen.id + '} · ' + chosen.saleCode + (chosen.isPackage ? ' · package' : '')));
            box.appendChild(text);
            previewEl.appendChild(box);
            if (dialog) dialog.setEnabled('insert', !chosen.notForSale);
        };

        var renderResults = function (items, query) {
            if (!resultsEl) return;
            resultsEl.textContent = '';
            if (!items.length) {
                resultsEl.appendChild(el('span', 'habbo-picker-hint', 'Nothing in the catalogue matches "' + query + '".'));
                return;
            }
            items.forEach(function (item) {
                var button = el('button', 'habbo-picker-result' + (item.notForSale ? ' is-not-for-sale' : ''));
                button.type = 'button';
                button.title = item.name + ' (' + item.saleCode + ', ' + item.price + ' credits)' + (item.notForSale ? '\n' + item.notForSale : '');
                var src = furniImage(item, true);
                if (src) {
                    var img = el('img');
                    img.loading = 'lazy';
                    img.alt = '';
                    img.src = src;
                    img.onerror = function () { img.replaceWith(el('span', 'habbo-picker-noimage', '?')); };
                    button.appendChild(img);
                }
                button.appendChild(el('span', 'habbo-picker-name', item.name));
                if (chosen && chosen.id === item.id) button.classList.add('is-selected');
                button.addEventListener('click', function () {
                    chosen = item;
                    resultsEl.querySelectorAll('.is-selected').forEach(function (b) { b.classList.remove('is-selected'); });
                    button.classList.add('is-selected');
                    renderPreview();
                });
                button.addEventListener('dblclick', function () { if (!item.notForSale) insert(); });
                resultsEl.appendChild(button);
            });
        };

        var search = function (query) {
            clearTimeout(timer);
            query = (query || '').trim();
            if (query.length < 2) {
                if (resultsEl) {
                    resultsEl.textContent = '';
                    resultsEl.appendChild(el('span', 'habbo-picker-hint', 'Type at least 2 letters of the name or sale code, or a catalogue id.'));
                }
                return;
            }
            timer = setTimeout(function () {
                var id = ++searchId;
                fetchJson('/housekeeping/editor/shopfurni?query=' + encodeURIComponent(query)).then(function (items) {
                    if (id === searchId && !closed) renderResults(items || [], query);
                }).catch(function () {
                    if (id !== searchId || closed || !resultsEl) return;
                    resultsEl.textContent = '';
                    resultsEl.appendChild(el('span', 'habbo-picker-hint', 'The search failed. Try again.'));
                });
            }, 250);
        };

        var insert = function () {
            if (!chosen || chosen.notForSale) return;
            editor.focus();
            // The widget is a box with its own buy link: never put it inside a link (e.g. the cursor at the end of a
            // "Læs mere" link), it would render as part of that link.
            if (!editingMatch) {
                var anchor = editor.dom.getParent(editor.selection.getNode(), 'a');
                if (anchor) {
                    editor.selection.select(anchor);
                    editor.selection.collapse(false);
                }
            }
            editor.insertContent('{buy_furni_' + chosen.id + '}');
            closed = true;
            dialog.close();
        };

        dialog = editor.windowManager.open({
            title: (editingMatch ? 'Edit' : 'Insert') + ' furni for sale',
            size: 'medium',
            body: {
                type: 'panel',
                items: [
                    { type: 'input', name: 'query', label: 'Search the catalogue', placeholder: 'Name, sale code or catalogue id' },
                    { type: 'htmlpanel', html: '<div class="habbo-picker-results" role="listbox" aria-label="Furni"></div>', onInit: function (node) { resultsEl = node.querySelector('.habbo-picker-results') || node; } },
                    { type: 'htmlpanel', html: '<div class="habbo-picker-preview" aria-live="polite"></div>', onInit: function (node) { previewEl = node.querySelector('.habbo-picker-preview') || node; } }
                ]
            },
            initialData: { query: '' },
            buttons: [
                { type: 'cancel', text: 'Cancel' },
                { type: 'submit', name: 'insert', text: editingMatch ? 'Update' : 'Insert', buttonType: 'primary', enabled: false }
            ],
            onChange: function (api, details) {
                if (details.name === 'query') search(api.getData().query);
            },
            onSubmit: insert,
            onClose: function () { closed = true; clearTimeout(timer); }
        });

        // TinyMCE builds the htmlpanels after open() returns.
        setTimeout(function () {
            resultsEl = resultsEl || document.querySelector('.tox-dialog .habbo-picker-results');
            previewEl = previewEl || document.querySelector('.tox-dialog .habbo-picker-preview');
            renderPreview();
            search('');
            if (editingMatch) {
                fetchJson('/housekeeping/editor/shopfurni/' + editingMatch[1]).then(function (item) {
                    if (closed) return;
                    chosen = item;
                    dialog.setData({ query: item.name });
                    search(item.name);
                    renderPreview();
                }).catch(function () { });
            }
        }, 0);
    }

    tinymce.PluginManager.add('habboshop', function (editor) {
        editor.ui.registry.addButton('habboshop', {
            text: 'Buy furni',
            icon: 'shopping-cart',
            tooltip: 'Insert a furni players can buy with one click',
            onAction: function () { open(editor); }
        });
        return { getMetadata: function () { return { name: 'Buy furni' }; } };
    });
})();

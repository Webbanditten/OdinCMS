// TinyMCE plugin "habboroom": the housekeeping editors' "Room" button. It inserts {room_ID} (ID = rooms.id), which the
// site renders as the room's photo, name, owner, description, visitors and a "go to room" link
// (Models/RoomWidget.cs, Views/Shared/Containers/Room.cshtml). The photo is the one taken in housekeeping → room →
// Take photo; a room without one shows only its details. Select a code in the text and the dialog edits it.
// Search: /housekeeping/editor/rooms?query=, one room: /housekeeping/editor/rooms/{id}.
// Load after tinymce.min.js and before editor.js.
(function () {
    var CODE = /\{room_(\d{1,9})\}/i;

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
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
                previewEl.appendChild(el('span', 'habbo-picker-hint', 'Pick a room. The site shows its photo, name, owner and description with a "Gå til rummet" button.'));
                if (dialog) dialog.setEnabled('insert', false);
                return;
            }
            var box = el('div', 'habbo-room-preview');
            if (chosen.photo) {
                var img = el('img');
                img.alt = '';
                img.src = chosen.photo;
                box.appendChild(img);
            }
            var text = el('div', 'habbo-room-text');
            var title = el('div');
            title.appendChild(el('strong', null, chosen.name));
            if (chosen.owner) title.appendChild(document.createTextNode(' af ' + chosen.owner));
            text.appendChild(title);
            if (chosen.description) text.appendChild(el('div', null, chosen.description));
            if (!chosen.photo) {
                var hint = el('div', 'habbo-shop-reason', 'No photo yet. ');
                var link = el('a', null, 'Take one');
                link.href = '/housekeeping/rooms/view/' + chosen.id;
                link.target = '_blank';
                hint.appendChild(link);
                hint.appendChild(document.createTextNode(' first, or the site shows only the details.'));
                text.appendChild(hint);
            }
            text.appendChild(el('div', 'habbo-picker-caption', '{room_' + chosen.id + '}' + (chosen.owner ? '' : ' · public room')));
            box.appendChild(text);
            previewEl.appendChild(box);
            if (dialog) dialog.setEnabled('insert', true);
        };

        var renderResults = function (items, query) {
            if (!resultsEl) return;
            resultsEl.textContent = '';
            if (!items.length) {
                resultsEl.appendChild(el('span', 'habbo-picker-hint', 'No room matches "' + query + '".'));
                return;
            }
            items.forEach(function (item) {
                var button = el('button', 'habbo-picker-result habbo-room-result' + (item.photo ? '' : ' is-without-photo'));
                button.type = 'button';
                button.title = item.name + (item.owner ? ' af ' + item.owner : ' (public room)') + ' · id ' + item.id + (item.photo ? '' : '\nNo photo yet');
                if (item.photo) {
                    var img = el('img');
                    img.loading = 'lazy';
                    img.alt = '';
                    img.src = item.photo;
                    button.appendChild(img);
                } else {
                    button.appendChild(el('span', 'habbo-picker-noimage', 'no photo'));
                }
                button.appendChild(el('span', 'habbo-picker-name', item.name));
                if (chosen && chosen.id === item.id) button.classList.add('is-selected');
                button.addEventListener('click', function () {
                    chosen = item;
                    resultsEl.querySelectorAll('.is-selected').forEach(function (b) { b.classList.remove('is-selected'); });
                    button.classList.add('is-selected');
                    renderPreview();
                });
                button.addEventListener('dblclick', function () { insert(); });
                resultsEl.appendChild(button);
            });
        };

        var search = function (query) {
            clearTimeout(timer);
            query = (query || '').trim();
            if (query.length < 2 && !/^\d+$/.test(query)) {
                if (resultsEl) {
                    resultsEl.textContent = '';
                    resultsEl.appendChild(el('span', 'habbo-picker-hint', 'Type at least 2 letters of the room or owner name, or a room id.'));
                }
                return;
            }
            timer = setTimeout(function () {
                var id = ++searchId;
                fetchJson('/housekeeping/editor/rooms?query=' + encodeURIComponent(query)).then(function (items) {
                    if (id === searchId && !closed) renderResults(items || [], query);
                }).catch(function () {
                    if (id !== searchId || closed || !resultsEl) return;
                    resultsEl.textContent = '';
                    resultsEl.appendChild(el('span', 'habbo-picker-hint', 'The search failed. Try again.'));
                });
            }, 250);
        };

        var insert = function () {
            if (!chosen) return;
            editor.focus();
            // The widget has its own links: never put it inside a link.
            if (!editingMatch) {
                var anchor = editor.dom.getParent(editor.selection.getNode(), 'a');
                if (anchor) {
                    editor.selection.select(anchor);
                    editor.selection.collapse(false);
                }
            }
            editor.insertContent('{room_' + chosen.id + '}');
            closed = true;
            dialog.close();
        };

        dialog = editor.windowManager.open({
            title: (editingMatch ? 'Edit' : 'Insert') + ' room',
            size: 'medium',
            body: {
                type: 'panel',
                items: [
                    { type: 'input', name: 'query', label: 'Search rooms', placeholder: 'Room name, owner or room id' },
                    { type: 'htmlpanel', html: '<div class="habbo-picker-results" role="listbox" aria-label="Rooms"></div>', onInit: function (node) { resultsEl = node.querySelector('.habbo-picker-results') || node; } },
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
                fetchJson('/housekeeping/editor/rooms/' + editingMatch[1]).then(function (item) {
                    if (closed) return;
                    chosen = item;
                    dialog.setData({ query: item.name });
                    search(item.name);
                    renderPreview();
                }).catch(function () { });
            }
        }, 0);
    }

    tinymce.PluginManager.add('habboroom', function (editor) {
        editor.ui.registry.addButton('habboroom', {
            text: 'Room',
            icon: 'home',
            tooltip: 'Insert a room with its photo and a "go to room" link',
            onAction: function () { open(editor); }
        });
        return { getMetadata: function () { return { name: 'Room' }; } };
    });
})();

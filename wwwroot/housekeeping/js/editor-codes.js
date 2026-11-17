// TinyMCE plugin "habbocodes": the housekeeping editors' Codes button. It opens an overview of the codes the site
// renders, and a click inserts one:
//   - box widgets ({staff}, {credits}, ...), only when the editor option habbo_box_codes is on (page boxes set it
//     through initHabboEditor(selector, { boxCodes: true }); news can't render them);
//   - highscore tables per game, plus a builder for any period and row count.
// The list is Models/ContentCodes.cs, rendered into <script id="habbo-content-codes"> by _EditorScripts.cshtml.
// A box widget replaces the whole box, so inserting one asks before it replaces other text, and the editor shows a
// warning while a box mixes a box widget with text the site won't show.
(function () {
    var PERIODS = [
        { value: 'week', text: 'This week' },
        { value: 'month', text: 'This month' },
        { value: 'all', text: 'All time (running totals)' }
    ];

    function catalog() {
        var node = document.getElementById('habbo-content-codes');
        try {
            return node ? JSON.parse(node.textContent) : null;
        } catch (e) {
            return null;
        }
    }

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    // {highscores_<game>[_<period>][_<rows>]}: a week and 10 rows are the defaults, so they stay out of the code.
    function highscoreCode(game, period, rows) {
        var count = parseInt(rows, 10);
        var customRows = count >= 1 && count <= 50 && count !== 10;
        var code = 'highscores_' + game;
        if (period !== 'week' || customRows) code += '_' + period;
        if (customRows) code += '_' + count;
        return '{' + code + '}';
    }

    // The box widget the site would show (it checks them in this order and the first one found wins).
    function boxCodeIn(editor, boxes) {
        var html = editor.getContent();
        for (var i = 0; i < boxes.length; i++) {
            if (html.indexOf(boxes[i].code) !== -1) return boxes[i].code;
        }
        return null;
    }

    // Whether the editor holds anything besides `code` (text, images, tables...).
    function hasOtherContent(editor, code) {
        var text = editor.getContent({ format: 'text' });
        if (code) text = text.split(code).join('');
        return text.replace(/ /g, ' ').trim() !== '' || /<(img|iframe|video|object|embed|table)\b/i.test(editor.getContent());
    }

    function insertCode(editor, boxes, code, isBox) {
        if (!isBox) {
            editor.focus();
            editor.insertContent(code);
            return;
        }
        // A box widget replaces the whole box, so it replaces the text instead of joining it.
        var replace = function () {
            editor.setContent(code);
            editor.focus();
        };
        if (!hasOtherContent(editor, boxCodeIn(editor, boxes))) {
            replace();
            return;
        }
        editor.windowManager.confirm(code + ' replaces the whole box: the site shows only that widget, not the text in the editor. Replace the text with ' + code + '?', function (ok) {
            if (ok) replace();
        });
    }

    function table(headers, rows) {
        var node = el('table', 'habbo-codes-table');
        var head = el('tr');
        headers.forEach(function (h) { head.appendChild(el('th', null, h)); });
        node.appendChild(el('thead')).appendChild(head);
        var body = node.appendChild(el('tbody'));
        rows.forEach(function (cells) {
            var row = body.appendChild(el('tr'));
            cells.forEach(function (cell) {
                var td = row.appendChild(el('td'));
                if (typeof cell === 'string') td.textContent = cell;
                else td.appendChild(cell);
            });
        });
        return node;
    }

    function open(editor, codes, withBoxes) {
        var dialog;
        var overview = null;
        var boxes = withBoxes ? codes.boxes : [];

        var codeButton = function (code, isBox) {
            var button = el('button', 'habbo-code', code);
            button.type = 'button';
            button.title = 'Insert ' + code;
            button.addEventListener('click', function () {
                dialog.close();
                insertCode(editor, boxes, code, isBox);
            });
            return button;
        };

        var fill = function (root) {
            root.appendChild(el('p', 'habbo-codes-intro', 'Click a code to insert it at the cursor.'));
            if (withBoxes) {
                var current = boxCodeIn(editor, boxes);
                root.appendChild(el('h3', null, 'Box widgets'));
                root.appendChild(el('p', 'habbo-codes-intro', 'The box shows only the widget; its title and theme stay, other text in the box is not shown.' + (current ? ' This box has ' + current + '.' : '')));
                root.appendChild(table(['Code', 'Widget', 'Shows'], codes.boxes.map(function (box) {
                    return [codeButton(box.code, true), box.name, box.description];
                })));
            }
            root.appendChild(el('h3', null, 'Highscore tables'));
            root.appendChild(el('p', 'habbo-codes-intro', 'Anywhere in the text, as many as you like.'));
            root.appendChild(table(['List'].concat(codes.games.map(function (g) { return g.name; })), codes.highscores.map(function (list) {
                return [list.name].concat(list.codes.map(function (code) { return codeButton(code, false); }));
            })));
            root.appendChild(el('p', 'habbo-codes-intro', 'Another period or row count: pick it below, then Insert highscore table.'));
        };

        var initial = { game: codes.games[0].id, period: 'week', rows: '10' };
        initial.code = highscoreCode(initial.game, initial.period, initial.rows);

        dialog = editor.windowManager.open({
            title: 'Codes',
            size: 'medium',
            body: {
                type: 'panel',
                items: [
                    { type: 'htmlpanel', html: '<div class="habbo-codes"></div>', onInit: function (node) { overview = node.querySelector('.habbo-codes') || node; fill(overview); } },
                    {
                        type: 'grid', columns: 4, items: [
                            { type: 'selectbox', name: 'game', label: 'Game', items: codes.games.map(function (g) { return { value: g.id, text: g.name }; }) },
                            { type: 'selectbox', name: 'period', label: 'Period', items: PERIODS },
                            { type: 'input', name: 'rows', label: 'Rows (1-50)', inputMode: 'numeric' },
                            { type: 'input', name: 'code', label: 'Code' }
                        ]
                    }
                ]
            },
            initialData: initial,
            buttons: [
                { type: 'cancel', text: 'Close' },
                { type: 'submit', name: 'insert', text: 'Insert highscore table', buttonType: 'primary' }
            ],
            onChange: function (api, details) {
                if (details.name === 'code') return;
                var data = api.getData();
                api.setData({ code: highscoreCode(data.game, data.period, data.rows) });
            },
            onSubmit: function (api) {
                var code = api.getData().code.trim();
                api.close();
                if (code) {
                    editor.focus();
                    editor.insertContent(code);
                }
            }
        });

        // TinyMCE focuses the first field (Game, at the bottom), which scrolls the overview out of view.
        setTimeout(function () {
            var body = overview && overview.closest('.tox-dialog__body-content');
            if (body) body.scrollTop = 0;
        }, 0);
    }

    // A warning in the editor while the box mixes a box widget with other content. Closed by hand, it stays closed
    // until the box is fixed and mixed again.
    function watchBoxCodes(editor, boxes) {
        var notification = null;
        var shownFor = null;
        var dismissed = false;
        var timer = null;

        var check = function () {
            var code = boxCodeIn(editor, boxes);
            var mixed = code && hasOtherContent(editor, code) ? code : null;
            if (notification && editor.notificationManager.getNotifications().indexOf(notification) === -1) {
                notification = null;
                dismissed = true;
            }
            if (!mixed) dismissed = false;
            if (notification && mixed !== shownFor) {
                notification.close();
                notification = null;
            }
            if (mixed && !notification && !dismissed) {
                notification = editor.notificationManager.open({
                    type: 'warning',
                    text: 'This box contains ' + mixed + ', so the site shows only that widget: the other text in the editor is not shown.'
                });
                shownFor = mixed;
            }
        };

        editor.on('init', check);
        editor.on('input change SetContent Undo Redo', function () {
            clearTimeout(timer);
            timer = setTimeout(check, 300);
        });
    }

    tinymce.PluginManager.add('habbocodes', function (editor) {
        editor.options.register('habbo_box_codes', { processor: 'boolean', default: false });
        var codes = catalog();
        if (!codes) return;
        var withBoxes = editor.options.get('habbo_box_codes');

        editor.ui.registry.addButton('habbocodes', {
            text: 'Codes',
            icon: 'template',
            tooltip: withBoxes ? 'Insert a box widget or highscore table code' : 'Insert a highscore table code',
            onAction: function () { open(editor, codes, withBoxes); }
        });
        if (withBoxes) watchBoxCodes(editor, codes.boxes);

        return { getMetadata: function () { return { name: 'Habbo content codes' }; } };
    });
})();

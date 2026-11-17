// Shared TinyMCE setup for housekeeping (news, pages, campaign news action).
// Requires ~/housekeeping/js/tinymce/tinymce.min.js (TinyMCE 8, self-hosted under the GPL).
//
// Codes: when the page has a codes overview for the editor (Areas/Housekeeping/Views/Shared/_ContentCodes.cshtml,
// matched by the textarea id), a click on a code there inserts it, the toolbar gets a "Codes" menu with the same
// codes, and the overview warns when a box widget code would hide the rest of the text.
// Furni and Habbo images: pages that load editor-images.js before this file get the Furni and Habbo buttons
// (the habbofurni and habboavatar plugins).
(function () {
    // TinyMCE's default toolbar (for the plugins loaded below), so adding the Codes menu keeps the usual buttons.
    var defaultToolbar = 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | outdent indent';

    function panelFor(selector) {
        var id = selector.charAt(0) === '#' ? selector.slice(1) : selector;
        var panels = document.querySelectorAll('.content-codes');
        for (var i = 0; i < panels.length; i++) {
            if (panels[i].getAttribute('data-editor') === id) return panels[i];
        }
        return null;
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

    // Box widget codes in the order the site checks them (the first one found wins).
    function boxCodes(panel) {
        var buttons = panel.querySelectorAll('.content-code[data-box]');
        return Array.prototype.map.call(buttons, function (button) { return button.getAttribute('data-code'); });
    }

    function boxCodeIn(editor, panel) {
        var html = editor.getContent();
        return boxCodes(panel).filter(function (code) { return html.indexOf(code) !== -1; })[0] || null;
    }

    // Whether the editor holds anything besides `code` (text, images, tables...).
    function hasOtherContent(editor, code) {
        var text = editor.getContent({ format: 'text' });
        if (code) text = text.split(code).join('');
        return text.replace(/ /g, ' ').trim() !== '' || /<(img|iframe|video|object|embed|table)\b/i.test(editor.getContent());
    }

    function insertCode(editor, panel, code, isBox) {
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
        if (!hasOtherContent(editor, boxCodeIn(editor, panel))) {
            replace();
            return;
        }
        editor.windowManager.confirm(code + ' replaces the whole box: the site shows only that widget, not the text in the editor. Replace the text with ' + code + '?', function (ok) {
            if (ok) replace();
        });
    }

    function openHighscoreDialog(editor, games) {
        var initial = { game: games[0].Id, period: 'week', rows: '10' };
        initial.code = highscoreCode(initial.game, initial.period, initial.rows);
        editor.windowManager.open({
            title: 'Highscore table',
            body: {
                type: 'panel',
                items: [
                    { type: 'selectbox', name: 'game', label: 'Game', items: games.map(function (game) { return { value: game.Id, text: game.Name }; }) },
                    {
                        type: 'selectbox', name: 'period', label: 'Period', items: [
                            { value: 'week', text: 'This week' },
                            { value: 'month', text: 'This month' },
                            { value: 'all', text: 'All time (the players\' running totals)' }
                        ]
                    },
                    { type: 'input', name: 'rows', label: 'Rows (1-50)', inputMode: 'numeric' },
                    { type: 'input', name: 'code', label: 'Code' }
                ]
            },
            initialData: initial,
            buttons: [
                { type: 'cancel', text: 'Cancel' },
                { type: 'submit', text: 'Insert', buttonType: 'primary' }
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
    }

    function gamesOf(panel) {
        var custom = panel.querySelector('.content-codes-custom');
        return custom ? JSON.parse(custom.getAttribute('data-games')) : [];
    }

    // The toolbar menu: one submenu per group of the overview ("Box widgets", then one per game).
    function codesMenu(editor, panel) {
        var groups = [];
        var itemsByGroup = {};
        panel.querySelectorAll('.content-code').forEach(function (button) {
            var group = button.getAttribute('data-group');
            if (!itemsByGroup[group]) {
                itemsByGroup[group] = [];
                groups.push(group);
            }
            itemsByGroup[group].push({
                type: 'menuitem',
                text: button.getAttribute('data-label') + ': ' + button.getAttribute('data-code'),
                onAction: function () { insertCode(editor, panel, button.getAttribute('data-code'), button.hasAttribute('data-box')); }
            });
        });
        var menu = groups.map(function (group) {
            return { type: 'nestedmenuitem', text: group, getSubmenuItems: function () { return itemsByGroup[group]; } };
        });
        var games = gamesOf(panel);
        if (games.length) {
            menu.push({ type: 'separator' });
            menu.push({ type: 'menuitem', text: 'Other highscore table…', onAction: function () { openHighscoreDialog(editor, games); } });
        }
        return menu;
    }

    function wirePanel(editor, panel) {
        panel.addEventListener('click', function (event) {
            var button = event.target.closest('.content-code, .content-codes-custom');
            if (!button) return;
            event.preventDefault();
            if (button.classList.contains('content-codes-custom')) {
                openHighscoreDialog(editor, gamesOf(panel));
            } else {
                insertCode(editor, panel, button.getAttribute('data-code'), button.hasAttribute('data-box'));
            }
        });

        var warning = panel.querySelector('.content-codes-warning');
        if (!warning) return;
        var update = function () {
            var code = boxCodeIn(editor, panel);
            warning.hidden = !code || !hasOtherContent(editor, code);
            if (!warning.hidden) {
                warning.textContent = 'This box contains ' + code + ', so the site shows only that widget: the other text in the editor is not shown.';
            }
        };
        editor.on('init input change keyup SetContent Undo Redo', update);
    }

    window.initHabboEditor = function (selector) {
        var panel = panelFor(selector);
        var images = ['habbofurni', 'habboavatar'].filter(function (name) { return !!tinymce.PluginManager.get(name); });
        var toolbar = defaultToolbar + ' | ' + images.concat(panel ? ['habbocodes'] : []).join(' ');
        return tinymce.init({
            selector: selector,
            license_key: 'gpl',
            promotion: false,
            relative_urls: false,
            convert_urls: false,
            remove_script_host: false,
            // TinyMCE 6+ dropped forced_root_block: '' (the old setting). A <div> root block has no default
            // margins, and linebreak mode makes Enter insert <br> like the old force_br_newlines did, so
            // articles render the same on the site.
            forced_root_block: 'div',
            newline_behavior: 'linebreak',
            // TinyMCE 5's print, paste and imagetools plugins no longer exist (paste is core now).
            plugins: 'advlist autolink lists link image charmap preview anchor searchreplace visualblocks code fullscreen insertdatetime media table wordcount' + (images.length ? ' ' + images.join(' ') : ''),
            toolbar: toolbar,
            // Wrap instead of hiding buttons behind "...", so Furni, Habbo and Codes stay visible in narrow editors.
            toolbar_mode: 'wrap',
            setup: function (editor) {
                if (!panel) return;
                editor.ui.registry.addMenuButton('habbocodes', {
                    text: 'Codes',
                    tooltip: 'Insert a box widget or highscore table code',
                    fetch: function (callback) { callback(codesMenu(editor, panel)); }
                });
                wirePanel(editor, panel);
            }
        });
    };
})();

// Admin tools > Room categories (Areas/Housekeeping/Views/RoomCategories/Index): the navigator's guest room categories
// as two sortable lists, the best rated lists (always first) and the room categories. window.roomCategoriesData is
// RoomCategoriesController.PageView; Save posts the whole arrangement to /housekeeping/roomcategories/save. Deleting a
// category that has rooms asks where they go; like every other change it waits for Save.
(function ($) {
    'use strict';

    var data = window.roomCategoriesData;
    var LISTS = {
        daily: { name: 'Dagens bedst bedømte rum', about: 'votes of the last 24 hours' },
        weekly: { name: 'Ugens bedst bedømte rum', about: 'votes of the last 7 days' },
        alltime: { name: 'Bedst bedømte rum nogensinde', about: 'all votes' }
    };
    var dirty = false;
    var deleted = [];   // [{ id, moveTo }]
    var newCount = 0;

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function changed() {
        dirty = true;
        $('#rc-savebar').prop('hidden', false);
        refreshAddList();
    }

    function roomsText(rooms, now) {
        return rooms + (rooms === 1 ? ' room' : ' rooms') + (now ? ' · ' + now + ' in them now' : '');
    }

    // ---- Rows ----

    function eye(hidden) {
        return el('button', { type: 'button', 'class': 'rc-eye', title: 'Show or hide it in the navigator' }, hidden ? 'Hidden' : 'Shown');
    }

    function listRow(c, key) {
        var li = el('li', { 'class': 'rc-row' + (c.hidden ? ' is-hidden' : ''), 'data-key': key, 'data-ranking': c.ranking });
        li.appendChild(el('span', { 'class': 'rc-handle', title: 'Drag to move' }, '⠿'));
        li.appendChild(el('span', { 'class': 'rc-name' }, c.name));
        li.appendChild(el('span', { 'class': 'rc-tag' }, LISTS[c.ranking] ? LISTS[c.ranking].about : c.ranking));
        if (key.indexOf('new') === 0) li.appendChild(el('span', { 'class': 'rc-tag rc-new' }, 'new'));
        li.appendChild(el('span', { 'class': 'rc-spacer' }));
        li.appendChild(eye(c.hidden));
        li.appendChild(el('button', { type: 'button', 'class': 'rc-small', 'data-rename': '' }, 'Rename'));
        li.appendChild(el('button', { type: 'button', 'class': 'rc-small', 'data-delete': '' }, 'Delete'));
        return li;
    }

    function categoryRow(c, key) {
        var li = el('li', { 'class': 'rc-row' + (c.hidden ? ' is-hidden' : ''), 'data-key': key, 'data-rooms': c.rooms || 0, 'data-now': c.visitorsNow || 0 });
        li.appendChild(el('span', { 'class': 'rc-handle', title: 'Drag to move' }, '⠿'));
        li.appendChild(el('span', { 'class': 'rc-name' }, c.name));
        li.appendChild(el('span', { 'class': 'rc-meta' }, roomsText(c.rooms || 0, c.visitorsNow || 0)));
        if (key.indexOf('new') === 0) li.appendChild(el('span', { 'class': 'rc-tag rc-new' }, 'new'));
        if (c.fuse) li.appendChild(el('span', { 'class': 'rc-tag', title: 'Only players with this right see the category' }, 'seen by ' + c.fuse));
        li.appendChild(el('span', { 'class': 'rc-spacer' }));
        var by = el('label', { title: 'Who may pick this category for their room' }, 'Rooms by ');
        var select = el('select', { 'class': 'rc-by' });
        select.appendChild(el('option', { value: 'everyone' }, 'everyone'));
        select.appendChild(el('option', { value: 'staff' }, 'staff only'));
        select.value = c.everyone === false ? 'staff' : 'everyone';
        by.appendChild(select);
        li.appendChild(by);
        var trading = el('label', { title: 'Players may trade in the rooms of this category' });
        var box = el('input', { type: 'checkbox', 'class': 'rc-trading' });
        box.checked = !!c.trading;
        trading.appendChild(box);
        trading.appendChild(document.createTextNode(' Trading'));
        li.appendChild(trading);
        li.appendChild(eye(c.hidden));
        li.appendChild(el('button', { type: 'button', 'class': 'rc-small', 'data-rename': '' }, 'Rename'));
        li.appendChild(el('button', { type: 'button', 'class': 'rc-small', 'data-delete': '' }, 'Delete'));
        return li;
    }

    // ---- Page ----

    function render() {
        var page = document.getElementById('rc-page');
        var box = el('section', { 'class': 'rc-box', id: 'rc-root' });
        var header = el('header');
        header.appendChild(el('span', { 'class': 'rc-name' }, data.root.name));
        header.appendChild(el('small', {}, 'the Hotelværelser tab'));
        header.appendChild(el('span', { 'class': 'rc-spacer' }));
        header.appendChild(el('button', { type: 'button', 'class': 'rc-small', 'data-rename-root': '' }, 'Rename'));
        box.appendChild(header);

        var listsTitle = el('div', { 'class': 'rc-group-title' }, 'Best rated lists ');
        listsTitle.appendChild(el('span', { 'class': 'rc-hint' }, 'always first: the rooms with the best score from players\' votes'));
        listsTitle.appendChild(el('span', { 'class': 'rc-spacer' }));
        var add = el('select', { id: 'rc-add-list', 'class': 'rc-small' });
        listsTitle.appendChild(add);
        box.appendChild(listsTitle);
        var lists = el('ul', { 'class': 'rc-list', id: 'rc-lists', 'data-empty': 'No best rated lists.' });
        data.lists.forEach(function (c) { lists.appendChild(listRow(c, String(c.id))); });
        box.appendChild(lists);

        var catsTitle = el('div', { 'class': 'rc-group-title' }, 'Room categories ');
        catsTitle.appendChild(el('span', { 'class': 'rc-hint' }, 'players pick one for their room in its settings'));
        catsTitle.appendChild(el('span', { 'class': 'rc-spacer' }));
        catsTitle.appendChild(el('button', { type: 'button', 'class': 'rc-small', id: 'rc-new' }, '+ New category'));
        box.appendChild(catsTitle);
        var cats = el('ul', { 'class': 'rc-list', id: 'rc-categories', 'data-empty': 'No room categories.' });
        data.categories.forEach(function (c) { cats.appendChild(categoryRow(c, String(c.id))); });
        box.appendChild(cats);
        page.appendChild(box);

        if (data.outside.length) {
            var outside = el('section', { 'class': 'rc-box rc-outside' });
            var oh = el('header');
            oh.appendChild(el('span', { 'class': 'rc-name' }, 'Not in the navigator'));
            outside.appendChild(oh);
            data.outside.forEach(function (c) {
                var p = el('p');
                p.appendChild(el('strong', {}, c.name));
                p.appendChild(document.createTextNode(' · ' + roomsText(c.rooms, c.visitorsNow) + '. '));
                p.appendChild(el('span', { 'class': 'rc-hint' }, c.id === 2
                    ? 'New rooms start here until their owner picks a category; players find them by searching or through their owner. Rooms of a deleted category can be moved here.'
                    : 'The navigator doesn\'t open it.'));
                outside.appendChild(p);
            });
            page.appendChild(outside);
        }

        refreshAddList();
        $('#rc-lists, #rc-categories').sortable({
            handle: '.rc-handle', items: '> .rc-row', placeholder: 'rc-placeholder', tolerance: 'pointer', axis: 'y', update: changed
        });
    }

    function presentLists() {
        return $('#rc-lists > .rc-row').map(function () { return this.dataset.ranking; }).get();
    }

    function refreshAddList() {
        var add = document.getElementById('rc-add-list');
        add.innerHTML = '';
        var missing = Object.keys(LISTS).filter(function (k) { return presentLists().indexOf(k) < 0; });
        add.appendChild(el('option', { value: '' }, missing.length ? '+ Add a best rated list…' : 'All best rated lists are here'));
        missing.forEach(function (k) { add.appendChild(el('option', { value: k }, LISTS[k].name + ' (' + LISTS[k].about + ')')); });
        add.disabled = !missing.length;
    }

    // ---- Actions ----

    function rename(nameEl) {
        var input = el('input', { type: 'text', maxlength: '100', 'class': 'rc-rename-input' });
        input.value = nameEl.text();
        nameEl.replaceWith(input);
        input.focus();
        input.select();
        var done = function (keep) {
            if (!input.parentNode) return;
            var value = input.value.trim();
            var span = el('span', { 'class': 'rc-name' }, keep && value ? value : nameEl.text());
            $(input).replaceWith(span);
            if (keep && value && value !== nameEl.text()) changed();
        };
        $(input).on('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); done(true); }
            if (e.key === 'Escape') { e.preventDefault(); done(false); }
        }).on('blur', function () { done(true); });
    }

    function targets(exceptKey) {
        var list = [];
        $('#rc-categories > .rc-row').not('.is-deleted').each(function () {
            var key = this.dataset.key;
            if (key === exceptKey || key.indexOf('new') === 0) return;
            list.push({ id: +key, name: $(this).find('.rc-name').text() });
        });
        data.outside.forEach(function (c) { list.push({ id: c.id, name: c.name + ' (not in the navigator)' }); });
        return list;
    }

    function deleteRow(row) {
        var key = row[0].dataset.key;
        var rooms = +(row[0].dataset.rooms || 0);
        var finish = function (moveTo) {
            if (key.indexOf('new') === 0) {
                row.remove();
            } else {
                deleted.push({ id: +key, moveTo: moveTo || 0 });
                row.addClass('is-deleted');
                if (moveTo && rooms) {
                    var target = $('#rc-categories > .rc-row[data-key="' + moveTo + '"]');
                    if (target.length) {
                        target[0].dataset.rooms = +(target[0].dataset.rooms) + rooms;
                        target.find('.rc-meta').text(roomsText(+target[0].dataset.rooms, +target[0].dataset.now) + ' (after saving)');
                    }
                }
            }
            changed();
        };
        if (!rooms) { finish(0); return; }
        var options = {};
        targets(key).forEach(function (t) { options[t.id] = t.name; });
        Swal.fire({
            title: 'Delete ' + row.find('.rc-name').text() + '?',
            text: 'Its ' + rooms + (rooms === 1 ? ' room moves' : ' rooms move') + ' to the category you pick. The deletion happens when you save.',
            input: 'select',
            inputOptions: options,
            inputValue: '2',
            showCancelButton: true,
            confirmButtonText: 'Delete'
        }).then(function (answer) {
            if (!answer.value) return;
            finish(+answer.value);
        });
    }

    function payload() {
        var rows = function (selector, isList) {
            return $(selector + ' > .rc-row').not('.is-deleted').map(function () {
                var item = { key: this.dataset.key, name: $(this).find('.rc-name').text(), hidden: this.classList.contains('is-hidden') };
                if (isList) item.ranking = this.dataset.ranking;
                else {
                    item.trading = $(this).find('.rc-trading').prop('checked');
                    item.everyone = $(this).find('.rc-by').val() === 'everyone';
                }
                return item;
            }).get();
        };
        return {
            busiestFirst: $('#rc-busiest').prop('checked'),
            rootName: $('#rc-root > header .rc-name').text(),
            lists: rows('#rc-lists', true),
            categories: rows('#rc-categories', false),
            deleted: deleted
        };
    }

    function save() {
        $('.rc-rename-input').trigger('blur');
        $('#rc-save').prop('disabled', true);
        $('#rc-error').text('');
        fetch('/housekeeping/roomcategories/save', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload())
        }).then(function (response) {
            return response.text().then(function (text) {
                if (!response.ok) throw new Error(text || response.statusText);
                dirty = false;
                location.reload();
            });
        }).catch(function (error) {
            $('#rc-error').text(error.message);
            $('#rc-save').prop('disabled', false);
        });
    }

    $(function () {
        render();
        $('#rc-busiest').on('change', changed);
        $(document).on('click', '.rc-eye', function () {
            var row = $(this).closest('.rc-row').toggleClass('is-hidden');
            this.textContent = row.hasClass('is-hidden') ? 'Hidden' : 'Shown';
            changed();
        });
        $(document).on('change', '.rc-by, .rc-trading', changed);
        $(document).on('click', '[data-rename]', function () { rename($(this).closest('.rc-row').find('.rc-name')); });
        $(document).on('dblclick', '.rc-row .rc-name', function () { rename($(this)); });
        $(document).on('click', '[data-rename-root]', function () { rename($('#rc-root > header .rc-name')); });
        $(document).on('click', '[data-delete]', function () { deleteRow($(this).closest('.rc-row')); });
        $(document).on('click', '#rc-new', function () {
            newCount++;
            var row = $(categoryRow({ name: 'New category', rooms: 0, everyone: true }, 'new' + newCount));
            $('#rc-categories').append(row).sortable('refresh');
            changed();
            rename(row.find('.rc-name'));
            row[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
        $(document).on('change', '#rc-add-list', function () {
            var kind = this.value;
            if (!kind) return;
            newCount++;
            $('#rc-lists').append(listRow({ name: LISTS[kind].name, ranking: kind }, 'new' + newCount)).sortable('refresh');
            changed();
        });
        $('#rc-save').on('click', save);
        $('#rc-discard').on('click', function () { dirty = false; location.reload(); });
        window.addEventListener('beforeunload', function (e) {
            if (!dirty) return;
            e.preventDefault();
            e.returnValue = '';
        });
    });
})(jQuery);

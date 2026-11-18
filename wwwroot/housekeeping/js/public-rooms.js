// Admin tools > Public rooms (Areas/Housekeeping/Views/PublicRooms/Index): the navigator's public rooms as a tree that
// can be rearranged. window.publicRoomsData is PublicRoomsController.PageView; Save posts the whole arrangement to
// /housekeeping/publicrooms/save (categories in order, rooms in their categories' order, hidden flags, deleted categories).
// Drag and drop is jQuery UI sortable: room lists connect to each other, "Not in the navigator" only gives rooms away
// (hiding is the Shown/Hidden switch), and the categories under the root sort among themselves.
(function ($) {
    'use strict';

    var data = window.publicRoomsData;
    var ROOT = String(data.root.id);
    var dirty = false;
    var deleted = [];
    var newCount = 0;

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function changed() {
        dirty = true;
        $('#pr-savebar').prop('hidden', false);
        refreshCounts();
        refreshMoveLists();
    }

    // ---- Rooms ----

    function roomRow(room) {
        var li = el('li', { 'class': 'pr-room' + (room.hidden ? ' is-hidden' : ''), 'data-id': room.id });
        li.appendChild(el('span', { 'class': 'pr-handle', title: 'Drag to move' }, '⠿'));
        var name = el('span', { 'class': 'pr-name' });
        name.appendChild(el('a', { href: '/housekeeping/rooms/view/' + room.id, target: '_blank', title: 'Open the room in Room admin' }, room.name));
        li.appendChild(name);
        li.appendChild(el('small', {}, String(room.id)));
        if (room.outside) {
            li.appendChild(el('span', { 'class': 'pr-reason', title: room.outside }, room.outside));
        } else {
            li.appendChild(el('span', { 'class': 'pr-parts', title: partsText(room) }, partsText(room)));
        }
        li.appendChild(el('span', { 'class': 'pr-visitors', title: 'Visitors now / at most' + (room.parts.length ? ', with its parts' : '') }, room.visitorsNow + '/' + room.visitorsMax));
        var move = el('select', { 'class': 'pr-move', title: 'Move to another category' });
        li.appendChild(move);
        var eye = el('button', { type: 'button', 'class': 'pr-eye', title: 'Show or hide it in the navigator' }, room.hidden ? 'Hidden' : 'Shown');
        li.appendChild(eye);
        if (room.outside) eye.style.display = 'none';
        return li;
    }

    function partsText(room) {
        if (!room.parts.length) return '';
        return 'with ' + room.parts.map(function (p) { return p.name; }).join(', ');
    }

    // ---- Categories ----

    function roomList(key, rooms, empty) {
        var ul = el('ul', { 'class': 'pr-rooms', 'data-key': key, 'data-empty': empty });
        rooms.forEach(function (room) { ul.appendChild(roomRow(room)); });
        return ul;
    }

    function categoryBox(category, isNew) {
        var key = isNew ? category.key : String(category.id);
        var section = el('section', { 'class': 'pr-cat' + (isNew ? ' is-new' : '') + (category.hidden ? ' is-hidden' : ''), 'data-key': key });
        var header = el('header');
        header.appendChild(el('span', { 'class': 'pr-handle', title: 'Drag to move the category' }, '⠿'));
        header.appendChild(el('span', { 'class': 'pr-cat-name' }, category.name));
        if (category.clubOnly) header.appendChild(el('span', { 'class': 'pr-tag', title: 'Odin lets only Habbo Club members into the rooms of this category' }, 'Habbo Club members only'));
        if (category.fuse) header.appendChild(el('span', { 'class': 'pr-tag', title: 'Only players with this right see the category' }, category.fuse));
        header.appendChild(el('small', { 'class': 'pr-count' }));
        header.appendChild(el('span', { 'class': 'pr-spacer' }));
        header.appendChild(el('button', { type: 'button', 'class': 'pr-eye pr-cat-eye', title: 'Show or hide the whole category in the navigator' }, category.hidden ? 'Hidden' : 'Shown'));
        header.appendChild(el('button', { type: 'button', 'class': 'pr-small-button', 'data-rename': '' }, 'Rename'));
        if (!category.clubOnly) header.appendChild(el('button', { type: 'button', 'class': 'pr-small-button', 'data-delete': '' }, 'Delete'));
        section.appendChild(header);
        section.appendChild(roomList(key, category.rooms || [], 'No rooms: drag rooms here.'));
        return section;
    }

    function render() {
        var tree = document.getElementById('pr-tree');
        var root = el('section', { 'class': 'pr-cat pr-root', 'data-key': ROOT });
        var header = el('header');
        header.appendChild(el('span', { 'class': 'pr-cat-name' }, data.root.name));
        header.appendChild(el('small', { 'class': 'pr-count' }));
        header.appendChild(el('span', { 'class': 'pr-spacer' }));
        header.appendChild(el('button', { type: 'button', 'class': 'pr-small-button', 'data-rename': '' }, 'Rename'));
        root.appendChild(header);
        root.appendChild(roomList(ROOT, data.root.rooms, 'No rooms: drag rooms here.'));
        var title = el('div', { 'class': 'pr-subs-title' }, 'Categories in ' + data.root.name + ' (listed after its rooms)');
        title.appendChild(el('button', { type: 'button', 'class': 'pr-small-button', id: 'pr-new' }, '+ New category'));
        root.appendChild(title);
        var subs = el('div', { 'class': 'pr-subs', id: 'pr-subs' });
        data.categories.forEach(function (c) { subs.appendChild(categoryBox(c, false)); });
        root.appendChild(subs);
        tree.appendChild(root);

        var outside = el('section', { 'class': 'pr-cat pr-outside' });
        var oh = el('header');
        oh.appendChild(el('span', { 'class': 'pr-cat-name' }, 'Not in the navigator'));
        oh.appendChild(el('small', { 'class': 'pr-count' }));
        outside.appendChild(oh);
        outside.appendChild(el('span', { 'class': 'pr-hint' }, 'Their category is missing or isn\'t in ' + data.root.name + ', so players can only get there by a walkway, a link or following a friend. Drag one into a category to list it.'));
        var list = roomList('outside', data.outside, 'Every public room is in the navigator.');
        list.classList.add('pr-rooms-outside');
        outside.appendChild(list);
        document.getElementById('pr-outside-column').appendChild(outside);

        refreshCounts();
        refreshMoveLists();
        makeSortable();
    }

    function refreshCounts() {
        $('.pr-cat').each(function () {
            var rooms = $(this).children('.pr-rooms').children('.pr-room');
            var hidden = rooms.filter('.is-hidden').length;
            var text = rooms.length + (rooms.length === 1 ? ' room' : ' rooms') + (hidden ? ', ' + hidden + ' hidden' : '');
            $(this).children('header').find('.pr-count').text(text);
            // A category can only be deleted when it's empty.
            var del = $(this).children('header').find('[data-delete]');
            del.prop('disabled', rooms.length > 0).attr('title', rooms.length > 0 ? 'Move its rooms out first' : 'Delete the category');
        });
    }

    function categories() {
        var list = [{ key: ROOT, name: $('.pr-root > header .pr-cat-name').text() }];
        $('#pr-subs > .pr-cat').each(function () {
            list.push({ key: this.dataset.key, name: $(this).children('header').find('.pr-cat-name').text(), hidden: this.classList.contains('is-hidden') });
        });
        return list;
    }

    function refreshMoveLists() {
        var cats = categories();
        $('.pr-room').each(function () {
            var select = this.querySelector('.pr-move');
            var current = $(this).closest('.pr-rooms').data('key');
            select.innerHTML = '';
            select.appendChild(el('option', { value: '' }, 'Move to…'));
            cats.forEach(function (c, i) {
                if (String(c.key) === String(current)) return;
                select.appendChild(el('option', { value: c.key }, (i === 0 ? '' : '\u00a0\u00a0') + c.name));
            });
        });
    }

    function makeSortable() {
        var targets = '.pr-rooms:not(.pr-rooms-outside)';
        var options = {
            connectWith: targets,
            handle: '.pr-handle',
            items: '> .pr-room',
            placeholder: 'pr-placeholder',
            tolerance: 'pointer',
            receive: function (e, ui) { joined(ui.item); },
            update: changed
        };
        $(targets).sortable(options);
        $('.pr-rooms-outside').sortable($.extend({}, options, { receive: null }));
        $('#pr-subs').sortable({
            handle: '> header > .pr-handle',
            items: '> .pr-cat',
            placeholder: 'pr-cat-placeholder',
            tolerance: 'pointer',
            update: changed
        });
    }

    // A room taken out of "Not in the navigator" is listed from now on.
    function joined(item) {
        if (!item.find('.pr-reason').length) return;
        item.find('.pr-reason').replaceWith(el('span', { 'class': 'pr-parts' }, ''));
        item.find('.pr-eye').show();
        item.addClass('is-moved');
    }

    // ---- Actions ----

    function rename(section) {
        var name = section.children('header').find('.pr-cat-name');
        if (!name.length) return;
        var input = el('input', { type: 'text', maxlength: '100', value: name.text() });
        name.replaceWith(input);
        input.focus();
        input.select();
        var done = function (keep) {
            if (!input.parentNode) return;
            var value = input.value.trim();
            var span = el('span', { 'class': 'pr-cat-name' }, keep && value ? value : name.text());
            $(input).replaceWith(span);
            if (keep && value && value !== name.text()) changed();
        };
        $(input).on('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); done(true); }
            if (e.key === 'Escape') { e.preventDefault(); done(false); }
        }).on('blur', function () { done(true); });
    }

    function addCategory() {
        newCount++;
        var section = $(categoryBox({ key: 'new' + newCount, name: 'New category', rooms: [] }, true));
        $('#pr-subs').append(section);
        section.children('.pr-rooms').sortable($('.pr-rooms').not('.pr-rooms-outside').first().sortable('option'));
        $('#pr-subs').sortable('refresh');
        changed();
        rename(section);
        section[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function deleteCategory(section) {
        if (section.children('.pr-rooms').children().length) return;
        var key = section[0].dataset.key;
        if (key.indexOf('new') !== 0) deleted.push(+key);
        section.remove();
        changed();
    }

    function payload() {
        var cats = categories();
        var rooms = [];
        $('.pr-rooms').not('.pr-rooms-outside').each(function () {
            var key = String($(this).data('key'));
            $(this).children('.pr-room').each(function () {
                rooms.push({ id: +this.dataset.id, category: key, hidden: this.classList.contains('is-hidden') });
            });
        });
        return {
            busiestFirst: $('#pr-busiest').prop('checked'),
            rootName: cats[0].name,
            categories: cats.slice(1).map(function (c) { return { key: String(c.key), name: c.name, hidden: !!c.hidden }; }),
            deleted: deleted,
            rooms: rooms
        };
    }

    function save() {
        $('.pr-cat input').trigger('blur');
        $('#pr-save').prop('disabled', true);
        $('#pr-error').text('');
        fetch('/housekeeping/publicrooms/save', {
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
            $('#pr-error').text(error.message);
            $('#pr-save').prop('disabled', false);
        });
    }

    $(function () {
        render();
        $('#pr-busiest').on('change', changed);
        $(document).on('click', '.pr-cat-eye', function () {
            var section = $(this).closest('.pr-cat').toggleClass('is-hidden');
            this.textContent = section.hasClass('is-hidden') ? 'Hidden' : 'Shown';
            changed();
        });
        $(document).on('click', '.pr-eye:not(.pr-cat-eye)', function () {
            var row = $(this).closest('.pr-room').toggleClass('is-hidden');
            this.textContent = row.hasClass('is-hidden') ? 'Hidden' : 'Shown';
            changed();
        });
        $(document).on('change', '.pr-move', function () {
            if (!this.value) return;
            var row = $(this).closest('.pr-room');
            var target = $('.pr-rooms[data-key="' + this.value + '"]');
            joined(row);
            row.addClass('is-moved').appendTo(target);
            changed();
            row[0].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        });
        $(document).on('click', '[data-rename]', function () { rename($(this).closest('.pr-cat')); });
        $(document).on('dblclick', '.pr-cat > header .pr-cat-name', function () { rename($(this).closest('.pr-cat')); });
        $(document).on('click', '[data-delete]', function () { deleteCategory($(this).closest('.pr-cat')); });
        $(document).on('click', '#pr-new', addCategory);
        $('#pr-save').on('click', save);
        $('#pr-discard').on('click', function () { dirty = false; location.reload(); });
        window.addEventListener('beforeunload', function (e) {
            if (!dirty) return;
            e.preventDefault();
            e.returnValue = '';
        });
    });
})(jQuery);

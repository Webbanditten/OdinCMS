// Housekeeping > Reward management: the month view, the calendar builder and the furni pools.
// Pages: Areas/Housekeeping/Views/Rewards (Index, Builder, Pools); JSON actions in RewardsController.
// Needs jQuery, select2, jquery.datetimepicker and SweetAlert2 (the housekeeping layout loads the last).
window.Rewards = (function ($) {
    'use strict';

    var BASE = '/housekeeping/rewards/';
    var WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    var MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

    // ---- Helpers ----

    function api(action, body) {
        var options = { credentials: 'same-origin', headers: {} };
        if (body !== undefined) {
            options.method = 'POST';
            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(body);
        }
        return fetch(BASE + action, options).then(function (response) {
            if (!response.ok) {
                return response.text().then(function (text) {
                    throw new Error(text && text.length < 300 ? text : 'The server answered ' + response.status + '.');
                });
            }
            return (response.headers.get('content-type') || '').indexOf('json') >= 0 ? response.json() : null;
        });
    }

    function fail(error) {
        Swal.fire({ icon: 'error', title: 'That didn\'t work', text: error.message });
    }

    function notify(text) {
        Swal.fire({ toast: true, position: 'top-end', timer: 2500, showConfirmButton: false, icon: 'success', title: text });
    }

    function confirmBox(title, text, confirmText) {
        return Swal.fire({ title: title, text: text, icon: 'warning', showCancelButton: true, confirmButtonText: confirmText || 'Yes', cancelButtonText: 'Cancel' })
            .then(function (result) { return !!result.value; });
    }

    function askText(title, value, placeholder) {
        return Swal.fire({ title: title, input: 'text', inputValue: value || '', inputPlaceholder: placeholder || '', showCancelButton: true, cancelButtonText: 'Cancel' })
            .then(function (result) { return result.value ? String(result.value).trim() : null; });
    }

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function button(text, className, onClick) {
        var b = el('button', className, text);
        b.type = 'button';
        if (onClick) b.addEventListener('click', onClick);
        return b;
    }

    function pad(n) { return (n < 10 ? '0' : '') + n; }
    function ymd(date) { return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()); }
    function parseYmd(text) { var p = text.split('-'); return new Date(+p[0], +p[1] - 1, +p[2]); }
    function addDays(date, days) { var d = new Date(date); d.setDate(d.getDate() + days); return d; }
    function dayTitle(date) { return WEEKDAYS[(date.getDay() + 6) % 7] + ' ' + date.getDate() + ' ' + MONTHS[date.getMonth()].slice(0, 3); }
    // "2026-12-01 00:00" (picker) <-> "2026-12-01T00:00:00" (JSON)
    function pickerToIso(text) { return text.trim().replace(' ', 'T') + (text.trim().length === 16 ? ':00' : ''); }
    function isoToPicker(text) { return text.slice(0, 16).replace('T', ' '); }

    function datePicker(input, withTime) {
        $(input).datetimepicker(withTime ? { format: 'Y-m-d H:i', step: 60 } : { timepicker: false, format: 'Y-m-d' });
    }

    // ---- Furni ----

    function furniUrl() {
        var meta = document.querySelector('meta[name="habbo-furni-url"]');
        return (meta && meta.content) || '/habbo-imaging/furni';
    }

    // image: what the imager draws it with when that isn't the sprite (posters: "poster27").
    function furniIcon(furni) {
        var parts = (furni.image || furni.sprite || '').split('*');
        return furniUrl() + '?sprite=' + encodeURIComponent(parts[0]) + (parts[1] ? '&color=' + parts[1] : '') + '&icon=true';
    }

    function furniName(furni) {
        return furni.name && furni.name !== 'na' ? furni.name : furni.sprite;
    }

    function furniChip(furni, withName) {
        var chip = el('span', 'reward-furni');
        chip.title = furniName(furni) + ' (' + furni.sprite + ')';
        var img = el('img');
        img.src = furniIcon(furni);
        img.alt = '';
        chip.appendChild(img);
        if (withName) chip.appendChild(el('span', 'reward-furni-name', furniName(furni)));
        return chip;
    }

    function badgeChip(code) {
        var chip = el('span', 'reward-furni reward-badge');
        chip.title = 'Badge ' + code;
        var img = el('img');
        img.src = (window.rewardsBadgeUrl || '') + '/' + encodeURIComponent(code) + '.gif';
        img.alt = code;
        chip.appendChild(img);
        return chip;
    }

    // A select2 furni search (multiple). The furni behind each selected option is kept for the icons.
    function furniSearch(select, parent) {
        var $select = $(select);
        $select.data('furni', {});
        $select.select2({
            width: '100%',
            multiple: true,
            placeholder: 'Search furni by name or sprite',
            minimumInputLength: 2,
            dropdownParent: parent ? $(parent) : $(document.body),
            ajax: {
                url: '/api/hotel/furni/search',
                dataType: 'json',
                delay: 250,
                data: function (params) { return { query: params.term }; },
                processResults: function (data) {
                    return {
                        results: data.slice(0, 80).map(function (f) {
                            return { id: f.id, text: furniName(f) + ' (' + f.sprite + ')', furni: { id: f.id, name: f.name, sprite: f.sprite, image: f.imageSprite } };
                        })
                    };
                }
            },
            templateResult: function (result) {
                if (!result.furni) return result.text;
                return $('<span class="reward-option">').append($('<img>').attr('src', furniIcon(result.furni)), $('<span>').text(result.text));
            }
        });
        $select.on('select2:select', function (e) {
            if (e.params.data.furni) $select.data('furni')[e.params.data.id] = e.params.data.furni;
        });
        return $select;
    }

    function setFurni($select, list) {
        var map = $select.data('furni');
        $select.empty();
        list.forEach(function (f) {
            map[f.id] = f;
            $select.append(new Option(furniName(f) + ' (' + f.sprite + ')', f.id, true, true));
        });
        $select.trigger('change');
    }

    function getFurni($select) {
        var map = $select.data('furni');
        return ($select.val() || []).map(function (id) { return map[id] || { id: +id, name: '', sprite: '' }; });
    }

    // ---- Modal ----

    function modal(title) {
        var backdrop = el('div', 'reward-modal-backdrop');
        var box = el('div', 'reward-modal');
        box.appendChild(el('h3', null, title));
        var body = el('div', 'reward-modal-body');
        var footer = el('div', 'reward-modal-footer');
        box.appendChild(body);
        box.appendChild(footer);
        backdrop.appendChild(box);
        document.body.appendChild(backdrop);
        return {
            box: box,
            body: body,
            footer: footer,
            close: function () {
                $(box).find('select').each(function () { if ($(this).data('select2')) $(this).select2('destroy'); });
                backdrop.remove();
            }
        };
    }

    function field(label, input, hint) {
        var wrap = el('label', 'reward-field');
        wrap.appendChild(el('span', 'reward-field-label', label));
        wrap.appendChild(input);
        if (hint) wrap.appendChild(el('span', 'reward-field-hint', hint));
        return wrap;
    }

    function input(type, value, attrs) {
        var i = el(type === 'textarea' ? 'textarea' : 'input');
        if (type !== 'textarea') i.type = type;
        if (value !== undefined && value !== null) i.value = value;
        Object.keys(attrs || {}).forEach(function (k) { i.setAttribute(k, attrs[k]); });
        return i;
    }

    // Furni, badge (with preview) and, optionally, the rest of a reward; used by the month view and the builder.
    function rewardFields(body, values, withDetails) {
        var fields = {};
        if (withDetails) {
            var times = el('div', 'rewards-row');
            fields.from = input('text', values.from, { autocomplete: 'off' });
            fields.to = input('text', values.to, { autocomplete: 'off' });
            times.appendChild(field('Available from', fields.from));
            times.appendChild(field('Until', fields.to));
            body.appendChild(times);
            datePicker(fields.from, true);
            datePicker(fields.to, true);
        }
        var select = el('select');
        select.multiple = true;
        body.appendChild(field('Furni in the present', select));
        fields.furni = furniSearch(select, body);
        setFurni(fields.furni, values.items || []);

        var badgeRow = el('div', 'rewards-row');
        fields.badge = input('text', values.badge || '', { maxlength: '3', placeholder: 'e.g. XM6', autocomplete: 'off' });
        var preview = el('span', 'reward-badge-preview');
        var showBadge = function () {
            preview.textContent = '';
            var code = fields.badge.value.trim();
            if (code.length === 3) preview.appendChild(badgeChip(code.toUpperCase()));
        };
        fields.badge.addEventListener('input', showBadge);
        showBadge();
        var badgeField = field('Badge (optional)', fields.badge);
        badgeField.appendChild(preview);
        badgeRow.appendChild(badgeField);
        body.appendChild(badgeRow);

        if (withDetails) {
            fields.note = input('textarea', values.note || '', { rows: '3' });
            body.appendChild(field('Gift note (the card in the present)', fields.note));
            fields.label = input('text', values.label || '', { list: 'reward-labels', placeholder: 'Calendar name, optional', autocomplete: 'off' });
            body.appendChild(field('Label', fields.label));
        }
        return fields;
    }

    // ---- Month view ----

    function initMonth() {
        var params = new URLSearchParams(location.search);
        var now = new Date();
        var shown = params.get('month') ? parseYmd(params.get('month') + '-01') : new Date(now.getFullYear(), now.getMonth(), 1);
        var state = { rewards: [], labels: [], lastNote: '' };
        var grid = document.getElementById('reward-month');

        var labelList = el('datalist');
        labelList.id = 'reward-labels';
        document.body.appendChild(labelList);

        function load() {
            document.getElementById('month-title').textContent = MONTHS[shown.getMonth()] + ' ' + shown.getFullYear();
            api('Month?year=' + shown.getFullYear() + '&month=' + (shown.getMonth() + 1)).then(function (data) {
                state.rewards = data.rewards;
                state.labels = data.labels;
                var latest = data.rewards.slice().reverse().find(function (r) { return r.description; });
                if (latest && !state.lastNote) state.lastNote = latest.description;
                render();
                renderLabels();
            }).catch(fail);
        }

        function render() {
            grid.textContent = '';
            WEEKDAYS.forEach(function (d) { grid.appendChild(el('div', 'reward-month-head', d)); });
            var first = addDays(shown, -((shown.getDay() + 6) % 7));
            var today = ymd(new Date());
            for (var i = 0; i < 42; i++) grid.appendChild(dayCell(addDays(first, i), today));
        }

        function dayCell(date, today) {
            var key = ymd(date);
            var cell = el('div', 'reward-day' + (date.getMonth() !== shown.getMonth() ? ' is-other' : '') + (key === today ? ' is-today' : ''));
            cell.appendChild(el('div', 'reward-day-number', String(date.getDate())));
            var rewards = state.rewards.filter(function (r) { return r.availableFrom.slice(0, 10) === key; });
            rewards.forEach(function (reward) { cell.appendChild(entry(reward)); });
            // A click on the day itself (not on a reward) adds one.
            cell.addEventListener('click', function (e) {
                if (e.target === cell || e.target.classList.contains('reward-day-number')) openEditor(null, date);
            });
            cell.title = rewards.length ? '' : 'Click to add a reward on ' + dayTitle(date);
            return cell;
        }

        function entry(reward) {
            var box = el('div', 'reward-entry');
            var icons = el('div', 'reward-entry-icons');
            reward.items.forEach(function (f) { icons.appendChild(furniChip(f)); });
            if (reward.badge) icons.appendChild(badgeChip(reward.badge));
            box.appendChild(icons);
            var meta = el('div', 'reward-entry-meta');
            if (reward.label) meta.appendChild(el('span', 'reward-entry-label', reward.label));
            if (reward.redeemed) meta.appendChild(el('span', 'reward-entry-redeemed', '✓ ' + reward.redeemed));
            var hours = reward.availableFrom.slice(11, 16) !== '00:00' || reward.availableTo.slice(11, 16) !== '00:00';
            if (hours) meta.appendChild(el('span', 'reward-entry-time', reward.availableFrom.slice(11, 16) + '–' + reward.availableTo.slice(11, 16)));
            box.appendChild(meta);
            box.title = reward.items.map(furniName).join(', ') + (reward.badge ? (reward.items.length ? ' + ' : '') + 'badge ' + reward.badge : '');
            box.addEventListener('click', function () { openEditor(reward); });
            return box;
        }

        function renderLabels() {
            labelList.textContent = '';
            var select = document.getElementById('delete-label');
            select.textContent = '';
            select.appendChild(new Option('Pick a calendar', ''));
            state.labels.forEach(function (label) {
                labelList.appendChild(new Option(label, label));
                select.appendChild(new Option(label, label));
            });
        }

        function openEditor(reward, date) {
            var m = modal(reward ? 'Reward on ' + dayTitle(new Date(reward.availableFrom)) : 'New reward on ' + dayTitle(date));
            var start = reward ? reward.availableFrom : ymd(date) + 'T00:00:00';
            var end = reward ? reward.availableTo : ymd(addDays(date, 1)) + 'T00:00:00';
            if (reward && reward.redeemed) {
                m.body.appendChild(el('p', 'reward-modal-info', reward.redeemed + ' player' + (reward.redeemed === 1 ? '' : 's') + ' got this reward. Changes only reach players who haven\'t got it yet.'));
            }
            var fields = rewardFields(m.body, {
                from: isoToPicker(start), to: isoToPicker(end),
                items: reward ? reward.items : [], badge: reward ? reward.badge : '',
                note: reward ? reward.description : state.lastNote, label: reward ? reward.label : ''
            }, true);

            m.footer.appendChild(button('Save', 'rewards-primary', function () {
                var body = {
                    id: reward ? reward.id : 0,
                    availableFrom: pickerToIso(fields.from.value),
                    availableTo: pickerToIso(fields.to.value),
                    items: getFurni(fields.furni).map(function (f) { return f.id; }),
                    badge: fields.badge.value.trim(),
                    note: fields.note.value,
                    label: fields.label.value.trim()
                };
                api('Save', body).then(function () {
                    state.lastNote = body.note;
                    m.close();
                    notify('Saved');
                    load();
                }).catch(fail);
            }));
            if (reward) {
                m.footer.appendChild(button('Delete', 'rewards-danger', function () {
                    var text = reward.redeemed
                        ? reward.redeemed + ' players got this reward. Deleting it also deletes the record of who got it, and the website stops showing it.'
                        : 'Nobody has got this reward yet.';
                    confirmBox('Delete this reward?', text, 'Delete').then(function (ok) {
                        if (!ok) return;
                        api('Delete', { id: reward.id }).then(function () { m.close(); notify('Deleted'); load(); }).catch(fail);
                    });
                }));
            }
            m.footer.appendChild(button('Cancel', null, m.close));
        }

        document.getElementById('month-prev').addEventListener('click', function () { shown = new Date(shown.getFullYear(), shown.getMonth() - 1, 1); load(); });
        document.getElementById('month-next').addEventListener('click', function () { shown = new Date(shown.getFullYear(), shown.getMonth() + 1, 1); load(); });
        document.getElementById('month-today').addEventListener('click', function () { shown = new Date(now.getFullYear(), now.getMonth(), 1); load(); });

        ['copy-from', 'copy-to', 'copy-target'].forEach(function (id) { datePicker(document.getElementById(id), false); });
        document.getElementById('copy-run').addEventListener('click', function () {
            var from = document.getElementById('copy-from').value, to = document.getElementById('copy-to').value, target = document.getElementById('copy-target').value;
            var label = document.getElementById('copy-label').value.trim();
            if (!from || !to || !target) { fail(new Error('Pick the period and where its first day should land.')); return; }
            confirmBox('Copy ' + from + ' to ' + to + '?', 'The first day lands on ' + target + (label ? ', labelled "' + label + '"' : '') + '.', 'Copy').then(function (ok) {
                if (!ok) return;
                api('CopyPeriod', { from: from, to: to, target: target, label: label }).then(function (result) {
                    notify(result.copied + ' rewards copied');
                    shown = new Date(parseYmd(target).getFullYear(), parseYmd(target).getMonth(), 1);
                    load();
                }).catch(fail);
            });
        });
        document.getElementById('delete-run').addEventListener('click', function () {
            var label = document.getElementById('delete-label').value;
            if (!label) return;
            confirmBox('Delete the upcoming rewards of "' + label + '"?', 'Rewards that have already started stay.', 'Delete').then(function (ok) {
                if (!ok) return;
                api('DeleteUpcoming', { label: label }).then(function (result) { notify(result.deleted + ' rewards deleted'); load(); }).catch(fail);
            });
        });

        load();
    }

    // ---- Calendar builder ----

    var PICKS = [['random', 'Random'], ['in_order', 'In order'], ['cycle', 'Alternate (in order, over and over)']];
    var WHENS = [['every_day', 'Every day'], ['weekdays', 'On weekdays'], ['days', 'On these days'], ['every_n', 'Every Nth day'], ['last_day', 'On the last day']];

    function initBuilder() {
        var state = { templates: [], pools: [], slots: [], days: [], existing: {} };
        var $ = window.jQuery;
        var slotsBox = document.getElementById('plan-slots');
        var start = document.getElementById('plan-start');
        datePicker(start, false);

        function select(options, value) {
            var s = el('select');
            options.forEach(function (o) { s.appendChild(new Option(o[1], o[0], false, o[0] === value)); });
            return s;
        }

        function renderSlots() {
            $(slotsBox).find('select').each(function () { if ($(this).data('select2')) $(this).select2('destroy'); });
            slotsBox.textContent = '';
            state.slots.forEach(function (slot, index) {
                var row = el('div', 'reward-slot');
                var sources = state.pools.map(function (p) { return ['pool:' + p.id, 'Pool: ' + p.name + ' (' + p.items.length + ')']; });
                sources.push(['items', 'These furni']);
                var source = select(sources, slot.poolId ? 'pool:' + slot.poolId : 'items');
                var pick = select(PICKS, slot.pick);
                var when = select(WHENS, slot.when);
                var count = input('number', slot.count || 1, { min: '1', max: '10' });

                var line = el('div', 'rewards-row');
                line.appendChild(field('Furni from', source));
                line.appendChild(field('Pick', pick));
                line.appendChild(field('When', when));
                var whenExtra = el('div', 'reward-slot-when');
                line.appendChild(whenExtra);
                line.appendChild(field('Per day', count));
                line.appendChild(button('Remove', 'rewards-danger reward-slot-remove', function () { state.slots.splice(index, 1); renderSlots(); }));
                row.appendChild(line);

                var itemsBox = el('div', 'reward-slot-items');
                row.appendChild(itemsBox);

                function renderWhen() {
                    whenExtra.textContent = '';
                    if (slot.when === 'weekdays') {
                        WEEKDAYS.forEach(function (d, i) {
                            var box = input('checkbox');
                            box.checked = (slot.weekdays || []).indexOf(i + 1) >= 0;
                            box.addEventListener('change', function () {
                                var set = (slot.weekdays || []).filter(function (x) { return x !== i + 1; });
                                if (box.checked) set.push(i + 1);
                                slot.weekdays = set.sort();
                            });
                            var l = el('label', 'rewards-check');
                            l.appendChild(box);
                            l.appendChild(document.createTextNode(d));
                            whenExtra.appendChild(l);
                        });
                    } else if (slot.when === 'days') {
                        var days = input('text', (slot.days || []).join(', '), { placeholder: '1, 7, 14, 21', size: '14' });
                        days.addEventListener('input', function () {
                            slot.days = days.value.split(/[ ,;]+/).map(Number).filter(function (n) { return n > 0; });
                        });
                        whenExtra.appendChild(field('Day numbers', days));
                    } else if (slot.when === 'every_n') {
                        var n = input('number', slot.everyN || 7, { min: '1', max: '31' });
                        n.addEventListener('input', function () { slot.everyN = +n.value || 1; });
                        whenExtra.appendChild(field('Every', n, 'days, from day 1'));
                    }
                }

                function renderItems() {
                    itemsBox.textContent = '';
                    if (slot.poolId) {
                        var pool = state.pools.find(function (p) { return p.id === slot.poolId; });
                        if (pool) {
                            var preview = el('div', 'reward-slot-preview');
                            pool.items.slice(0, 30).forEach(function (f) { preview.appendChild(furniChip(f)); });
                            if (pool.items.length > 30) preview.appendChild(el('span', 'rewards-hint', '+' + (pool.items.length - 30) + ' more'));
                            itemsBox.appendChild(preview);
                        }
                        return;
                    }
                    var s = el('select');
                    s.multiple = true;
                    itemsBox.appendChild(s);
                    var $s = furniSearch(s);
                    setFurni($s, slot.itemFurni || []);
                    $s.on('change', function () {
                        slot.itemFurni = getFurni($s);
                        slot.items = slot.itemFurni.map(function (f) { return f.id; });
                    });
                }

                source.addEventListener('change', function () {
                    slot.poolId = source.value.indexOf('pool:') === 0 ? +source.value.slice(5) : null;
                    renderItems();
                });
                pick.addEventListener('change', function () { slot.pick = pick.value; });
                when.addEventListener('change', function () { slot.when = when.value; renderWhen(); });
                count.addEventListener('input', function () { slot.count = Math.max(1, Math.min(10, +count.value || 1)); });
                renderWhen();
                renderItems();
                slotsBox.appendChild(row);
            });
        }

        function plan() {
            return {
                days: Math.max(1, Math.min(62, +document.getElementById('plan-days').value || 1)),
                note: document.getElementById('plan-note').value,
                label: document.getElementById('plan-label').value.trim(),
                slots: state.slots.map(function (s) {
                    return {
                        poolId: s.poolId || null, items: s.poolId ? [] : (s.items || []), pick: s.pick, when: s.when,
                        weekdays: s.weekdays || [], days: s.days || [], everyN: s.everyN || 7, count: s.count || 1
                    };
                })
            };
        }

        function loadPlan(p) {
            document.getElementById('plan-days').value = p.days;
            document.getElementById('plan-note').value = p.note || '';
            var label = p.label || '';
            var year = start.value ? start.value.slice(0, 4) : '';
            if (label && year && !/\d{4}/.test(label)) label += ' ' + year;
            document.getElementById('plan-label').value = label;
            var ids = [];
            p.slots.forEach(function (s) { ids = ids.concat(s.items || []); });
            state.slots = p.slots.map(function (s) {
                return { poolId: s.poolId || null, items: s.items || [], pick: s.pick, when: s.when, weekdays: s.weekdays || [], days: s.days || [], everyN: s.everyN || 7, count: s.count || 1 };
            });
            // Fixed furni need names and sprites for their icons: the pools know most; ask the furni search for the rest.
            var known = {};
            state.pools.forEach(function (pool) { pool.items.forEach(function (f) { known[f.id] = f; }); });
            Promise.all(ids.filter(function (id) { return !known[id]; }).map(function (id) {
                return fetch('/api/hotel/furni?id=' + id).then(function (r) { return r.ok ? r.json() : null; }).then(function (f) { if (f) known[id] = { id: f.id, name: f.name, sprite: f.sprite, image: f.imageSprite }; });
            })).then(function () {
                state.slots.forEach(function (s) { s.itemFurni = (s.items || []).map(function (id) { return known[id]; }).filter(Boolean); });
                renderSlots();
            });
        }

        function renderTemplates(selectedId) {
            var s = document.getElementById('template-select');
            s.textContent = '';
            s.appendChild(new Option('Start from scratch', ''));
            state.templates.forEach(function (t) { s.appendChild(new Option(t.name, t.id, false, t.id === selectedId)); });
            document.getElementById('template-delete').disabled = !s.value;
        }

        document.getElementById('template-select').addEventListener('change', function () {
            var t = state.templates.find(function (x) { return String(x.id) === this.value; }, this);
            document.getElementById('template-delete').disabled = !t;
            if (t) loadPlan(t.plan);
        });
        document.getElementById('template-delete').addEventListener('click', function () {
            var id = +document.getElementById('template-select').value;
            var t = state.templates.find(function (x) { return x.id === id; });
            if (!t) return;
            confirmBox('Delete the template "' + t.name + '"?', 'Calendars made from it stay.', 'Delete').then(function (ok) {
                if (!ok) return;
                api('DeleteTemplate', { id: id }).then(function () { return api('Templates'); }).then(function (list) { state.templates = list; renderTemplates(); notify('Template deleted'); }).catch(fail);
            });
        });
        document.getElementById('slot-add').addEventListener('click', function () {
            state.slots.push({ poolId: state.pools.length ? state.pools[0].id : null, items: [], pick: 'random', when: 'every_day', weekdays: [], days: [], everyN: 7, count: 1 });
            renderSlots();
        });

        function renderDays() {
            var grid = document.getElementById('plan-days-grid');
            grid.textContent = '';
            var clashes = 0;
            state.days.forEach(function (day, index) {
                var date = parseYmd(day.date);
                var card = el('div', 'reward-plan-day');
                var head = el('div', 'reward-plan-day-head');
                head.appendChild(el('strong', null, dayTitle(date)));
                head.appendChild(el('span', 'rewards-hint', 'Day ' + (index + 1)));
                card.appendChild(head);
                var icons = el('div', 'reward-entry-icons');
                day.items.forEach(function (f) { icons.appendChild(furniChip(f, true)); });
                if (day.badge) icons.appendChild(badgeChip(day.badge));
                if (!day.items.length && !day.badge) icons.appendChild(el('span', 'rewards-hint', 'Nothing (skipped)'));
                card.appendChild(icons);
                var existing = state.existing[day.date];
                if (existing) {
                    clashes++;
                    card.classList.add('has-existing');
                    card.appendChild(el('div', 'reward-plan-existing', 'Already has: ' + existing.join(', ')));
                }
                card.addEventListener('click', function () { editDay(index); });
                grid.appendChild(card);
            });
            var count = state.days.filter(function (d) { return d.items.length || d.badge; }).length;
            document.getElementById('plan-result-title').textContent = 'Preview: ' + dayTitle(parseYmd(state.days[0].date)) + ' – ' + dayTitle(parseYmd(state.days[state.days.length - 1].date)) +
                (clashes ? ' (' + clashes + ' day' + (clashes === 1 ? '' : 's') + ' already have rewards)' : '');
            var create = document.getElementById('plan-create');
            create.textContent = 'Create ' + count + ' reward' + (count === 1 ? '' : 's');
            create.disabled = count === 0;
            document.getElementById('plan-result').hidden = false;
        }

        function editDay(index) {
            var day = state.days[index];
            var m = modal(dayTitle(parseYmd(day.date)) + ' (day ' + (index + 1) + ')');
            var fields = rewardFields(m.body, { items: day.items, badge: day.badge }, false);
            m.footer.appendChild(button('Keep', 'rewards-primary', function () {
                day.items = getFurni(fields.furni);
                day.badge = fields.badge.value.trim().toUpperCase();
                m.close();
                renderDays();
            }));
            m.footer.appendChild(button('Cancel', null, m.close));
        }

        document.getElementById('plan-preview').addEventListener('click', function () {
            if (!start.value) { fail(new Error('Pick the first day.')); return; }
            if (!state.slots.length) { fail(new Error('Add at least one line.')); return; }
            api('Preview', { plan: plan(), start: start.value }).then(function (result) {
                state.days = result.days;
                state.existing = result.existing;
                renderDays();
                document.getElementById('plan-result').scrollIntoView({ behavior: 'smooth' });
            }).catch(fail);
        });

        document.getElementById('plan-create').addEventListener('click', function () {
            var p = plan();
            if (!p.note.trim()) { fail(new Error('Write the gift note first.')); return; }
            var clashes = Object.keys(state.existing).length;
            var text = (p.label ? 'Labelled "' + p.label + '". ' : 'Without a label. ') +
                (clashes ? clashes + ' of these days already have rewards; players would get both.' : '');
            confirmBox(document.getElementById('plan-create').textContent + '?', text, 'Create').then(function (ok) {
                if (!ok) return;
                api('CreateCalendar', {
                    note: p.note, label: p.label,
                    days: state.days.map(function (d) { return { date: d.date + 'T00:00:00', items: d.items.map(function (f) { return f.id; }), badge: d.badge || null }; })
                }).then(function (result) {
                    notify(result.created + ' rewards created');
                    setTimeout(function () { location.href = BASE + '?month=' + state.days[0].date.slice(0, 7); }, 900);
                }).catch(fail);
            });
        });

        document.getElementById('plan-save-template').addEventListener('click', function () {
            var current = state.templates.find(function (t) { return String(t.id) === document.getElementById('template-select').value; });
            askText('Save as template', current ? current.name : '', 'e.g. Julekalender (pingviner)').then(function (name) {
                if (!name) return;
                var p = plan();
                p.label = p.label.replace(/\s*\d{4}$/, ''); // the year goes on when the template is used
                api('SaveTemplate', { name: name, plan: p }).then(function (saved) {
                    return api('Templates').then(function (list) { state.templates = list; renderTemplates(saved.id); notify('Template saved'); });
                }).catch(fail);
            });
        });

        Promise.all([api('Templates'), api('PoolList')]).then(function (results) {
            state.templates = results[0];
            state.pools = results[1];
            renderTemplates();
            if (!state.slots.length) document.getElementById('slot-add').click();
        }).catch(fail);
    }

    // ---- Pools ----

    function initPools() {
        var state = { pools: [], selected: null };
        var addSelect = furniSearch(document.getElementById('pool-add'));

        function load(selectId) {
            return api('PoolList').then(function (pools) {
                state.pools = pools;
                if (selectId !== undefined) state.selected = selectId;
                renderList();
                renderDetail();
            }).catch(fail);
        }

        function renderList() {
            var list = document.getElementById('pool-list');
            list.textContent = '';
            state.pools.forEach(function (pool) {
                var li = el('li', pool.id === state.selected ? 'is-selected' : '');
                li.appendChild(el('span', 'reward-pool-name', pool.name));
                li.appendChild(el('span', 'rewards-hint', String(pool.items.length)));
                var toCheck = pool.items.filter(function (f) { return f.needsReview; }).length;
                if (toCheck) li.appendChild(el('span', 'reward-check-tag', toCheck + ' to check'));
                li.addEventListener('click', function () { state.selected = pool.id; renderList(); renderDetail(); });
                list.appendChild(li);
            });
        }

        function renderDetail() {
            var pool = state.pools.find(function (p) { return p.id === state.selected; });
            var detail = document.getElementById('pool-detail');
            detail.hidden = !pool;
            if (!pool) return;
            document.getElementById('pool-name').value = pool.name;
            document.getElementById('pool-description').value = pool.description || '';
            var onlyCheck = document.getElementById('pool-only-check').checked;
            var grid = document.getElementById('pool-items');
            grid.textContent = '';
            pool.items.forEach(function (f, index) {
                if (onlyCheck && !f.needsReview) return;
                var card = el('div', 'reward-pool-item' + (f.needsReview ? ' needs-review' : ''));
                card.appendChild(furniChip(f));
                var info = el('div', 'reward-pool-item-info');
                info.appendChild(el('strong', null, furniName(f)));
                info.appendChild(el('span', 'rewards-hint', f.sprite));
                if (f.needsReview) {
                    var tag = el('span', 'reward-check-tag', 'check');
                    tag.title = f.note || '';
                    info.appendChild(tag);
                    if (f.note) info.appendChild(el('span', 'rewards-hint', f.note));
                }
                card.appendChild(info);
                var actions = el('div', 'reward-pool-item-actions');
                if (f.needsReview) actions.appendChild(button('✓', 'rewards-primary', function () { act('ConfirmPoolItem', { poolId: pool.id, item: f.id }); }));
                if (index > 0) actions.appendChild(button('↑', null, function () { act('MovePoolItem', { poolId: pool.id, item: f.id, direction: -1 }); }));
                if (index < pool.items.length - 1) actions.appendChild(button('↓', null, function () { act('MovePoolItem', { poolId: pool.id, item: f.id, direction: 1 }); }));
                actions.appendChild(button('×', 'rewards-danger', function () { act('RemovePoolItem', { poolId: pool.id, item: f.id }); }));
                card.appendChild(actions);
                grid.appendChild(card);
            });
            if (!grid.children.length) grid.appendChild(el('p', 'rewards-hint', onlyCheck ? 'Nothing left to check.' : 'No furni yet: search above to add some.'));
        }

        function act(action, body) {
            api(action, body).then(function () { return load(); }).catch(fail);
        }

        document.getElementById('pool-only-check').addEventListener('change', renderDetail);
        document.getElementById('pool-save').addEventListener('click', function () {
            api('SavePool', { id: state.selected, name: document.getElementById('pool-name').value, description: document.getElementById('pool-description').value })
                .then(function () { notify('Saved'); return load(); }).catch(fail);
        });
        document.getElementById('pool-delete').addEventListener('click', function () {
            var pool = state.pools.find(function (p) { return p.id === state.selected; });
            confirmBox('Delete the pool "' + pool.name + '"?', 'Rewards already made from it stay. Templates that use it skip it.', 'Delete').then(function (ok) {
                if (!ok) return;
                api('DeletePool', { id: pool.id }).then(function () { notify('Pool deleted'); return load(null); }).catch(fail);
            });
        });
        document.getElementById('pool-new').addEventListener('click', function () {
            askText('New pool', '', 'e.g. Påske 2027').then(function (name) {
                if (!name) return;
                api('SavePool', { id: 0, name: name, description: '' }).then(function (saved) { return load(saved.id); }).catch(fail);
            });
        });
        document.getElementById('pool-add-run').addEventListener('click', function () {
            var ids = getFurni(addSelect).map(function (f) { return f.id; });
            if (!ids.length || !state.selected) return;
            api('AddPoolItems', { poolId: state.selected, items: ids }).then(function () {
                setFurni(addSelect, []);
                notify(ids.length + ' added');
                return load();
            }).catch(fail);
        });

        load(null).then(function () {
            if (state.pools.length && state.selected === null) {
                state.selected = state.pools[0].id;
                renderList();
                renderDetail();
            }
        });
    }

    return { initMonth: initMonth, initBuilder: initBuilder, initPools: initPools };
})(window.jQuery);

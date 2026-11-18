// Housekeeping > Ads (Areas/Housekeeping/Views/Ads/Index): create and edit ads for every ad place.
// Talks JSON to AdsController: Ad?id= (one ad), Save, Toggle, Delete; Preview?image=&format= draws a slanted billboard.
(function ($) {
    'use strict';

    var BASE = '/housekeeping/ads/';
    var editingId = 0;
    var frames = null;     // wwwroot/housekeeping/ads/frames/frames.json: format -> { room, point }
    var imageSize = null;  // natural size of the chosen picture

    function api(method, action, body) {
        return fetch(BASE + action, {
            method: method,
            credentials: 'same-origin',
            headers: body ? { 'Content-Type': 'application/json' } : {},
            body: body ? JSON.stringify(body) : undefined
        }).then(function (response) {
            return response.text().then(function (text) {
                if (!response.ok) throw new Error(text || response.statusText);
                return text ? JSON.parse(text) : null;
            });
        });
    }

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        if (text) node.textContent = text;
        return node;
    }

    function placement() { return $('#ad-placement').val(); }
    function option() { return $('#ad-placement option:selected'); }
    function isBillboard() { return placement() === 'room_billboard'; }

    function loadFrames() {
        if (frames) return Promise.resolve(frames);
        return fetch('/housekeeping/ads/frames/frames.json').then(function (r) { return r.json(); }).then(function (f) { frames = f; return f; });
    }

    // ---- Form state ----

    function syncPlacement() {
        $('.ad-billboard').toggle(isBillboard());
        $('#ad-click').closest('div').prev('label').addBack().toggle(placement() !== 'pool_screen');
        var o = option();
        $('#ad-placement-hint').text(o.data('size') + '. ' + o.data('where'));
        syncRooms();
        preview();
    }

    function syncRooms() {
        var fit = $('#ad-fit').val();
        $('#ad-rooms .ad-room').each(function () {
            var ok = fit === 'auto' || this.dataset.format === fit;
            this.classList.toggle('is-disabled', !ok);
            var box = this.querySelector('input');
            box.disabled = !ok;
            if (!ok) box.checked = false;
        });
    }

    function imageInfo() {
        var src = $('#ad-image').val().trim();
        imageSize = null;
        $('#ad-image-info').text('');
        if (!src) { preview(); return; }
        var probe = new Image();
        probe.onload = function () {
            imageSize = { w: probe.naturalWidth, h: probe.naturalHeight };
            var o = option(), text = imageSize.w + '×' + imageSize.h;
            if (!isBillboard()) {
                var w = +o.data('width'), h = +o.data('height');
                if (placement() === 'room_loading') {
                    if (imageSize.w > w || imageSize.h > h) text += ' — bigger than ' + w + '×' + h + ', the loading window cuts it off';
                } else if (imageSize.w !== w || imageSize.h !== h) {
                    text += ' — the place is ' + w + '×' + h + ', so it is ' + (placement().indexOf('website') === 0 ? 'stretched' : 'cut or padded');
                }
            }
            $('#ad-image-info').text(text);
            preview();
        };
        probe.onerror = function () { $('#ad-image-info').text('The picture could not be loaded.'); preview(); };
        probe.src = src;
    }

    // ---- Preview ----

    function preview() {
        var box = document.getElementById('ad-preview');
        var src = $('#ad-image').val().trim();
        box.innerHTML = '';
        if (!src) { box.appendChild(el('span', { 'class': 'ad-hint' }, 'Pick a picture.')); return; }
        if (isBillboard()) {
            var fit = $('#ad-fit').val();
            loadFrames().then(function (f) {
                box.innerHTML = '';
                Object.keys(f).filter(function (format) { return fit === 'auto' || fit === format; }).forEach(function (format) {
                    var frame = f[format];
                    var figure = el('figure');
                    var wrap = el('div', { 'class': 'ad-frame' });
                    var bg = el('img', { 'class': 'ad-frame-bg', src: '/housekeeping/ads/frames/' + format + '.png', alt: '' });
                    var ad = el('img', { alt: '' });
                    ad.onload = function () {
                        ad.style.left = (frame.point[0] - Math.floor(ad.naturalWidth / 2)) + 'px';
                        ad.style.top = (frame.point[1] - Math.floor(ad.naturalHeight / 2)) + 'px';
                        // The client draws it with matte ink: white around the picture's edge is see-through.
                        ad.style.mixBlendMode = 'multiply';
                    };
                    ad.src = fit === 'auto' ? BASE + 'Preview?image=' + encodeURIComponent(src) + '&format=' + format + '&t=' + Date.now() : src;
                    wrap.appendChild(bg);
                    wrap.appendChild(ad);
                    figure.appendChild(wrap);
                    figure.appendChild(el('figcaption', {}, format.replace('_', ', facing ') + ' (' + frame.room + ')'));
                    box.appendChild(figure);
                });
            });
            return;
        }
        var o = option(), w = +o.data('width'), h = +o.data('height');
        var slot = el('div', { 'class': 'ad-slot' });
        slot.style.width = w + 'px';
        slot.style.height = h + 'px';
        var img = el('img', { src: src, alt: '' });
        if (placement().indexOf('website') === 0) { img.style.width = w + 'px'; img.style.height = h + 'px'; }
        slot.appendChild(img);
        var figure = el('figure');
        figure.appendChild(slot);
        figure.appendChild(el('figcaption', {}, o.text() + ', ' + w + '×' + h));
        box.appendChild(figure);
    }

    // ---- Open, save ----

    function open(ad, rooms) {
        editingId = ad ? ad.id : 0;
        $('#ad-form-title').text(ad ? 'Edit ' + ad.name : 'New ad');
        $('#ad-name').val(ad ? ad.name : '');
        $('#ad-placement').val(ad ? ad.placement : 'room_billboard');
        $('#ad-image').val(ad ? ad.image : '');
        $('#ad-fit').val(ad && ad.placement === 'room_billboard' ? ad.fit : 'auto');
        $('#ad-click').val(ad ? ad.clickUrl || '' : '');
        $('#ad-starts').val(ad && ad.startsAt ? ad.startsAt.replace('T', ' ').substring(0, 16) : '');
        $('#ad-ends').val(ad && ad.endsAt ? ad.endsAt.replace('T', ' ').substring(0, 16) : '');
        $('#ad-weight').val(ad ? ad.weight : 1);
        $('#ad-enabled').prop('checked', ad ? ad.enabled : true);
        $('#ad-rooms input').each(function () { this.checked = !!rooms && rooms.indexOf(+this.value) >= 0; });
        $('#ad-error').text('');
        $('#ad-form').prop('hidden', false);
        syncPlacement();
        imageInfo();
        document.getElementById('ad-form').scrollIntoView({ behavior: 'smooth' });
    }

    function date(value) {
        value = (value || '').trim();
        return value ? value.replace(' ', 'T') + ':00' : null;
    }

    function save() {
        var body = {
            id: editingId,
            name: $('#ad-name').val(),
            placement: placement(),
            image: $('#ad-image').val(),
            fit: $('#ad-fit').val(),
            clickUrl: placement() === 'pool_screen' ? '' : $('#ad-click').val(),
            startsAt: date($('#ad-starts').val()),
            endsAt: date($('#ad-ends').val()),
            enabled: $('#ad-enabled').prop('checked'),
            weight: parseInt($('#ad-weight').val(), 10) || 1,
            rooms: isBillboard() ? $('#ad-rooms input:checked').map(function () { return +this.value; }).get() : []
        };
        $('#ad-save').prop('disabled', true);
        $('#ad-error').text('');
        api('POST', 'Save', body).then(function () { location.reload(); })
            .catch(function (error) { $('#ad-error').text(error.message); })
            .then(function () { $('#ad-save').prop('disabled', false); });
    }

    function remove(id, name) {
        Swal.fire({
            title: 'Delete ' + name + '?',
            text: 'Its views and clicks go with it. Turning it off keeps them.',
            icon: 'warning', showCancelButton: true, confirmButtonText: 'Delete'
        }).then(function (answer) {
            if (!answer.value && !answer.isConfirmed) return;
            api('POST', 'Delete', { id: id }).then(function () { location.reload(); })
                .catch(function (error) { Swal.fire('Ad', error.message, 'error'); });
        });
    }

    $(function () {
        $('.datetimepicker').datetimepicker({ format: 'Y-m-d H:i', step: 15 });
        $('#ad-dates-clear').on('click', function (e) { e.preventDefault(); $('#ad-starts, #ad-ends').val(''); });
        $('#ad-rooms-none').on('click', function (e) { e.preventDefault(); $('#ad-rooms input').prop('checked', false); });
        $('#ad-placement').on('change', function () { syncPlacement(); imageInfo(); });
        $('#ad-fit').on('change', function () { syncRooms(); preview(); });
        $('#ad-image').on('change', imageInfo);
        $('#ad-new').on('click', function () { open(null, null); });
        $('#ad-cancel').on('click', function () { $('#ad-form').prop('hidden', true); });
        $('#ad-save').on('click', save);
        $(document).on('click', '[data-edit]', function (e) {
            e.preventDefault();
            api('GET', 'Ad?id=' + this.dataset.edit).then(function (r) { open(r.ad, r.rooms); })
                .catch(function (error) { Swal.fire('Ad', error.message, 'error'); });
        });
        $(document).on('click', '[data-toggle]', function (e) {
            e.preventDefault();
            api('POST', 'Toggle', { id: +this.dataset.toggle, enabled: this.dataset.enabled === '1' }).then(function () { location.reload(); })
                .catch(function (error) { Swal.fire('Ad', error.message, 'error'); });
        });
        $(document).on('click', '[data-delete]', function (e) {
            e.preventDefault();
            remove(+this.dataset.delete, this.dataset.name);
        });
    });
})(jQuery);

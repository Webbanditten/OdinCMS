// Housekeeping > Vouchers (Areas/Housekeeping/Views/Vouchers/Index): create and edit vouchers with credits and furni.
// Talks JSON to VouchersController: Furni?q= (catalogue search), Voucher?code= (one voucher), Save, Delete.
(function ($) {
    'use strict';

    var BASE = '/housekeeping/vouchers/';
    var editing = null;   // code of the voucher being edited, null when creating
    var chosen = [];      // [{ saleCode, name, sprite, amount }]
    var searchTimer = null;

    function furniIcon(sprite) {
        var meta = document.querySelector('meta[name="habbo-furni-url"]');
        var base = (meta && meta.content) || '/habbo-imaging/furni';
        var parts = (sprite || '').split('*');
        return parts[0] ? base + '?sprite=' + encodeURIComponent(parts[0]) + (parts[1] ? '&color=' + parts[1] : '') + '&icon=true' : '';
    }

    function setIcon(img, sprite) {
        var url = furniIcon(sprite);
        if (!url) { img.style.visibility = 'hidden'; return; }
        img.onerror = function () { img.style.visibility = 'hidden'; };
        img.src = url;
    }

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

    // ---- Furni ----

    function renderChosen() {
        var list = $('#vch-chosen').empty();
        chosen.forEach(function (line, index) {
            var li = $('<li>');
            var img = $('<img class="vch-icon" alt="">')[0];
            setIcon(img, line.sprite);
            var amount = $('<input type="number" min="1" max="100">').val(line.amount).on('change input', function () {
                line.amount = Math.max(1, Math.min(100, parseInt(this.value, 10) || 1));
            });
            var remove = $('<a href="#">remove</a>').on('click', function (e) {
                e.preventDefault();
                chosen.splice(index, 1);
                renderChosen();
            });
            li.append(img, amount, $('<span>').text('× ' + line.name + ' '), $('<small class="vch-hint">').text(line.saleCode), remove);
            list.append(li);
        });
        $('#vch-furni-empty').toggle(chosen.length === 0);
    }

    function addFurni(furni) {
        var existing = chosen.filter(function (line) { return line.saleCode === furni.saleCode; })[0];
        if (existing) existing.amount = Math.min(100, existing.amount + 1);
        else chosen.push({ saleCode: furni.saleCode, name: furni.name, sprite: furni.sprite, amount: 1 });
        renderChosen();
    }

    function search() {
        var query = $('#vch-search').val().trim();
        var box = $('#vch-results');
        if (query.length < 2) { box.prop('hidden', true).empty(); return; }
        api('GET', 'Furni?q=' + encodeURIComponent(query)).then(function (results) {
            box.empty();
            if (!results.length) box.append($('<div class="vch-result is-disabled">').text('Nothing in the catalogue matches.'));
            results.forEach(function (furni) {
                var row = $('<div class="vch-result">');
                var img = $('<img class="vch-icon" alt="">')[0];
                setIcon(img, furni.sprite);
                var text = $('<div>').append($('<span>').text(furni.name));
                if (furni.isPackage) text.append($('<span class="vch-tag">').text('package'));
                if (furni.isHidden) text.append($('<span class="vch-tag is-hidden">').text('not for sale'));
                text.append($('<br>'), $('<small>').text(furni.saleCode + (furni.price ? ' · ' + furni.price + ' credits' : '')));
                if (furni.notAllowed) {
                    row.addClass('is-disabled').attr('title', furni.notAllowed);
                    text.append($('<br>'), $('<small>').text(furni.notAllowed));
                } else {
                    row.on('mousedown', function (e) { e.preventDefault(); addFurni(furni); });
                }
                row.append(img, text);
                box.append(row);
            });
            box.prop('hidden', false);
        }).catch(function () { box.prop('hidden', true); });
    }

    // ---- Form ----

    function resetForm() {
        editing = null;
        chosen = [];
        $('#vch-form-title').text('New voucher');
        $('#vch-code').val('').prop('disabled', false);
        $('#vch-code-hint').text('Letters, digits, - and _. Players type it in any case.');
        $('.vch-create-only').show();
        $('#vch-count').val(1);
        $('#vch-credits').val(0);
        $('#vch-use').val('single');
        $('#vch-expiry').val('');
        $('#vch-save').text('Create voucher');
        $('#vch-cancel').prop('hidden', true);
        $('#vch-error').text('');
        renderChosen();
    }

    function edit(code) {
        api('GET', 'Voucher?code=' + encodeURIComponent(code)).then(function (data) {
            var voucher = data.voucher;
            editing = voucher.code;
            chosen = voucher.furni.filter(function (f) { return !f.missing; }).map(function (f) {
                return { saleCode: f.saleCode, name: f.name, sprite: f.sprite, amount: f.amount };
            });
            $('#vch-form-title').text('Edit voucher ' + voucher.code);
            $('#vch-code').val(voucher.code).prop('disabled', true);
            $('#vch-code-hint').text('The code can\'t change.' + (data.history.length ? ' Redeemed ' + data.history.length + ' times.' : ''));
            $('.vch-create-only').hide();
            $('#vch-credits').val(voucher.credits);
            $('#vch-use').val(voucher.isSingleUse ? 'single' : 'multi');
            $('#vch-expiry').val(voucher.expiryDate ? voucher.expiryDate.replace('T', ' ').substring(0, 16) : '');
            $('#vch-save').text('Save voucher');
            $('#vch-cancel').prop('hidden', false);
            $('#vch-error').text('');
            $('#vch-created').prop('hidden', true);
            renderChosen();
            document.getElementById('vch-form').scrollIntoView({ behavior: 'smooth' });
        }).catch(function (error) { Swal.fire('Voucher', error.message || 'Not found', 'error'); });
    }

    function save() {
        var count = parseInt($('#vch-count').val(), 10) || 1;
        var body = {
            original: editing,
            voucher: {
                code: editing ? null : $('#vch-code').val().trim(),
                count: editing ? 1 : count,
                credits: parseInt($('#vch-credits').val(), 10) || 0,
                expiry: $('#vch-expiry').val().trim(),
                singleUse: $('#vch-use').val() === 'single',
                furni: chosen.map(function (line) { return { saleCode: line.saleCode, amount: line.amount }; })
            }
        };
        $('#vch-error').text('');
        $('#vch-save').prop('disabled', true);
        api('POST', 'Save', body).then(function (result) {
            if (editing) { location.reload(); return; }
            $('#vch-created-codes').val(result.codes.join('\n'));
            $('#vch-created').prop('hidden', false);
            Swal.fire('Voucher', result.codes.length === 1 ? 'Created ' + result.codes[0] + '.' : 'Created ' + result.codes.length + ' codes.', 'success')
                .then(function () { sessionStorage.setItem('vch-created', result.codes.join('\n')); location.reload(); });
        }).catch(function (error) {
            $('#vch-error').text(error.message);
        }).then(function () { $('#vch-save').prop('disabled', false); });
    }

    function remove(code) {
        Swal.fire({
            title: 'Delete ' + code + '?',
            text: 'Nobody can redeem it any more. Its redemptions stay in the history.',
            icon: 'warning', showCancelButton: true, confirmButtonText: 'Delete'
        }).then(function (answer) {
            if (!answer.value && !answer.isConfirmed) return;
            api('POST', 'Delete', { code: code }).then(function () { location.reload(); })
                .catch(function (error) { Swal.fire('Voucher', error.message, 'error'); });
        });
    }

    $(function () {
        $('.datetimepicker').datetimepicker({ format: 'Y-m-d H:i', step: 15 });
        $('#vch-expiry-clear').on('click', function (e) { e.preventDefault(); $('#vch-expiry').val(''); });
        $('#vch-search').on('input', function () { clearTimeout(searchTimer); searchTimer = setTimeout(search, 250); })
            .on('focus', search)
            .on('blur', function () { setTimeout(function () { $('#vch-results').prop('hidden', true); }, 150); });
        $('#vch-save').on('click', save);
        $('#vch-cancel').on('click', resetForm);
        $('#vch-copy').on('click', function () {
            var text = $('#vch-created-codes').val();
            if (navigator.clipboard) navigator.clipboard.writeText(text);
            else { $('#vch-created-codes')[0].select(); document.execCommand('copy'); }
        });
        $(document).on('click', '[data-edit]', function (e) { e.preventDefault(); edit($(this).data('edit')); });
        $(document).on('click', '[data-delete]', function (e) { e.preventDefault(); remove(String($(this).data('delete'))); });
        document.querySelectorAll('img.vch-icon[data-sprite]').forEach(function (img) { setIcon(img, img.getAttribute('data-sprite')); });

        // Codes made just before the reload stay on screen to copy.
        var created = null;
        try { created = sessionStorage.getItem('vch-created'); sessionStorage.removeItem('vch-created'); } catch (x) { }
        if (created) { $('#vch-created-codes').val(created); $('#vch-created').prop('hidden', false); }
        renderChosen();
    });
})(jQuery);

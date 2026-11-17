// Housekeeping catalogue editor: the in-game catalogue (catalogue-view.js) on the left, editable as in the game, and the
// page, its products and the furni master data on the right. Saves go to the database; "Publish to the hotel" makes Odin
// reload the catalogue (reload_catalogue), after which players see the changes the next time they open a page.
var CatalogueEditor = (function () {
    'use strict';

    var LAYOUTS = {
        ctlg_frontpage1: 'Front page (1 teaser)', ctlg_frontpage2: 'Front page (2 teasers)',
        ctlg_layout1: 'Products: big preview', ctlg_layout2: 'Products: icons + preview (most pages)',
        ctlg_productpage1: 'One product', ctlg_productpage2: 'Two products', ctlg_productpage2b: 'Two products with texts',
        ctlg_productpage3: 'Three products', ctlg_productpage3b: 'Three products with texts',
        ctlg_infopage1: 'Info page (1 image)', ctlg_infopage2: 'Info page (2 images)',
        ctlg_club1: 'Habbo Club (info)', ctlg_club2: 'Habbo Club (buy)', ctlg_camera1: 'Camera', ctlg_camera2: 'Camera (film)',
        ctlg_pets: 'Pets (choose a pet)', ctlg_pets2: 'Pets (accessories)', ctlg_plasto: 'Plastic (colour picker)',
        ctlg_trophies: 'Trophies (colour + dedication)', ctlg_spaces: 'Spaces (wallpaper, floor, landscape)',
        ctlg_soundmachine: 'Trax / sound machine', ctlg_presents: 'Gifts', ctlg_recycler: 'Recycler', ctlg_purse: 'Purse',
        ctlg_collectibles: 'Collectibles', ctlg_norares: 'No rares'
    };
    // Layouts whose controls the game builds itself (their page class), beyond the page's texts and images.
    var INTERACTIVE = {
        ctlg_plasto: 'The colour buttons come from the products\' colour variants (sprite*colour).',
        ctlg_trophies: 'The colour buttons and the dedication field are drawn by the game.',
        ctlg_pets: 'The pet models and the name field are drawn by the game.',
        ctlg_spaces: 'Wallpaper, floor and landscape choices come from the game\'s own lists.',
        ctlg_recycler: 'The recycler is drawn entirely by the game.',
        ctlg_purse: 'The purse content is drawn by the game.',
        ctlg_presents: 'The gift boxes are drawn by the game.'
    };

    var opts, view, gameData = {}, pages = [], current = null, draft = null, products = [], dupes = [];
    var selectedProduct = -1, openProduct = -1, dirtyPage = false, tab = 'page';
    var $ = function (sel, root) { return (root || document).querySelector(sel); };
    var $$ = function (sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); };

    function h(tag, attrs, children) {
        var node = document.createElement(tag);
        Object.keys(attrs || {}).forEach(function (k) {
            var v = attrs[k];
            if (v === undefined || v === null || v === false) return;
            if (k === 'class') node.className = v;
            else if (k === 'text') node.textContent = v;
            else if (k === 'html') node.innerHTML = v;
            else if (k.indexOf('on') === 0) node.addEventListener(k.slice(2), v);
            else if (k === 'value') node.value = v;
            else if (k === 'checked') node.checked = !!v;
            else node.setAttribute(k, v === true ? '' : v);
        });
        (children || []).forEach(function (c) { if (c !== null && c !== undefined && c !== false) node.appendChild(typeof c === 'string' ? document.createTextNode(c) : c); });
        return node;
    }

    function api(path, body) {
        return fetch(opts.base + '/' + path, body === undefined ? { credentials: 'same-origin' } : {
            method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body)
        }).then(function (r) {
            if (r.ok) return r.status === 200 && (r.headers.get('content-type') || '').indexOf('json') >= 0 ? r.json() : null;
            return r.text().then(function (t) { throw new Error(t || r.statusText); });
        });
    }

    function status(text, kind) {
        var node = $('#cated-status');
        node.textContent = text;
        node.className = 'cated-status' + (kind ? ' is-' + kind : '');
    }

    function fail(err) {
        // Server errors can be a whole exception page; the first line says what went wrong.
        var message = String((err && err.message) || err).split('\n')[0].slice(0, 300);
        status(message, 'error');
        Swal.fire({ icon: 'error', title: 'That didn\'t work', text: message });
    }

    function furniUrl(sprite, extra) {
        var meta = document.querySelector('meta[name="habbo-furni-url"]');
        var base = (meta && meta.content) || '/habbo-imaging/furni';
        var parts = (sprite || '').split('*');
        return base + '?sprite=' + encodeURIComponent(parts[0]) + (parts[1] ? '&color=' + parts[1] : '') + (extra || '');
    }

    // ---- Preview ----

    function viewProducts() {
        return products.map(function (p) {
            return {
                name: p.name, description: p.description, price: p.price, isPackage: p.isPackage, saleCode: p.saleCode,
                sprite: p.definition ? p.definition.sprite : '', colour: p.definition ? p.definition.colour : '',
                packages: (p.packages || []).map(function (x) {
                    return { sprite: x.definition ? x.definition.sprite : '', colour: x.definition ? x.definition.colour : '', amount: x.amount };
                })
            };
        }).filter(function (p, i) { return !products[i].hidden; });
    }

    // The client keeps a page's link list (u) while you move through the linked pages: [page, ...links].
    function linkChain() {
        if (!draft) return null;
        if ((draft.links || []).length) return [draft.nameIndex].concat(draft.links);
        var parent = pages.filter(function (p) { return p.id !== draft.id && (p.links || []).indexOf(draft.nameIndex) >= 0; })[0];
        return parent ? [parent.nameIndex].concat(parent.links) : null;
    }

    function renderPreview() {
        if (!view) return;
        var showHidden = $('#cated-show-hidden').checked;
        var index = pages.filter(function (p) { return showHidden || p.indexVisible || (current && p.id === current.id); })
            .map(function (p) { return { id: p.id, name: p.id === (draft && draft.id) ? draft.name : p.name, hidden: !p.indexVisible }; });
        var visible = viewProducts();
        view.render({ index: index, pageId: draft ? draft.id : 0, page: draft, products: visible, purse: 500, linkChain: linkChain() });
        var visibleIndex = selectedProduct >= 0 ? visibleIndexOf(selectedProduct) : -1;
        if (visibleIndex >= 0) view.selectProduct(visibleIndex, true);
        markFocus();
    }

    function visibleIndexOf(productIndex) {
        if (!products[productIndex] || products[productIndex].hidden) return -1;
        return products.slice(0, productIndex).filter(function (p) { return !p.hidden; }).length;
    }

    function productIndexOfVisible(visibleIndex) {
        var n = -1;
        for (var i = 0; i < products.length; i++) {
            if (!products[i].hidden) n++;
            if (n === visibleIndex) return i;
        }
        return -1;
    }

    var focusField = null;
    function markFocus() {
        $$('.ctlg-editable.is-focus').forEach(function (n) { n.classList.remove('is-focus'); });
        if (focusField) $$('.ctlg-editable[data-field="' + focusField + '"]').forEach(function (n) { n.classList.add('is-focus'); });
    }

    // ---- Pages ----

    function loadPages() {
        return api('Pages').then(function (list) { pages = list; renderIndexTab(); });
    }

    function confirmLeave() {
        if (!dirtyPage) return Promise.resolve(true);
        return Swal.fire({
            title: 'Unsaved changes', text: 'This page has changes that aren\'t saved. Leave them?', icon: 'warning',
            showCancelButton: true, confirmButtonText: 'Leave them', cancelButtonText: 'Stay'
        }).then(function (r) { return !!r.value; });
    }

    function selectPage(id, force) {
        if (current && current.id === id && !force) return Promise.resolve();
        return (force ? Promise.resolve(true) : confirmLeave()).then(function (ok) {
            if (!ok) return;
            status('Loading the page…');
            return api('Page?id=' + id).then(function (data) {
                current = data.page;
                draft = JSON.parse(JSON.stringify(data.page));
                products = data.products;
                dupes = data.duplicateSaleCodes || [];
                selectedProduct = -1; openProduct = -1; dirtyPage = false; focusField = null;
                if (history.replaceState) history.replaceState(null, '', '?page=' + id);
                status('Page "' + current.name + '": ' + products.length + ' products.');
                renderPageTab(); renderProductsTab(); renderIndexTab(); renderPreview();
            });
        }).catch(fail);
    }

    function setDirty() {
        dirtyPage = true;
        status('Unsaved changes on this page.', 'dirty');
    }

    function field(label, input, hint, wide, key) {
        return h('label', { class: 'cated-field' + (wide ? ' cated-wide' : ''), 'data-key': key || null }, [
            h('span', {}, [h('span', { text: label }), hint ? h('span', { class: 'cated-hint', text: hint }) : null]), input
        ]);
    }

    function textInput(value, onInput, attrs) {
        return h('input', Object.assign({ type: 'text', value: value || '', autocomplete: 'off', oninput: function (e) { onInput(e.target.value); } }, attrs || {}));
    }

    function textArea(value, onInput, rows) {
        return h('textarea', { rows: rows || 3, oninput: function (e) { onInput(e.target.value); } }, [String(value || '').replace(/<br\s*\/?>/gi, '\n')]);
    }

    function pageChanged() { setDirty(); renderPreview(); }

    function imageField(value, onChange, kind) {
        var input = textInput(value, function (v) { onChange(v); thumb(); });
        var img = h('img', { class: 'cated-thumb', alt: '' });
        function thumb() {
            var src = CatalogueView.pageImageUrl(input.value.trim(), gameData);
            img.src = src ? src.url : '';
            img.style.visibility = src ? '' : 'hidden';
            input.title = input.value && !src ? 'The game has no image with this name' : '';
            input.style.borderColor = input.value && !src ? '#CC0000' : '';
        }
        thumb();
        return h('div', { class: 'cated-image-field' }, [
            img, input,
            h('button', { type: 'button', text: 'Choose…', onclick: function () {
                pickImage(kind, function (name) { input.value = name; onChange(name); thumb(); });
            } }),
            h('button', { type: 'button', text: '×', title: 'No image', onclick: function () { input.value = ''; onChange(''); thumb(); } })
        ]);
    }

    function renderPageTab() {
        var root = $('#cated-tab-page');
        root.textContent = '';
        if (!draft) { root.appendChild(h('p', { text: 'Pick a page in the catalogue index.' })); return; }
        var info = CatalogueView.layoutInfo(draft.layout);

        // Index & access
        var fuses = (gameData.fuses || ['DEFAULT']).slice();
        if (fuses.indexOf(draft.fuse) < 0) fuses.push(draft.fuse);
        var fuseSelect = h('select', { onchange: function (e) { draft.fuse = e.target.value; setDirty(); } },
            fuses.map(function (f) { return h('option', { value: f, text: f === 'DEFAULT' ? 'Everyone (DEFAULT)' : f, selected: f === draft.fuse ? 'selected' : null }); }));
        root.appendChild(h('div', { class: 'cated-section' }, [
            h('h3', {}, ['Index & access ', h('span', { class: 'cated-hint', text: '(page ' + draft.id + ')' })]),
            h('div', { class: 'cated-grid' }, [
                field('Name', textInput(draft.name, function (v) { draft.name = v; pageChanged(); }), 'in the index and page', false, 'name'),
                field('Name index', textInput(draft.nameIndex, function (v) { draft.nameIndex = v; setDirty(); }), 'internal id, used by links'),
                field('Who can see it', fuseSelect, 'fuse right'),
                h('div', { class: 'cated-field' }, [
                    h('span', { text: 'Visibility' }),
                    h('label', { class: 'cated-check' }, [h('input', { type: 'checkbox', checked: draft.indexVisible, onchange: function (e) { draft.indexVisible = e.target.checked; pageChanged(); } }), 'In the index']),
                    h('label', { class: 'cated-check' }, [h('input', { type: 'checkbox', checked: draft.clubOnly, onchange: function (e) { draft.clubOnly = e.target.checked; setDirty(); } }), 'Habbo Club members only'])
                ])
            ])
        ]));

        // Layout
        var layouts = CatalogueView.layouts();
        if (layouts.indexOf(draft.layout) < 0) layouts.push(draft.layout);
        var layoutSelect = h('select', { onchange: function (e) { draft.layout = e.target.value; setDirty(); renderPageTab(); renderPreview(); } },
            layouts.map(function (l) { return h('option', { value: l, text: (LAYOUTS[l] || l) + '  (' + l.replace('ctlg_', '') + ')', selected: l === draft.layout ? 'selected' : null }); }));
        var slots = [];
        if (info.icons) slots.push(info.icons + ' product icons per page');
        if (info.productSlots) slots.push(info.productSlots + ' product' + (info.productSlots > 1 ? 's' : ''));
        if (info.teasers) slots.push(info.teasers + ' image' + (info.teasers > 1 ? 's' : ''));
        if (info.texts.length) slots.push(info.texts.length + ' text' + (info.texts.length > 1 ? 's' : ''));
        root.appendChild(h('div', { class: 'cated-section' }, [
            h('h3', { text: 'Layout' }),
            field('Layout', layoutSelect, slots.join(', '), true),
            INTERACTIVE[draft.layout] ? h('p', { class: 'cated-hint', text: INTERACTIVE[draft.layout] }) : null
        ]));

        // Content, per what the layout shows
        var content = h('div', { class: 'cated-grid' });
        var unused = h('div', { class: 'cated-grid' });
        var add = function (shown, node) { (shown ? content : unused).appendChild(node); };
        add(info.header, field('Headline image', imageField(draft.headline, function (v) { draft.headline = v; pageChanged(); }, 'headline'), 'top of the page', true, 'headline'));
        add(info.headerText, field('Text', textArea(draft.body, function (v) { draft.body = v.replace(/\n/g, '<br>'); pageChanged(); }, 4), 'under the headline', true, 'body'));
        var textNumbers = info.texts.slice();
        Object.keys(draft.texts || {}).forEach(function (k) { if (textNumbers.indexOf(+k) < 0) textNumbers.push(+k); });
        textNumbers.sort(function (a, b) { return a - b; }).forEach(function (n) {
            add(info.texts.indexOf(n) >= 0, field('Text ' + n, textArea((draft.texts || {})[n], function (v) {
                draft.texts = draft.texts || {};
                draft.texts[n] = v.replace(/\n/g, '<br>');
                pageChanged();
            }, 3), 'ctlg_text_' + n, true, 'text:' + n));
        });
        var teaserCount = Math.max(info.teasers, (draft.teasers || []).length);
        var productPage = /^ctlg_productpage\d/.test(draft.layout);
        for (var t = 1; t <= teaserCount; t++) {
            (function (n) {
                add(n <= info.teasers, field('Image ' + n, imageField((draft.teasers || [])[n - 1], function (v) {
                    draft.teasers = draft.teasers || [];
                    while (draft.teasers.length < n) draft.teasers.push('');
                    draft.teasers[n - 1] = v;
                    while (draft.teasers.length && !draft.teasers[draft.teasers.length - 1]) draft.teasers.pop();
                    pageChanged();
                }, 'teaser'), productPage ? 'empty = the products\' pictures' : 'teaser', true, 'teaser:' + n));
            })(t);
        }
        add(info.description, field('Product hint', textArea(draft.labelPick, function (v) { draft.labelPick = v.replace(/\n/g, '<br>'); pageChanged(); }, 2),
            'shown where the description goes until a product is picked', true, 'labelPick'));
        var special = (draft.labelExtraS || '').split(':');
        var specialType = special.length > 1 ? special[0] : '1';
        var specialText = special.length > 1 ? special.slice(1).join(':') : (draft.labelExtraS || '');
        var setSpecial = function () { draft.labelExtraS = specialText ? specialType + ':' + specialText : ''; pageChanged(); };
        add(info.special, field('Badge', h('div', { class: 'cated-image-field' }, [
            h('select', { onchange: function (e) { specialType = e.target.value; setSpecial(); } }, [
                h('option', { value: '1', text: 'Style 1', selected: specialType === '1' ? 'selected' : null }),
                h('option', { value: '2', text: 'Style 2', selected: specialType === '2' ? 'selected' : null })
            ]),
            textInput(specialText, function (v) { specialText = v; setSpecial(); })
        ]), 'the special text bubble', true, 'labelExtraS'));

        // Linked pages (u): previous / next buttons between pages
        var others = pages.filter(function (p) { return p.id !== draft.id; });
        var linkList = h('div', { class: 'cated-parts' });
        var drawLinks = function () {
            linkList.textContent = '';
            (draft.links || []).forEach(function (nameIndex, i) {
                var target = pages.filter(function (p) { return p.nameIndex === nameIndex; })[0];
                linkList.appendChild(h('div', { class: 'cated-part' }, [
                    h('span', { class: 'cated-grow', text: (i + 2) + '. ' + (target ? target.name + ' (' + nameIndex + ')' : nameIndex + ' — no such page') }),
                    h('button', { type: 'button', text: '×', onclick: function () { draft.links.splice(i, 1); drawLinks(); pageChanged(); } })
                ]));
            });
        };
        drawLinks();
        var linkPick = h('select', {}, [h('option', { value: '', text: 'Link a page…' })].concat(others.map(function (p) {
            return h('option', { value: p.nameIndex, text: p.name + ' (' + p.nameIndex + ')' + (p.indexVisible ? '' : ' – hidden') });
        })));
        linkPick.onchange = function () {
            if (!linkPick.value) return;
            draft.links = (draft.links || []).concat([linkPick.value]);
            linkPick.value = '';
            drawLinks(); pageChanged();
        };
        var hasSubpages = (assetsWindow(draft.layout) || []).some(function (e) { return e.id === 'ctlg_nextpage_button'; });
        add(hasSubpages || (draft.links || []).length > 0, field('Sub-pages', h('div', {}, [linkList, linkPick]),
            'the arrows page through: this page, then these (often hidden pages)', true, 'links'));

        root.appendChild(h('div', { class: 'cated-section' }, [h('h3', { text: 'Content' }), content]));
        if (unused.childNodes.length) {
            root.appendChild(h('details', { class: 'cated-section cated-unused' }, [
                h('summary', { text: 'Stored, but not shown by this layout (' + unused.childNodes.length + ')' }), unused
            ]));
        }

        root.appendChild(h('div', { class: 'cated-toolbar' }, [
            h('button', { type: 'button', class: 'cated-primary', text: 'Save page', onclick: savePage }),
            h('button', { type: 'button', text: 'Undo changes', onclick: function () { selectPage(current.id, true); } }),
            h('span', { class: 'cated-grow' }),
            h('button', { type: 'button', class: 'cated-danger', text: 'Delete page', onclick: deletePage })
        ]));
    }

    function assetsWindow(layout) {
        try { return CatalogueView.windowElements(layout); } catch (e) { return []; }
    }

    function focusOn(fieldKey) {
        focusField = fieldKey;
        if (fieldKey.indexOf('product:') === 0) return;
        showTab('page');
        var node = $('#cated-tab-page [data-key="' + fieldKey + '"]');
        if (node && node.closest('details')) node.closest('details').open = true;
        $$('#cated-tab-page .cated-field.is-focus').forEach(function (n) { n.classList.remove('is-focus'); });
        if (node) {
            node.classList.add('is-focus');
            node.scrollIntoView({ block: 'center', behavior: 'smooth' });
            var input = node.querySelector('textarea, input[type=text], select');
            if (input) input.focus({ preventScroll: true });
        }
        markFocus();
    }

    function savePage() {
        status('Saving…');
        return api('SavePage', draft).then(function (r) {
            dirtyPage = false;
            status('Saved. Publish to show it in the hotel.');
            return loadPages().then(function () { return selectPage(r.id, true); });
        }).catch(fail);
    }

    function deletePage() {
        Swal.fire({
            title: 'Delete "' + draft.name + '"?', icon: 'warning', showCancelButton: true, confirmButtonText: 'Delete it',
            text: 'Its ' + products.length + ' products leave the catalogue too, unless another page also sells them.'
        }).then(function (r) {
            if (!r.value) return;
            api('DeletePage', { id: draft.id }).then(function () {
                dirtyPage = false;
                current = draft = null;
                return loadPages().then(function () { if (pages.length) selectPage(pages[0].id, true); });
            }).catch(fail);
        });
    }

    function newPage() {
        confirmLeave().then(function (ok) {
            if (!ok) return;
            Swal.fire({ title: 'New page', input: 'text', inputPlaceholder: 'Name in the index', showCancelButton: true, confirmButtonText: 'Create' })
                .then(function (r) {
                    if (!r.value) return;
                    var page = { name: r.value, nameIndex: r.value, layout: 'ctlg_layout2', indexVisible: false, fuse: 'DEFAULT', teasers: [], texts: {}, links: [] };
                    api('SavePage', page).then(function (res) {
                        dirtyPage = false;
                        status('Page created (hidden from players until you tick "In the index").');
                        return loadPages().then(function () { return selectPage(res.id, true); });
                    }).catch(fail);
                });
        });
    }

    // ---- Index tab (all pages, drag to reorder) ----

    function renderIndexTab() {
        var root = $('#cated-tab-index');
        root.textContent = '';
        root.appendChild(h('p', { class: 'cated-hint', text: 'Drag pages to change their order in the game. Hidden pages are only reachable through links (sub-pages).' }));
        var list = h('ul', { class: 'cated-pages' });
        pages.forEach(function (p) {
            var tags = [];
            if (!p.indexVisible) tags.push(h('span', { class: 'cated-tag is-hidden', text: 'hidden' }));
            if (p.clubOnly) tags.push(h('span', { class: 'cated-tag is-club', text: 'HC' }));
            if (p.fuse && p.fuse !== 'DEFAULT') tags.push(h('span', { class: 'cated-tag' + (/club/.test(p.fuse) ? ' is-club' : ' is-staff'), text: p.fuse.replace('fuse_catalogue_', '') }));
            var li = h('li', { draggable: 'true', 'data-id': p.id, class: current && p.id === current.id ? 'is-active' : '' }, [
                h('span', { class: 'cated-page-name', text: p.name }),
                h('span', {}, tags),
                h('span', { class: 'cated-page-meta', text: (LAYOUTS[p.layout] ? p.layout.replace('ctlg_', '') : p.layout) + ' · ' + p.productCount + ' products' })
            ]);
            li.onclick = function () { selectPage(p.id); };
            list.appendChild(li);
        });
        sortable(list, function (ids) {
            api('ReorderPages', { ids: ids }).then(function () {
                status('Order saved. Publish to show it in the hotel.');
                return loadPages().then(renderPreview);
            }).catch(fail);
        });
        root.appendChild(list);
    }

    // Native drag & drop reordering of an <ul>'s <li data-id>.
    function sortable(list, onDrop) {
        var dragging = null;
        list.addEventListener('dragstart', function (e) {
            dragging = e.target.closest('li');
            if (!dragging || dragging.parentNode !== list) { dragging = null; return; }
            dragging.classList.add('is-dragging');
            e.dataTransfer.effectAllowed = 'move';
        });
        list.addEventListener('dragover', function (e) {
            if (!dragging) return;
            e.preventDefault();
            var over = e.target.closest('li');
            $$('.is-drop-before', list).forEach(function (n) { n.classList.remove('is-drop-before'); });
            if (over && over !== dragging && over.parentNode === list) over.classList.add('is-drop-before');
        });
        list.addEventListener('drop', function (e) {
            if (!dragging) return;
            e.preventDefault();
            var over = e.target.closest('li');
            if (over && over !== dragging && over.parentNode === list) list.insertBefore(dragging, over);
            else if (!over) list.appendChild(dragging);
        });
        list.addEventListener('dragend', function () {
            if (!dragging) return;
            dragging.classList.remove('is-dragging');
            $$('.is-drop-before', list).forEach(function (n) { n.classList.remove('is-drop-before'); });
            dragging = null;
            onDrop($$(':scope > li', list).map(function (li) { return +li.getAttribute('data-id'); }));
        });
    }

    // ---- Products tab ----

    function renderProductsTab() {
        var root = $('#cated-tab-products');
        root.textContent = '';
        if (!draft) return;
        var info = CatalogueView.layoutInfo(draft.layout);
        var capacity = info.icons ? info.icons + ' per page of icons (the arrows page through the rest)' : info.productSlots ? 'this layout shows ' + info.productSlots : 'this layout shows no products';
        root.appendChild(h('div', { class: 'cated-toolbar' }, [
            h('button', { type: 'button', class: 'cated-primary', text: '+ Add furni', onclick: function () { addFurniDialog(); } }),
            h('button', { type: 'button', text: '+ New furni', onclick: function () { furniDialog(null, true); } }),
            h('span', { class: 'cated-grow' }),
            h('span', { class: 'cated-hint', text: products.length + ' products; ' + capacity })
        ]));
        if (dupes.length) {
            root.appendChild(h('p', { class: 'cated-warning', text: 'Sale code used by more than one product: ' + dupes.join(', ') + '. The game buys the first one it finds, so give each its own code.' }));
        }
        var list = h('ul', { class: 'cated-products' });
        products.forEach(function (p, i) {
            var sprite = p.isPackage ? ((p.packages[0] || {}).definition || {}).sprite : (p.definition || {}).sprite;
            var li = h('li', { draggable: 'true', 'data-id': p.id, class: (i === openProduct ? 'is-selected' : '') + (p.hidden ? ' is-hidden' : '') });
            var row = h('div', { class: 'cated-product-row' }, [
                h('span', { class: 'cated-drag', text: '⋮⋮', title: 'Drag to reorder' }),
                h('img', { src: sprite ? furniUrl(sprite, '&icon=true') : '', alt: '' }),
                h('span', { class: 'cated-product-name' }, [
                    h('b', { text: p.name || '(no name)' }),
                    h('small', { text: (p.isPackage ? 'Package · ' : '') + p.saleCode + (p.hidden ? ' · hidden' : '') + (dupes.indexOf(p.saleCode) >= 0 ? ' · duplicate code!' : '') })
                ]),
                h('span', { class: 'cated-price', text: p.price + ' cr' + (p.amount > 1 ? ' · ' + p.amount + 'x' : '') })
            ]);
            row.onclick = function () {
                openProduct = openProduct === i ? -1 : i;
                selectedProduct = i;
                renderProductsTab(); renderPreview();
            };
            li.appendChild(row);
            if (i === openProduct) li.appendChild(productForm(p));
            list.appendChild(li);
        });
        sortable(list, function (ids) {
            products.sort(function (a, b) { return ids.indexOf(a.id) - ids.indexOf(b.id); });
            openProduct = -1; selectedProduct = -1;
            api('ReorderProducts', { pageId: draft.id, ids: ids }).then(function () {
                status('Product order saved. Publish to show it in the hotel.');
                renderProductsTab(); renderPreview();
            }).catch(fail);
        });
        root.appendChild(list);
    }

    function productForm(p) {
        var edit = JSON.parse(JSON.stringify(p));
        var ownTexts = function () { return edit.isPackage || edit.specialSpriteId > 0; };
        var form = h('div', { class: 'cated-product-edit' });
        var live = function () {
            // Show the edit in the preview straight away.
            var target = products[openProduct];
            target.price = edit.price; target.hidden = edit.hidden; target.amount = edit.amount;
            target.isPackage = edit.isPackage; target.packages = edit.packages;
            target.name = edit.isPackage ? edit.packageName : ownTexts() ? edit.ownName : (edit.definition || {}).name;
            target.description = edit.isPackage ? edit.packageDescription : ownTexts() ? edit.ownDescription : (edit.definition || {}).description;
            renderPreview();
        };
        var draw = function () {
            form.textContent = '';
            var grid = h('div', { class: 'cated-grid' }, [
                field('Price (credits)', h('input', { type: 'number', min: 0, value: edit.price, oninput: function (e) { edit.price = +e.target.value; live(); } })),
                field('Amount', h('input', { type: 'number', min: 1, value: edit.amount, oninput: function (e) { edit.amount = +e.target.value; } }), 'furni per purchase'),
                field('Sale code', textInput(edit.saleCode, function (v) { edit.saleCode = v; }), 'what the client buys by'),
                field('Special sprite id', h('input', { type: 'number', min: 0, value: edit.specialSpriteId, oninput: function (e) { edit.specialSpriteId = +e.target.value; draw(); live(); } }), 'posters etc. (0 = none)'),
                h('div', { class: 'cated-field cated-wide' }, [
                    h('label', { class: 'cated-check' }, [h('input', { type: 'checkbox', checked: edit.hidden, onchange: function (e) { edit.hidden = e.target.checked; live(); } }), 'Hidden (not sold, not shown)']),
                    h('label', { class: 'cated-check' }, [h('input', { type: 'checkbox', checked: edit.isPackage, onchange: function (e) {
                        edit.isPackage = e.target.checked;
                        if (edit.isPackage && !edit.packages.length && edit.definition) edit.packages = [{ definitionId: edit.definitionId, amount: 1, specialSpriteId: 0, definition: edit.definition }];
                        if (edit.isPackage && !edit.packageName) edit.packageName = (edit.definition || {}).name || '';
                        draw(); live();
                    } }), 'Package (several furni for one price)'])
                ])
            ]);
            form.appendChild(grid);
            if (edit.isPackage) {
                form.appendChild(h('div', { class: 'cated-grid' }, [
                    field('Package name', textInput(edit.packageName, function (v) { edit.packageName = v; live(); }), null, true),
                    field('Package description', textInput(edit.packageDescription, function (v) { edit.packageDescription = v; live(); }), null, true)
                ]));
                var parts = h('div', { class: 'cated-parts' });
                edit.packages.forEach(function (part, i) {
                    parts.appendChild(h('div', { class: 'cated-part' }, [
                        h('img', { src: part.definition ? furniUrl(part.definition.sprite, '&icon=true') : '', alt: '' }),
                        h('span', { class: 'cated-grow', text: part.definition ? part.definition.name + ' (' + part.definition.sprite + ')' : '#' + part.definitionId }),
                        h('input', { type: 'number', min: 1, value: part.amount, title: 'How many', oninput: function (e) { part.amount = +e.target.value; live(); } }),
                        h('button', { type: 'button', text: '×', onclick: function () { edit.packages.splice(i, 1); draw(); live(); } })
                    ]));
                });
                form.appendChild(field('Furni in the package', h('div', {}, [parts,
                    h('button', { type: 'button', text: '+ Furni', onclick: function () {
                        addFurniDialog(function (picked) {
                            picked.forEach(function (f) { edit.packages.push({ definitionId: f.id, amount: 1, specialSpriteId: 0, definition: f }); });
                            draw(); live();
                        });
                    } })
                ]), null, true));
            } else if (ownTexts()) {
                form.appendChild(h('div', { class: 'cated-grid' }, [
                    field('Name', textInput(edit.ownName, function (v) { edit.ownName = v; live(); }), 'own name (special sprite)', true),
                    field('Description', textInput(edit.ownDescription, function (v) { edit.ownDescription = v; live(); }), null, true)
                ]));
            }
            var d = edit.definition;
            form.appendChild(h('p', { class: 'cated-hint' }, [
                d ? 'Furni: ' + d.name + ' (' + d.sprite + ', #' + d.id + ')' + (edit.isPackage || ownTexts() ? '' : '. Name and description come from the furni.') + ' ' : 'No furni linked. ',
                d ? h('button', { type: 'button', text: 'Edit furni', onclick: function () { furniDialog(d, false); } }) : null,
                ' ',
                h('button', { type: 'button', text: 'Change furni…', onclick: function () {
                    addFurniDialog(function (picked) {
                        if (!picked.length) return;
                        edit.definitionId = picked[0].id; edit.definition = picked[0];
                        draw(); live();
                    }, true);
                } })
            ]));
            var otherPages = (edit.pageIds || []).filter(function (id) { return id !== draft.id; });
            if (otherPages.length) {
                form.appendChild(h('p', { class: 'cated-hint', text: 'Also sold on: ' + otherPages.map(function (id) { var pg = pages.filter(function (x) { return x.id === id; })[0]; return pg ? pg.name : '#' + id; }).join(', ') + '. Changes apply there too.' }));
            }
            form.appendChild(h('div', { class: 'cated-toolbar' }, [
                h('button', { type: 'button', class: 'cated-primary', text: 'Save product', onclick: function () {
                    api('SaveProduct', edit).then(function () {
                        status('Product saved. Publish to show it in the hotel.');
                        return selectPage(draft.id, true).then(function () { openProduct = products.map(function (x) { return x.id; }).indexOf(edit.id); selectedProduct = openProduct; renderProductsTab(); renderPreview(); });
                    }).catch(fail);
                } }),
                h('button', { type: 'button', text: 'Cancel', onclick: function () { selectPage(draft.id, true); } }),
                h('span', { class: 'cated-grow' }),
                h('button', { type: 'button', class: 'cated-danger', text: otherPages.length ? 'Remove from this page' : 'Remove from the catalogue', onclick: function () {
                    api('RemoveProduct', { id: edit.id, pageId: draft.id }).then(function () {
                        status('Product removed. Publish to show it in the hotel.');
                        return loadPages().then(function () { return selectPage(draft.id, true); });
                    }).catch(fail);
                } })
            ]));
        };
        draw();
        return form;
    }

    // ---- Dialogs ----

    function modal(title, body, buttons) {
        var back = h('div', { class: 'cated-modal-back' });
        var close = function () { back.remove(); document.removeEventListener('keydown', esc); };
        var esc = function (e) { if (e.key === 'Escape') close(); };
        document.addEventListener('keydown', esc);
        var dialog = h('div', { class: 'cated-modal', role: 'dialog' }, [
            h('div', { class: 'cated-modal-head' }, [h('span', { text: title }), h('button', { type: 'button', text: '×', onclick: close })]),
            h('div', { class: 'cated-modal-body' }, [body]),
            h('div', { class: 'cated-modal-foot' }, (buttons || []).map(function (b) {
                return h('button', { type: 'button', class: b.primary ? 'cated-primary' : '', text: b.text, onclick: function () { if (b.click(close) !== false) close(); } });
            }))
        ]);
        back.appendChild(dialog);
        back.addEventListener('mousedown', function (e) { if (e.target === back) close(); });
        document.body.appendChild(back);
        return { close: close, dialog: dialog };
    }

    // Images the game can show on a page: cast members first (as the client looks them up), then the image library.
    function pickImage(kind, done) {
        var names = Object.keys(CatalogueView.castImages()).map(function (n) { return { name: n, cast: true }; })
            .concat((gameData.libraryImages || []).filter(function (n) { return !CatalogueView.castImages()[n]; }).map(function (n) { return { name: n, cast: false }; }));
        var guess = kind === 'headline' ? /head|header|hdr/ : /teaser|pic|img|_t\d|promo/;
        names.sort(function (a, b) { return (guess.test(b.name) - guess.test(a.name)) || a.name.localeCompare(b.name); });
        var search = h('input', { type: 'text', placeholder: 'Search images…', autocomplete: 'off' });
        var grid = h('div', { class: 'cated-image-grid' });
        var dlg;
        var draw = function () {
            var q = search.value.trim().toLowerCase();
            grid.textContent = '';
            names.filter(function (n) { return !q || n.name.toLowerCase().indexOf(q) >= 0; }).slice(0, 300).forEach(function (n) {
                var src = CatalogueView.pageImageUrl(n.name, gameData);
                grid.appendChild(h('figure', { onclick: function () { done(n.name); dlg.close(); } }, [
                    h('img', { src: src ? src.url : '', alt: '', loading: 'lazy' }),
                    h('figcaption', {}, [n.name, h('br'), h('small', { text: n.cast ? 'in the game\'s casts' : 'image library' })])
                ]));
            });
        };
        search.oninput = draw;
        draw();
        dlg = modal('Choose an image', h('div', {}, [
            search,
            h('p', { class: 'cated-hint', text: 'New images: upload "<name>_' + (gameData.language || 'da') + '.gif" to ' + (gameData.imageLibraryUrl || '') + 'catalogue/ and type the name.' }),
            grid
        ]), [{ text: 'Close', click: function () {} }]);
        setTimeout(function () { search.focus(); }, 0);
    }

    // Search furni master data; pick several (or one) and add them to the page, or hand them to `done`.
    function addFurniDialog(done, single) {
        var picked = [];
        var search = h('input', { type: 'text', placeholder: 'Search by name, sprite or id…', autocomplete: 'off' });
        var results = h('div', { class: 'cated-furni-results' });
        var count = h('span', { class: 'cated-hint' });
        var timer;
        var run = function () {
            var q = search.value.trim();
            if (q.length < 2) { results.textContent = ''; return; }
            api('Furni?query=' + encodeURIComponent(q)).then(function (list) {
                results.textContent = '';
                list.forEach(function (f) {
                    var on = f.onPages.map(function (id) { var pg = pages.filter(function (x) { return x.id === id; })[0]; return pg ? pg.name : '#' + id; });
                    var card = h('div', { class: 'cated-furni-card' + (picked.some(function (x) { return x.id === f.id; }) ? ' is-picked' : ''), title: on.length ? 'On: ' + on.join(', ') : 'Not in the catalogue' }, [
                        h('img', { src: furniUrl(f.sprite, '&icon=true'), alt: '', loading: 'lazy' }),
                        h('div', {}, [h('b', { text: f.name || f.sprite }), h('small', { text: f.sprite + ' · #' + f.id + (on.length ? ' · ' + on.length + ' page' + (on.length > 1 ? 's' : '') : ' · not sold') })])
                    ]);
                    card.onclick = function () {
                        var at = picked.map(function (x) { return x.id; }).indexOf(f.id);
                        if (single) picked = at >= 0 ? [] : [f];
                        else if (at >= 0) picked.splice(at, 1); else picked.push(f);
                        $$('.cated-furni-card', results).forEach(function (c) { c.classList.remove('is-picked'); });
                        run();
                        count.textContent = picked.length + ' picked';
                    };
                    results.appendChild(card);
                });
                if (!list.length) results.appendChild(h('p', { text: 'No furni found. Create it with "New furni".' }));
            }).catch(fail);
        };
        search.oninput = function () { clearTimeout(timer); timer = setTimeout(run, 250); };
        modal(done ? 'Pick furni' : 'Add furni to "' + draft.name + '"', h('div', {}, [search, results, count]), [
            { text: 'Cancel', click: function () {} },
            { text: done ? 'Use' : 'Add to the page', primary: true, click: function () {
                if (!picked.length) return false;
                if (done) { done(picked); return; }
                api('AddProducts', { pageId: draft.id, ids: picked.map(function (f) { return f.id; }) }).then(function () {
                    status(picked.length + ' added for 3 credits each: set their prices. Publish to show them in the hotel.');
                    return loadPages().then(function () { return selectPage(draft.id, true); }).then(function () { showTab('products'); });
                }).catch(fail);
            } }
        ]);
        setTimeout(function () { search.focus(); }, 0);
    }

    // Furni master data (items_definitions): edit one, or create one and put it on the current page.
    function furniDialog(furni, isNew) {
        var f = furni ? JSON.parse(JSON.stringify(furni)) : { id: 0, sprite: '', spriteId: 0, name: '', description: '', colour: '0,0,0', length: 1, width: 1, topHeight: 0, maxStatus: '2', behaviour: 'solid', interactor: 'default', isTradable: true, isRecyclable: true, drinkIds: '' };
        var addToPage = !!(isNew && draft);
        var preview = h('div', { class: 'cated-furni-preview' });
        var castState = h('span', { class: 'cated-hint' });
        var behaviours = function () { return (f.behaviour || '').split(',').map(function (b) { return b.trim(); }).filter(Boolean); };
        var drawPreview = function () {
            preview.textContent = '';
            if (!f.sprite) return;
            preview.appendChild(h('img', { src: furniUrl(f.sprite, '&direction=2'), alt: '' }));
            preview.appendChild(h('img', { src: furniUrl(f.sprite, '&icon=true'), alt: '' }));
            preview.appendChild(castState);
            var url = (gameData.furniCastUrl || '').replace('%typeid%', f.sprite.split('*')[0]);
            if (!url) return;
            castState.textContent = 'Checking the graphics…';
            fetch(url, { method: 'HEAD', mode: 'cors' }).then(function (r) {
                castState.textContent = r.ok ? 'Graphics found (hh_furni_xx_' + f.sprite.split('*')[0] + '.cct).' : 'No graphics on the asset server yet: upload hh_furni_xx_' + f.sprite.split('*')[0] + '.cct to hof_furni/, or the game can\'t show it.';
                castState.style.color = r.ok ? '' : '#CC0000';
            }).catch(function () { castState.textContent = ''; });
        };
        var spriteTimer;
        var wall = behaviours().indexOf('wall_item') >= 0;
        var behaviourBox = h('div', { class: 'cated-behaviours' }, (gameData.behaviours || []).map(function (b) {
            return h('label', {}, [h('input', { type: 'checkbox', value: b, checked: behaviours().indexOf(b) >= 0, onchange: function () {
                f.behaviour = $$('input:checked', behaviourBox).map(function (i) { return i.value; }).join(',');
            } }), b]);
        }));
        var setWall = function (isWall) {
            var list = behaviours().filter(function (b) { return b !== 'wall_item'; });
            if (isWall) list.push('wall_item');
            f.behaviour = list.join(',');
            $$('input', behaviourBox).forEach(function (i) { i.checked = list.indexOf(i.value) >= 0; });
        };
        var body = h('div', {}, [
            preview,
            h('div', { class: 'cated-grid' }, [
                field('Sprite (class name)', textInput(f.sprite, function (v) { f.sprite = v.trim(); clearTimeout(spriteTimer); spriteTimer = setTimeout(drawPreview, 400); }),
                    'names the graphics; colour variants: name*N'),
                field('Sprite id', h('input', { type: 'number', min: 0, value: f.spriteId, oninput: function (e) { f.spriteId = +e.target.value; } }), 'the client\'s furni id'),
                field('Name', textInput(f.name, function (v) { f.name = v; })),
                field('Description', textInput(f.description, function (v) { f.description = v; })),
                field('Type', h('select', { onchange: function (e) { setWall(e.target.value === 'wall'); } }, [
                    h('option', { value: 'floor', text: 'Floor furni', selected: wall ? null : 'selected' }),
                    h('option', { value: 'wall', text: 'Wall item', selected: wall ? 'selected' : null })
                ])),
                field('Interaction', h('select', { onchange: function (e) { f.interactor = e.target.value; } }, (gameData.interactors || ['default']).map(function (i) {
                    return h('option', { value: i, text: i, selected: i === f.interactor ? 'selected' : null });
                }))),
                field('Size (length × width)', h('div', { class: 'cated-image-field' }, [
                    h('input', { type: 'number', min: 0, value: f.length, oninput: function (e) { f.length = +e.target.value; } }), '×',
                    h('input', { type: 'number', min: 0, value: f.width, oninput: function (e) { f.width = +e.target.value; } })
                ]), 'tiles'),
                field('Height', h('input', { type: 'number', min: 0, step: '0.01', value: f.topHeight, oninput: function (e) { f.topHeight = +e.target.value; } }), 'stack height on top'),
                field('Colours', textInput(f.colour, function (v) { f.colour = v; }), 'part colours, e.g. #FFFFFF,#CC0000'),
                field('States', textInput(f.maxStatus, function (v) { f.maxStatus = v; }), 'how many states (on/off = 2)'),
                field('Drink ids', textInput(f.drinkIds, function (v) { f.drinkIds = v; }), 'vending machines'),
                h('div', { class: 'cated-field' }, [
                    h('span', { text: 'Trading' }),
                    h('label', { class: 'cated-check' }, [h('input', { type: 'checkbox', checked: f.isTradable, onchange: function (e) { f.isTradable = e.target.checked; } }), 'Tradable']),
                    h('label', { class: 'cated-check' }, [h('input', { type: 'checkbox', checked: f.isRecyclable, onchange: function (e) { f.isRecyclable = e.target.checked; } }), 'Recyclable'])
                ]),
                field('Behaviours', behaviourBox, 'how it acts in a room', true),
                isNew && draft ? h('label', { class: 'cated-check cated-wide' }, [h('input', { type: 'checkbox', checked: addToPage, onchange: function (e) { addToPage = e.target.checked; } }), 'Also sell it on "' + draft.name + '"']) : null
            ])
        ]);
        drawPreview();
        modal(isNew ? 'New furni' : 'Furni #' + f.id + ': ' + f.name, body, [
            { text: 'Cancel', click: function () {} },
            { text: isNew ? 'Create furni' : 'Save furni', primary: true, click: function (close) {
                api('SaveFurni', { furni: f, addToPageId: addToPage && draft ? draft.id : 0 }).then(function (r) {
                    close();
                    status(isNew ? 'Furni created (#' + r.id + ')' + (r.productIds.length ? ' and added to the page' : '') + '. Publish to show it in the hotel.' : 'Furni saved. Publish to show it in the hotel.');
                    return loadPages().then(function () { return draft ? selectPage(draft.id, true) : null; }).then(function () { if (r.productIds.length) showTab('products'); });
                }).catch(fail);
                return false;
            } }
        ]);
    }

    function publish() {
        Swal.fire({
            title: 'Publish the catalogue?', icon: 'question', showCancelButton: true, confirmButtonText: 'Publish',
            text: 'The hotel reloads all pages, products and furni from the database. Players see the changes when they next open a page.'
        }).then(function (r) {
            if (!r.value) return;
            api('Publish', {}).then(function () { status('Published: the hotel is reloading the catalogue.'); }).catch(fail);
        });
    }

    // ---- Tabs and start ----

    function showTab(name) {
        tab = name;
        $$('#cated-tabs button').forEach(function (b) { b.classList.toggle('is-active', b.getAttribute('data-tab') === name); });
        ['page', 'products', 'index'].forEach(function (t) { $('#cated-tab-' + t).hidden = t !== name; });
    }

    function init(options) {
        opts = options;
        $$('#cated-tabs button').forEach(function (b) { b.onclick = function () { showTab(b.getAttribute('data-tab')); }; });
        $('#cated-zoom').onchange = function (e) { $('#cated-stage').classList.toggle('is-2x', e.target.checked); try { localStorage.setItem('cated-zoom', e.target.checked ? '1' : ''); } catch (x) { } };
        try { if (localStorage.getItem('cated-zoom')) { $('#cated-zoom').checked = true; $('#cated-stage').classList.add('is-2x'); } } catch (x) { }
        $('#cated-show-hidden').onchange = renderPreview;
        $('#cated-publish').onclick = publish;
        $('#cated-new-page').onclick = newPage;
        $('#cated-new-furni').onclick = function () { furniDialog(null, true); };
        window.addEventListener('beforeunload', function (e) { if (dirtyPage) { e.preventDefault(); e.returnValue = ''; } });

        view = new CatalogueView($('#cated-stage'), {
            editMode: true,
            onSelectPage: function (id) { selectPage(id); },
            onSelectPageByName: function (nameIndex) {
                var target = pages.filter(function (p) { return p.nameIndex === nameIndex; })[0];
                if (target) selectPage(target.id);
            },
            onField: focusOn,
            onSelectProduct: function (visibleIndex) {
                selectedProduct = openProduct = productIndexOfVisible(visibleIndex);
                showTab('products');
                renderProductsTab();
                var row = $('#cated-tab-products li.is-selected');
                if (row) row.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            }
        });
        view.load().then(function () {
            return api('GameData?keys=' + encodeURIComponent(CatalogueView.textKeys().join(',')));
        }).then(function (data) {
            gameData = data;
            view.options.gameData = data;
            return loadPages();
        }).then(function () {
            var start = pages.filter(function (p) { return p.id === opts.pageId; })[0] || pages.filter(function (p) { return p.indexVisible; })[0] || pages[0];
            if (start) return selectPage(start.id, true);
            status('No catalogue pages yet. Create one with "New page".');
        }).catch(fail);
    }

    return { init: init };
})();

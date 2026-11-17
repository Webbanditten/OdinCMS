// Media library: the housekeeping image picker. Browses uploads (/images/CATEGORY/NAME), promo pictures and room
// photos (MediaController.List), with search, categories and uploading by drop, paste or file picker
// (MediaController.Upload). Self-contained (own styles), so it works on any housekeeping page, over TinyMCE dialogs and
// in the site's edit mode.
//
//   HabboMedia.open({ title, category, onSelect: function (item) { item.url } })
//   HabboMedia.upload(files, category) -> Promise<items>      (used for images pasted into the editors)
(function () {
    var STYLE = '\
.hm-overlay{position:fixed;inset:0;z-index:100000;background:rgba(0,0,0,.55);display:flex;align-items:center;justify-content:center;font:13px Verdana,Arial,sans-serif;color:#222}\
.hm-panel{background:#fff;width:min(980px,96vw);height:min(680px,92vh);border-radius:6px;box-shadow:0 8px 40px rgba(0,0,0,.4);display:flex;flex-direction:column;overflow:hidden}\
.hm-head{display:flex;align-items:center;gap:10px;padding:10px 14px;background:#0058a8;color:#fff}\
.hm-head h2{margin:0;font-size:15px;flex:1;color:#fff}\
.hm-head input{width:240px;padding:5px 8px;border:0;border-radius:3px}\
.hm-close{background:none;border:0;color:#fff;font-size:22px;cursor:pointer;line-height:1}\
.hm-body{flex:1;display:flex;min-height:0}\
.hm-cats{width:170px;border-right:1px solid #ddd;overflow-y:auto;background:#f6f6f6;padding:6px 0}\
.hm-cats button{display:flex;justify-content:space-between;width:100%;padding:6px 12px;border:0;background:none;text-align:left;cursor:pointer;font:inherit;text-transform:capitalize}\
.hm-cats button:hover{background:#e8eef6}.hm-cats button.is-active{background:#ffcc00;font-weight:bold}\
.hm-cats small{color:#777;font-weight:normal}\
.hm-cats h4{margin:10px 12px 4px;font-size:11px;color:#555;text-transform:uppercase}\
.hm-cats .hm-folder{text-transform:none;font-size:12px}\
.hm-main{flex:1;display:flex;flex-direction:column;min-width:0;position:relative}\
.hm-grid{flex:1;overflow-y:auto;padding:10px;display:grid;grid-template-columns:repeat(auto-fill,minmax(128px,1fr));gap:8px;align-content:start}\
.hm-tile{display:flex;flex-direction:column;border:2px solid #ddd;border-radius:4px;background:#fff;cursor:pointer;padding:0;font:inherit;text-align:left;overflow:hidden}\
.hm-tile:hover{border-color:#996600}.hm-tile.is-selected{border-color:#0058a8;box-shadow:0 0 0 2px #9cc3ea}\
.hm-thumb{height:96px;display:flex;align-items:center;justify-content:center;background:repeating-conic-gradient(#eee 0% 25%,#fff 0% 50%) 50%/14px 14px;position:relative}\
.hm-thumb img{max-width:100%;max-height:96px;object-fit:contain}\
.hm-thumb span{position:absolute;right:3px;bottom:3px;background:rgba(0,0,0,.6);color:#fff;font-size:10px;padding:0 3px;border-radius:2px}\
.hm-name{padding:3px 5px;font-size:11px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}\
.hm-empty{grid-column:1/-1;color:#777;padding:30px;text-align:center}\
.hm-drop{position:absolute;inset:6px;border:3px dashed #0058a8;background:rgba(0,88,168,.08);display:none;align-items:center;justify-content:center;font-size:16px;color:#0058a8;pointer-events:none}\
.hm-main.is-dragging .hm-drop{display:flex}\
.hm-foot{display:flex;align-items:center;gap:10px;padding:8px 14px;border-top:1px solid #ddd;background:#fafafa}\
.hm-sel{flex:1;display:flex;align-items:center;gap:8px;min-width:0}\
.hm-sel img{max-width:64px;max-height:40px;background:repeating-conic-gradient(#eee 0% 25%,#fff 0% 50%) 50%/10px 10px}\
.hm-sel code{white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-size:11px;color:#555}\
.hm-foot button{padding:6px 14px;border:1px solid #aaa;border-radius:3px;background:#fff;cursor:pointer;font:inherit}\
.hm-foot .hm-use{background:#0058a8;border-color:#0058a8;color:#fff;font-weight:bold}.hm-foot .hm-use:disabled{opacity:.5;cursor:default}\
.hm-up{display:flex;align-items:center;gap:6px}.hm-up select,.hm-up input[type=text]{padding:4px}\
.hm-msg{font-size:12px;color:#b00}';

    var labels = { all: 'All images', 'promo pictures': 'Promo pictures', 'room photos': 'Room photos' };
    var lastCategory = null;

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function injectStyle() {
        if (document.getElementById('hm-style')) return;
        var style = el('style');
        style.id = 'hm-style';
        style.textContent = STYLE;
        document.head.appendChild(style);
    }

    function upload(files, category) {
        var form = new FormData();
        Array.prototype.forEach.call(files, function (f, i) {
            // Pasted images arrive as "image.png", TinyMCE's "blobid0.png" or unnamed: give them a dated name.
            var name = f.name && !/^(image|blobid\d+|mceclip\d+|imagetools\d+)\.\w+$/i.test(f.name) ? f.name : 'pasted_' + new Date().toISOString().replace(/[-:T]/g, '').slice(0, 14) + (i ? '_' + i : '') + '.' + ((f.type || 'image/png').split('/')[1] || 'png').replace('jpeg', 'jpg');
            form.append('files', f, name);
        });
        form.append('category', category || 'content');
        return fetch('/housekeeping/media/upload', { method: 'POST', body: form, credentials: 'same-origin' }).then(function (r) {
            return r.json().then(function (data) {
                if (!r.ok) throw new Error(data.error || r.statusText);
                if (!data.items.length) throw new Error(data.errors.join('; ') || 'Nothing uploaded');
                return data;
            });
        });
    }

    function open(options) {
        options = options || {};
        injectStyle();
        var state = { items: [], categories: [], canUpload: false, category: options.category || lastCategory || 'all', query: '', selected: null,
            folders: [], folderItems: {}, siteResults: [] };

        var overlay = el('div', 'hm-overlay');
        var panel = el('div', 'hm-panel');
        panel.setAttribute('role', 'dialog');
        panel.setAttribute('aria-modal', 'true');
        var head = el('div', 'hm-head');
        head.appendChild(el('h2', null, options.title || 'Choose an image'));
        var search = el('input');
        search.type = 'search';
        search.placeholder = 'Search by name';
        head.appendChild(search);
        var close = el('button', 'hm-close', '×');
        close.type = 'button';
        close.title = 'Close (Esc)';
        head.appendChild(close);

        var body = el('div', 'hm-body');
        var cats = el('div', 'hm-cats');
        var main = el('div', 'hm-main');
        var grid = el('div', 'hm-grid');
        var drop = el('div', 'hm-drop', 'Drop images to upload them');
        main.appendChild(grid);
        main.appendChild(drop);
        body.appendChild(cats);
        body.appendChild(main);

        var foot = el('div', 'hm-foot');
        var sel = el('div', 'hm-sel');
        var up = el('div', 'hm-up');
        var fileInput = el('input');
        fileInput.type = 'file';
        fileInput.accept = 'image/png,image/gif,image/jpeg,image/webp';
        fileInput.multiple = true;
        fileInput.style.display = 'none';
        var upButton = el('button', null, 'Upload...');
        upButton.type = 'button';
        var upCategory = el('select');
        var msg = el('span', 'hm-msg');
        up.appendChild(upButton);
        up.appendChild(el('span', null, 'to'));
        up.appendChild(upCategory);
        up.appendChild(fileInput);
        var cancel = el('button', null, 'Cancel');
        cancel.type = 'button';
        var use = el('button', 'hm-use', 'Use image');
        use.type = 'button';
        use.disabled = true;
        foot.appendChild(sel);
        foot.appendChild(msg);
        foot.appendChild(up);
        foot.appendChild(cancel);
        foot.appendChild(use);

        panel.appendChild(head);
        panel.appendChild(body);
        panel.appendChild(foot);
        overlay.appendChild(panel);
        document.body.appendChild(overlay);
        search.focus();

        function finish(item) {
            document.removeEventListener('keydown', onKey, true);
            document.removeEventListener('paste', onPaste, true);
            overlay.remove();
            if (item && options.onSelect) options.onSelect(item);
        }

        function isSite(category) { return category.indexOf('site:') === 0; }

        function visible() {
            var q = state.query.toLowerCase();
            var match = function (i) { return !q || i.name.toLowerCase().indexOf(q) >= 0 || i.url.toLowerCase().indexOf(q) >= 0; };
            if (isSite(state.category)) return (state.folderItems[state.category] || []).filter(match);
            var own = state.items.filter(function (i) { return (state.category === 'all' || i.category === state.category) && match(i); });
            return state.category === 'all' && q ? own.concat(state.siteResults) : own;
        }

        // Site folders load when opened.
        function openFolder(category) {
            if (state.folderItems[category]) return Promise.resolve(state.folderItems[category]);
            return fetch('/housekeeping/media/folder?path=' + encodeURIComponent(category.slice(5)), { credentials: 'same-origin' })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (items) { state.folderItems[category] = items; return items; });
        }

        function selectCategory(category) {
            state.category = category;
            lastCategory = category;
            renderCats();
            syncUploadCategory();
            if (isSite(category) && !state.folderItems[category]) {
                grid.textContent = '';
                grid.appendChild(el('div', 'hm-empty', 'Loading...'));
                openFolder(category).then(function () { if (state.category === category) renderGrid(); });
            } else {
                renderGrid();
            }
        }

        function renderCats() {
            cats.textContent = '';
            var counts = { all: state.items.length };
            state.items.forEach(function (i) { counts[i.category] = (counts[i.category] || 0) + 1; });
            ['all'].concat(state.categories, ['promo pictures', 'room photos']).forEach(function (c) {
                if (c !== 'all' && !counts[c] && state.categories.indexOf(c) < 0) return;
                var b = el('button', c === state.category ? 'is-active' : null);
                b.type = 'button';
                b.appendChild(el('span', null, labels[c] || c));
                b.appendChild(el('small', null, String(counts[c] || 0)));
                b.addEventListener('click', function () { selectCategory(c); });
                cats.appendChild(b);
            });
            if (state.folders.length) {
                cats.appendChild(el('h4', null, 'Site images (read only)'));
                state.folders.forEach(function (f) {
                    var c = 'site:' + f.path;
                    var b = el('button', 'hm-folder' + (c === state.category ? ' is-active' : ''));
                    b.type = 'button';
                    b.title = '/' + f.path;
                    b.appendChild(el('span', null, f.path.replace(/^images\//, '')));
                    b.appendChild(el('small', null, String(f.count)));
                    b.addEventListener('click', function () { selectCategory(c); });
                    cats.appendChild(b);
                });
            }
            var active = cats.querySelector('.is-active');
            if (active && active.scrollIntoView) active.scrollIntoView({ block: 'nearest' });
        }

        function renderSelection() {
            sel.textContent = '';
            var item = state.selected;
            use.disabled = !item;
            if (!item) return;
            var img = el('img');
            img.src = item.url;
            sel.appendChild(img);
            var text = el('div');
            text.appendChild(el('strong', null, item.name));
            text.appendChild(el('br'));
            text.appendChild(el('code', null, item.url.replace(/\?v=\d+$/, '') + (item.width ? ' · ' + item.width + 'x' + item.height : '')));
            sel.appendChild(text);
        }

        function renderGrid() {
            grid.textContent = '';
            var items = visible();
            if (!items.length) {
                grid.appendChild(el('div', 'hm-empty', state.loaded ? (isSite(state.category) ? 'Nothing matches in this folder.' : 'Nothing matches. Drop an image here to upload it.') : 'Loading...'));
                return;
            }
            items.forEach(function (item) {
                var tile = el('button', 'hm-tile' + (state.selected === item ? ' is-selected' : ''));
                tile.type = 'button';
                tile.title = item.name + '\n' + item.url.replace(/\?v=\d+$/, '');
                var thumb = el('div', 'hm-thumb');
                var img = el('img');
                img.loading = 'lazy';
                img.alt = '';
                img.src = item.url;
                var size = el('span');
                img.onload = function () {
                    item.width = img.naturalWidth;
                    item.height = img.naturalHeight;
                    size.textContent = item.width + 'x' + item.height;
                    if (state.selected === item) renderSelection();
                };
                thumb.appendChild(img);
                thumb.appendChild(size);
                tile.appendChild(thumb);
                tile.appendChild(el('div', 'hm-name', item.name));
                tile.addEventListener('click', function () {
                    state.selected = item;
                    grid.querySelectorAll('.is-selected').forEach(function (t) { t.classList.remove('is-selected'); });
                    tile.classList.add('is-selected');
                    renderSelection();
                });
                tile.addEventListener('dblclick', function () { finish(item); });
                grid.appendChild(tile);
            });
        }

        function syncUploadCategory() {
            upCategory.textContent = '';
            var list = state.categories.slice();
            if (list.indexOf('content') < 0) list.push('content');
            list.forEach(function (c) {
                var o = el('option', null, c);
                o.value = c;
                upCategory.appendChild(o);
            });
            var preferred = state.categories.indexOf(state.category) >= 0 ? state.category : (options.category && list.indexOf(options.category) >= 0 ? options.category : 'content');
            upCategory.value = preferred;
            up.style.display = state.canUpload ? '' : 'none';
        }

        function doUpload(files) {
            if (!state.canUpload || !files || !files.length) return;
            msg.textContent = 'Uploading...';
            upload(files, upCategory.value).then(function (data) {
                msg.textContent = data.errors.length ? data.errors.join('; ') : '';
                data.items.forEach(function (item) { state.items.unshift(item); });
                if (state.categories.indexOf(upCategory.value) < 0) state.categories.push(upCategory.value);
                state.category = upCategory.value;
                state.query = '';
                search.value = '';
                state.selected = data.items[0];
                renderCats();
                renderGrid();
                renderSelection();
            }).catch(function (e) { msg.textContent = e.message; });
        }

        function onKey(e) {
            if (e.key === 'Escape') { e.stopPropagation(); finish(null); }
            else if (e.key === 'Enter' && state.selected && document.activeElement !== search) { e.preventDefault(); finish(state.selected); }
        }
        function onPaste(e) {
            var files = [];
            Array.prototype.forEach.call((e.clipboardData && e.clipboardData.items) || [], function (i) { if (i.kind === 'file' && /^image\//.test(i.type)) files.push(i.getAsFile()); });
            if (files.length) { e.preventDefault(); doUpload(files); }
        }
        document.addEventListener('keydown', onKey, true);
        document.addEventListener('paste', onPaste, true);

        close.addEventListener('click', function () { finish(null); });
        cancel.addEventListener('click', function () { finish(null); });
        overlay.addEventListener('mousedown', function (e) { if (e.target === overlay) finish(null); });
        use.addEventListener('click', function () { finish(state.selected); });
        var siteTimer = null, siteSeq = 0;
        search.addEventListener('input', function () {
            state.query = search.value.trim();
            state.siteResults = [];
            renderGrid();
            clearTimeout(siteTimer);
            if (state.category !== 'all' || state.query.length < 2) return;
            var query = state.query, seq = ++siteSeq;
            siteTimer = setTimeout(function () {
                fetch('/housekeeping/media/searchsite?query=' + encodeURIComponent(query), { credentials: 'same-origin' })
                    .then(function (r) { return r.ok ? r.json() : []; })
                    .then(function (items) { if (seq === siteSeq && state.query === query) { state.siteResults = items; renderGrid(); } });
            }, 250);
        });
        upButton.addEventListener('click', function () { fileInput.click(); });
        fileInput.addEventListener('change', function () { doUpload(fileInput.files); fileInput.value = ''; });
        main.addEventListener('dragover', function (e) { if (state.canUpload) { e.preventDefault(); main.classList.add('is-dragging'); } });
        main.addEventListener('dragleave', function (e) { if (e.target === main || !main.contains(e.relatedTarget)) main.classList.remove('is-dragging'); });
        main.addEventListener('drop', function (e) { e.preventDefault(); main.classList.remove('is-dragging'); doUpload(e.dataTransfer.files); });

        renderGrid();
        var json = function (url) {
            return fetch(url, { credentials: 'same-origin' }).then(function (r) {
                if (!r.ok) throw new Error(r.status === 403 || r.redirected ? 'No access' : r.statusText);
                return r.json();
            });
        };
        Promise.all([json('/housekeeping/media/list'), json('/housekeeping/media/folders').catch(function () { return []; })]).then(function (results) {
            var data = results[0];
            state.items = data.items;
            state.categories = data.categories;
            state.canUpload = data.canUpload;
            state.folders = results[1];
            state.loaded = true;
            var known = function (c) {
                return c === 'all' || state.items.some(function (i) { return i.category === c; }) || data.categories.indexOf(c) >= 0 ||
                    state.folders.some(function (f) { return 'site:' + f.path === c; });
            };
            var current = (options.current || '').replace(/\?v=\d+$/, '').replace(/^https?:\/\/[^\/]+/, '');
            var same = function (i) { return i.url.replace(/\?v=\d+$/, '') === current; };
            // The current image: in the library, or in one of the site folders.
            var own = current ? state.items.filter(same)[0] : null;
            var folder = current && !own ? current.replace(/^\//, '').replace(/\/[^\/]*$/, '') : '';
            if (own) state.selected = own;
            else if (folder && state.folders.some(function (f) { return f.path.toLowerCase() === folder.toLowerCase(); })) state.category = 'site:' + folder;
            if (!known(state.category)) state.category = 'all';
            var ready = isSite(state.category) ? openFolder(state.category) : Promise.resolve();
            return ready.then(function () {
                if (!state.selected && current && isSite(state.category)) state.selected = (state.folderItems[state.category] || []).filter(same)[0] || null;
                renderCats();
                renderGrid();
                renderSelection();
                syncUploadCategory();
            });
        }).catch(function (e) {
            grid.textContent = '';
            grid.appendChild(el('div', 'hm-empty', 'The library could not be loaded: ' + e.message));
        });
    }

    window.HabboMedia = { open: open, upload: upload };
})();

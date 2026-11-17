// Edit mode for staff on the site's own pages (CMS pages: PageController + Views/Shared/PageDesigns). Loaded by
// _Layout.cshtml for users with housekeeping_pages, configured by window.cmsEditConfig. Off: an "Edit page" button.
// On (cookie cms_edit=1): every box (Views/Shared/Containers/Container.cshtml wraps them in .cms-box) gets tools to edit,
// move, hide and delete it, every column an "Add box" button, and the promo area tools for its promos.
//
// Editing happens in a side panel: the housekeeping editor in an iframe (?embed=1, _HousekeepingEmbed.cshtml). While
// it is open, the page previews the unsaved content live: boxes are re-rendered by the site's own box template
// (PageEditController.Preview), promos are updated in place. A form posted in the panel that leads to another page
// means it was saved: the page reloads where it was.
//
// The site runs Prototype next to this, so this sticks to plain DOM calls and posts form-encoded data, not JSON.
(function () {
    var cfg = window.cmsEditConfig;
    if (!cfg) return;

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function reload() {
        try { sessionStorage.setItem('cms_scroll', String(window.scrollY)); } catch (e) { }
        location.reload();
    }

    function setEditing(on) {
        document.cookie = 'cms_edit=' + (on ? '1' : '0') + '; path=/; samesite=lax' + (on ? '' : '; max-age=0');
        reload();
    }

    function toast(message) {
        var t = el('div', 'cms-toast', message);
        document.body.appendChild(t);
        setTimeout(function () { t.remove(); }, 4000);
    }

    function post(url, data) {
        var body = new URLSearchParams();
        Object.keys(data).forEach(function (k) { body.append(k, data[k] == null ? '' : data[k]); });
        return fetch(url, { method: 'POST', credentials: 'same-origin', body: body, headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(function (r) {
            if (r.redirected || r.status === 401 || r.status === 403) throw new Error('No access (logged out?)');
            if (!r.ok) throw new Error(r.statusText || ('HTTP ' + r.status));
            return r;
        });
    }

    try {
        var scroll = sessionStorage.getItem('cms_scroll');
        if (scroll !== null) {
            sessionStorage.removeItem('cms_scroll');
            window.addEventListener('load', function () { window.scrollTo(0, +scroll); });
        }
    } catch (e) { }

    if (!cfg.editing) {
        var toggle = el('button', 'cms-toggle', '✎ Edit page');
        toggle.type = 'button';
        toggle.title = 'Edit this page right here: boxes, promos and their order';
        toggle.addEventListener('click', function () { setEditing(true); });
        document.body.appendChild(toggle);
        return;
    }

    // ---- Edit mode ----
    document.body.classList.add('cms-editing');
    var bar = el('div', 'cms-bar');
    bar.appendChild(el('span', null, '✎ Editing'));
    bar.appendChild(el('strong', null, cfg.pageName));
    bar.appendChild(el('span', null, 'Hover a box for its tools. Changes show here as you type; nothing is saved until you press Submit.'));
    bar.appendChild(el('span', 'cms-bar-spacer'));
    var manage = el('a', null, 'Page in housekeeping');
    manage.href = '/housekeeping/pages/manage/' + cfg.pageId;
    manage.target = '_blank';
    bar.appendChild(manage);
    var done = el('button', null, 'Done editing');
    done.type = 'button';
    done.addEventListener('click', function () { setEditing(false); });
    bar.appendChild(done);
    document.body.appendChild(bar);

    // ---- Side panel ----
    var drawer = el('div', 'cms-drawer');
    var head = el('div', 'cms-drawer-head');
    var headTitle = el('span');
    var close = el('button', null, '×');
    close.type = 'button';
    close.title = 'Close without saving';
    head.appendChild(headTitle);
    head.appendChild(close);
    var frame = el('iframe');
    frame.title = 'Editor';
    drawer.appendChild(head);
    drawer.appendChild(frame);
    document.body.appendChild(drawer);

    var session = null; // { preview, restore, submitted, path }

    function openDrawer(url, title, preview) {
        closeDrawer(true);
        session = { preview: preview || null, restore: preview && preview.snapshot ? preview.snapshot() : null, submitted: false, path: null };
        headTitle.textContent = '';
        headTitle.appendChild(document.createTextNode(title + ' '));
        headTitle.appendChild(el('small', null, 'live preview on the page'));
        frame.src = url + (url.indexOf('?') >= 0 ? '&' : '?') + 'embed=1';
        drawer.classList.add('cms-open');
        document.body.classList.add('cms-drawer-open');
        if (preview && preview.target) {
            preview.target.classList.add('cms-box-active');
            preview.target.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }

    function closeDrawer(discard) {
        if (!session) return;
        if (discard && session.restore) session.restore();
        if (session.preview && session.preview.target) session.preview.target.classList.remove('cms-box-active');
        if (session.preview && session.preview.cleanup) session.preview.cleanup();
        session = null;
        drawer.classList.remove('cms-open');
        document.body.classList.remove('cms-drawer-open');
        frame.src = 'about:blank';
    }
    close.addEventListener('click', function () {
        // Lists (e.g. all promos) can change things without a form (reordering): show the page as it is now.
        var list = session && !session.preview;
        closeDrawer(true);
        if (list) reload();
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && session && !document.querySelector('.hm-overlay')) closeDrawer(true);
    });

    frame.addEventListener('load', function () {
        if (!session) return;
        var win = frame.contentWindow, path;
        try { path = win.location.pathname.toLowerCase(); } catch (e) { return; }
        if (path === 'blank') return;
        // Saved: housekeeping redirects to its lists with ?message=... ("Container updated", "Promo edited"). A form
        // that comes back without it had a validation error and stays open.
        if (session.submitted && /[?&]message=/.test(win.location.search)) { closeDrawer(false); reload(); return; }
        session.submitted = false;
        session.path = path;
        win.document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('submit', function () { if (session) session.submitted = true; });
        });
        if (session.preview && session.preview.wire) session.preview.wire(win);
    });

    function debounce(fn, ms) {
        var t = null;
        return function () { clearTimeout(t); t = setTimeout(fn, ms); };
    }

    // ---- Boxes ----
    function boxContent(box) {
        return Array.prototype.filter.call(box.childNodes, function (n) { return !(n.classList && n.classList.contains('cms-tools')); });
    }

    function boxPreview(box, pageId, id) {
        var original = boxContent(box).map(function (n) { return n.cloneNode(true); });
        var seq = 0;
        return {
            target: box,
            snapshot: function () {
                return function () {
                    if (box.classList.contains('cms-box-new')) { box.remove(); return; }
                    boxContent(box).forEach(function (n) { n.remove(); });
                    original.forEach(function (n) { box.appendChild(n); });
                };
            },
            wire: function (win) {
                var form = win.document.querySelector('form');
                if (!form) return;
                var field = function (name) { return form.querySelector('[name="' + name + '"]'); };
                var render = debounce(function () {
                    var editor = win.tinymce && win.tinymce.get('editor');
                    var mine = ++seq;
                    post('/housekeeping/pageedit/preview', {
                        id: id, pageId: pageId,
                        title: field('Title') ? field('Title').value : '',
                        text: editor ? editor.getContent() : (field('Text') ? field('Text').value : ''),
                        theme: field('Theme') ? field('Theme').value : '',
                        type: field('Type') ? field('Type').value : ''
                    }).then(function (r) { return r.text(); }).then(function (html) {
                        if (mine !== seq || !session) return;
                        boxContent(box).forEach(function (n) { n.remove(); });
                        var holder = document.createElement('div');
                        holder.innerHTML = html;
                        while (holder.firstChild) box.appendChild(holder.firstChild);
                    }).catch(function (e) { toast('Preview failed: ' + e.message); });
                }, 300);
                ['Title', 'Theme', 'Type'].forEach(function (name) {
                    var f = field(name);
                    if (f) { f.addEventListener('input', render); f.addEventListener('change', render); }
                });
                // TinyMCE starts after the page: wait for it, then follow every change.
                var tries = 0;
                (function hook() {
                    var editor = win.tinymce && win.tinymce.get('editor');
                    if (editor && editor.initialized) {
                        editor.on('input change keyup undo redo SetContent ExecCommand', render);
                        if (box.classList.contains('cms-box-new')) render();
                    } else if (tries++ < 100) {
                        setTimeout(hook, 100);
                    }
                })();
            }
        };
    }

    function addBoxTools(box) {
        var id = box.getAttribute('data-cms-box');
        var state = box.getAttribute('data-cms-state');
        var title = box.getAttribute('data-cms-title') || 'box';
        var tools = el('div', 'cms-tools');
        if (state) tools.appendChild(el('span', 'cms-badge' + (state === 'scheduled' ? ' cms-badge-scheduled' : ''), state === 'hidden' ? 'Hidden' : 'Not scheduled now'));
        var add = function (label, tip, action, className) {
            var b = el('button', className || null, label);
            b.type = 'button';
            b.title = tip;
            b.addEventListener('click', function (e) { e.preventDefault(); e.stopPropagation(); action(b); });
            tools.appendChild(b);
            return b;
        };
        add('✎ Edit', 'Edit "' + title + '"', function () {
            openDrawer('/housekeeping/pages/updatecontainer/' + id, 'Edit box: ' + title, boxPreview(box, cfg.pageId, id));
        });
        var move = function (direction) {
            return function () {
                post('/housekeeping/pageedit/move', { id: id, direction: direction }).then(function (r) { return r.json(); }).then(function (data) {
                    if (data.moved) reload(); else toast('It can\'t move further that way.');
                }).catch(function (e) { toast(e.message); });
            };
        };
        add('↑', 'Move up', move('up'));
        add('↓', 'Move down', move('down'));
        add('←', 'Move to the column on the left', move('left'));
        add('→', 'Move to the column on the right', move('right'));
        add(state === 'hidden' ? 'Show' : 'Hide', state === 'hidden' ? 'Show the box to visitors' : 'Hide the box from visitors (in edit mode it stays, dimmed)', function () {
            post('/housekeeping/pageedit/toggle', { id: id }).then(reload).catch(function (e) { toast(e.message); });
        });
        add('×', 'Delete the box', function (b) {
            if (!b.classList.contains('cms-confirm')) {
                b.classList.add('cms-confirm');
                b.textContent = 'Delete? Click again';
                setTimeout(function () { b.classList.remove('cms-confirm'); b.textContent = '×'; }, 3000);
                return;
            }
            post('/housekeeping/pageedit/delete', { id: id }).then(reload).catch(function (e) { toast(e.message); });
        }, 'cms-danger');
        box.insertBefore(tools, box.firstChild);
    }

    document.querySelectorAll('[data-cms-box]').forEach(addBoxTools);

    document.querySelectorAll('td[data-cms-column]').forEach(function (column) {
        var number = column.getAttribute('data-cms-column');
        var add = el('button', 'cms-add', '+ Add box');
        add.type = 'button';
        add.addEventListener('click', function () {
            var box = el('div', 'cms-box cms-box-new');
            column.insertBefore(box, add);
            openDrawer('/housekeeping/pages/createcontainer/' + cfg.pageId + '?column=' + number, 'New box in column ' + number, boxPreview(box, cfg.pageId, 0));
        });
        column.appendChild(add);
    });

    // ---- Promos ----
    var area = document.getElementById('promoarea');
    if (area && area.getAttribute('data-cms-page')) {
        var pageId = area.getAttribute('data-cms-page');
        var ids = (area.getAttribute('data-cms-promos') || '').split(',').filter(function (x) { return x; });
        var shown = function () { return typeof window.currentPromo === 'number' ? window.currentPromo : 0; };

        var promoPreview = function () {
            var image = document.getElementById('promoimage');
            var text = document.getElementById('promotext-content');
            var links = document.getElementById('promolinks-list');
            var saved = { image: image ? image.innerHTML : '', text: text ? text.innerHTML : '', links: links ? links.innerHTML : '' };
            return {
                target: area,
                snapshot: function () {
                    return function () {
                        if (image) image.innerHTML = saved.image;
                        if (text) text.innerHTML = saved.text;
                        if (links) links.innerHTML = saved.links;
                    };
                },
                wire: function (win) {
                    var form = win.document.querySelector('form');
                    if (!form || !image) return;
                    var value = function (id) { var f = win.document.getElementById(id); return f ? f.value : ''; };
                    var render = function () {
                        image.textContent = '';
                        var img = el('img');
                        img.alt = '';
                        if (value('Img')) img.src = value('Img');
                        image.appendChild(img);
                        if (text) text.textContent = value('Teaser');
                        if (links) {
                            links.textContent = '';
                            [['Link1Text', 'Link1Href'], ['Link2Text', 'Link2Href']].forEach(function (pair) {
                                if (!value(pair[0])) return;
                                var a = el('a', null, value(pair[0]));
                                a.href = value(pair[1]) || '#';
                                links.appendChild(el('li')).appendChild(a);
                            });
                        }
                    };
                    ['Img', 'Teaser', 'Link1Text', 'Link1Href', 'Link2Text', 'Link2Href'].forEach(function (id) {
                        var f = win.document.getElementById(id);
                        if (f) { f.addEventListener('input', render); f.addEventListener('change', render); }
                    });
                    if (/\/create$/.test(win.location.pathname.toLowerCase())) render();
                }
            };
        };

        var tools = el('div', 'cms-tools cms-promo-tools');
        var add = function (label, tip, action) {
            var b = el('button', null, label);
            b.type = 'button';
            b.title = tip;
            b.addEventListener('click', function (e) { e.preventDefault(); action(); });
            tools.appendChild(b);
        };
        if (ids.length) {
            add('✎ Edit this promo', 'Edit the promo shown now', function () {
                openDrawer('/housekeeping/promo/update/' + ids[shown()], 'Edit promo ' + (shown() + 1), promoPreview());
            });
        }
        add('+ New promo', 'Add a promo to this page', function () {
            openDrawer('/housekeeping/promo/create?pageId=' + pageId, 'New promo', promoPreview());
        });
        add('All promos', 'Order, hide and edit every promo of this page', function () {
            openDrawer('/housekeeping/promo?pageId=' + pageId, 'Promos', null);
        });
        tools.insertBefore(el('span', 'cms-tools-label', 'Promos'), tools.firstChild);
        area.parentNode.insertBefore(tools, area);
    }
})();

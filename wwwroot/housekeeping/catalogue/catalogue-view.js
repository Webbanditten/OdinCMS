// The in-game catalogue, rebuilt in HTML from the client's own data (see tools/catalogue/build_assets.py):
// layouts.json holds every catalogue window (habbo_catalogue + the ctlg_* page layouts) with each element's member,
// position, size, ink and text style; members/ and images/ are the cast bitmaps. The page logic follows the client's
// "Catalogue Interface Class" (hh_cat_code): which page field goes into which element, the page index, product icons
// and paging, product selection, the special text badge.
//
// new CatalogueView(container, options).load() then .render(state):
//   state.index    [{ id, name, hidden }]           the page index (right column)
//   state.pageId   the selected page's id
//   state.page     { layout, headline, teasers[], body, labelPick, labelExtraS, texts{n: text}, links[] }
//   state.products [{ name, description, price, sprite, colour, isPackage, packages: [{ sprite, amount }] }]
//   state.purse    credits shown in the purse line
// options: gameData (from CatalogueController.GameData), onSelectPage(id), onField(field), editMode
(function () {
    'use strict';

    var BASE = '/housekeeping/catalogue/';
    var assets = null;
    var inkCache = {};

    function loadAssets() {
        if (assets) return Promise.resolve(assets);
        return Promise.all([
            fetch(BASE + 'layouts.json').then(function (r) { return r.json(); }),
            fetch(BASE + 'images.json').then(function (r) { return r.json(); })
        ]).then(function (results) {
            assets = { windows: results[0].windows, members: results[0].members, images: results[1] };
            return assets;
        });
    }

    // Director inks on a canvas: 36 (background transparent) drops white, 8 (matte) drops the white connected to the
    // edges. The exported bitmaps are opaque with white backgrounds.
    function inked(src, ink, remote) {
        if (ink !== 8 && ink !== 36) return Promise.resolve(src);
        var key = src + '|' + ink;
        if (inkCache[key]) return inkCache[key];
        inkCache[key] = new Promise(function (resolve) {
            var img = new Image();
            if (remote) img.crossOrigin = 'anonymous';
            img.onload = function () {
                try {
                    var canvas = document.createElement('canvas');
                    canvas.width = img.naturalWidth;
                    canvas.height = img.naturalHeight;
                    var ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0);
                    var data = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    var px = data.data, w = canvas.width, h = canvas.height;
                    var white = function (i) { return px[i] === 255 && px[i + 1] === 255 && px[i + 2] === 255; };
                    if (ink === 36) {
                        for (var i = 0; i < px.length; i += 4) if (white(i)) px[i + 3] = 0;
                    } else {
                        var stack = [], seen = new Uint8Array(w * h);
                        for (var x = 0; x < w; x++) { stack.push(x, 0); stack.push(x, h - 1); }
                        for (var y = 0; y < h; y++) { stack.push(0, y); stack.push(w - 1, y); }
                        while (stack.length) {
                            var sy = stack.pop(), sx = stack.pop();
                            if (sx < 0 || sy < 0 || sx >= w || sy >= h) continue;
                            var p = sy * w + sx;
                            if (seen[p]) continue;
                            seen[p] = 1;
                            if (!white(p * 4)) continue;
                            px[p * 4 + 3] = 0;
                            stack.push(sx + 1, sy, sx - 1, sy, sx, sy + 1, sx, sy - 1);
                        }
                    }
                    ctx.putImageData(data, 0, 0);
                    resolve(canvas.toDataURL());
                } catch (e) {
                    resolve(src); // a remote image without CORS: show it as it is
                }
            };
            img.onerror = function () { resolve(null); };
            img.src = src;
        });
        return inkCache[key];
    }

    function el(tag, className, styles) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (styles) Object.keys(styles).forEach(function (k) { node.style[k] = styles[k]; });
        return node;
    }

    function box(e, offset) {
        return { left: (offset[0] + e.locH) + 'px', top: (offset[1] + e.locV) + 'px', width: e.width + 'px', height: e.height + 'px' };
    }

    function memberUrl(member) {
        var file = assets.members[(member || '').trim()];
        return file ? BASE + 'members/' + file : null;
    }

    // Where the client finds a page image: a cast member first, else the image library in the client's language.
    // known = the image is a cast member or was found in the library; otherwise the library URL is still tried, as the
    // client does (an image uploaded a moment ago shows up without waiting for the server's cache).
    function pageImageUrl(name, gameData) {
        if (!name) return null;
        if (assets.images[name]) return { url: BASE + 'images/' + assets.images[name].file, remote: false, known: true };
        if (!gameData || !gameData.imageLibraryUrl) return null;
        return {
            url: gameData.imageLibraryUrl + 'catalogue/' + encodeURIComponent(name) + '_' + gameData.language + '.gif',
            remote: true, known: (gameData.libraryImages || []).indexOf(name) >= 0
        };
    }

    function furniUrl(sprite, extra) {
        var meta = document.querySelector('meta[name="habbo-furni-url"]');
        var base = (meta && meta.content) || '/habbo-imaging/furni';
        var parts = (sprite || '').split('*');
        return base + '?sprite=' + encodeURIComponent(parts[0]) + (parts[1] ? '&color=' + parts[1] : '') + (extra || '');
    }

    // The game colours furni with the definition's part colours ("#FFFFFF,#533E10,..."). The Flash imager draws them
    // uncoloured: multiply its picture by the first non-white part colour, which is how plastic, pillows etc. look.
    // The furni renderer (meta habbo-furni-coloured = 1) already draws them with their colours.
    var furniColoured = (function () { var m = document.querySelector('meta[name="habbo-furni-coloured"]'); return !!m && m.content === '1'; })();
    var tintCache = {};
    function tintColour(colours) {
        if (furniColoured) return null;
        var list = String(colours || '').split(',').map(function (c) { return c.trim().toUpperCase(); });
        for (var i = 0; i < list.length; i++) if (/^#[0-9A-F]{6}$/.test(list[i]) && list[i] !== '#FFFFFF') return list[i];
        return null;
    }
    function furniImage(img, sprite, colours, extra) {
        var url = furniUrl(sprite, extra);
        var tint = tintColour(colours);
        // Furni without graphics: the game shows "no_icon_small" for the icon (Catalogue Component smallPrewImg).
        img.onerror = /icon=true/.test(extra || '') ? function () { img.onerror = null; img.src = memberUrl('no_icon_small') || ''; } : null;
        img.src = url;
        if (!tint) return;
        var key = url + '|' + tint;
        tinted(url, tint).then(function (dataUrl) { if (dataUrl && img.getAttribute('data-src') === key) img.src = dataUrl; });
        img.setAttribute('data-src', key);
    }

    function tinted(url, tint) {
        var key = url + '|' + tint;
        tintCache[key] = tintCache[key] || new Promise(function (resolve) {
            var source = new Image();
            source.crossOrigin = 'anonymous';
            source.onload = function () {
                try {
                    var canvas = document.createElement('canvas');
                    canvas.width = source.naturalWidth; canvas.height = source.naturalHeight;
                    var ctx = canvas.getContext('2d');
                    ctx.drawImage(source, 0, 0);
                    var data = ctx.getImageData(0, 0, canvas.width, canvas.height), px = data.data;
                    var r = parseInt(tint.substr(1, 2), 16), g = parseInt(tint.substr(3, 2), 16), b = parseInt(tint.substr(5, 2), 16);
                    for (var i = 0; i < px.length; i += 4) {
                        if (!px[i + 3]) continue;
                        if (Math.max(px[i], px[i + 1], px[i + 2]) - Math.min(px[i], px[i + 1], px[i + 2]) > 24) continue; // already coloured
                        px[i] = px[i] * r / 255; px[i + 1] = px[i + 1] * g / 255; px[i + 2] = px[i + 2] * b / 255;
                    }
                    ctx.putImageData(data, 0, 0);
                    resolve(canvas.toDataURL());
                } catch (e) { resolve(null); } // no CORS: keep the uncoloured picture
            };
            source.onerror = function () { resolve(null); };
            source.src = url;
        });
        return tintCache[key];
    }

    // ---- Package pictures (hh_cat_code "Deal Preview Class"), drawn on canvases like the client's image objects ----

    function loadImage(src) {
        return new Promise(function (resolve) {
            if (!src) { resolve(null); return; }
            var img = new Image();
            if (/^https?:/.test(src) && src.indexOf(location.origin) !== 0) img.crossOrigin = 'anonymous';
            img.onload = function () { resolve(img); };
            img.onerror = function () { resolve(null); };
            img.src = src;
        });
    }

    function canvasOf(width, height) {
        var canvas = document.createElement('canvas');
        canvas.width = Math.max(1, width); canvas.height = Math.max(1, height);
        return canvas;
    }

    // Ink 36 on the way out: white becomes see-through.
    function whiteToAlpha(canvas) {
        var ctx = canvas.getContext('2d'), data = ctx.getImageData(0, 0, canvas.width, canvas.height), px = data.data;
        for (var i = 0; i < px.length; i += 4) if (px[i] === 255 && px[i + 1] === 255 && px[i + 2] === 255) px[i + 3] = 0;
        ctx.putImageData(data, 0, 0);
        return canvas;
    }

    // image.trimWhiteSpace(): crop to what isn't white (or see-through).
    function trimmed(canvas) {
        var ctx = canvas.getContext('2d'), w = canvas.width, h = canvas.height, px = ctx.getImageData(0, 0, w, h).data;
        var x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) {
            var i = (y * w + x) * 4;
            if (!px[i + 3] || (px[i] === 255 && px[i + 1] === 255 && px[i + 2] === 255)) continue;
            if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
        }
        if (x1 < 0) return canvasOf(1, 1);
        var out = canvasOf(x1 - x0 + 1, y1 - y0 + 1);
        out.getContext('2d').drawImage(canvas, x0, y0, out.width, out.height, 0, 0, out.width, out.height);
        return out;
    }

    // Lingo integer division (truncates towards zero).
    function idiv(a, b) { return Math.trunc(a / b); }

    // The furni's small icon in its part colours (Preview_renderer), or no_icon_small without graphics; white see-through.
    function furniIcon(sprite, colours) {
        var url = furniUrl(sprite, '&icon=true'), tint = tintColour(colours);
        return (tint ? tinted(url, tint).then(function (d) { return d || url; }) : Promise.resolve(url))
            .then(loadImage)
            .then(function (img) { return img || loadImage(memberUrl('no_icon_small')); })
            .then(function (img) {
                if (!img) return null;
                var canvas = canvasOf(img.naturalWidth, img.naturalHeight);
                canvas.getContext('2d').drawImage(img, 0, 0);
                return whiteToAlpha(canvas);
            });
    }

    // getNumberImage: ctlg_dyndeal_button_left + _center (stretched) + _right, digits from ctlg_dyndeal_N at (2, 3)
    // (leading zero dropped), trimmed; white is see-through where it is drawn (ink 36).
    var badgeCache = {};
    function countBadge(count) {
        if (badgeCache[count]) return badgeCache[count];
        var names = ['ctlg_dyndeal_button_left', 'ctlg_dyndeal_button_center', 'ctlg_dyndeal_button_right'];
        for (var d = 0; d < 10; d++) names.push('ctlg_dyndeal_' + d);
        badgeCache[count] = Promise.all(names.map(function (n) { return loadImage(memberUrl(n)); })).then(function (imgs) {
            var left = imgs[0], center = imgs[1], right = imgs[2], digitImg = function (d) { return d >= 0 ? imgs[3 + d] : null; };
            var digits = [count % 10, idiv(count, 10) % 10]; // [ones, tens]
            var width = 0;
            for (var i = 1; i >= 0; i--) {
                if (digits[i] === 0) { digits[i] = -1; continue; }
                width += i;
                break;
            }
            digits.forEach(function (d) { if (digitImg(d)) width += digitImg(d).naturalWidth; });
            var canvas = canvasOf(80, 20), ctx = canvas.getContext('2d');
            ctx.fillStyle = '#FFFFFF'; ctx.fillRect(0, 0, 80, 20);
            var start = 0;
            if (left) { ctx.drawImage(left, 0, 0); start = left.naturalWidth; }
            if (center) for (var x = start; x <= start + width; x++) ctx.drawImage(center, x, 0);
            if (right) ctx.drawImage(right, start + width + 1, 0);
            for (var k = 1; k >= 0; k--) {
                var img = digitImg(digits[k]);
                if (!img) continue;
                var digit = canvasOf(img.naturalWidth, img.naturalHeight);
                digit.getContext('2d').drawImage(img, 0, 0);
                ctx.drawImage(whiteToAlpha(digit), start + 2, 3);
                start += img.naturalWidth + 1;
            }
            return whiteToAlpha(trimmed(canvas));
        });
        return badgeCache[count];
    }

    // renderDealPreviewImage for one kind of furni: its icon (centred, at most 8 px down) with the count badge at
    // (2, 0), trimmed; the slot then centres it.
    function dealSlotPicture(part, slotWidth, slotHeight) {
        return Promise.all([furniIcon(part.sprite, part.colour), countBadge(part.amount || 1)]).then(function (r) {
            var icon = r[0], badge = r[1];
            var canvas = canvasOf(slotWidth, slotHeight), ctx = canvas.getContext('2d');
            if (icon) ctx.drawImage(icon, idiv(slotWidth - icon.width, 2), Math.min(8, slotHeight - icon.height));
            if (badge) ctx.drawImage(badge, 2, 0);
            var picture = trimmed(canvas);
            var slot = canvasOf(slotWidth, slotHeight);
            slot.getContext('2d').drawImage(picture, idiv(slotWidth - picture.width, 2), idiv(slotHeight - picture.height, 2));
            return slot.toDataURL();
        });
    }

    // getPicture: the package's furni (at most 3 x 2) in 39 px cells on ctlg_dyndeal_background, counts above 1
    // right-aligned at (41, 28) of the cell (catalogue.deal.* variables), white see-through, trimmed and centred.
    var DEAL = { cell: 39, columns: 3, rows: 2, left: 11, right: 15, top: 80, bottom: 20, numberX: 41, numberY: 28 };
    function dealPreviewPicture(parts, width, height) {
        var shown = parts.slice(0, DEAL.columns * DEAL.rows);
        return Promise.all([loadImage(memberUrl('ctlg_dyndeal_background'))]
            .concat(shown.map(function (p) { return furniIcon(p.sprite, p.colour); }))
            .concat(shown.map(function (p) { return (p.amount || 1) > 1 ? countBadge(p.amount) : null; })))
            .then(function (r) {
                var background = r[0], icons = r.slice(1, 1 + shown.length), badges = r.slice(1 + shown.length);
                var canvas = canvasOf(DEAL.columns * DEAL.cell + 1 + DEAL.left + DEAL.right, DEAL.rows * DEAL.cell + 1 + DEAL.top + DEAL.bottom);
                var ctx = canvas.getContext('2d');
                ctx.fillStyle = '#FFFFFF'; ctx.fillRect(0, 0, canvas.width, canvas.height);
                if (background) ctx.drawImage(background, 0, 0);
                for (var i = shown.length; i >= 1; i--) {
                    var x = ((i - 1) % DEAL.columns) * DEAL.cell + DEAL.left, y = idiv(i - 1, DEAL.columns) * DEAL.cell + DEAL.top;
                    var icon = icons[i - 1], badge = badges[i - 1];
                    if (icon) ctx.drawImage(icon, x + idiv(DEAL.cell - icon.width, 2), y + idiv(DEAL.cell - icon.height, 2));
                    if (badge) ctx.drawImage(badge, x + DEAL.numberX - badge.width, y + DEAL.numberY - idiv(badge.height, 2));
                }
                var picture = trimmed(whiteToAlpha(canvas));
                var out = canvasOf(width, height);
                out.getContext('2d').drawImage(picture, idiv(width - picture.width, 2), idiv(height - picture.height, 2));
                return out.toDataURL();
            });
    }

    function fillCentered(node, src, ink, remote) {
        inked(src, ink, remote).then(function (url) {
            node.textContent = '';
            if (!url) return;
            var img = el('img', 'ctlg-centered');
            img.src = url;
            img.alt = '';
            node.appendChild(img);
        });
    }

    function textOf(gameData, key) {
        return key && gameData && gameData.texts && gameData.texts[key] !== undefined ? gameData.texts[key] : '';
    }

    function br(text) { return (text || '').replace(/<br\s*\/?>/gi, '\n'); }

    // Director wraps text only at spaces; browsers also break after hyphens ("Habbo-" / "pung"). Words with a hyphen
    // go in a no-wrap span.
    function writeText(node, text) {
        node.textContent = '';
        String(text || '').split(/( +|\n)/).forEach(function (part) {
            if (part.indexOf('-') > 0) {
                var word = document.createElement('span');
                word.style.whiteSpace = 'nowrap';
                word.textContent = part;
                node.appendChild(word);
            } else if (part) {
                node.appendChild(document.createTextNode(part));
            }
        });
    }

    function CatalogueView(container, options) {
        this.container = container;
        this.options = options || {};
        this.state = null;
        this.productOffset = 0;
        this.selected = -1;
    }

    CatalogueView.prototype.load = function () { return loadAssets(); };

    CatalogueView.layouts = function () {
        return Object.keys(assets.windows).filter(function (k) { return /^ctlg_/.test(k) && k !== 'ctlg_loading' && k !== 'ctlg_recycler_open'; }).sort();
    };

    // What a layout can show, for the editor: texts slots, teaser slots, product icons, and so on.
    CatalogueView.layoutInfo = function (layout) {
        var window = assets.windows[layout === 'ctlg_recycler' && assets.windows.ctlg_recycler_open ? 'ctlg_recycler_open' : layout];
        var info = { header: false, headerText: false, teasers: 0, texts: [], description: false, special: false, icons: 0, productSlots: 0, productName: false };
        if (!window) return info;
        window.elements.forEach(function (e) {
            var id = e.id || '';
            var m;
            if (id === 'ctlg_header_img') info.header = true;
            else if (id === 'ctlg_header_text') info.headerText = true;
            else if ((m = /^ctlg_teaserimg_(\d+)$/.exec(id))) info.teasers = Math.max(info.teasers, +m[1]);
            else if ((m = /^ctlg_text_(\d+)$/.exec(id))) info.texts.push(+m[1]);
            else if (id === 'ctlg_description') info.description = true;
            else if (id === 'ctlg_special_txt') info.special = true;
            else if ((m = /^ctlg_small_img_(\d+)$/.exec(id))) info.icons = Math.max(info.icons, +m[1]);
            else if ((m = /^ctlg_buy_(\d+)$/.exec(id))) info.productSlots = Math.max(info.productSlots, +m[1]);
            else if (id === 'ctlg_product_name') info.productName = true;
        });
        info.texts = info.texts.filter(function (n, i, a) { return a.indexOf(n) === i; }).sort(function (a, b) { return a - b; });
        return info;
    };

    CatalogueView.textKeys = function () {
        var keys = ['catalog_coins_amount', 'catalog_get_credits_link', 'credits', 'credit', 'catalog_selectproduct', 'catalog_select_product', 'catalog_page', 'buy', 'play_preview'];
        for (var pet = 0; pet < 10; pet++) keys.push('pet_race_' + pet + '_000');
        keys.push('recycler_info_open');
        Object.keys(assets.windows).forEach(function (k) {
            assets.windows[k].elements.forEach(function (e) { if (e.key) keys.push(e.key); });
        });
        return keys.filter(function (k, i, a) { return k && a.indexOf(k) === i; });
    };

    CatalogueView.pageImageUrl = function (name, gameData) { return pageImageUrl(name, gameData); };
    CatalogueView.castImages = function () { return assets.images; };
    CatalogueView.windowElements = function (layout) { return (assets.windows[layout] || { elements: [] }).elements; };

    CatalogueView.prototype.render = function (state) {
        var keepProduct = this.state && state && this.state.pageId === state.pageId && this.state.page && state.page && this.state.page.layout === state.page.layout;
        if (!keepProduct) { this.productOffset = 0; this.selected = -1; }
        this.state = state;
        var root = this.container;
        root.textContent = '';
        var frame = el('div', 'ctlg-window' + (this.options.editMode ? ' ctlg-edit' : ''));
        root.appendChild(frame);
        var main = assets.windows.habbo_catalogue;
        frame.style.width = main.width + 'px';
        frame.style.height = main.height + 'px';
        this.frame = frame;
        this.drawWindow(main, [0, 0]);
        var layout = state.page && assets.windows[state.page.layout];
        // The recycler page shows hh_recycler's "open" window in place of its layout (Recycler Interface Class).
        if (layout && state.page.layout === 'ctlg_recycler' && assets.windows.ctlg_recycler_open) layout = assets.windows.ctlg_recycler_open;
        if (layout) this.drawWindow(layout, layout.offset);
        else if (state.page) {
            var missing = el('div', 'ctlg-missing', { left: '20px', top: '60px' });
            missing.textContent = state.page.layout ? 'The client has no layout "' + state.page.layout + '".'
                : 'A folder: in the v31 catalogue it groups pages in the tree. The old catalogue doesn\'t show it.';
            frame.appendChild(missing);
        }
        this.fillPage();
    };

    CatalogueView.prototype.drawWindow = function (window, offset) {
        var self = this;
        var gameData = this.options.gameData;
        this.elements = this.elements || {};
        if (window === assets.windows.habbo_catalogue) this.elements = {};
        window.elements.forEach(function (e) {
            var node;
            var type = e.type;
            var member = (e.member || '').trim();
            if (type === 'piece' || (type === 'button' && member !== 'shadow.pixel' && memberUrl(member))) {
                if (member === 'shadow.pixel') {
                    node = el('div', 'ctlg-el', box(e, offset));
                    node.style.background = '#000';
                } else {
                    var url = memberUrl(member);
                    node = el(url ? 'img' : 'div', 'ctlg-el' + (url ? '' : ' ctlg-missing-member'), box(e, offset));
                    if (url) {
                        node.alt = '';
                        inked(url, e.ink, false).then(function (src) { if (src) node.src = src; });
                    }
                }
                if (e.blend !== undefined && e.blend < 100) node.style.opacity = e.blend / 100;
            } else if (type === 'pattern') {
                node = el('div', 'ctlg-el', box(e, offset));
                var purl = memberUrl(member);
                if (purl) inked(purl, e.ink, false).then(function (src) { if (src) node.style.backgroundImage = 'url(' + src + ')'; });
            } else if (type === 'text') {
                node = el('div', 'ctlg-el ctlg-text', box(e, offset));
                var bold = e.font === 'vb' || (e.fontStyle || []).indexOf('bold') >= 0;
                if (bold) node.classList.add('ctlg-bold');
                if ((e.fontStyle || []).indexOf('underline') >= 0) node.style.textDecoration = 'underline';
                var colour = e.txtColor || e.color;
                if (colour) node.style.color = /^[0-9A-Fa-f]{6}$/.test(colour) ? '#' + colour : colour;
                if (e.alignment) node.style.textAlign = e.alignment === 'center' ? 'center' : e.alignment === 'right' ? 'right' : 'left';
                if (e.fixedLineSpace) node.style.lineHeight = e.fixedLineSpace + 'px';
                else if (e.lineHeight) node.style.lineHeight = e.lineHeight + 'px';
                if (e.wordWrap === 0) node.style.whiteSpace = 'pre';
                // boxType #adjust grows the text member, but the element's sprite stays its size: text past it is cut.
                writeText(node, textOf(gameData, e.key));
            } else if (type === 'button') {
                // A text button (Common Button Class): left cap, stretched middle, right cap from button<model>.element,
                // bold Volter centred with the model's margins; the box grows with the text, anchored per alignment.
                node = el('div', 'ctlg-el ctlg-button-wrap', box(e, offset));
                var letter = { 1: 'a', 2: 'b', 3: 'c' }[e.model] || 'b';
                var button = el('span', 'ctlg-button ctlg-button-' + letter);
                var capL = el('img'); capL.src = memberUrl('button.' + letter + '.left.active') || ''; capL.alt = '';
                var capR = el('img'); capR.src = memberUrl('button.' + letter + '.right.active') || ''; capR.alt = '';
                var label = el('span', 'ctlg-button-label');
                label.style.backgroundImage = 'url(' + memberUrl('button.' + letter + '.middle.active') + ')';
                label.textContent = textOf(gameData, e.key) || e.key || '';
                button.appendChild(capL); button.appendChild(label); button.appendChild(capR);
                node.style.justifyContent = e.alignment === 'right' ? 'flex-end' : e.alignment === 'center' ? 'center' : 'flex-start';
                if (e.maxwidth) node.style.maxWidth = e.maxwidth + 'px';
                node.appendChild(button);
            } else if (type === 'image') {
                node = el('div', 'ctlg-el ctlg-image', box(e, offset));
            } else if (type === 'scrollbarv') {
                node = el('div', 'ctlg-el ctlg-scrollbar', box(e, offset));
            } else {
                return;
            }
            if (e.id) {
                node.setAttribute('data-id', e.id);
                (self.elements[e.id] = self.elements[e.id] || []).push(node);
            }
            self.frame.appendChild(node);
        });
    };

    CatalogueView.prototype.get = function (id) { return (this.elements[id] || [])[0] || null; };

    CatalogueView.prototype.setText = function (id, text, field) {
        var node = this.get(id);
        if (!node) return;
        writeText(node, br(text));
        this.editable(node, field);
    };

    CatalogueView.prototype.hide = function (id) {
        (this.elements[id] || []).forEach(function (n) { n.style.visibility = 'hidden'; });
    };

    CatalogueView.prototype.show = function (id) {
        (this.elements[id] || []).forEach(function (n) { n.style.visibility = ''; });
    };

    CatalogueView.prototype.editable = function (node, field) {
        if (!node || !field || !this.options.editMode) return;
        var self = this;
        node.classList.add('ctlg-editable');
        node.setAttribute('data-field', field);
        node.setAttribute('data-hint', { headline: 'headline image', body: 'text', labelPick: 'product hint', labelExtraS: 'badge' }[field] ||
            field.replace(/^text:/, 'text ').replace(/^teaser:/, 'image '));
        node.onclick = function (ev) {
            if (self.options.onField) { ev.stopPropagation(); self.options.onField(field); }
        };
    };

    CatalogueView.prototype.fillPage = function () {
        var self = this, state = this.state, page = state.page || {};
        this.classSelect = null;
        var gameData = this.options.gameData || {};

        // The page index (renderPageList): 21 px rows, bold text 6 px in, #DDDDDD with #AAAAAA lines, selected #EEEEEE.
        var pages = this.get('ctlg_pages');
        if (pages) {
            pages.classList.add('ctlg-pages');
            var list = el('div', 'ctlg-pages-list');
            var active = null;
            (state.index || []).forEach(function (entry) {
                var row = el('div', 'ctlg-page-row' + (entry.id === state.pageId ? ' is-active' : '') + (entry.hidden ? ' is-hidden' : ''));
                var left = el('img', 'ctlg-page-left');
                left.src = memberUrl(entry.id === state.pageId ? 'ctlg.pagelist.left.active' : 'ctlg.pagelist.left') || '';
                left.alt = '';
                row.appendChild(left);
                var label = el('span', 'ctlg-page-name');
                label.textContent = entry.name;
                row.appendChild(label);
                if (entry.hidden) row.title = 'Hidden: players don\'t see this page in the index';
                row.onclick = function () { if (self.options.onSelectPage) self.options.onSelectPage(entry.id); };
                if (entry.id === state.pageId) active = row;
                list.appendChild(row);
            });
            pages.appendChild(list);
            this.drawScrollbar(list, this.get('ctlg_pages_scroll'));
            // Like the game the index keeps its scroll position; it only moves when the open page is out of view.
            if (this.pageListScroll !== undefined) list.scrollTop = this.pageListScroll;
            if (active && (active.offsetTop < list.scrollTop || active.offsetTop + 21 > list.scrollTop + list.clientHeight)) {
                list.scrollTop = Math.max(0, active.offsetTop - 21 * 3);
            }
            list.addEventListener('scroll', function () { self.pageListScroll = list.scrollTop; });
            list.dispatchEvent(new Event('scroll'));
        }

        // The purse line.
        var amount = state.purse === undefined ? '-' : String(state.purse);
        this.setText('catalog_credits_bottom', (textOf(gameData, 'catalog_coins_amount') || '%amount%').replace('%amount%', amount));
        this.setText('catalog_get_credits_bottom', textOf(gameData, 'catalog_get_credits_link'));
        var link = this.get('catalog_get_credits_bottom');
        if (link) link.classList.add('ctlg-link');

        if (!state.page) return;
        var layout = page.layout;
        var info = CatalogueView.layoutInfo(layout);

        // Header image and text, text slots, teasers (feedPageData).
        var header = this.get('ctlg_header_img');
        if (header) {
            var src = pageImageUrl(page.headline, gameData);
            if (src) fillCentered(header, src.url, 8, src.remote);
            this.editable(header, 'headline');
        }
        if (this.get('ctlg_header_text')) this.setText('ctlg_header_text', page.body, 'body');
        info.texts.forEach(function (n) { self.setText('ctlg_text_' + n, (page.texts || {})[n] || '', 'text:' + n); });
        var products = state.products || [];
        var productPage = /^ctlg_productpage\d/.test(layout);
        for (var t = 1; t <= info.teasers; t++) {
            var teaser = this.get('ctlg_teaserimg_' + t);
            if (!teaser) continue;
            var name = (page.teasers || [])[t - 1];
            if (name) {
                var tsrc = pageImageUrl(name, gameData);
                if (tsrc) fillCentered(teaser, tsrc.url, 8, tsrc.remote); // ink 36 in the game, but only palette index 0 is see-through: white inside the picture stays (the matte keeps it)
            } else if (productPage && !(page.teasers || []).length && products[t - 1]) {
                var pic = el('img', 'ctlg-centered'); pic.alt = '';
                teaser.textContent = '';
                furniImage(pic, products[t - 1].sprite, products[t - 1].colour, '&direction=2');
                teaser.appendChild(pic);
            }
            this.editable(teaser, productPage && !(page.teasers || []).length ? null : 'teaser:' + t);
        }

        // Layouts with a description line show the teaser text until a product is picked.
        if (/^ctlg_(layout1|layout2|soundmachine)$/.test(layout)) {
            if (info.description) this.setText('ctlg_description', page.labelPick, 'labelPick');
            this.hide('ctlg_buy_button');
            this.setText('ctlg_page_text', textOf(gameData, 'catalog_page'));
        }
        this.hide('ctlg_price_box');

        // Special text badge: "type:text" over catalog_special_txtbg<type>.
        this.showSpecial(page.labelExtraS);

        // Products: icons with paging, or one block per product (product pages). Layouts with their own page class
        // (Plastic, pets, spaces, recycler) lay out their products themselves.
        var pageClass = PAGE_CLASSES[layout];
        if (pageClass) {
            this.hide('ctlg_next_button'); this.hide('ctlg_prev_button'); this.hide('ctlg_page_counter');
            pageClass.call(this, products);
        } else if (info.icons > 0) this.showIcons(info.icons);
        else if (info.productSlots > 0) this.showProductBlocks(info.productSlots);
        else {
            this.hide('ctlg_next_button');
            this.hide('ctlg_prev_button');
            this.hide('ctlg_page_counter');
            this.hide('ctlg_page_text');
        }
        this.showSubPages(state.linkChain, page);
        // Trax: "play preview" shows once a sound set is picked (Catalogue soundmachine Class setPreviewState).
        this.hide('play_preview_icon'); this.hide('play_preview_text');
        if (this.selected >= 0) this.selectProduct(this.selected, true);
    };

    // Linked pages (showSubPageCounter): "n/m" and the arrows through [page, ...links] while inside that chain.
    CatalogueView.prototype.showSubPages = function (chain, page) {
        var self = this;
        var position = chain ? chain.indexOf(page.nameIndex) : -1;
        if (!chain || position < 0) {
            this.hide('ctlg_nextpage_button'); this.hide('ctlg_prevpage_button'); this.hide('ctlg_subpage_counter');
            if (!(CatalogueView.layoutInfo(page.layout).icons > 0)) this.hide('ctlg_page_text');
            return;
        }
        this.setText('ctlg_subpage_counter', (position + 1) + '/' + chain.length);
        this.setText('ctlg_page_text', textOf(this.options.gameData, 'catalog_page'));
        var go = function (nameIndex) { if (self.options.onSelectPageByName) self.options.onSelectPageByName(nameIndex); };
        this.arrow('ctlg_prevpage_button', position > 0, function () { go(chain[position - 1]); });
        this.arrow('ctlg_nextpage_button', position < chain.length - 1, function () { go(chain[position + 1]); });
    };

    // An arrow button: active, or deactivated (Common Button Class: blend 50, no click).
    CatalogueView.prototype.arrow = function (id, active, click) {
        var node = this.get(id);
        if (!node) return;
        this.show(id);
        node.style.opacity = active ? '' : '0.5';
        node.style.cursor = active ? 'pointer' : 'default';
        node.onclick = active ? function (ev) { ev.stopPropagation(); click(); } : null;
    };

    // Fuse's vertical scrollbar (scrollbarv1): up button, stretched bar, lift, down button.
    CatalogueView.prototype.drawScrollbar = function (list, node) {
        if (!node) return;
        node.textContent = '';
        node.classList.add('ctlg-scrollbar');
        var height = parseInt(node.style.height, 10);
        var bar = el('img', 'ctlg-scroll-bar'); bar.src = memberUrl('scrollbar.vertical.active'); bar.alt = '';
        var up = el('img', 'ctlg-scroll-up'); up.src = memberUrl('button.scroll.up.active'); up.alt = '';
        var down = el('img', 'ctlg-scroll-down'); down.src = memberUrl('button.scroll.down.active'); down.alt = '';
        var lift = el('img', 'ctlg-scroll-lift'); lift.src = memberUrl('button.scroll.lift.active'); lift.alt = '';
        [bar, up, down, lift].forEach(function (n) { node.appendChild(n); });
        var buttonHeight = 14, liftHeight = 15;
        var track = height - 2 * buttonHeight - liftHeight;
        var place = function () {
            var max = list.scrollHeight - list.clientHeight;
            lift.style.display = max > 0 ? '' : 'none';
            lift.style.top = buttonHeight + (max > 0 ? Math.round(track * list.scrollTop / max) : 0) + 'px';
        };
        list.addEventListener('scroll', place);
        up.onclick = function () { list.scrollTop -= 21; };
        down.onclick = function () { list.scrollTop += 21; };
        bar.onclick = function (ev) {
            var y = ev.offsetY;
            list.scrollTop += (y < parseInt(lift.style.top, 10) ? -1 : 1) * list.clientHeight;
        };
        lift.onmousedown = function (ev) {
            ev.preventDefault();
            var startY = ev.clientY, start = list.scrollTop, max = list.scrollHeight - list.clientHeight;
            var move = function (e) { list.scrollTop = start + (e.clientY - startY) * max / Math.max(1, track); };
            var stop = function () { document.removeEventListener('mousemove', move); document.removeEventListener('mouseup', stop); };
            document.addEventListener('mousemove', move);
            document.addEventListener('mouseup', stop);
        };
        setTimeout(place, 0);
    };

    // A package's icon (Deal Preview Class): several furni = the deal box with its number in bold #FFFFCC;
    // one furni several times = its icon with the count badge (ctlg_dyndeal_* digits).
    CatalogueView.prototype.dealIcon = function (product, dealNumber) {
        var parts = product.packages || [];
        var wrap = el('div', 'ctlg-deal');
        if (parts.length > 1) {
            var box = el('img', 'ctlg-centered'); box.src = memberUrl('ctlg_pic_deal_icon_narrow'); box.alt = '';
            var holder = el('div', 'ctlg-deal-box');
            holder.appendChild(box);
            var number = el('span', 'ctlg-deal-number'); number.textContent = String(dealNumber);
            holder.appendChild(number);
            wrap.appendChild(holder);
            return wrap;
        }
        var part = parts[0] || { sprite: product.sprite, colour: product.colour, amount: 1 };
        var picture = el('img', 'ctlg-centered'); picture.alt = '';
        dealSlotPicture(part, 32, 32).then(function (url) { picture.src = url; });
        wrap.appendChild(picture);
        return wrap;
    };

    CatalogueView.prototype.showSpecial = function (text) {
        var img = this.get('ctlg_special_img');
        var txt = this.get('ctlg_special_txt');
        if (img) { img.textContent = ''; this.editable(img, 'labelExtraS'); }
        if (txt) { txt.textContent = ''; this.editable(txt, 'labelExtraS'); }
        if (!text || text.length < 2 || !img) return;
        var parts = text.split(':');
        var type = parseInt(parts[0], 10) || 1;
        var bg = memberUrl('catalog_special_txtbg' + type);
        if (bg) fillCentered(img, bg, 8, false);
        if (txt) writeText(txt, br(parts[parts.length - 1]).trim());
    };

    CatalogueView.prototype.showIcons = function (perPage) {
        var self = this, products = this.state.products || [];
        for (var n = 1; n <= perPage; n++) {
            var slot = this.get('ctlg_small_img_' + n);
            if (!slot) continue;
            slot.textContent = '';
            slot.onclick = null;
            slot.classList.remove('is-selected', 'ctlg-clickable');
            var index = this.productOffset + n - 1;
            var product = products[index];
            if (!product) continue;
            slot.classList.add('ctlg-clickable');
            if (index === this.selected) slot.classList.add('is-selected');
            if (product.isPackage) {
                // dealNumber counts the multi-furni deals on the page, in order (Catalogue Handler Class).
                var dealNumber = products.filter(function (p, i) { return i <= index && p.isPackage && (p.packages || []).length > 1; }).length;
                slot.appendChild(this.dealIcon(product, dealNumber));
            } else {
                var icon = el('img', 'ctlg-centered');
                icon.alt = '';
                furniImage(icon, product.sprite, product.colour, '&icon=true');
                slot.appendChild(icon);
            }
            slot.title = product.name;
            (function (i) { slot.onclick = function (ev) { ev.stopPropagation(); self.selectProduct(i); }; })(index);
        }
        var pagesCount = Math.ceil(products.length / perPage);
        if (pagesCount > 1) {
            this.show('ctlg_page_counter'); this.show('ctlg_page_text');
            this.setText('ctlg_page_counter', (Math.floor(this.productOffset / perPage) + 1) + '/' + pagesCount);
            this.arrow('ctlg_next_button', this.productOffset + perPage < products.length, function () { self.productOffset += perPage; self.showIcons(perPage); });
            this.arrow('ctlg_prev_button', this.productOffset > 0, function () { self.productOffset -= perPage; self.showIcons(perPage); });
        } else {
            this.hide('ctlg_next_button'); this.hide('ctlg_prev_button'); this.hide('ctlg_page_counter'); this.hide('ctlg_page_text');
        }
    };

    CatalogueView.prototype.priceText = function (price) {
        var gameData = this.options.gameData || {};
        return price + ' ' + (parseInt(price, 10) > 1 ? textOf(gameData, 'credits') : textOf(gameData, 'credit'));
    };

    CatalogueView.prototype.showProductBlocks = function (slots) {
        var products = this.state.products || [];
        for (var n = 1; n <= slots; n++) {
            var product = products[n - 1];
            if (!product) {
                ['ctlg_buy_' + n, 'ctlg_product_name_' + n, 'ctlg_description_' + n, 'ctlg_price_' + n].forEach(this.hide, this);
                continue;
            }
            this.setText('ctlg_product_name_' + n, product.name);
            this.setText('ctlg_description_' + n, product.description);
            this.setText('ctlg_price_' + n, this.priceText(product.price));
        }
    };

    CatalogueView.prototype.selectProduct = function (index, quiet) {
        var products = this.state.products || [];
        var product = products[index];
        if (!product) return;
        this.selected = index;
        if (this.classSelect) { this.classSelect(index, quiet); return; }
        var info = CatalogueView.layoutInfo(this.state.page.layout);
        if (info.icons > 0) this.showIcons(info.icons);
        var preview = this.get('ctlg_teaserimg_1');
        if (preview && info.productName) {
            preview.onclick = null;
            preview.classList.remove('ctlg-editable');
            if (product.isPackage && (product.packages || []).length) {
                preview.textContent = '';
                var deal = el('img', 'ctlg-centered'); deal.alt = '';
                var size = [parseInt(preview.style.width, 10) || 162, parseInt(preview.style.height, 10) || 162];
                dealPreviewPicture(product.packages, size[0], size[1]).then(function (url) { deal.src = url; });
                preview.appendChild(deal);
            } else {
                preview.textContent = '';
                var big = el('img', 'ctlg-centered'); big.alt = '';
                furniImage(big, product.sprite, product.colour, '&direction=2');
                preview.appendChild(big);
            }
        }
        this.setText('ctlg_product_name', product.name);
        this.setText('ctlg_description', product.description);
        this.show('ctlg_price_box');
        this.setText('ctlg_price_1', this.priceText(product.price));
        this.show('ctlg_buy_button');
        this.showSpecial('');
        if (/^sound_set_/.test(product.sprite || '')) {
            this.show('play_preview_icon'); this.show('play_preview_text');
            this.setText('play_preview_text', textOf(this.options.gameData, 'play_preview'));
        }
        if (!quiet && this.options.onSelectProduct) this.options.onSelectProduct(index);
    };

    // Shows one product's name, description, price, preview and the buy button (the page classes' selectProduct).
    CatalogueView.prototype.showProduct = function (product) {
        var preview = this.get('ctlg_teaserimg_1');
        if (preview) {
            preview.textContent = '';
            preview.onclick = null;
            preview.classList.remove('ctlg-editable');
            var big = el('img', 'ctlg-centered'); big.alt = '';
            furniImage(big, product.sprite, product.colour, '&direction=2');
            preview.appendChild(big);
        }
        this.setText('ctlg_product_name', product.name);
        this.setText('ctlg_description', product.description);
        this.setText('ctlg_price_1', this.priceText(product.price));
        this.show('ctlg_buy_button');
    };

    // Image of a member multiplied by a colour (ink 41, "darken", with the sprite's bgColor).
    function darkened(member, hex) {
        var key = member + '|' + hex;
        inkCache[key] = inkCache[key] || new Promise(function (resolve) {
            var img = new Image();
            img.onload = function () {
                var canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth; canvas.height = img.naturalHeight;
                var ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                var data = ctx.getImageData(0, 0, canvas.width, canvas.height), px = data.data;
                var r = parseInt(hex.substr(1, 2), 16), g = parseInt(hex.substr(3, 2), 16), b = parseInt(hex.substr(5, 2), 16);
                for (var i = 0; i < px.length; i += 4) {
                    if (px[i] === 255 && px[i + 1] === 255 && px[i + 2] === 255 && hex === '#FFFFFF') continue;
                    px[i] = px[i] * r / 255; px[i + 1] = px[i + 1] * g / 255; px[i + 2] = px[i + 2] * b / 255;
                }
                ctx.putImageData(data, 0, 0);
                resolve(canvas.toDataURL());
            };
            img.onerror = function () { resolve(null); };
            img.src = memberUrl(member);
        });
        return inkCache[key];
    }

    // Page classes (hh_cat_code): layouts whose products the game lays out itself.
    var PAGE_CLASSES = {
        // Catalogue plasto Class: one icon per furni type (class before "*", sorted), colour boxes for its variants
        // (bgColor = the variant's last part colour), the first type and colour picked.
        ctlg_plasto: function (products) {
            var self = this;
            var byClass = {};
            products.forEach(function (p, index) {
                var base = (p.sprite || '').split('*')[0];
                (byClass[base] = byClass[base] || []).push({ product: p, index: index, key: (p.sprite || '').toLowerCase() });
            });
            var lingoSort = function (a, b) { return a < b ? -1 : a > b ? 1 : 0; };
            var groups = Object.keys(byClass).sort(function (a, b) { return lingoSort(a.toLowerCase(), b.toLowerCase()); })
                .map(function (k) { return byClass[k].sort(function (a, b) { return lingoSort(a.key, b.key); }); });
            var boxes = 0;
            while (this.get('ctlg_selectcolor_bg_' + (boxes + 1))) boxes++;
            var select = function (g, c, quiet) {
                var group = groups[g];
                if (!group || !group[c]) return;
                for (var n = 1; n <= groups.length; n++) {
                    var slot = self.get('ctlg_small_img_' + n);
                    if (slot) slot.style.background = n === g + 1 ? 'url(' + memberUrl('ctlg_small_active2_bg') + ') center no-repeat' : '';
                }
                for (var f = 1; f <= boxes; f++) {
                    var bg = self.get('ctlg_selectcolor_bg_' + f), button = self.get('ctlg_selectcolor_' + f);
                    var variant = group[f - 1];
                    var colours = variant ? String(variant.product.colour || '').split(',') : [];
                    var colour = variant ? (colours[colours.length - 1] || '#FFFFFF').trim().toUpperCase() : '#FFFFFF';
                    if (!/^#[0-9A-F]{6}$/.test(colour)) colour = '#FFFFFF';
                    if (bg) {
                        (function (node, col) { darkened('catalog_plasto_colourbutton1', col).then(function (src) { if (src) node.src = src; }); })(bg, colour);
                        bg.style.opacity = variant ? '' : '0.3';
                        bg.style.cursor = variant ? 'pointer' : '';
                        bg.onclick = variant ? (function (i) { return function (ev) { ev.stopPropagation(); select(g, i); }; })(f - 1) : null;
                    }
                    if (button) {
                        button.style.opacity = variant ? '0.8' : '0.3';
                        button.style.pointerEvents = 'none';
                    }
                }
                var picked = group[c];
                self.selected = picked.index;
                self.showProduct(picked.product);
                if (!quiet && self.options.onSelectProduct) self.options.onSelectProduct(picked.index);
            };
            groups.forEach(function (group, g) {
                var slot = self.get('ctlg_small_img_' + (g + 1));
                if (!slot) return;
                slot.textContent = '';
                var icon = el('img', 'ctlg-centered'); icon.alt = '';
                furniImage(icon, group[0].product.sprite, null, '&icon=true');
                slot.appendChild(icon);
                slot.classList.add('ctlg-clickable');
                slot.title = group[0].product.name;
                slot.onclick = function (ev) { ev.stopPropagation(); select(g, 0); };
            });
            this.classSelect = function (index, quiet) {
                groups.forEach(function (group, g) {
                    group.forEach(function (v, c) { if (v.index === index) select(g, c, quiet); });
                });
            };
            if (groups.length) select(0, 0, true);
        },

        // Catalogue pets Class: a random race of the product's pet type (sale code petsN); we show race 000. The pet
        // itself is drawn by the game.
        ctlg_pets: function (products) {
            var self = this, gameData = this.options.gameData || {};
            var at = 0;
            var show = function (i, quiet) {
                var product = products[i];
                if (!product) return;
                at = i;
                self.selected = i;
                var type = String(product.saleCode || '').slice(-1);
                self.setText('ctlg_text_2', textOf(gameData, 'pet_race_' + type + '_000'));
                self.setText('ctlg_price_1', self.priceText(product.price));
                self.show('ctlg_buy_button');
                self.arrow('ctlg_prevmodel_button', products.length > 1, function () { show((at + products.length - 1) % products.length); });
                self.arrow('ctlg_nextmodel_button', products.length > 1, function () { show((at + 1) % products.length); });
                if (!quiet && self.options.onSelectProduct) self.options.onSelectProduct(i);
            };
            this.classSelect = function (index, quiet) { show(index, quiet); };
            show(0, true);
        },

        // Catalogue spaces Class: wallpaper, floor and landscape come from the game's own lists and are drawn with
        // palette swaps; the preview is the game's default (wall 1, floor 1, landscape 1).
        ctlg_spaces: function () {
            var area = this.get('catalog_wall_preview_b_right');
            if (!area) return;
            var img = el('img', 'ctlg-el', { left: area.style.left, top: area.style.top, width: '290px', height: '149px', marginLeft: '-1px' });
            img.src = memberUrl('spaces_preview_default') || '';
            img.alt = '';
            img.title = 'The game\'s default preview (its wallpapers and floors aren\'t catalogue data)';
            area.parentNode.insertBefore(img, area.nextSibling);
        },

        // Recycler Interface Class ("open" state): the slots and the rewards come from the recycler, not the page.
        ctlg_recycler: function () {
            ['rec_prev', 'rec_next', 'rec_current_name', 'rec_current_btn', 'rec_accept_text'].forEach(this.hide, this);
            this.setText('rec_page', '1/1');
            // The header text is the recycler's own (recycler_info_open) unless the page has one.
            if (!(this.state.page || {}).body) this.setText('ctlg_header_text', textOf(this.options.gameData, 'recycler_info_open'), 'body');
        }
    };

    window.CatalogueView = CatalogueView;
})();

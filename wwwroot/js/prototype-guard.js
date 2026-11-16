/*
 * prototype-guard.js: load BEFORE prototype.js (1.7) and myhabbo/visual.js (bundles Prototype 1.6.0.2).
 *
 * The Habbo-era Prototype copies rewrite browser built-ins in ways modern libraries can't cope with. Ruffle 0.7+
 * (Flash figure editor, badge editor, Habbowood) broke on every page that loads them. This keeps the built-ins
 * modern code relies on, while letting Prototype's own code keep working:
 *
 * 1. Array.prototype.filter/map/find/every/some/entries: Prototype mixes Enumerable into arrays. Its versions
 *    call the callback as (value, index) with no array argument, treat a missing callback as identity, and
 *    "entries" returns an array instead of an iterator. Ruffle's classnames helper does
 *    `list.filter((v, i, a) => a.indexOf(v) === i)` and crashed. We pin the native implementations, wrapped
 *    so an omitted callback still means identity (Prototype's own toArray() calls this.map()), and ignore
 *    Prototype's reassignment.
 *
 * 2. window.Element: both versions replace the global Element constructor with their own function (used as
 *    `new Element('div', {...})` and for statics such as Element.extend). 1.7 keeps the native
 *    Element.prototype on it; 1.6.0.2 doesn't, so `node instanceof Element` becomes false for every DOM node.
 *    Ruffle builds its player's shadow DOM with that check, ended up with an empty template, and threw
 *    "Cannot read properties of null (reading 'addEventListener')". We let Prototype install its function,
 *    but give it the native prototype, exactly as Prototype 1.7 itself does.
 */
(function () {
    if (window.__prototypeGuard) return;
    Object.defineProperty(window, '__prototypeGuard', { value: true });

    // 1. Arrays
    var proto = Array.prototype;
    function identity(value) { return value; }
    function tolerant(native) {
        return function (callback, thisArg) {
            return native.call(this, typeof callback === 'function' ? callback : identity, thisArg);
        };
    }
    var pinned = {
        filter: proto.filter && tolerant(proto.filter),
        map: proto.map && tolerant(proto.map),
        find: proto.find && tolerant(proto.find),
        every: proto.every && tolerant(proto.every),
        some: proto.some && tolerant(proto.some),
        entries: proto.entries
    };
    Object.keys(pinned).forEach(function (name) {
        var fn = pinned[name];
        if (!fn) return;
        Object.defineProperty(proto, name, {
            configurable: true,
            enumerable: false,
            get: function () { return fn; },
            set: function () { /* ignore Prototype's Object.extend(Array.prototype, Enumerable) */ }
        });
    });

    // 2. window.Element
    var nativeElementPrototype = window.Element && window.Element.prototype;
    if (nativeElementPrototype) {
        var current = window.Element;
        Object.defineProperty(window, 'Element', {
            configurable: true,
            enumerable: false,
            get: function () { return current; },
            set: function (fn) {
                if (typeof fn === 'function' && fn.prototype !== nativeElementPrototype) {
                    try { fn.prototype = nativeElementPrototype; } catch (e) { /* non-writable: leave it */ }
                }
                current = fn;
            }
        });
    }
})();

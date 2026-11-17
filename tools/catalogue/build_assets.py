"""Build the housekeeping catalogue preview's assets from the decompiled Habbo client.

Usage: python3 tools/catalogue/build_assets.py <habbo-vibe-playground>/decompiled
       python3 tools/catalogue/build_assets.py --v31 <dump of hh_cat_gfx_v31> <habbo-vibe-playground>/decompiled

Writes wwwroot/housekeeping/catalogue/:
  layouts.json   every ctlg_*.window of hh_cat_gfx_all plus habbo_catalogue.window, as element lists
                 (positions relative to the window; `offset` = where it merges into the catalogue window: its client rect)
  members/       the bitmaps those windows use (pieces, buttons), by member name
  images/        cast bitmaps a page can name as header or teaser image (the client takes a cast member before
                 downloading c_images/catalogue/<name>_<language>.gif)
  images.json    the names in images/, with their size

Casts are searched in the order the client resolves member names that matter here: the Danish patch first, then
the catalogue graphics, then the interface casts.

--v31 writes the same files under wwwroot/housekeeping/catalogue/v31/ for the web client's catalogue (hh_cat_new),
from a dump of its graphics cast (habbo-vibe-playground's `node out/cli/dumpCast.js hh_cat_gfx_v31.cct <dir>`):
every window of that cast, its members first, then the decompiled interface casts for the rest.
"""
import json
import os
import re
import shutil
import struct
import sys

CASTS = ["hh_patch_dk", "hh_cat_gfx_all", "hh_interface", "hh_custom_gfx", "hh_shared", "hh_purse", "hh_club",
         "hh_soundmachine", "hh_recycler", "hh_pets_common", "hh_pets", "hh_photo", "fuse_client"]
IMAGE_CASTS = ["hh_patch_dk", "hh_cat_gfx_all"]
INT_PROPS = {"locH", "locV", "width", "height", "ink", "blend", "fontSize", "lineHeight", "fixedLineSpace", "wordWrap",
             "maxwidth", "offset"}
KEEP = ["member", "type", "id", "locH", "locV", "width", "height", "ink", "blend", "font", "fontSize", "fontStyle",
        "alignment", "key", "wordWrap", "txtColor", "color", "bgColor", "lineHeight", "fixedLineSpace", "boxType",
        "cursor", "client", "strech", "model", "maxwidth"]


def parse_window(path):
    text = open(path, encoding="latin-1").read()
    rect = re.search(r"<rect>\s*\[([^\]]*)\]", text)
    left, top, right, bottom = [int(v) for v in rect.group(1).split(",")] if rect else (0, 0, 0, 0)
    client = re.search(r"<clientrect>\s*rect\(([^)]*)\)", text)
    client_rect = [int(v) for v in client.group(1).split(",")] if client else [0, 0, right - left, bottom - top]
    elements = []
    for body in re.findall(r"\[(#member:.*?)\]\s*(?=\[#member|</elements>)", text, re.S):
        props = {}
        for m in re.finditer(r'#(\w+):\s*("([^"]*)"|\[[^\]]*\]|#?[\w.]+|-?\d+)', body):
            key, raw, quoted = m.group(1), m.group(2), m.group(3)
            value = quoted if quoted is not None else raw
            if key in INT_PROPS:
                try:
                    value = int(value)
                except ValueError:
                    pass
            elif isinstance(value, str) and value.startswith("[") and value.endswith("]"):
                value = [v.strip().lstrip("#") for v in value[1:-1].split(",") if v.strip()]
            elif isinstance(value, str) and value.startswith("#"):
                value = value[1:]
            props[key] = value
        elements.append({k: props[k] for k in KEEP if k in props})
    return {"rect": [left, top, right, bottom], "width": right - left, "height": bottom - top, "clientRect": client_rect,
            "elements": elements}


def index_bitmaps(root):
    """member name -> png path, first cast in CASTS wins (then any other cast)."""
    found = {}
    casts = CASTS + sorted(c for c in os.listdir(root) if c not in CASTS and os.path.isdir(os.path.join(root, c)))
    for cast in casts:
        folder = os.path.join(root, cast)
        if not os.path.isdir(folder):
            continue
        for name in os.listdir(folder):
            if not name.endswith(".png"):
                continue
            member = re.sub(r"^\d+_", "", name)[:-4]
            found.setdefault(member, os.path.join(folder, name))
            found.setdefault(member.strip(), os.path.join(folder, name))
    return found


def png_size(path):
    with open(path, "rb") as f:
        head = f.read(24)
    return struct.unpack(">II", head[16:24])


def safe(member):
    return re.sub(r"[^A-Za-z0-9_.-]", "_", member.strip())


# Bitmaps the v31 page code draws: tree nodes (treeview.node.* variables of hh_cat_new), the product strip.
V31_EXTRA = (["tree_basicslot_unselected", "tree_basicslot_selected", "testarrow.down", "testarrow.right",
              "stripitem.basic.bg.unselected", "stripitem.basic.bg.selected", "ctlg_loading_icon2", "purse_coins_small",
              "no_icon_small", "catalog_special_txtbg1", "catalog_special_txtbg2", "ctlg_pic_deal_icon_narrow",
              "ctlg_small_active_bg", "ctlg_small_active2_bg",
              "ctlg_dyndeal_background", "ctlg_dyndeal_button_left", "ctlg_dyndeal_button_center", "ctlg_dyndeal_button_right",
              "button.scroll.up.active", "scrollbar.vertical.active", "button.scroll.lift.active", "button.scroll.down.active"]
             + ["tree_col%d_%s" % (n, s) for n in range(1, 12) for s in ("unselected", "selected")]
             + ["katalogi_ikoni.%s" % n for n in ("purse", "furni", "plants", "trax", "pixels", "credits", "recycler", "pets", "hc",
                                                 "special_discounts")]
             + ["ctlg_dyndeal_%d" % d for d in range(10)]
             + ["button.%s.%s.active" % (m, side) for m in "abc" for side in ("left", "middle", "right")])


def opaque_png(path):
    """Drop a PNG's alpha channel in place. The cast dump keeps 32-bit members' raw alpha byte, which Director ignores
    unless the member uses alpha; the tree's arrows have junk there (whole rows at 0), so the bitmaps the v31 page code
    draws itself are written opaque (they are drawn with ink 36, white = transparent)."""
    import zlib
    data = open(path, "rb").read()
    pos, chunks = 8, []
    while pos < len(data):
        length, kind = struct.unpack(">I4s", data[pos:pos + 8])
        chunks.append((kind, data[pos + 8:pos + 8 + length]))
        pos += 12 + length
    header = next(body for kind, body in chunks if kind == b"IHDR")
    width, height, depth, colour = struct.unpack(">IIBB", header[:10])
    if depth != 8 or colour != 6:
        return
    raw = zlib.decompress(b"".join(body for kind, body in chunks if kind == b"IDAT"))
    stride, out, prev = width * 4, bytearray(), bytearray(width * 4)
    for y in range(height):
        kind, line = raw[y * (stride + 1)], bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        for i in range(stride):
            a = line[i - 4] if i >= 4 else 0
            b = prev[i]
            c = prev[i - 4] if i >= 4 else 0
            if kind == 1: line[i] = (line[i] + a) & 255
            elif kind == 2: line[i] = (line[i] + b) & 255
            elif kind == 3: line[i] = (line[i] + (a + b) // 2) & 255
            elif kind == 4:
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        prev = line
        out.append(0)
        for x in range(width):
            out += line[x * 4:x * 4 + 3]

    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body))
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)) + \
        chunk(b"IDAT", zlib.compress(bytes(out), 9)) + chunk(b"IEND", b"")
    open(path, "wb").write(png)


OPAQUE_V31 = re.compile(r"^(testarrow\.|tree_|katalogi_ikoni\.|stripitem\.)")


def main_v31(dump, root):
    out = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "..", "wwwroot", "housekeeping", "catalogue", "v31"))
    for sub in ("members", "images"):
        shutil.rmtree(os.path.join(out, sub), ignore_errors=True)
        os.makedirs(os.path.join(out, sub))
    bitmaps = {}
    for name in os.listdir(dump):
        if name.endswith(".png"):
            member = re.sub(r"^\d+_", "", name)[:-4]
            bitmaps.setdefault(member, os.path.join(dump, name))
            bitmaps.setdefault(member.strip(), os.path.join(dump, name))
    for member, path in index_bitmaps(root).items():
        bitmaps.setdefault(member, path)
    windows = {}
    for name in sorted(os.listdir(dump)):
        if name.endswith(".window.txt"):
            windows[re.sub(r"^\d+_", "", name)[: -len(".window.txt")]] = parse_window(os.path.join(dump, name))
    main_client = windows["habbo_catalogue"]["clientRect"]
    members, missing = {}, set()
    wanted = set(V31_EXTRA)
    for key, window in windows.items():
        window["offset"] = [0, 0] if key == "habbo_catalogue" else [main_client[0], main_client[1]]
        for element in window["elements"]:
            member = element.get("member", "")
            if element.get("type") in ("piece", "button", "pattern") and member and member.strip() != "shadow.pixel":
                wanted.add(member.strip())
    for member in sorted(wanted):
        path = bitmaps.get(member)
        if path:
            members[member] = safe(member) + ".png"
            shutil.copyfile(path, os.path.join(out, "members", safe(member) + ".png"))
            if OPAQUE_V31.match(member):
                opaque_png(os.path.join(out, "members", safe(member) + ".png"))
        else:
            missing.add(member)
    # Members the cast dump can't decode (JPEG-compressed in the cast), saved from the running client.
    overrides = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overrides_v31")
    for name in sorted(os.listdir(overrides)):
        if name.endswith(".png"):
            members[name[:-4]] = safe(name[:-4]) + ".png"
            missing.discard(name[:-4])
            shutil.copyfile(os.path.join(overrides, name), os.path.join(out, "members", members[name[:-4]]))
    # The old build's overrides (e.g. the spaces page's default preview) for the old pages v31 wraps.
    old_overrides = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overrides")
    for name in sorted(os.listdir(old_overrides)):
        if name.endswith(".png") and name[:-4] not in members:
            members[name[:-4]] = safe(name[:-4]) + ".png"
            shutil.copyfile(os.path.join(old_overrides, name), os.path.join(out, "members", members[name[:-4]]))
    # Page images: a cast member with the image's name wins over the library (Catalogpage Default Class mergeWindow).
    images = {}
    for folder in [os.path.join(root, "hh_patch_dk"), dump]:
        for name in sorted(os.listdir(folder)):
            if not name.endswith(".png"):
                continue
            member = re.sub(r"^\d+_", "", name)[:-4].strip()
            width, height = png_size(os.path.join(folder, name))
            if member in images or width < 8 or height < 8:
                continue
            images[member] = {"file": safe(member) + ".png", "width": width, "height": height}
            shutil.copyfile(os.path.join(folder, name), os.path.join(out, "images", safe(member) + ".png"))
    # Sizes of the members the page code positions itself (tree nodes, icons, arrows).
    sizes = {m: list(png_size(os.path.join(out, "members", f))) for m, f in members.items()}
    with open(os.path.join(out, "layouts.json"), "w", encoding="utf-8") as f:
        json.dump({"windows": windows, "members": members, "sizes": sizes}, f, ensure_ascii=False, separators=(",", ":"))
    with open(os.path.join(out, "images.json"), "w", encoding="utf-8") as f:
        json.dump(images, f, ensure_ascii=False, separators=(",", ":"), sort_keys=True)
    print(f"v31: {len(windows)} windows, {len(members)} members, {len(images)} cast images; missing members: {sorted(missing)}")


def main():
    if sys.argv[1] == "--v31":
        return main_v31(sys.argv[2], sys.argv[3])
    root = sys.argv[1]
    out = os.path.join(os.path.dirname(__file__), "..", "..", "wwwroot", "housekeeping", "catalogue")
    out = os.path.normpath(out)
    for sub in ("members", "images"):
        shutil.rmtree(os.path.join(out, sub), ignore_errors=True)
        os.makedirs(os.path.join(out, sub))

    bitmaps = index_bitmaps(root)
    gfx = os.path.join(root, "hh_cat_gfx_all")
    windows = {}
    for name in sorted(os.listdir(gfx)):
        if name.endswith(".window.txt"):
            key = re.sub(r"^\d+_", "", name)[: -len(".window.txt")]
            windows[key] = parse_window(os.path.join(gfx, name))
    # Windows other page classes merge on top of their layout (the recycler's "open" state on ctlg_recycler).
    for cast, key in [("hh_recycler", "ctlg_recycler_open")]:
        for name in os.listdir(os.path.join(root, cast)):
            if re.sub(r"^\d+_", "", name) == key + ".window.txt":
                windows[key] = parse_window(os.path.join(root, cast, name))

    # Fuse merges a layout into the window's client rect (Window Instance Class buildVisual: elements move by the
    # accumulated border, which for habbo_catalogue is rect(10, 31, ...)).
    main_client = windows["habbo_catalogue"]["clientRect"]
    missing = set()
    members = {}
    for key, window in windows.items():
        window["offset"] = [0, 0] if key == "habbo_catalogue" else [main_client[0], main_client[1]]
        for element in window["elements"]:
            member = element.get("member", "")
            if element.get("type") in ("piece", "button", "pattern") and member and member.strip() != "shadow.pixel":
                path = bitmaps.get(member) or bitmaps.get(member.strip())
                if path:
                    members[member.strip()] = safe(member) + ".png"
                    shutil.copyfile(path, os.path.join(out, "members", safe(member) + ".png"))
                else:
                    missing.add(member.strip())
    # Bitmaps the page code draws (index edges, special text backgrounds, active icon background).
    for member in ["ctlg.pagelist.left", "ctlg.pagelist.left.active", "catalog_special_txtbg1", "catalog_special_txtbg2",
                   "ctlg_small_active_bg", "ctlg_small_active2_bg", "purse_coins_small", "no_icon_small",
                   # deal (package) icons: Deal Preview Class
                   "ctlg_pic_deal_icon_narrow", "ctlg_dyndeal_background", "ctlg_dyndeal_button_left", "ctlg_dyndeal_button_center",
                   "ctlg_dyndeal_button_right"] + ["ctlg_dyndeal_%d" % d for d in range(10)] + [
                   # the page index scrollbar (hh_interface scrollbarv1.element)
                   "button.scroll.up.active", "scrollbar.vertical.active", "button.scroll.lift.active",
                   "button.scroll.down.active"] + [
                   # text buttons (hh_interface button<model>.element: a = 1, b = 2, c = 3)
                   "button.%s.%s.active" % (m, side) for m in "abc" for side in ("left", "middle", "right")]:
        path = bitmaps.get(member)
        if path:
            members[member] = safe(member) + ".png"
            shutil.copyfile(path, os.path.join(out, "members", safe(member) + ".png"))

    images = {}
    for cast in IMAGE_CASTS:
        folder = os.path.join(root, cast)
        for name in sorted(os.listdir(folder)):
            if not name.endswith(".png"):
                continue
            member = re.sub(r"^\d+_", "", name)[:-4].strip()
            if member in images:
                continue
            width, height = png_size(os.path.join(folder, name))
            if width < 8 or height < 8:
                continue
            images[member] = {"file": safe(member) + ".png", "width": width, "height": height, "cast": cast}
            shutil.copyfile(os.path.join(folder, name), os.path.join(out, "images", safe(member) + ".png"))

    # Members whose export is wrong (see overrides/README.md).
    overrides = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overrides")
    for name in sorted(os.listdir(overrides)):
        if name.endswith(".png"):
            members.setdefault(name[:-4], safe(name[:-4]) + ".png")
            shutil.copyfile(os.path.join(overrides, name), os.path.join(out, "members", members[name[:-4]]))

    with open(os.path.join(out, "layouts.json"), "w", encoding="utf-8") as f:
        json.dump({"windows": windows, "members": members}, f, ensure_ascii=False, separators=(",", ":"))
    with open(os.path.join(out, "images.json"), "w", encoding="utf-8") as f:
        json.dump(images, f, ensure_ascii=False, separators=(",", ":"), sort_keys=True)
    print(f"{len(windows)} windows, {len(members)} members, {len(images)} cast images; missing members: {sorted(missing)}")


if __name__ == "__main__":
    main()

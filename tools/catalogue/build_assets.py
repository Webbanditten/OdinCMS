"""Build the housekeeping catalogue preview's assets from the decompiled Habbo client.

Usage: python3 tools/catalogue/build_assets.py <habbo-vibe-playground>/decompiled

Writes wwwroot/housekeeping/catalogue/:
  layouts.json   every ctlg_*.window of hh_cat_gfx_all plus habbo_catalogue.window, as element lists
                 (positions relative to the window; `offset` = where it merges into the catalogue window: its client rect)
  members/       the bitmaps those windows use (pieces, buttons), by member name
  images/        cast bitmaps a page can name as header or teaser image (the client takes a cast member before
                 downloading c_images/catalogue/<name>_<language>.gif)
  images.json    the names in images/, with their size

Casts are searched in the order the client resolves member names that matter here: the Danish patch first, then
the catalogue graphics, then the interface casts.
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


def main():
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

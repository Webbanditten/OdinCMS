# Catalogue assets for the housekeeping editor

`wwwroot/housekeeping/catalogue/` holds what the housekeeping catalogue editor needs to draw pages exactly like the
game client does. It is generated from the decompiled Shockwave client (habbo-vibe-playground's `decompiled/`):

```bash
python3 tools/catalogue/build_assets.py ../habbo-vibe-playground/decompiled
```

- `layouts.json`: `windows` = every `ctlg_*.window` of `hh_cat_gfx_all`, `habbo_catalogue.window` and the recycler's
  `ctlg_recycler_open.window`, as element lists (member, type, id, position, size, ink, blend, font, text key, ...);
  `offset` is where a layout merges into the catalogue window (its client rect, 10,31). `members` = member name →
  file in `members/`.
- `members/`: the bitmaps those windows use (window frame, buttons, scrollbar, deal badges, ...), plus
  `overrides/` (see its README for why).
- `images/` + `images.json`: page images (headlines, teasers) that live in the casts `hh_patch_dk` and
  `hh_cat_gfx_all`. The client looks for a cast member first and then for `<image.library.url>catalogue/<name>_<language>.gif`;
  the editor does the same (the library listing comes from `CatalogueController.GameData`).

The drawing itself is `wwwroot/housekeeping/catalogue/catalogue-view.js`, which follows the client's
`hh_cat_code` scripts (Catalogue Interface Class and the page classes for Plastic, pets, spaces and the recycler).

# Member overrides

Bitmaps that `build_assets.py` copies over the exported cast members because the export is wrong.

- `catalog_bg_beam.png`: the catalogue's ring binder. The member's palette reference points at a field
  (member 116 of hh_cat_gfx_all), so the cast export fell back to black and white. In the game the window element
  draws it with `#palette: #systemMac`; this file is the member's palette indices (read in DirPlayer with
  `member("catalog_bg_beam").image.getPixel(x, y).paletteIndex`) mapped through the Mac system palette.
- `spaces_preview_default.png`: the Spaces page's room preview as the game draws it by default (wallpaper 1, floor 1,
  landscape 1; captured in DirPlayer). The game draws it from indexed wall/floor bitmaps with palette swaps that the
  exported PNGs can't reproduce, and its choices come from the game's own lists, not from catalogue data.

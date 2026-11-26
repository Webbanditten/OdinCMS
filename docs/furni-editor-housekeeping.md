# Furni editor in Housekeeping

The furni editor (dirplayer-rs `habbo/furni-editor`, public at https://webbanditten.github.io/dirplayer-rs/furni-editor/)
edits and builds furni casts in the browser. Housekeeping adds what the public page can't do: **publish a cast to the
hotel** (`hof_furni` on the asset host) and **create or update its `items_definitions` row**, so a new furni can go from
the editor to the catalogue. Publishing writes files every client downloads, so the whole feature sits behind one fuse
of its own, every cast is checked on the server before it is written, and **only furni made in this editor are ever
written**: the hotel's own casts are opened read-only, to copy under a new name.

## Access: one fuse, nobody by default

- Fuse **`housekeeping_furni_editor`** (`fuses.user_group = CUSTOM`). The migration creates it and grants it to **no
  rank**. A Hotel Manager gives it out in Housekeeping > Ranks; a dedicated rank ("Furni designer") keeps it to named
  people, since fuses are per rank.
- Every action checks that fuse itself (`[HousekeepingFilter(Fuse.housekeeping_furni_editor)]` on the controller, not
  only the menu): the page, opening a cast, validating, publishing, the history and restoring. No other fuse
  (catalogue, administrator, upload) grants any of it.
- POSTs carry the antiforgery token; the login cookie is the site's (SameSite, HttpOnly).
- Every publish and restore goes to the audit log (who, sprite, bytes, SHA-256, what it replaced).

## How the page works

`/housekeeping/furni-editor` shows the editor in an iframe (setting `keplercms:furniEditor:url`, default the GitHub
Pages build, `?embed=housekeeping`) next to a Housekeeping panel. They talk with `postMessage`, and the Housekeeping
side only accepts messages from that URL's origin. The editor never calls the CMS itself: privileged actions run in
the Housekeeping page, with its cookie, after a click in Housekeeping's own UI.

1. **Open.** Search the hotel's definitions (sprite, name), or the list of furni made here; Housekeeping fetches the
   large and the small cast (`hh_furni_xx_<sprite>.cct`, `hh_furni_xx_s_<sprite>.cct`) and hands them with the
   definition's colours to the editor, which edits them as a pair (64 px / 32 px, Alt+S). A hotel furni is read-only:
   "New furni from this…" copies it under a new name (members borrowed from other furni are renamed too). A new furni
   can also start from a Flash import.
2. **Publish.** "Publish to hotel…" in the editor sends both casts to the Housekeeping page. It uploads them to
   `Validate`, which checks them (below) and whether the sprite may be published (new, or made here), and answers with
   a report per cast: member counts, the classes, the size, warnings, the revision it becomes.
3. **Confirm in Housekeeping.** The report and a definition form (name, description, size from the cast, part colours
   from the editor, behaviour preset: furniture, seat, bed, lamp/switchable, tradable/recyclable) are shown in
   Housekeeping's panel. **Publish** sends the same upload id; the server checks it again, keeps the old cast in the
   history, writes the new one, saves the definition and sends `reload_catalogue` (Odin reloads definitions and pages).
4. **Versions.** Each sprite's versions (below) with who published them, when and what changed; **Open** loads one in
   the editor, **Roll back** makes it the current one again.

Players get a new revision on their next login (see Versions).

5. **Try and sell.** For a furni made here that is live, the panel (and the publish result) has *Give me one*
   (`GiveMe`: one into your own hand, as a voucher gives furni, + `reload_inventory`; it needs no catalogue item), and
   *Put it in the catalogue* (`AddToCatalogue`: a new product on
   the page you pick, at your price, + `reload_catalogue`). Both only take sprites made here (`Check` = "mine") and go
   to the audit log. The page's layout, texts and packages stay in the catalogue editor.

What the editor tells the form (message `publish`): `states` (the `.data` state count: more than one picks the kind
"Lamp / switches on and off" and its `max_status`), `wall`, and, when a Habbo sat or lay on the furni in the editor's
room, `kind` (`seat` / `bed`) and `height` (the seat height set there, the definition's `top_height`). Opening a furni
sends its definition's `top_height` to the editor (`open.height`), so the Habbo starts where the hotel has it. The
Habbo comes from `/habbo-imaging/avatarimage`, which sends `Access-Control-Allow-Origin: *` for that.

## Step by step (what the page shows)

The side panel's "How publishing works" box has the five steps and highlights where you are, from what the editor
reports (message `state`: the open class, whether it is the hotel's, whether it is a wall item):

1. **Open a furni**: search the hotel's here, or open a cast / import a Flash `.swf` in the editor.
2. **Make it yours**: a hotel furni is read-only. *Make a copy…* in the editor (a dialog asks for the class name)
   renames every member, field and index to it; on your own furni the same button is *Rename…*. *Publish…* on a hotel
   furni asks for that name itself and then goes on to publish.
3. **Edit** it; *Check* lists what the hotel would refuse.
4. **Publish…** in the editor: the publish box (right under the steps, scrolled into view) shows the check and the
   definition form; *Publish to the hotel* writes it.
5. **Sell or give it**: a catalogue page (catalogue editor) or a voucher. Players get it on their next login.

Wall items (OdinCMS#133 era, dirplayer-rs `cea9e65e`): the editor converts Flash wall items to the hotel's wall
layout (`leftwall <class>_<layer>_<frame>`, Item Object classes, see dirplayer-rs `habbo/furni-editor/src/flash.ts`),
previews them on a wall, and the publish form picks the kind "Wall item" (`wall_item`, 1×1) by itself from the cast.

## Only furni made here

A sprite may be published when it is **new**: not reserved (`s_…` is a small cast's name, `poster…` casts are numbered
by the catalogue), no `hh_furni_xx_<sprite>.cct` or `hh_furni_xx_s_<sprite>.cct` in hof_furni, no `items_definitions`
row with that sprite (or `sprite*n`). Or when it is **made here**: it has versions, its live casts are still byte for
byte the ones the last version wrote (SHA-256), and no definition other than the one the editor saved has that sprite.
Anything else is refused with the reason: a hotel cast, or one of ours that someone changed by hand, is never written
over. The definition is only ever created by the first publish or updated later; the editor never touches another one.

## Versions, revisions and rollback

Every publish is a **version**: a revision number per sprite, the cast's bytes, the definition as it was saved, who,
when, a note, and where it came from (editor, Flash import, roll back of version n).

- Table `furni_versions` (Odin's `update.1.52.sql`: sprite, revision, both casts' SHA-256 and sizes, the definition id
  and JSON, user, time, note, source `editor`/`rollback`, rolled_back_from) and the casts in
  `historyDir/<sprite>/r<n>.cct` and `r<n>_s.cct`, written once (create-new) and never changed. Each version is a whole
  furni: a publish without a small cast keeps the current one.
- Revisions only go up. **Roll back to version n** publishes n's casts again, exactly (both sizes), as a new revision
  (checked again, like any publish), optionally with n's definition too. The list then reads r1, r2, r3 "rolled back to
  r1": nothing is lost and the roll back can itself be undone. **Open** loads any version into the editor.
- Odin sends sprite and highest revision (from `furni_versions`, read at every request) in the furni revision list the
  client asks for before its first download (message 295, `SPRITE_LIST`, empty until v1.40.0). With
  `dynamic.download.name.template=hof_furni/hh_furni_xx_%typeid%.cct?v=%revision%` a new revision is a new URL, so
  clients download it past their cache and any proxy (a small cast `s_x` uses x's revision). Players get it on their
  next login; a cast a client has already loaded stays until then. Furni without versions get `?v=` (empty), which the
  asset host ignores. Verified in the web client (DirPlayer harness: the request was `…claude_test_chair.cct?v=3`);
  Shockwave not tried yet.
- `hof_furni/hh_furni_xx_<sprite>.cct` is always the current version (one file per sprite, as the client asks for it).
- Casts are a few KB to a few hundred KB, so every version is kept; `historyDir` belongs in the prod backups next to the
  database dumps (it isn't in the dumps).

## What the server accepts

A cast every client downloads could carry code: Director casts can hold Lingo scripts, and a member named like a client
class or field would replace it for everyone who loads the cast. So `validate` (and `publish` again) parses the cast
itself (afterburned container: Fver, Fcdr, ABMP, ILS; then `CAS*`, `KEY*` and every `CASt`) and refuses it unless:

- it is an afterburned cast (`XFIR`/`RIFX` + `FGDC`), at most `keplercms:furniEditor:maxBytes` (default 3 MB);
- the sprite matches `^[a-z0-9_]{1,64}$` (file name `hh_furni_xx_<sprite>.cct`, nothing else is ever written);
- chunks are only `KEY*`, `CAS*`, `CASt`, `BITD`, `ALFA`, `CLUT`, `STXT`, `DRCF`, `Fmap`, `ccl `, `Cinf`, `VERS`, `LctX`,
  `Lnam` (the last two empty: no `Lscr` anywhere). Every Habbo furni cast has exactly these (checked on 17 casts);
- members are only bitmaps, fields/texts and palettes (types 1, 3, 12, 4);
- member names are the furni's own: `<sprite>…`, `s_<sprite>…`, `leftwall <sprite>…` / `rightwall <sprite>…` (and
  their `s_` forms), `memberalias.index`, `asset.index`, `copyright`; palettes may be named freely (bitmaps find them
  by number);
- `asset.index`'s `#id` is the sprite (or `s_<sprite>`) and its `#classes` are furni classes the client has (Active
  Object Class, Active Object Extension Class, Item Object Class, Item Object Extension Class and the furniture classes
  of `hh_furni_classes`, kept as a list in code);
- `memberalias.index` names and targets are the furni's own members.

Anything else is refused with the reason; there is no override.

## Files on prod

- The CMS container gets two bind mounts: `./dcr/files/hof_furni` → `/furni-casts` (read-write; the only place it writes
  casts) and `./furni-history` → `/furni-history` (not served). Settings `keplercms:furniEditor:castDir`,
  `historyDir`. Without them the page shows the editor but publishing is off.
- Writes are atomic (temporary file in the same folder, then rename); every version is in `/furni-history/<sprite>/`
  (see Versions).
- Rollback of the feature: remove the two mounts (publishing turns itself off), or take the fuse away.

## Hotel colours on the public page

`GET /api/hotel/furni/search` is public, read-only data (names, colours, sizes). It gets
`Access-Control-Allow-Origin` for the editor's origin (setting `keplercms:furniEditor:corsOrigins`), so the public page
shows the hotel's colour variants too. Nothing else gets CORS.

## Rollout on prod (Patrick's OK 2026-11-25: fuse for rank 1)

1. DB backup, then Odin's `tools/migrations/update.1.52.sql` (`furni_versions`) and
   `sql/migrations/2026-11-22_furni_editor_fuse.sql` (the fuse, granted to nobody). Both only add.
2. Odin ≥ v1.40.0 (the revision list from `furni_versions`; an older Odin ignores the table).
3. CMS `:latest` with this branch, plus in compose for `kepler_cms`: the two mounts above and
   `keplercms__furniEditor__castDir: /furni-casts`, `keplercms__furniEditor__historyDir: /furni-history`. Files the CMS
   writes are 0644, which the asset host's Apache reads. Add `furni-history/` to the prod backups.
4. The fuse goes to rank 1 (Hotel Manager), Patrick's choice; more ranks in Housekeeping > Ranks.
5. The download template: `dynamic.download.name.template=hof_furni/hh_furni_xx_%typeid%.cct?v=%revision%` in
   `gamedata/external_vars.php` and `debug/external_vars.php`. Both clients read that file since 2026-11-23 (the web
   wrapper only includes it), and per the one-set-of-casts rule it is checked in DirPlayer, not a real Shockwave player.
   Furni without versions get `?v=` (empty), which the asset host ignores.

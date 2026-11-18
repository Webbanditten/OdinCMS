-- =====================================================================
-- RELEASE MIGRATION - 2026-11-18
-- The Habbowood page (/entertainment/habbowood), built like habbo.com's 2007 one
-- =====================================================================
-- A CMS page with the "habbowood" design (Views/Shared/PageDesigns/habbowood.cshtml): the intro animation
-- ({habbowood_intro}) over the best rated and newest films ({habbowood_top}, {habbowood_new}, portlet-film boxes), a gold
-- film strip box with the welcome text on the right, and the ad sidebar. Everything is editable in Housekeeping > Pages
-- or in edit mode. Plus the texts of the widgets and the studio / end / my movies / watch pages (ResourceSet
-- habbowood; the code has the same Danish fallbacks).
--
-- Only inserts what isn't there yet. Rollback: DELETE the page's boxes and the page (slug entertainment/habbowood).
-- =====================================================================

INSERT INTO cms_pages (news_header, name, slug, display_header, news, design, hidden)
SELECT NULL, 'Habbowood', 'entertainment/habbowood', 0, 0, 'habbowood', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_pages WHERE slug = 'entertainment/habbowood');

SET @page := (SELECT id FROM cms_pages WHERE slug = 'entertainment/habbowood');

INSERT INTO cms_containers (title, text, page_id, type, `column`, theme, `order`, hidden)
SELECT x.title, x.text, @page, 'container', x.col, x.theme, x.ord, 0
FROM (
    SELECT 'Habbowood' AS title, '{habbowood_intro}' AS text, 1 AS col, 'nobox' AS theme, 0 AS ord
    UNION ALL SELECT 'Habbowood',
        '<p style="text-align: center;"><img src="/c_images/album2036/habbowood.gif" width="141" height="28" alt="Habbowood" /></p>
<p>Velkommen til <strong>Habbowood</strong>, hotellets eget filmstudie! Her instruerer du dine egne film, med dine venner som stjernerne.</p>
<p><a href="/habbowood"><img src="/c_images/album1644/unknown_4.gif" width="45" height="45" align="left" hspace="5" alt="" /></a><a href="/habbowood"><strong>Lav en film!</strong></a><br />Vælg kulisser, skuespillere og replikker, og vis filmen frem, når den er færdig.</p>
<div class="clear"></div>
<p><strong>Bemærk:</strong> Du skal være <strong>logget ind</strong> med din Habbo for at lave film. Det er gratis!</p>
<p><img src="/c_images/album2201/golden_star.gif" width="15" height="15" alt="" /> <a href="#film">Bedst bedømte film</a><br />
<img src="/c_images/album2201/golden_star.gif" width="15" height="15" alt="" /> <a href="#film">Nyeste film</a><br />
<img src="/c_images/album2201/golden_star.gif" width="15" height="15" alt="" /> <a href="/habbowood">Habbowood-studiet</a></p>',
        2, 'portlet-goldenfilm', 0
    UNION ALL SELECT 'Bedst bedømte film', '{habbowood_top}', 3, 'portlet-film', 0
    UNION ALL SELECT 'Nyeste film', '{habbowood_new}', 4, 'portlet-film', 0
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_containers WHERE page_id = @page);

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT x.id, x.value, 'da-DK', 'habbowood', 0
FROM (
    SELECT 'director' AS id, 'Instruktør:' AS value
    UNION ALL SELECT 'end_edit', 'Ret i filmen'
    UNION ALL SELECT 'end_mine_text', 'Alle dine film ligger under Mine film. Der kan du se dem, rette i dem, der ikke er udgivet endnu, og slette dem.'
    UNION ALL SELECT 'end_mine_title', 'Dine film'
    UNION ALL SELECT 'end_new', 'Lav en ny film'
    UNION ALL SELECT 'end_preview', 'Se den først'
    UNION ALL SELECT 'end_publish', 'Udgiv filmen'
    UNION ALL SELECT 'end_published', 'Filmen er udgivet, så alle kan se den og give den stjerner.'
    UNION ALL SELECT 'end_text', 'Når du udgiver filmen, kan alle på Habbo se den og give den stjerner. Du kan ikke rette i den, når den er udgivet.'
    UNION ALL SELECT 'end_title', 'Din film er gemt!'
    UNION ALL SELECT 'end_watch', 'Se filmen'
    UNION ALL SELECT 'menu_main', 'Habbowood'
    UNION ALL SELECT 'menu_mine', 'Mine film'
    UNION ALL SELECT 'menu_shoot', 'Lav en film'
    UNION ALL SELECT 'menu_watch', 'Se film'
    UNION ALL SELECT 'mine_delete', 'Slet'
    UNION ALL SELECT 'mine_delete_confirm', 'Vil du slette filmen? Det kan ikke fortrydes.'
    UNION ALL SELECT 'mine_draft', 'Ikke udgivet'
    UNION ALL SELECT 'mine_empty', 'Tom film'
    UNION ALL SELECT 'mine_help_text', 'Her er alle de film, du har lavet. En film, der ikke er udgivet, kan kun du se, og du kan rette i den. Udgiver du den, kan alle se den og give den stjerner.'
    UNION ALL SELECT 'mine_help_title', 'Om dine film'
    UNION ALL SELECT 'mine_none', 'Du har ikke lavet nogen film endnu.'
    UNION ALL SELECT 'mine_published', 'Udgivet'
    UNION ALL SELECT 'no_movies', 'Der er ingen færdige film endnu. Lav den første i Habbowood-studiet!'
    UNION ALL SELECT 'rating', 'Bedømmelse:'
    UNION ALL SELECT 'votes', 'Stemmer:'
    UNION ALL SELECT 'watch_about', 'Om filmen'
    UNION ALL SELECT 'watch_login', 'Log ind for at give filmen stjerner.'
    UNION ALL SELECT 'watch_preview', 'Filmen er ikke udgivet endnu, så det er kun dig, der kan se den.'
    UNION ALL SELECT 'watch_rate', 'Giv filmen stjerner:'
    UNION ALL SELECT 'watch_top', 'Bedst bedømte film'
    UNION ALL SELECT 'watch_voted', 'Du har givet filmen stjerner. Tak!'
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations l WHERE l.ResourceId = x.id AND l.ResourceSet = 'habbowood' AND l.LocaleId = 'da-DK');

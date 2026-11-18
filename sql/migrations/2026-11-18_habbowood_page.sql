-- =====================================================================
-- RELEASE MIGRATION - 2026-11-18
-- The Habbowood page (/entertainment/habbowood), built like habbo.com's 2007 one
-- =====================================================================
-- A CMS page with the "habbowood" design (Views/Shared/PageDesigns/habbowood.cshtml): the intro animation
-- ({habbowood_intro}) over the best rated and newest films ({habbowood_top}, {habbowood_new}, portlet-film boxes), a gold
-- film strip box with the welcome text on the right, and the ad sidebar. Everything is editable in Housekeeping > Pages
-- or in edit mode. Plus the widgets' texts (ResourceSet habbowood; the code has the same Danish fallbacks).
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
    SELECT 'menu_main' AS id, 'Habbowood' AS value
    UNION ALL SELECT 'menu_shoot', 'Lav en film'
    UNION ALL SELECT 'menu_watch', 'Se film'
    UNION ALL SELECT 'director', 'Instruktør:'
    UNION ALL SELECT 'rating', 'Bedømmelse:'
    UNION ALL SELECT 'votes', 'Stemmer:'
    UNION ALL SELECT 'no_movies', 'Der er ingen færdige film endnu. Lav den første i Habbowood-studiet!'
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations l WHERE l.ResourceId = x.id AND l.ResourceSet = 'habbowood' AND l.LocaleId = 'da-DK');

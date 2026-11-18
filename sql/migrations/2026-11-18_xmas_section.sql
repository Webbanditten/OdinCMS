-- =====================================================================
-- RELEASE MIGRATION - 2026-11-18
-- The Christmas section (like habbo.dk's XMAS 2006): /xmas keeps the calendar, plus Tilbud, Juleaktiviteter,
-- Julekonkurrencer and Send et julekort (the e-card, now e-mailed to the friend), and free Habbo Home decorations.
-- =====================================================================
-- Rollback: prod-staging/xmas-section/rollback.sql (pages, boxes, menu items, setting, texts; the two schema changes
-- are harmless to keep, and the CMS before this release ignores them).
-- =====================================================================

-- A card e-mailed to the friend records when (the address isn't kept).
SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'cms_ecards' AND COLUMN_NAME = 'emailed_at');
SET @sql := IF(@col = 0, 'ALTER TABLE cms_ecards ADD COLUMN emailed_at DATETIME NULL AFTER created_at', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Free Home gifts fetched ({xmas_home_gift}): one per gift and player.
CREATE TABLE IF NOT EXISTS `cms_home_gifts` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `gift_key` VARCHAR(50) NOT NULL,
    `user_id` INT NOT NULL,
    `claimed_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `cms_home_gifts_once` (`gift_key`, `user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- This Christmas's decorations (cms_homes_item_data ids): animated tree, Rasta.Santa, icicles, Christmas lights, a red
-- present, the snowman and the "Xmas Starsky" background.
INSERT INTO settings (setting, value)
SELECT 'cms.xmas_home_gift', '11331,11323,11322,11303,11309,186,2604' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM settings WHERE setting = 'cms.xmas_home_gift');

-- ---------------------------------------------------------------- /xmas (page 38): the calendar stays as it is
UPDATE cms_pages SET name = 'Xmas' WHERE slug = 'xmas' AND name = 'Xmas 2025';
UPDATE cms_containers SET title = 'Jul på Habbo' WHERE id = 144 AND title = 'Xmas 2024 på Habbo';
UPDATE cms_containers
SET text = REPLACE(text, '<br />Check-in for at finde og k&oslash;be disse l&aelig;kre godbidder i kataloget!',
                   '<br />Tjek ind på hotellet, og find godbidderne i Kataloget. <a href="/xmas/tilbud"><b>Se alle juletilbud her »</b></a>')
WHERE id = 145;

SET @xmas := (SELECT id FROM cms_pages WHERE slug = 'xmas');
INSERT INTO cms_containers (title, text, page_id, type, `column`, theme, `order`, hidden, start_date, end_date)
SELECT x.title, x.text, @xmas, 'container', 2, 'maskbox-snow-light', x.ord, 0, x.starts, x.ends
FROM (
    SELECT 'Send et julekort' AS title,
           '<img style="float: right; margin-left: 6px;" src="/c_images/album1777/Gift_cart.gif" alt="" />Har du en god ven på hotellet? Lav et personligt Habbo-julekort med din egen Habbo, en julebaggrund og din hilsen, og send det på e-mail!<br /><br /><a href="/xmas/julekort"><b>Lav dit julekort her »</b></a>' AS text,
           1 AS ord, NULL AS starts, NULL AS ends
    UNION ALL SELECT 'Julepynt til dit Habbo Home', '{xmas_home_gift}', 2, '2026-12-01 00:00:00', '2027-01-01 00:00:00'
    UNION ALL SELECT 'VIND VIND VIND!',
           '<img style="float: left; margin-right: 6px;" src="/c_images/album1777/duck_gift.gif" alt="" />Hele december er der konkurrencer med lækre præmier. Hold øje, for der kommer nye hele tiden!<br /><br /><a href="/xmas/konkurrencer"><b>Til julekonkurrencerne »</b></a>',
           3, NULL, NULL
) x
WHERE @xmas IS NOT NULL AND NOT EXISTS (SELECT 1 FROM cms_containers c WHERE c.page_id = @xmas AND c.title = CONVERT(x.title USING latin1));

-- ---------------------------------------------------------------- the sub-pages
INSERT INTO cms_pages (news_header, name, slug, display_header, news, design, hidden)
SELECT NULL, x.name, x.slug, 1, 0, x.design, 0
FROM (
    SELECT 'Juletilbud' AS name, 'xmas/tilbud' AS slug, 'one_wide_one_medium' AS design
    UNION ALL SELECT 'Juleaktiviteter', 'xmas/aktiviteter', 'one_wide_one_medium'
    UNION ALL SELECT 'Julekonkurrencer', 'xmas/konkurrencer', 'one_wide_one_medium'
    UNION ALL SELECT 'Send et julekort', 'xmas/julekort', 'one_wide'
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_pages p WHERE p.slug = CONVERT(x.slug USING latin1));

SET @tilbud := (SELECT id FROM cms_pages WHERE slug = 'xmas/tilbud');
SET @aktiviteter := (SELECT id FROM cms_pages WHERE slug = 'xmas/aktiviteter');
SET @konkurrencer := (SELECT id FROM cms_pages WHERE slug = 'xmas/konkurrencer');
SET @julekort := (SELECT id FROM cms_pages WHERE slug = 'xmas/julekort');

INSERT INTO cms_containers (title, text, page_id, type, `column`, theme, `order`, hidden)
SELECT x.title, x.text, x.page, 'container', x.col, x.theme, x.ord, x.hid
FROM (
    -- Juletilbud
    SELECT @tilbud AS page, 1 AS col, 0 AS ord, 'maskbox-snow-light' AS theme, 0 AS hid, 'Juletilbud' AS title,
           '<img style="float: right; margin-left: 8px;" src="/c_images/album1777/Present_carrying.gif" alt="" />Mangler dit rum lidt julestemning? Her er julens godbidder. Klik på Køb, så ligger tingene i din hånd, næste gang du tjekker ind på hotellet.<br /><br />{buy_furni_161}<br />{buy_furni_1295}' AS text
    UNION ALL SELECT @tilbud, 2, 0, 'maskbox-snow-light', 0, 'Julemusik',
           'Sæt julemusik på din Trax-maskine! Rasta.Santa har selv valgt dem:<br /><br />{buy_furni_475}<br />{buy_furni_500}<br />{buy_furni_493}'
    UNION ALL SELECT @tilbud, 2, 1, 'maskbox-snow-light', 0, 'Mere i Kataloget',
           'Der er endnu flere julemøbler i Kataloget. Tjek ind på hotellet, og åbn Kataloget!<br /><br /><a href="/client" target="client" onclick="openOrFocusHabbo(this); return false;"><b>Gå til hotellet »</b></a>'
    -- Juleaktiviteter
    UNION ALL SELECT @aktiviteter, 1, 0, 'maskbox-snow-light', 0, 'Juleaktiviteter',
           '<img style="float: left; margin-right: 8px;" src="/c_images/album1777/gift_zydem.gif" alt="" />December er fyldt med julehygge på Habbo! Her er, hvad du kan lave i julen:<br /><br /><b>Julekalenderen</b>: log ind på hotellet hver dag, og få dagens gave. <a href="/xmas">Se kalenderen »</a><br /><br /><b>Sneboldkrig</b>: saml dit hold, og giv den gaz i SnowStorm. De bedste kommer på listen her på siden! <a href="/games/snowstorm">Læs om SnowStorm »</a><br /><br /><b>Julekort</b>: send en julehilsen til dine venner. <a href="/xmas/julekort">Lav et kort »</a><br /><br /><b>Julepynt</b>: hent GRATIS julepynt til dit Habbo Home. <a href="/xmas">Hent det her »</a><br /><br />Hold også øje med nyhederne, for Staff holder juleevents på hotellet hele måneden.'
    UNION ALL SELECT @aktiviteter, 1, 1, 'maskbox-snow-light', 0, 'SnowStorm: månedens bedste', '{highscores_snowstorm_month}'
    UNION ALL SELECT @aktiviteter, 2, 0, 'maskbox-snow-light', 0, 'SnowStorm-mestre', '{champions_snowstorm}'
    -- Julekonkurrencer
    UNION ALL SELECT @konkurrencer, 1, 0, 'maskbox-snow-light', 0, 'VIND VIND VIND!',
           '<img style="float: left; margin-right: 8px;" src="/c_images/album1777/duck_gift.gif" alt="" />Hele julemåneden er der konkurrencer her på Habbo, med lækre præmier! Hold øje med denne side, for der kommer nye konkurrencer hele tiden.<br /><br />Læs reglerne i hver konkurrence, før du deltager. Vinderne får præmierne direkte på hotellet, og deres navne kommer på listen her på siden.'
    -- A draft for staff: fill in the brackets and show it.
    UNION ALL SELECT @konkurrencer, 1, 1, 'maskbox-snow-light', 1, 'Konkurrence: Årets julerum',
           'Byg det mest julede rum på hele hotellet! Pynt dit rum op til jul, giv det et navn med ordet <b>Jul</b>, og [sådan deltager man] senest <b>[dato]</b>.<br /><br /><b>Præmier:</b><br />1. pladsen: [præmie]<br />2. pladsen: [præmie]<br />3. pladsen: [præmie]<br /><br />Skynd dig i gang!'
    UNION ALL SELECT @konkurrencer, 2, 0, 'maskbox-snow-light', 0, 'Vindere',
           '<img style="float: right; margin-left: 6px;" src="/c_images/stickers/xmas_box_darkred.gif" alt="" />Når en konkurrence er slut, kommer vinderne her. Tillykke på forhånd!'
    -- Send et julekort
    UNION ALL SELECT @julekort, 1, 0, 'maskbox-snow-light', 0, 'Send et personligt Habbo-julekort',
           '<img style="float: left; margin-right: 10px;" src="/c_images/album1777/Gift_cart.gif" alt="" />Glæd en ven med et Habbo-julekort! Sådan gør du:<br /><br /><b>1.</b> Vælg din Habbo og en julebaggrund.<br /><b>2.</b> Skriv en overskrift og din julehilsen.<br /><b>3.</b> Skriv din vens navn og e-mail, og send kortet!<br /><br />Er du logget ind, sender vi kortet til din vens e-mail. Du får også et link til kortet, som du selv kan give videre. HUSK: skriv kun en vens e-mail, hvis vennen er med på det.'
    UNION ALL SELECT @julekort, 1, 1, 'nobox', 0, 'Julekort', '{xmas_ecard}'
) x
WHERE x.page IS NOT NULL AND NOT EXISTS (SELECT 1 FROM cms_containers c WHERE c.page_id = x.page AND c.title = CONVERT(x.title USING latin1));

-- ---------------------------------------------------------------- the XMAS tab's sub-menu
-- The tab ran 2026-12-01 to 2026-01-01 (last year's end date), so it never showed: it now ends at New Year 2027.
UPDATE cms_menu SET end_date = '2027-01-01 00:00:00' WHERE href = '/xmas' AND parent = 0 AND end_date = '2026-01-01 00:00:00';
SET @tab := (SELECT id FROM cms_menu WHERE href = '/xmas' AND parent = 0 LIMIT 1);
INSERT INTO cms_menu (parent, `order`, page, icon, state, text, href)
SELECT @tab, x.ord, 0, NULL, 'display', x.text, x.href
FROM (
    SELECT 0 AS ord, 'Xmas' AS text, '/xmas' AS href
    UNION ALL SELECT 1, 'Tilbud', '/xmas/tilbud'
    UNION ALL SELECT 2, 'Juleaktiviteter', '/xmas/aktiviteter'
    UNION ALL SELECT 3, 'Julekonkurrencer', '/xmas/konkurrencer'
    UNION ALL SELECT 4, 'Send et julekort', '/xmas/julekort'
) x
WHERE @tab IS NOT NULL AND NOT EXISTS (SELECT 1 FROM cms_menu m WHERE m.parent = @tab AND m.href = CONVERT(x.href USING latin1));

-- ---------------------------------------------------------------- texts (the code has the same Danish fallbacks)
UPDATE cms_localizations SET Value = 'Når du har sendt et kort, kommer det her. Har du skrevet din vens e-mail på kortet, har vi sendt det til dem. Linket kan du også selv give videre.'
WHERE ResourceId = 'mine_text' AND ResourceSet = 'ecard' AND LocaleId = 'da-DK';

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT x.id, x.value, 'da-DK', x.rs, 0
FROM (
    SELECT 'make_own' AS id, 'Lav dit eget Habbo-julekort her!' AS value, 'ecard' AS rs
    UNION ALL SELECT 'login_to_mail', 'Log ind, hvis kortet skal sendes direkte til din vens e-mail. Ellers får du et link til kortet, som du selv kan give videre.', 'ecard'
    UNION ALL SELECT 'emailed', 'sendt på e-mail', 'ecard'
    UNION ALL SELECT 'ecard_subject', '{sender} har sendt dig et Habbo-julekort!', 'emails'
    UNION ALL SELECT 'ecard_header', 'Du har fået et Habbo-julekort!', 'emails'
    UNION ALL SELECT 'ecard_hello', 'Hej {name}!', 'emails'
    UNION ALL SELECT 'ecard_hello_anon', 'Hej!', 'emails'
    UNION ALL SELECT 'ecard_body', '{sender} har lavet et julekort til dig på Habbo Danmark.', 'emails'
    UNION ALL SELECT 'ecard_button', 'Se dit julekort', 'emails'
    UNION ALL SELECT 'ecard_footer', 'Du får denne e-mail, fordi en Habbo skrev din adresse på et julekort. Vi gemmer ikke din adresse.', 'emails'
    UNION ALL SELECT 'home_gift_text', 'Pynt op til jul! Hent klistermærker og en julebaggrund til dit Habbo Home, helt GRATIS.', 'xmas'
    UNION ALL SELECT 'home_gift_button', 'Hent julepynt', 'xmas'
    UNION ALL SELECT 'home_gift_login', 'Log ind, og hent GRATIS julepynt til dit Habbo Home!', 'xmas'
    UNION ALL SELECT 'home_gift_ok', 'Så er julepynten din! Du finder den i dit Habbo Home under Inventar.', 'xmas'
    UNION ALL SELECT 'home_gift_taken', 'Du har allerede hentet årets julepynt. Glædelig jul!', 'xmas'
    UNION ALL SELECT 'home_gift_go', 'Gå til dit Habbo Home »', 'xmas'
    UNION ALL SELECT 'home_gift_closed', 'Julepynten kan ikke hentes lige nu.', 'xmas'
    UNION ALL SELECT 'home_gift_none', 'Julepynten er på vej. Kig forbi igen senere!', 'xmas'
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations l WHERE l.ResourceId = CONVERT(x.id USING utf8mb3) AND l.ResourceSet = CONVERT(x.rs USING utf8mb3) AND l.LocaleId = 'da-DK');

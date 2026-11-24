-- =====================================================================
-- RELEASE MIGRATION - 2026-11-24
-- Habbo Home: the 2006 web store and inventory, and spelling fixes in the Home texts
-- =====================================================================
-- The edit toolbar is Noter / Klistermærker / Widgets / Baggrunde (js/myhabbo-store.js): each opens that type's
-- inventory, "Køb ..." its store, and the note editor sells notes. New texts below; the rest are typo fixes in
-- existing texts (each UPDATE only changes the old wording, so texts edited since are left alone).
--
-- Idempotent: inserts only missing keys, updates only rows that still have the old text.
-- Rollback: the old values are in the WHERE clauses; the new keys can stay (nothing else reads them).
-- =====================================================================

-- ---------------------------------------------------------------- new texts

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT k.id, k.val, 'da-DK', 'habbohome', 0
FROM (
          SELECT 'store_purchase_stickers' AS id, 'Køb klistermærker' AS val
    UNION SELECT 'store_purchase_backgrounds', 'Køb baggrunde'
    UNION SELECT 'store_open_inventory', 'Åbn inventar'
    UNION SELECT 'store_confirm_question', 'Er du sikker på, at du vil købe dette produkt?'
    UNION SELECT 'store_price_is', 'Prisen er'
    UNION SELECT 'store_not_purchasable', 'Du kan ikke købe dette produkt.'
    UNION SELECT 'note_purchase', 'Køb noter'
    UNION SELECT 'note_none', 'Du har ikke flere noter.'
    UNION SELECT 'store_coins', 'mønter'
) k
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations c WHERE c.ResourceId = k.id AND c.ResourceSet = 'habbohome' AND c.LocaleId = 'da-DK');

-- the store's preview column is 110 px wide (as habbo.com's): "Køb Habbo Mønter" took two lines there
UPDATE cms_localizations SET Value = 'Køb mønter'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'store_getcredits' AND Value = 'Køb Habbo Mønter';

-- ---------------------------------------------------------------- spelling

UPDATE cms_localizations SET Value = 'Annuller'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId IN ('cancel', 'cancel_editing') AND Value = 'Anullere';

UPDATE cms_localizations SET Value = 'Fortsæt'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'store_continue' AND Value = 'Forsæt';

UPDATE cms_localizations
SET Value = 'Baggrundsbilledet, du har valgt, bliver på siden, indtil du vælger et andet eller lukker vinduet. Hvis du vil beholde baggrunden, skal du købe den og vælge den i dit inventar.'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'store_background_warning' AND Value LIKE 'Baggrunds billedet du har valgt%';

UPDATE cms_localizations SET Value = 'Venneanmodningen er sendt!'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'add_friend_message' AND Value = 'Venne anmodningen er sendt!';

UPDATE cms_localizations SET Value = REPLACE(REPLACE(Value, 'Det ikke tilladt', 'Det er ikke tilladt'), 'Dette inkludere ', 'Dette inkluderer ')
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'default_note1' AND Value LIKE '%Det ikke tilladt%';

UPDATE cms_localizations SET Value = REPLACE(Value, 'hotel værelser', 'hotelværelser')
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'default_note2' AND Value LIKE '%hotel værelser%';

-- also: the toolbar's "Inventar" became "Widgets"
UPDATE cms_localizations
SET Value = 'For at tilføje din venneliste til din side skal du trykke på Rediger og derefter på Widgets. Når du har placeret den, kan du flytte rundt på den og skifte dens udseende.'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'default_note3' AND Value LIKE 'For at tilføje din venneliste til din side, skal du trykke på rediger og trykke på inventar%';

UPDATE cms_localizations SET Value = 'Du har nået det maksimale antal elementer på dit Habbo Home'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'error_limit' AND Value = 'Du et nået det maksimale antal elementer for dit Habbo Home';

UPDATE cms_localizations SET Value = 'Gruppetype'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'edit_group_type' AND Value = 'Gruppe type';

UPDATE cms_localizations SET Value = 'Gruppeinfo'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'group_info' AND Value = 'Gruppe Info';

UPDATE cms_localizations SET Value = REPLACE(Value, 'administrator rettigheder', 'administratorrettigheder')
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'groups_confirm_give_rights' AND Value LIKE '%administrator rettigheder%';

UPDATE cms_localizations SET Value = REPLACE(Value, 'du  vil', 'du vil')
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'group_select_message' AND Value LIKE '%du  vil%';

UPDATE cms_localizations SET Value = REPLACE(Value, 'gruppe typen', 'gruppetypen')
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'group_type_warning' AND Value LIKE '%gruppe typen%';

UPDATE cms_localizations SET Value = 'Medlemmer'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'guestbook_membersonly' AND Value = 'guestbook_membersonly';

UPDATE cms_localizations SET Value = 'Der er ingen indlæg i gæstebogen.'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'guestbook_noentries' AND Value = 'Der ingen indlæg i gæstebogen.';

UPDATE cms_localizations SET Value = 'Du må højst skrive 200 tegn.'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'guestbook_warning_maxlength_200' AND Value = 'Det maksimalt tilladt at skrive 200 karaktere';

UPDATE cms_localizations SET Value = 'Er du sikker på, at du vil fjerne denne note? Det kan ikke fortrydes.'
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'stickie_warning_delete' AND Value LIKE 'Er du sikker på du vil fjerne denne stickie?%';

-- the group address pointed at an old domain
UPDATE cms_localizations SET Value = REPLACE(Value, 'https://habbo.webbanditten.dk/groups/', 'https://habbo-danmark.com/groups/')
WHERE ResourceSet = 'habbohome' AND LocaleId = 'da-DK' AND ResourceId = 'url_success' AND Value LIKE '%habbo.webbanditten.dk%';

UPDATE cms_homes_categories SET name = 'Advertisements' WHERE name = 'Advertisments';

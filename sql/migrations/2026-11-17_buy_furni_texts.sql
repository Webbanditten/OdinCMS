-- =====================================================================
-- RELEASE MIGRATION - 2026-11-17
-- Furni for sale in news and page boxes ({buy_furni_ID}): texts for what can't be bought
-- =====================================================================
-- The website only sells what any player can buy in the game's catalogue (FurniShopService). The purchase dialog and
-- the widget say so with these texts; the code falls back to the same Danish text while they are missing.
--
-- Fully idempotent: only inserts keys that aren't there yet.
-- =====================================================================

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'furni_not_for_sale', 'Dette møbel kan ikke købes på websitet.', 'da-DK', 'shared', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'furni_not_for_sale' AND ResourceSet = 'shared' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'furni_not_for_sale_short', 'Kan ikke købes lige nu.', 'da-DK', 'shared', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'furni_not_for_sale_short' AND ResourceSet = 'shared' AND LocaleId = 'da-DK');

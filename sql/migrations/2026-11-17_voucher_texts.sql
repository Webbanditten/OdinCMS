-- =====================================================================
-- RELEASE MIGRATION - 2026-11-17
-- Vouchers with furni: texts for the website's redeem dialog
-- =====================================================================
-- The redeem dialog (Views/Credits/Redeem.cshtml) now says what a code gave: credits and/or furni.
-- The view falls back to the same Danish text while these rows are missing.
--
-- Fully idempotent: only inserts keys that aren't there yet.
-- =====================================================================

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'success_code_credits', 'Du fik %credits% Habbo Mønter.', 'da-DK', 'credits', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'success_code_credits' AND ResourceSet = 'credits' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'success_code_furni', 'Du fik disse ting. De ligger i din hånd på hotellet:', 'da-DK', 'credits', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'success_code_furni' AND ResourceSet = 'credits' AND LocaleId = 'da-DK');

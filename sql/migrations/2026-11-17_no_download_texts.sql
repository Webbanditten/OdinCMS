-- =====================================================================
-- RELEASE MIGRATION - 2026-11-17
-- The downloadable client is retired: the client pages' message no longer offers it
-- =====================================================================
-- Odin's client.version (20) keeps downloaded clients from logging in, and /hotel/download is hidden. The messages
-- from 2026-11-17_client_fallback_texts.sql pointed to the download; these replace them. Run after that one.
-- =====================================================================

UPDATE cms_localizations SET Value = '<h3>Øv! Hotellet kan ikke starte lige nu</h3><p>Browserversionen af Habbo er ikke tilgængelig i øjeblikket. Prøv igen om lidt.</p>' WHERE ResourceSet = 'shared' AND ResourceId = 'noshockwave' AND LocaleId = 'da-DK';
UPDATE cms_localizations SET Value = '<h3>Øv! Hotellet kunne ikke starte i din browser</h3><p>Prøv at genindlæse siden. Virker det stadig ikke, så er din browser måske for gammel: prøv en nyere udgave af Chrome, Firefox, Edge eller Safari.</p>' WHERE ResourceSet = 'shared' AND ResourceId = 'webclient_unsupported' AND LocaleId = 'da-DK';

-- =====================================================================
-- RELEASE MIGRATION - 2026-11-19
-- Profile page as habbo.com's 2006 profile: one page per tab, privacy tab
-- =====================================================================
-- Views/Profile (ProfileController). The views fall back to the same Danish text while these rows are missing; the rows
-- make them editable under Housekeeping > Localization (set profile). No schema change: the privacy tab writes Odin's
-- users.allow_friend_requests and users.allow_stalking.
--
-- Fully idempotent. Rollback: DELETE FROM cms_localizations WHERE ResourceSet = 'profile' AND ResourceId IN (...the keys below...).
-- =====================================================================

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'tab_habbo', 'Habbo', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'tab_habbo' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'tab_mission', 'Mission', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'tab_mission' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'tab_email', 'Email', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'tab_email' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'tab_password', 'Adgangskode', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'tab_password' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'tab_privacy', 'Privatliv', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'tab_privacy' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'label_name', 'Habbo-navn:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'label_name' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'mission_title', 'Ret din mission', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'mission_title' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'email_title', 'Ret din email', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'email_title' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_title', 'Skift adgangskode', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_title' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_current_title', 'Din nuværende adgangskode', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_current_title' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_new_title', 'Vælg en ny adgangskode', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_new_title' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_button', 'Skift adgangskode', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_button' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_title', 'Dine indstillinger for privatliv', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_title' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_requests', 'Venneansøgninger:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_requests' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_requests_on', 'Andre Habboer må ansøge mig som ven', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_requests_on' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_requests_off', 'Ingen må ansøge mig som ven', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_requests_off' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_follow', 'Følg efter:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_follow' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_follow_on', 'Mine venner må følge efter mig til det rum, jeg er i', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_follow_on' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_follow_off', 'Ingen må følge efter mig', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_follow_off' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_note', 'Ændringerne gælder fra næste gang, du går ind på hotellet.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_note' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'privacy_saved', 'Dine indstillinger er gemt. De gælder fra næste gang, du går ind på hotellet.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'privacy_saved' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

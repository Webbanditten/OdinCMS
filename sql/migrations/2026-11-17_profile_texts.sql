-- =====================================================================
-- RELEASE MIGRATION - 2026-11-17
-- Profile page: mission, email and password tabs
-- =====================================================================
-- Views/Profile/Index.cshtml (ProfileController.UpdateMission/UpdateEmail/UpdatePassword). The view and controller fall
-- back to the same Danish text while these rows are missing. The four tab labels were stored with their key as the
-- value ("change_mission"), so the buttons showed the key; they get real labels unless someone already changed them.
--
-- Fully idempotent.
-- =====================================================================

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'mission_intro', 'Din mission står under din Habbo i hotellet og på din Habbo Home. Fortæl de andre, hvad du er ude på!', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'mission_intro' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'mission_label', 'Din mission:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'mission_label' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'mission_saved', 'Din mission er gemt.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'mission_saved' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'email_current', 'Din nuværende email:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'email_current' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'email_new', 'Ny email:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'email_new' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'email_password', 'Din adgangskode (for at være sikker på, at det er dig):', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'email_password' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'email_saved', 'Din email er ændret.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'email_saved' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_current', 'Nuværende adgangskode:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_current' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_new', 'Ny adgangskode (mindst 6 tegn):', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_new' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_repeat', 'Gentag ny adgangskode:', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_repeat' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_saved', 'Din adgangskode er ændret.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_saved' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'password_mismatch', 'De to nye adgangskoder er ikke ens.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'password_mismatch' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'wrong_password', 'Adgangskoden passer ikke.', 'da-DK', 'profile', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'wrong_password' AND ResourceSet = 'profile' AND LocaleId = 'da-DK');

UPDATE cms_localizations SET Value = 'Udseende' WHERE ResourceSet = 'habbohome' AND ResourceId = 'change_look' AND (Value = ResourceId OR Value IS NULL OR Value = '');
UPDATE cms_localizations SET Value = 'Mission' WHERE ResourceSet = 'habbohome' AND ResourceId = 'change_mission' AND (Value = ResourceId OR Value IS NULL OR Value = '');
UPDATE cms_localizations SET Value = 'Email' WHERE ResourceSet = 'habbohome' AND ResourceId = 'change_email' AND (Value = ResourceId OR Value IS NULL OR Value = '');
UPDATE cms_localizations SET Value = 'Adgangskode' WHERE ResourceSet = 'habbohome' AND ResourceId = 'change_password' AND (Value = ResourceId OR Value IS NULL OR Value = '');

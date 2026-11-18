-- Help tool wording after habbo.dk's own (Wayback Machine, 2006): the tool's Danish name "Habbo Hjælpeværktøj" and the
-- Hjælp og Sikkerhed menu's "Kontakt os". Run with --default-character-set=utf8mb4.
-- Rollback:
--   UPDATE cms_localizations SET Value = 'Habbo Hjælp' WHERE ResourceSet = 'iot' AND ResourceId = 'title' AND LocaleId = 'da-DK';
--   UPDATE cms_menu SET text = 'Kontakt Habbo' WHERE href = '/iot/go';
UPDATE cms_localizations SET Value = 'Habbo Hjælpeværktøj' WHERE ResourceSet = 'iot' AND ResourceId = 'title' AND LocaleId = 'da-DK';
UPDATE cms_menu SET text = 'Kontakt os' WHERE href = '/iot/go';

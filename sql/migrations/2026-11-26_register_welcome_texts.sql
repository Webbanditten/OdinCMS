-- The page a new Habbo sees after signing up (Views/Account/Done.cshtml): two texts were missing their verb.
-- Rollback: 'Tillykke du nu en Habbo!' and 'Du nu en Habbo.'
UPDATE cms_localizations SET Value = 'Tillykke, du er nu en Habbo!'
 WHERE ResourceSet = 'register' AND ResourceId = 'done' AND LocaleId = 'da-DK' AND Value = 'Tillykke du nu en Habbo!';
UPDATE cms_localizations SET Value = 'Du er nu en Habbo.'
 WHERE ResourceSet = 'register' AND ResourceId = 'now_a_habbo' AND LocaleId = 'da-DK' AND Value = 'Du nu en Habbo.';

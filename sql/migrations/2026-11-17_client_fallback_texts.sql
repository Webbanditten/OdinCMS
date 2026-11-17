-- =====================================================================
-- RELEASE MIGRATION - 2026-11-17
-- Client pages: the message when the hotel can't start in the browser
-- =====================================================================
-- Views/Client/_ClientUnavailable.cshtml. "noshockwave" (/client: no Shockwave and the browser client switched off)
-- used to tell everyone without Shockwave to download the client; it now says the browser version isn't available
-- right now and offers the download. "webclient_unsupported" is new (/client/web: no WebAssembly or DirPlayer failed
-- to load). The buttons ("Hent Habbo Hotel", "Prøv igen") are client_download / client_reload. The view falls back
-- to the same Danish texts while rows are missing.
--
-- Rollback of noshockwave (the previous text):
--   UPDATE cms_localizations SET Value = 0x3C703E0A3C696D67207372633D222F696D616765732F6672616E6B5F31392E6769662220616C743D226672616E6B2220616C69676E3D22726967687422202F3E0A48616C6CC3B86A210A3C62722F3E0A5669206B616E20736520647520696B6B65206861722053686F636B7761766520696E7374616C6C657265742073C3A520646572666F722020616E626566616C65722066616B7469736B2064752062656E7974746572206469672061662064656E6E65206D756C696768656420666F7220617420646F776E6C6F61646520656E20616620766F72657320696E7374616C6C6572626172652076657273696F6E65722C206461206C616E677420646520666C657374652062726F777365726520692064616720696B6B65206B616E20616676696B6C6520466C61736820656C6C65722053686F636B776176652E0A3C62722F3E0A3C7374726F6E673E447520736B616C20626C6F742067C3B872652066C3B86C67656E64653A3C2F7374726F6E673E0A3C62722F3E0A3C6F6C3E0A3C6C693E46C3B872737420736B616C206475203C6120687265663D222F686F74656C2F646F776E6C6F6164223E646F776E6C6F6164653C2F613E20686F74656C6C65743C2F6C693E0A3C6C693E496E7374616C6C65726520486162626F20486F74656C3C2F6C693E0A3C6C693E537461727420486162626F20486F74656C3C2F6C693E0A3C6C693E4475206E75206B6C61722074696C206174207370696C6C65213C2F6C693E0A3C2F6F6C3E202020202020202020202020202020200A3C2F703E0A0A3C703E3C6120687265663D222F686F74656C2F646F776E6C6F6164223E5472796B2068657220666F7220617420646F776E6C6F616420486162626F3C2F613E3C2F703E
--   WHERE ResourceSet = 'shared' AND ResourceId = 'noshockwave' AND LocaleId = 'da-DK';
-- =====================================================================

UPDATE cms_localizations SET Value = '<h3>Øv! Hotellet kan ikke starte i din browser lige nu</h3><p>Din browser kan ikke køre Shockwave, og browserversionen af Habbo er ikke tilgængelig i øjeblikket.</p><p>Du kan stadig spille med den downloadbare Habbo-klient til Windows og Mac.</p>' WHERE ResourceSet = 'shared' AND ResourceId = 'noshockwave' AND LocaleId = 'da-DK';

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'noshockwave', '<h3>Øv! Hotellet kan ikke starte i din browser lige nu</h3><p>Din browser kan ikke køre Shockwave, og browserversionen af Habbo er ikke tilgængelig i øjeblikket.</p><p>Du kan stadig spille med den downloadbare Habbo-klient til Windows og Mac.</p>', 'da-DK', 'shared', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'noshockwave' AND ResourceSet = 'shared' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'webclient_unsupported', '<h3>Øv! Hotellet kunne ikke starte i din browser</h3><p>Prøv at genindlæse siden. Virker det stadig ikke, så er din browser måske for gammel: prøv en nyere udgave af Chrome, Firefox, Edge eller Safari.</p><p>Du kan også spille med den downloadbare Habbo-klient til Windows og Mac.</p>', 'da-DK', 'shared', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'webclient_unsupported' AND ResourceSet = 'shared' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'client_download', 'Hent Habbo Hotel', 'da-DK', 'shared', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'client_download' AND ResourceSet = 'shared' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'client_reload', 'Prøv igen', 'da-DK', 'shared', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'client_reload' AND ResourceSet = 'shared' AND LocaleId = 'da-DK');

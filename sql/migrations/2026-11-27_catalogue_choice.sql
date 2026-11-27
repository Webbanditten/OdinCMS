-- Both catalogues in the hotel: a player picks the classic one (hh_cat_code) or Habbo r31's (hh_cat_new, folders) on the
-- profile's "Katalog" tab (Services/CatalogueChoice.cs). Run before the CMS that maps users.catalogue_v31.
-- Odin's update.1.53.sql adds catalogue_pages.tree_order (the v31 tree's own order).
ALTER TABLE users ADD COLUMN IF NOT EXISTS catalogue_v31 TINYINT(1) NOT NULL DEFAULT 0;
-- off | staff (ranks with fuse_catalogue_administrator) | everyone
INSERT INTO settings (setting, value) SELECT 'cms.catalogue.choice', 'staff' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM settings WHERE setting = 'cms.catalogue.choice');
-- Rollback: DELETE FROM settings WHERE setting = 'cms.catalogue.choice'; ALTER TABLE users DROP COLUMN catalogue_v31;
--           (roll the CMS back first: it maps the column)

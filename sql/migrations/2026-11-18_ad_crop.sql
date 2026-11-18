-- Billboard crop (Housekeeping > Ads: drag the preview, zoom): which part of a flat billboard picture the frames' screens
-- show. 0.5 / 0.5 / 1 = the middle, as before. Apply before deploying the CMS that maps the columns.
-- Rollback: ALTER TABLE cms_ads DROP COLUMN crop_x, DROP COLUMN crop_y, DROP COLUMN crop_zoom;
ALTER TABLE `cms_ads`
    ADD COLUMN `crop_x` DOUBLE NOT NULL DEFAULT 0.5 AFTER `fit`,
    ADD COLUMN `crop_y` DOUBLE NOT NULL DEFAULT 0.5 AFTER `crop_x`,
    ADD COLUMN `crop_zoom` DOUBLE NOT NULL DEFAULT 1 AFTER `crop_y`;

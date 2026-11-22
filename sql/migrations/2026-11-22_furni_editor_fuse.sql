-- The furni editor in Housekeeping (docs/furni-editor-housekeeping.md): its own fuse, granted to no rank.
-- Give it in Housekeeping > Ranks (a rank of its own keeps it to named people: fuses are per rank).
INSERT INTO fuses (fuse, user_group, description)
VALUES ('housekeeping_furni_editor', 'CUSTOM', 'Furni editor: publish furni casts to the hotel (hof_furni) and their definitions')
ON DUPLICATE KEY UPDATE description = VALUES(description);

-- Rollback:
-- DELETE FROM rank_rights WHERE fuse = 'housekeeping_furni_editor';
-- DELETE FROM fuses WHERE fuse = 'housekeeping_furni_editor';

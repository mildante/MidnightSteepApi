BEGIN;
LOCK TABLE "Furnitures" IN SHARE ROW EXCLUSIVE MODE;
DO $$
DECLARE
    sequence_name text := pg_get_serial_sequence('"Furnitures"', 'id');
    highest_id bigint;
BEGIN
    IF EXISTS (SELECT asset_name FROM "Furnitures"
               WHERE asset_name IN ('DarkWoodFloor', 'LightBueWallpaper')
               GROUP BY asset_name HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate room covering asset_name in catalog';
    END IF;
    IF sequence_name IS NOT NULL THEN
        SELECT GREATEST(COALESCE(max(id), 0),
            COALESCE(pg_sequence_last_value(sequence_name::regclass), 0)) INTO highest_id FROM "Furnitures";
        IF highest_id > 0 THEN PERFORM setval(sequence_name::regclass, highest_id, true); END IF;
    END IF;
END $$;
INSERT INTO "Furnitures" (name, price, category, max_stack, comfort, unlocked_level_id, asset_name, placement_layer)
SELECT 'Тёмный паркет', 0, 'floor', 1, 0, 0, 'DarkWoodFloor', 'Floor'
WHERE NOT EXISTS (SELECT 1 FROM "Furnitures" WHERE asset_name = 'DarkWoodFloor');
INSERT INTO "Furnitures" (name, price, category, max_stack, comfort, unlocked_level_id, asset_name, placement_layer)
SELECT 'Светло-голубые обои', 0, 'wallpaper', 1, 0, 0, 'LightBueWallpaper', 'Wall'
WHERE NOT EXISTS (SELECT 1 FROM "Furnitures" WHERE asset_name = 'LightBueWallpaper');
COMMIT;

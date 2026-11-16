BEGIN;
LOCK TABLE "Furnitures" IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE starter_furniture (
    name text, price integer, category text, max_stack integer, comfort integer,
    asset_name text PRIMARY KEY, placement_layer text
) ON COMMIT DROP;
INSERT INTO starter_furniture VALUES
    ('Розовый фонарь',           15, 'decoration',       20, 0, 'PinkLamp',       'Floor'),
    ('Деревянный стол',          15, 'furniture',        10, 0, 'Table',          'Floor'),
    ('Красный стул',             10, 'furniture',        10, 0, 'RedChair',       'Floor'),
    ('Плита с чайником',          0, 'furniture',         1, 0, 'HoneyKettle',    'Floor'),
    ('Полка с чайными листьями',  0, 'ingredient_crate',   1, 0, 'CrateTeaLeaves', 'Floor'),
    ('Полка для сладостей',       0, 'furniture',         1, 0, 'TeaShelf',       'Floor'),
    ('Барная стойка',             0, 'furniture',         1, 0, 'BarCounter',     'Floor'),
    ('Стандартная мусорка', 0, 'furniture', 10, 0, 'HoneyTrashBin', 'Floor'),
    ('Синие обои', 0, 'wallpaper', 1, 0, 'BlueWallpaper', 'Wall'),
    ('Деревянный паркет', 0, 'floor', 1, 0, 'WoodFloor', 'Floor');

DO $$
DECLARE
    sequence_name text := pg_get_serial_sequence('"Furnitures"', 'id');
    highest_id bigint;
BEGIN
    IF EXISTS (SELECT f.asset_name FROM "Furnitures" f
               JOIN starter_furniture s ON s.asset_name = f.asset_name
               GROUP BY f.asset_name HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate starter asset_name in Furnitures; resolve ambiguous keys first.';
    END IF;

    IF sequence_name IS NOT NULL THEN
        SELECT GREATEST(COALESCE(max(id), 0),
                        COALESCE(pg_sequence_last_value(sequence_name::regclass), 0))
        INTO highest_id FROM "Furnitures";
        IF highest_id > 0 THEN
            PERFORM setval(sequence_name::regclass, highest_id, true);
        END IF;
    END IF;
END $$;

INSERT INTO "Furnitures" (name, price, category, max_stack, comfort,
                          unlocked_level_id, asset_name, placement_layer)
SELECT s.name, s.price, s.category, s.max_stack, s.comfort,
       0, s.asset_name, s.placement_layer
FROM starter_furniture s
WHERE NOT EXISTS (SELECT 1 FROM "Furnitures" f WHERE f.asset_name = s.asset_name);

COMMIT;

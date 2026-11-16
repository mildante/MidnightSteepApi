BEGIN;
LOCK TABLE "Furnitures" IN SHARE ROW EXCLUSIVE MODE;

DO $$
DECLARE
    sequence_name text := pg_get_serial_sequence('"Furnitures"', 'id');
    highest_id bigint;
BEGIN
    IF (SELECT count(*) FROM "Levels" WHERE level_number BETWEEN 1 AND 5) <> 5 THEN
        RAISE EXCEPTION 'Run 01_reference_data.sql first: levels 1-5 are required.';
    END IF;
    IF EXISTS (SELECT asset_name FROM "Furnitures" GROUP BY asset_name HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate asset_name in Furnitures; resolve ambiguous keys before seeding.';
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

CREATE TEMP TABLE seed_furniture (
    name text, price integer, category text, max_stack integer, comfort integer,
    level_number integer, asset_name text PRIMARY KEY, placement_layer text
) ON COMMIT DROP;
INSERT INTO seed_furniture VALUES
    ('Деревянный пол',          0, 'floor',      1, 0, 1, 'wood_floor',       'Floor'),
    ('Ночные обои',             0, 'wallpaper',  1, 0, 1, 'night_wallpaper',  'Wall'),
    ('Прилавок',               0, 'furniture',  2, 1, 1, 'counter',          'Floor'),
    ('Печь для чая',            0, 'furniture',  2, 0, 1, 'stove',            'Floor'),
    ('Гостевой стол',          60, 'furniture',  8, 2, 1, 'guest_table',      'Floor'),
    ('Табурет',                20, 'furniture', 16, 1, 1, 'stool',            'Floor'),
    ('Чайная чашка',           10, 'decoration',16, 1, 1, 'tea_cup',          'Table'),
    ('Банка для чая',          15, 'decoration', 8, 1, 1, 'tea_jar',          'Table'),
    ('Ящик чайных листьев',     0, 'furniture',  4, 0, 1, 'crate_tea_leaves', 'Floor'),
    ('Ящик сахара',            0, 'furniture',  4, 0, 1, 'crate_sugar',      'Floor'),
    ('Ящик молока',            0, 'furniture',  4, 0, 2, 'crate_milk',       'Floor'),
    ('Ящик лимонов',           0, 'furniture',  4, 0, 2, 'crate_lemon',      'Floor'),
    ('Ящик мёда',              0, 'furniture',  4, 0, 3, 'crate_honey',      'Floor'),
    ('Ящик ягод',              0, 'furniture',  4, 0, 3, 'crate_berries',    'Floor'),
    ('Ящик лаванды',           0, 'furniture',  4, 0, 4, 'crate_lavender',   'Floor'),
    ('Ящик лепестков розы',     0, 'furniture',  4, 0, 4, 'crate_rose_petals','Floor'),
    ('Полка',                 80, 'furniture',  4, 2, 2, 'shelf',            'Floor'),
    ('Комнатное растение',    40, 'decoration', 8, 2, 2, 'plant',            'Floor'),
    ('Настенный фонарь',      50, 'decoration', 8, 2, 2, 'wall_lantern',     'Wall'),
    ('Ковёр',                 70, 'decoration', 4, 3, 3, 'rug',              'Floor'),
    ('Поднос с десертами',    45, 'decoration', 4, 2, 3, 'dessert_tray',     'Table'),
    ('Чайная вывеска',        90, 'decoration', 1, 4, 4, 'tea_sign',         'Wall'),
    ('Розовый фонарь',       120, 'decoration', 4, 5, 5, 'rose_lantern',     'Table');

UPDATE "Furnitures" f SET name = s.name, price = s.price, category = s.category,
    max_stack = s.max_stack, comfort = s.comfort, unlocked_level_id = l.id,
    placement_layer = s.placement_layer
FROM seed_furniture s JOIN "Levels" l ON l.level_number = s.level_number
WHERE f.asset_name = s.asset_name;

INSERT INTO "Furnitures" (name, price, category, max_stack, comfort, unlocked_level_id,
                          asset_name, placement_layer)
SELECT s.name, s.price, s.category, s.max_stack, s.comfort, l.id,
       s.asset_name, s.placement_layer
FROM seed_furniture s JOIN "Levels" l ON l.level_number = s.level_number
WHERE NOT EXISTS (SELECT 1 FROM "Furnitures" f WHERE f.asset_name = s.asset_name);

COMMIT;

SELECT f.id, f.name, f.category, f.price, l.level_number, f.asset_name
FROM "Furnitures" f JOIN "Levels" l ON l.id = f.unlocked_level_id
ORDER BY l.level_number, f.id;

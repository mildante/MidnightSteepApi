BEGIN;

LOCK TABLE "Characters", "Levels", "Ingredients", "Drinks", "Recipes"
    IN SHARE ROW EXCLUSIVE MODE;

DO $$
DECLARE
    table_name text;
    sequence_name text;
    highest_id bigint;
    sequence_value bigint;
BEGIN
    IF EXISTS (SELECT 1 FROM "Characters"
               WHERE (id = 1 AND name <> 'Мальчик') OR (id = 2 AND name <> 'Девочка')
                  OR (name = 'Мальчик' AND id <> 1) OR (name = 'Девочка' AND id <> 2)) THEN
        RAISE EXCEPTION 'Character IDs 1/2 conflict with existing data. Check Characters before seeding.';
    END IF;
    IF EXISTS (SELECT asset_name FROM "Ingredients" GROUP BY asset_name HAVING count(*) > 1)
       OR EXISTS (SELECT asset_name FROM "Drinks" GROUP BY asset_name HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate asset_name in Ingredients or Drinks; resolve ambiguous keys before seeding.';
    END IF;
    FOREACH table_name IN ARRAY ARRAY['Characters', 'Levels', 'Ingredients', 'Drinks', 'Recipes'] LOOP
        sequence_name := pg_get_serial_sequence(format('%I', table_name), 'id');
        IF sequence_name IS NOT NULL THEN
            EXECUTE format('SELECT COALESCE(max(id), 0) FROM %I', table_name) INTO highest_id;
            sequence_value := COALESCE(pg_sequence_last_value(sequence_name::regclass), 0);
            IF GREATEST(highest_id, sequence_value) > 0 THEN
                PERFORM setval(sequence_name::regclass, GREATEST(highest_id, sequence_value), true);
            END IF;
        END IF;
    END LOOP;
END $$;

INSERT INTO "Characters" (id, name) VALUES (1, 'Мальчик'), (2, 'Девочка')
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;

SELECT setval(pg_get_serial_sequence('"Characters"', 'id'),
    GREATEST((SELECT max(id) FROM "Characters"),
             COALESCE(pg_sequence_last_value(pg_get_serial_sequence('"Characters"', 'id')::regclass), 0)), true);
INSERT INTO "Levels" (level_number, total_orders, required_orders, required_revenue) VALUES
    (1,  5,  3,  60),
    (2,  8,  5, 100),
    (3, 12,  8, 160),
    (4, 16, 11, 220),
    (5, 20, 15, 300)
ON CONFLICT (level_number) DO UPDATE SET
    total_orders = EXCLUDED.total_orders,
    required_orders = EXCLUDED.required_orders,
    required_revenue = EXCLUDED.required_revenue;

CREATE TEMP TABLE seed_ingredients (name text, asset_name text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO seed_ingredients VALUES
    ('Чайные листья',   'crate_tea_leaves'),
    ('Сахар',          'crate_sugar'),
    ('Молоко',         'crate_milk'),
    ('Лимон',          'crate_lemon'),
    ('Мёд',            'crate_honey'),
    ('Ягоды',          'crate_berries'),
    ('Лаванда',        'crate_lavender'),
    ('Лепестки розы',  'crate_rose_petals');

UPDATE "Ingredients" i SET name = s.name
FROM seed_ingredients s WHERE i.asset_name = s.asset_name;
INSERT INTO "Ingredients" (name, asset_name)
SELECT s.name, s.asset_name FROM seed_ingredients s
WHERE NOT EXISTS (SELECT 1 FROM "Ingredients" i WHERE i.asset_name = s.asset_name);
CREATE TEMP TABLE seed_drinks (
    name text, price integer, cooking_time integer,
    level_number integer, asset_name text PRIMARY KEY
) ON COMMIT DROP;
INSERT INTO seed_drinks VALUES
    ('Чёрный чай',             20,  5, 1, 'drink_black_tea'),
    ('Сладкий чай',            25,  6, 1, 'drink_sweet_tea'),
    ('Чай с молоком',          30,  7, 2, 'drink_milk_tea'),
    ('Чай с лимоном',          30,  7, 2, 'drink_lemon_tea'),
    ('Медовый чай с лимоном',  40,  9, 3, 'drink_honey_lemon_tea'),
    ('Ягодный чай',            40,  9, 3, 'drink_berry_tea'),
    ('Лавандовый чай',         45, 10, 4, 'drink_lavender_tea'),
    ('Розовый чай',            45, 10, 4, 'drink_rose_tea'),
    ('Ночной цветочный чай',   60, 12, 5, 'drink_night_bloom_tea'),
    ('Полуночный ягодный чай', 65, 13, 5, 'drink_midnight_berry_tea');

UPDATE "Drinks" d SET name = s.name, price = s.price, cooking_time = s.cooking_time,
    unlock_level_id = l.id
FROM seed_drinks s JOIN "Levels" l ON l.level_number = s.level_number
WHERE d.asset_name = s.asset_name;
INSERT INTO "Drinks" (name, price, cooking_time, unlock_level_id, asset_name)
SELECT s.name, s.price, s.cooking_time, l.id, s.asset_name
FROM seed_drinks s JOIN "Levels" l ON l.level_number = s.level_number
WHERE NOT EXISTS (SELECT 1 FROM "Drinks" d WHERE d.asset_name = s.asset_name);
CREATE TEMP TABLE seed_recipes (
    drink_asset text, ingredient_asset text,
    PRIMARY KEY (drink_asset, ingredient_asset)
) ON COMMIT DROP;
INSERT INTO seed_recipes VALUES
    ('drink_black_tea',          'crate_tea_leaves'),
    ('drink_sweet_tea',          'crate_tea_leaves'),
    ('drink_sweet_tea',          'crate_sugar'),
    ('drink_milk_tea',           'crate_tea_leaves'),
    ('drink_milk_tea',           'crate_milk'),
    ('drink_lemon_tea',          'crate_tea_leaves'),
    ('drink_lemon_tea',          'crate_lemon'),
    ('drink_honey_lemon_tea',    'crate_tea_leaves'),
    ('drink_honey_lemon_tea',    'crate_lemon'),
    ('drink_honey_lemon_tea',    'crate_honey'),
    ('drink_berry_tea',          'crate_tea_leaves'),
    ('drink_berry_tea',          'crate_berries'),
    ('drink_lavender_tea',       'crate_tea_leaves'),
    ('drink_lavender_tea',       'crate_lavender'),
    ('drink_lavender_tea',       'crate_honey'),
    ('drink_rose_tea',           'crate_tea_leaves'),
    ('drink_rose_tea',           'crate_rose_petals'),
    ('drink_rose_tea',           'crate_sugar'),
    ('drink_night_bloom_tea',    'crate_tea_leaves'),
    ('drink_night_bloom_tea',    'crate_lavender'),
    ('drink_night_bloom_tea',    'crate_rose_petals'),
    ('drink_night_bloom_tea',    'crate_honey'),
    ('drink_midnight_berry_tea', 'crate_tea_leaves'),
    ('drink_midnight_berry_tea', 'crate_berries'),
    ('drink_midnight_berry_tea', 'crate_milk'),
    ('drink_midnight_berry_tea', 'crate_honey');

INSERT INTO "Recipes" (drink_id, ingredient_id)
SELECT d.id, i.id FROM seed_recipes s
JOIN "Drinks" d ON d.asset_name = s.drink_asset
JOIN "Ingredients" i ON i.asset_name = s.ingredient_asset
ON CONFLICT (drink_id, ingredient_id) DO NOTHING;

COMMIT;
SELECT l.level_number, d.name, d.price, d.cooking_time,
       string_agg(i.name || ' × 1', ', ' ORDER BY i.id) AS ingredients
FROM "Drinks" d JOIN "Levels" l ON l.id = d.unlock_level_id
JOIN "Recipes" r ON r.drink_id = d.id
JOIN "Ingredients" i ON i.id = r.ingredient_id
WHERE d.asset_name IN (
    'drink_black_tea', 'drink_sweet_tea', 'drink_milk_tea', 'drink_lemon_tea',
    'drink_honey_lemon_tea', 'drink_berry_tea', 'drink_lavender_tea', 'drink_rose_tea',
    'drink_night_bloom_tea', 'drink_midnight_berry_tea')
GROUP BY l.level_number, d.id, d.name, d.price, d.cooking_time
ORDER BY l.level_number, d.id;

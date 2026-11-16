BEGIN;
LOCK TABLE "Furnitures" IN SHARE ROW EXCLUSIVE MODE;
SELECT setval(pg_get_serial_sequence('"Furnitures"', 'id'), GREATEST(COALESCE((SELECT max(id) FROM "Furnitures"), 1), COALESCE(pg_sequence_last_value(pg_get_serial_sequence('"Furnitures"', 'id')::regclass), 1)), true);
CREATE TEMP TABLE missing_furniture (name text, price int, category text, max_stack int, comfort int, level_number int, asset_name text PRIMARY KEY, placement_layer text) ON COMMIT DROP;
INSERT INTO missing_furniture VALUES
('Тёмная полка с чайными листьями', 30, 'ingredient_crate', 1, 0, 1, 'DarkCrateTeaLeaves', 'Floor'),
('Тёмная полка с сахаром', 30, 'ingredient_crate', 1, 0, 1, 'DarkCrateSugar', 'Floor'),
('Тёмная полка с молоком', 30, 'ingredient_crate', 1, 0, 2, 'DarkCrateMilk', 'Floor'),
('Тёмная полка с лимонами', 30, 'ingredient_crate', 1, 0, 2, 'DarkCrateLemon', 'Floor'),
('Тёмная полка с мёдом', 30, 'ingredient_crate', 1, 0, 3, 'DarkCrateHoney', 'Floor'),
('Тёмная полка с ягодами', 30, 'ingredient_crate', 1, 0, 3, 'DarkCrateBerries', 'Floor'),
('Тёмная полка с лавандой', 30, 'ingredient_crate', 1, 0, 4, 'DarkCrateLavender', 'Floor'),
('Тёмная полка с лепестками розы', 30, 'ingredient_crate', 1, 0, 4, 'DarkCrateRosePetals', 'Floor'),
('Стандартная мусорка', 0, 'furniture', 10, 0, 0, 'HoneyTrashBin', 'Floor'),
('Мусорка с узором', 40, 'furniture', 10, 1, 2, 'HoneyTrashBin 1', 'Floor');
INSERT INTO "Furnitures" (name, price, category, max_stack, comfort, unlocked_level_id, asset_name, placement_layer)
SELECT s.name,s.price,s.category,s.max_stack,s.comfort,CASE WHEN s.level_number = 0 THEN 0 ELSE l.id END,s.asset_name,s.placement_layer
FROM missing_furniture s LEFT JOIN "Levels" l ON l.level_number = s.level_number
WHERE (s.level_number = 0 OR l.id IS NOT NULL) AND NOT EXISTS (SELECT 1 FROM "Furnitures" f WHERE f.asset_name = s.asset_name);
LOCK TABLE "Inventories" IN SHARE ROW EXCLUSIVE MODE;
INSERT INTO "Inventories" (save_id, furniture_id, quantity)
SELECT s.id,f.id,1 FROM "Saveses" s CROSS JOIN "Furnitures" f
WHERE f.asset_name IN ('HoneyTrashBin','BlueWallpaper','WoodFloor')
AND NOT EXISTS (SELECT 1 FROM "Inventories" i WHERE i.save_id = s.id AND i.furniture_id = f.id);
COMMIT;

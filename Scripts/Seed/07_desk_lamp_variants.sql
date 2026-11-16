BEGIN;
LOCK TABLE "Furnitures" IN SHARE ROW EXCLUSIVE MODE;

DO $$
BEGIN
    IF (SELECT count(*) FROM "Furnitures" WHERE asset_name = 'DeskLamp') <> 1 THEN
        RAISE EXCEPTION 'Exactly one DeskLamp catalog row is required';
    END IF;
    IF EXISTS (SELECT asset_name FROM "Furnitures"
               WHERE asset_name IN ('BlueDeskLamp', 'WhiteDeskLamp')
               GROUP BY asset_name HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate desk lamp asset_name in catalog';
    END IF;
END $$;

SELECT setval(pg_get_serial_sequence('"Furnitures"', 'id'),
    GREATEST(COALESCE((SELECT max(id) FROM "Furnitures"), 1),
        COALESCE(pg_sequence_last_value(pg_get_serial_sequence('"Furnitures"', 'id')::regclass), 1)), true);

INSERT INTO "Furnitures" (name, price, category, max_stack, comfort,
                          unlocked_level_id, asset_name, placement_layer)
SELECT variant.name, base.price, base.category, base.max_stack, base.comfort,
       base.unlocked_level_id, variant.asset_name, base.placement_layer
FROM "Furnitures" base
CROSS JOIN (VALUES ('Голубая настольная лампа', 'BlueDeskLamp'),
                   ('Белая настольная лампа', 'WhiteDeskLamp')) AS variant(name, asset_name)
WHERE base.asset_name = 'DeskLamp'
AND NOT EXISTS (SELECT 1 FROM "Furnitures" existing WHERE existing.asset_name = variant.asset_name);

COMMIT;

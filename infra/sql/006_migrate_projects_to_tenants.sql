-- 006 — Migracja danych: istniejące projekty dostają automatycznie utworzone zespoły (tenanty) wg `created_by`.
-- Idempotentny: każdy krok ma warunek (`WHERE created_by IS NULL`, `NOT IN`, `NOT EXISTS`) — ponowne uruchomienie
-- nie tworzy duplikatów. Na pustej bazie nic nie robi.
-- Przeniesione z changesetu Liquibase migrate-projects-to-tenants:1.

-- Step 1: Update created_by for existing tenants based on their first project creator
UPDATE tenants t
SET created_by = (
    SELECT p.created_by
    FROM projects p
    WHERE p.tenant_id = t.id
    LIMIT 1
)
WHERE created_by IS NULL;

-- Step 2: Create auto-generated tenant for each unique project creator who doesn't have a tenant yet
INSERT INTO tenants (id, name, description, is_active, created_by, created_at)
SELECT
    gen_random_uuid(),
    'Moje projekty - ' || COALESCE(created_by::text, 'Unknown'),
    'Automatycznie utworzony zespół dla projektów użytkownika',
    true,
    created_by,
    MIN(created_at)
FROM projects
WHERE created_by IS NOT NULL
  AND created_by NOT IN (SELECT DISTINCT created_by FROM tenants WHERE created_by IS NOT NULL)
GROUP BY created_by;

-- Step 3: Create tenant member entries for all tenant creators (they are automatic members)
INSERT INTO tenant_members (id, tenant_id, corporate_key, added_by, added_at)
SELECT
    gen_random_uuid(),
    t.id,
    'USER_' || t.created_by::text,  -- placeholder - replace with actual corporate key if available
    t.created_by,
    t.created_at
FROM tenants t
WHERE t.created_by IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM tenant_members tm
      WHERE tm.tenant_id = t.id AND tm.corporate_key = 'USER_' || t.created_by::text
  );

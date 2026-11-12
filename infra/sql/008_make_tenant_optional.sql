-- 008 — `projects.tenant_id` staje się opcjonalny (projekty prywatne); `source` unikalny globalnie.
-- Idempotentny: dodanie ograniczenia UNIQUE jest warunkowane, FK jest przebudowywany przez DROP IF EXISTS + ADD.
-- Przeniesione z changesetu Liquibase kreator:008-make-tenant-optional.

-- Make tenant_id nullable
ALTER TABLE projects
    ALTER COLUMN tenant_id DROP NOT NULL;

-- Drop the unique constraint that includes tenant_id
ALTER TABLE projects
    DROP CONSTRAINT IF EXISTS uq_tenant_source;

-- Create new unique constraint on source only (globally unique)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_projects_source') THEN
        ALTER TABLE projects
            ADD CONSTRAINT uq_projects_source UNIQUE (source);
    END IF;
END $$;

-- Update the foreign key to allow NULL
ALTER TABLE projects
    DROP CONSTRAINT IF EXISTS fk_projects_tenant;

ALTER TABLE projects
    ADD CONSTRAINT fk_projects_tenant
        FOREIGN KEY (tenant_id)
        REFERENCES tenants(id)
        ON DELETE SET NULL;

-- Add index for faster queries filtering by tenant
CREATE INDEX IF NOT EXISTS idx_projects_tenant_nullable
    ON projects(tenant_id)
    WHERE tenant_id IS NOT NULL;

-- Add comment
COMMENT ON COLUMN projects.tenant_id IS 'Optional team/tenant assignment. NULL for personal projects.';

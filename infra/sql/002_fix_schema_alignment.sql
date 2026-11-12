-- 002 — Dopasowanie schematu do modelu domenowego (encje Mapping, ParseResult, Approval, AuditLog, User).
-- Idempotentny: zmiany nazw kolumn i dodania ograniczeń są warunkowane stanem katalogu systemowego,
-- więc ponowne uruchomienie na bazie już zmigrowanej nic nie zmienia.
-- Przeniesione z changesetu Liquibase d2creator:002-fix-schema-alignment.

-- =====================================================
-- FIX MAPPINGS TABLE
-- =====================================================
-- Rename columns to match Mapping entity
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'field_name')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'tag_name') THEN
        ALTER TABLE mappings RENAME COLUMN field_name TO tag_name;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'mapper_type')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'mapping_type') THEN
        ALTER TABLE mappings RENAME COLUMN mapper_type TO mapping_type;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'mapping_config')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'mapping_json') THEN
        ALTER TABLE mappings RENAME COLUMN mapping_config TO mapping_json;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'display_order')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'sort_order') THEN
        ALTER TABLE mappings RENAME COLUMN display_order TO sort_order;
    END IF;
END $$;

ALTER TABLE mappings DROP COLUMN IF EXISTS is_active;
ALTER TABLE mappings DROP COLUMN IF EXISTS updated_at;

-- Add created_by column
ALTER TABLE mappings ADD COLUMN IF NOT EXISTS created_by UUID;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_mappings_created_by') THEN
        ALTER TABLE mappings ADD CONSTRAINT fk_mappings_created_by FOREIGN KEY (created_by)
            REFERENCES users(id) ON DELETE SET NULL;
    END IF;
END $$;

-- Update constraint name
ALTER TABLE mappings DROP CONSTRAINT IF EXISTS uq_version_field;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_project_version_tag') THEN
        ALTER TABLE mappings ADD CONSTRAINT uq_project_version_tag UNIQUE (project_version_id, tag_name);
    END IF;
END $$;

-- =====================================================
-- FIX PARSE_RESULTS TABLE
-- =====================================================
-- Remove old columns
ALTER TABLE parse_results DROP COLUMN IF EXISTS is_valid;
ALTER TABLE parse_results DROP COLUMN IF EXISTS errors;
ALTER TABLE parse_results DROP COLUMN IF EXISTS warnings;
ALTER TABLE parse_results DROP COLUMN IF EXISTS metadata;
ALTER TABLE parse_results DROP COLUMN IF EXISTS placeholders;
ALTER TABLE parse_results DROP COLUMN IF EXISTS created_at;

-- Add new columns to match ParseResult entity
ALTER TABLE parse_results ADD COLUMN IF NOT EXISTS parse_json JSONB;
ALTER TABLE parse_results ADD COLUMN IF NOT EXISTS sample_request JSONB;
ALTER TABLE parse_results ADD COLUMN IF NOT EXISTS validation_errors JSONB;
ALTER TABLE parse_results ADD COLUMN IF NOT EXISTS parsed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- =====================================================
-- FIX APPROVALS TABLE
-- =====================================================
-- Rename columns
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'approvals' AND column_name = 'comments')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'approvals' AND column_name = 'comment') THEN
        ALTER TABLE approvals RENAME COLUMN comments TO comment;
    END IF;
END $$;
ALTER TABLE approvals DROP COLUMN IF EXISTS requested_at;
ALTER TABLE approvals DROP COLUMN IF EXISTS responded_at;

-- Add created_at column
ALTER TABLE approvals ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- =====================================================
-- FIX AUDIT_LOG TABLE
-- =====================================================
-- Rename columns
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'audit_log' AND column_name = 'entity_type')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'audit_log' AND column_name = 'entity') THEN
        ALTER TABLE audit_log RENAME COLUMN entity_type TO entity;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'audit_log' AND column_name = 'user_id')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'audit_log' AND column_name = 'actor_id') THEN
        ALTER TABLE audit_log RENAME COLUMN user_id TO actor_id;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'audit_log' AND column_name = 'changes')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'audit_log' AND column_name = 'payload') THEN
        ALTER TABLE audit_log RENAME COLUMN changes TO payload;
    END IF;
END $$;

-- Remove unused columns
ALTER TABLE audit_log DROP COLUMN IF EXISTS ip_address;
ALTER TABLE audit_log DROP COLUMN IF EXISTS user_agent;

-- Update entity_id to allow NULL (Guid? in model)
ALTER TABLE audit_log ALTER COLUMN entity_id DROP NOT NULL;

-- Update constraint
ALTER TABLE audit_log DROP CONSTRAINT IF EXISTS fk_audit_log_user;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_audit_log_actor') THEN
        ALTER TABLE audit_log ADD CONSTRAINT fk_audit_log_actor FOREIGN KEY (actor_id)
            REFERENCES users(id) ON DELETE SET NULL;
    END IF;
END $$;

-- Remove action constraint (not needed - will be validated in application)
ALTER TABLE audit_log DROP CONSTRAINT IF EXISTS chk_action;

-- =====================================================
-- FIX USERS TABLE
-- =====================================================
-- Add tenant_id column if not exists (based on UserConfiguration)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'users' AND column_name = 'tenant_id') THEN
        ALTER TABLE users ADD COLUMN tenant_id UUID;
        ALTER TABLE users ADD CONSTRAINT fk_users_tenant FOREIGN KEY (tenant_id)
            REFERENCES tenants(id) ON DELETE CASCADE;
    END IF;
END $$;

-- Rename full_name to display_name (based on User entity)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'full_name')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'display_name') THEN
        ALTER TABLE users RENAME COLUMN full_name TO display_name;
    END IF;
END $$;

-- Update unique constraint
DROP INDEX IF EXISTS ux_users_tenant_username;
CREATE UNIQUE INDEX ux_users_tenant_username ON users(tenant_id, username);

-- =====================================================
-- UPDATE INDEXES
-- =====================================================

-- Remove old indexes
DROP INDEX IF EXISTS idx_project_versions_project_id;
DROP INDEX IF EXISTS idx_project_versions_status;
DROP INDEX IF EXISTS idx_project_versions_is_active;

-- Add index from ProjectVersionConfiguration
CREATE INDEX IF NOT EXISTS idx_project_versions_project_id_created_at
    ON project_versions(project_id, created_at);

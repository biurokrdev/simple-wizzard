-- 003 — Kolumny statusów z typu ENUM PostgreSQL na VARCHAR (zgodność z EF Core `HasConversion<string>()`).
-- Idempotentny: ponowne `ALTER COLUMN ... TYPE VARCHAR(50) USING status::text` na kolumnie już tekstowej jest no-op.
-- Typy `project_status` / `approval_status` celowo nie są usuwane (zgodność z istniejącymi bazami).
-- Przeniesione z changesetu Liquibase d2creator:003-change-status-to-varchar.

-- =====================================================
-- CHANGE PROJECT_VERSIONS STATUS TO VARCHAR
-- =====================================================
-- First, alter the column type from ENUM to VARCHAR
ALTER TABLE project_versions
    ALTER COLUMN status TYPE VARCHAR(50) USING status::text;

-- Set default value
ALTER TABLE project_versions
    ALTER COLUMN status SET DEFAULT 'Draft';

-- =====================================================
-- CHANGE APPROVALS STATUS TO VARCHAR
-- =====================================================
-- Alter the column type from ENUM to VARCHAR
ALTER TABLE approvals
    ALTER COLUMN status TYPE VARCHAR(50) USING status::text;

-- Set default value
ALTER TABLE approvals
    ALTER COLUMN status SET DEFAULT 'Requested';

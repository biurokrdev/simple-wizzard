-- 007 — Tabela `workers` (konfiguracja mapowania pól szablonu; `config_json` w JSONB).
-- Idempotentny (`IF NOT EXISTS`).
-- Uwaga (R-08): `created_at`/`updated_at` są `timestamp` bez strefy — niespójne z resztą schematu po 004; zachowane 1:1.
-- Przeniesione z changesetu Liquibase kreator:007-add-workers-table.

-- Workers table for field mapping configuration
CREATE TABLE IF NOT EXISTS workers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_version_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    worker_type VARCHAR(100) NOT NULL,
    config_json JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,

    CONSTRAINT fk_workers_project_version FOREIGN KEY (project_version_id)
        REFERENCES project_versions(id) ON DELETE CASCADE
);

-- Index for faster queries by project_version_id
CREATE INDEX IF NOT EXISTS idx_workers_project_version ON workers(project_version_id);

-- Index for GIN queries on config_json
CREATE INDEX IF NOT EXISTS idx_workers_config_json ON workers USING gin(config_json);

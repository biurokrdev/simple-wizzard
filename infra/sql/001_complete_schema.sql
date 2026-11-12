-- 001 — Pełny schemat początkowy D2 Creator: rozszerzenia, typy, tabele, indeksy, funkcje, triggery, dane startowe.
-- PostgreSQL 16+. Skrypt jest idempotentny — można go uruchomić ponownie bez skutków ubocznych.
-- Przeniesione 1:1 z changesetu Liquibase d2creator:001-complete-schema; skrypty 002–008 modyfikują ten schemat.
--
-- Skrypt 002 zmienia nazwy i usuwa część kolumn/indeksów tworzonych tutaj. Elementy, których 002 dotyka, są
-- tworzone tylko na bazie SPRZED 002 (marker: kolumna mappings.tag_name jeszcze nie istnieje) — inaczej ponowne
-- uruchomienie 001 na zmigrowanej bazie kończyłoby się błędem albo przywracało usunięte obiekty.

-- =====================================================
-- EXTENSIONS
-- =====================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- ENUMS
-- (od skryptu 003 kolumny statusów są VARCHAR; typy zostają dla zgodności z istniejącymi bazami)
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'project_status') THEN
        CREATE TYPE project_status AS ENUM (
            'Draft', 'InReview', 'Approved', 'Active', 'Inactive', 'Archived', 'Rejected'
        );
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'approval_status') THEN
        CREATE TYPE approval_status AS ENUM (
            'Pending', 'Approved', 'Rejected', 'Cancelled'
        );
    END IF;
END $$;

-- =====================================================
-- TABLES
-- =====================================================

-- Tenants table
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL UNIQUE,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(200) NOT NULL UNIQUE,
    full_name VARCHAR(200),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,

    CONSTRAINT chk_email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

-- Projects table
CREATE TABLE IF NOT EXISTS projects (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL,
    source VARCHAR(200) NOT NULL,
    name VARCHAR(500) NOT NULL,
    metadata JSONB DEFAULT '{}'::jsonb,
    retention_days INTEGER NOT NULL DEFAULT 90,
    default_template_name VARCHAR(500),
    created_by UUID,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    description TEXT,
    current_active_version_id UUID,

    CONSTRAINT chk_retention_days CHECK (retention_days BETWEEN 1 AND 3650),
    CONSTRAINT chk_source_length CHECK (LENGTH(source) >= 1),
    CONSTRAINT chk_name_length CHECK (LENGTH(name) >= 1),
    CONSTRAINT uq_tenant_source UNIQUE (tenant_id, source),
    CONSTRAINT fk_projects_tenant FOREIGN KEY (tenant_id)
        REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_projects_created_by FOREIGN KEY (created_by)
        REFERENCES users(id) ON DELETE SET NULL
);

-- Project versions table
CREATE TABLE IF NOT EXISTS project_versions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL,
    major INTEGER NOT NULL DEFAULT 1,
    minor INTEGER NOT NULL DEFAULT 0,
    version_tag VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT false,
    status project_status NOT NULL DEFAULT 'Draft',
    created_by UUID,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    step_data JSONB,
    notes TEXT,

    CONSTRAINT chk_version_numbers CHECK (major >= 0 AND minor >= 0),
    CONSTRAINT uq_project_version UNIQUE (project_id, major, minor),
    CONSTRAINT fk_project_versions_project FOREIGN KEY (project_id)
        REFERENCES projects(id) ON DELETE CASCADE,
    CONSTRAINT fk_project_versions_created_by FOREIGN KEY (created_by)
        REFERENCES users(id) ON DELETE SET NULL
);

-- Files table
CREATE TABLE IF NOT EXISTS files (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_version_id UUID NOT NULL,
    filename VARCHAR(500) NOT NULL,
    content BYTEA,
    storage_ref VARCHAR(1000),
    content_type VARCHAR(200),
    size BIGINT,
    sha256 VARCHAR(64),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_filename_length CHECK (LENGTH(filename) >= 1),
    CONSTRAINT chk_size_positive CHECK (size IS NULL OR size > 0),
    CONSTRAINT fk_files_project_version FOREIGN KEY (project_version_id)
        REFERENCES project_versions(id) ON DELETE CASCADE
);

-- Parse results table
CREATE TABLE IF NOT EXISTS parse_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    file_id UUID NOT NULL,
    is_valid BOOLEAN NOT NULL DEFAULT false,
    errors JSONB,
    warnings JSONB,
    metadata JSONB,
    placeholders TEXT[],
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_parse_results_file FOREIGN KEY (file_id)
        REFERENCES files(id) ON DELETE CASCADE
);

-- Mappings table
CREATE TABLE IF NOT EXISTS mappings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_version_id UUID NOT NULL,
    field_name VARCHAR(200) NOT NULL,
    mapper_type VARCHAR(100) NOT NULL,
    mapping_config JSONB NOT NULL,
    display_order INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,

    CONSTRAINT chk_field_name_length CHECK (LENGTH(field_name) >= 1),
    CONSTRAINT uq_version_field UNIQUE (project_version_id, field_name),
    CONSTRAINT fk_mappings_project_version FOREIGN KEY (project_version_id)
        REFERENCES project_versions(id) ON DELETE CASCADE
);

-- Approvals table
CREATE TABLE IF NOT EXISTS approvals (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_version_id UUID NOT NULL,
    approver_id UUID NOT NULL,
    status approval_status NOT NULL DEFAULT 'Pending',
    comments TEXT,
    requested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    responded_at TIMESTAMP,

    CONSTRAINT chk_responded_after_requested CHECK (
        responded_at IS NULL OR responded_at >= requested_at
    ),
    CONSTRAINT fk_approvals_project_version FOREIGN KEY (project_version_id)
        REFERENCES project_versions(id) ON DELETE CASCADE,
    CONSTRAINT fk_approvals_approver FOREIGN KEY (approver_id)
        REFERENCES users(id) ON DELETE RESTRICT
);

-- Audit log table
CREATE TABLE IF NOT EXISTS audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    user_id UUID,
    changes JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_action CHECK (action IN ('CREATE', 'UPDATE', 'DELETE', 'VIEW')),
    CONSTRAINT fk_audit_log_user FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE SET NULL
);

-- Application settings table
CREATE TABLE IF NOT EXISTS app_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(200) NOT NULL UNIQUE,
    value TEXT,
    value_type VARCHAR(50) NOT NULL DEFAULT 'string',
    category VARCHAR(100),
    description TEXT,
    is_encrypted BOOLEAN NOT NULL DEFAULT false,
    is_system BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,

    CONSTRAINT chk_key_format CHECK (key ~ '^[a-zA-Z0-9._-]+$'),
    CONSTRAINT chk_value_type CHECK (value_type IN ('string', 'number', 'boolean', 'json'))
);

-- =====================================================
-- INDEXES (kolumny, których 002 nie zmienia)
-- =====================================================

-- Tenants indexes
CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants(name);
CREATE INDEX IF NOT EXISTS idx_tenants_is_active ON tenants(is_active);

-- Projects indexes
CREATE INDEX IF NOT EXISTS idx_projects_tenant_id ON projects(tenant_id);
CREATE INDEX IF NOT EXISTS idx_projects_source ON projects(source);
CREATE INDEX IF NOT EXISTS idx_projects_created_by ON projects(created_by);
CREATE INDEX IF NOT EXISTS idx_projects_created_at ON projects(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_projects_metadata_gin ON projects USING gin(metadata);

-- Project versions indexes
CREATE INDEX IF NOT EXISTS idx_project_versions_created_at ON project_versions(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_project_versions_step_data ON project_versions USING gin(step_data);

-- Files indexes
CREATE INDEX IF NOT EXISTS idx_files_project_version_id ON files(project_version_id);
CREATE INDEX IF NOT EXISTS idx_files_filename ON files(filename);
CREATE INDEX IF NOT EXISTS idx_files_sha256 ON files(sha256);
CREATE INDEX IF NOT EXISTS idx_files_created_at ON files(created_at DESC);

-- Parse results indexes
CREATE INDEX IF NOT EXISTS idx_parse_results_file_id ON parse_results(file_id);

-- Mappings indexes
CREATE INDEX IF NOT EXISTS idx_mappings_project_version_id ON mappings(project_version_id);

-- Approvals indexes
CREATE INDEX IF NOT EXISTS idx_approvals_project_version_id ON approvals(project_version_id);
CREATE INDEX IF NOT EXISTS idx_approvals_approver_id ON approvals(approver_id);
CREATE INDEX IF NOT EXISTS idx_approvals_status ON approvals(status);

-- Users indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);

-- Audit log indexes
CREATE INDEX IF NOT EXISTS idx_audit_log_created_at ON audit_log(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_action ON audit_log(action);

-- App settings indexes
CREATE INDEX IF NOT EXISTS idx_app_settings_key ON app_settings(key);
CREATE INDEX IF NOT EXISTS idx_app_settings_category ON app_settings(category);

-- =====================================================
-- INDEXES zmieniane/usuwane przez 002 — tylko na bazie sprzed 002
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'tag_name') THEN
        -- usuwane w 002 (zastąpione przez idx_project_versions_project_id_created_at)
        CREATE INDEX IF NOT EXISTS idx_project_versions_project_id ON project_versions(project_id);
        CREATE INDEX IF NOT EXISTS idx_project_versions_status ON project_versions(status);
        CREATE INDEX IF NOT EXISTS idx_project_versions_is_active ON project_versions(is_active);
        -- kolumna is_valid usuwana w 002
        CREATE INDEX IF NOT EXISTS idx_parse_results_is_valid ON parse_results(is_valid);
        -- field_name -> tag_name (indeks zostaje pod starą nazwą), is_active usuwana w 002
        CREATE INDEX IF NOT EXISTS idx_mappings_field_name ON mappings(field_name);
        CREATE INDEX IF NOT EXISTS idx_mappings_is_active ON mappings(is_active);
        -- kolumna requested_at usuwana w 002
        CREATE INDEX IF NOT EXISTS idx_approvals_requested_at ON approvals(requested_at DESC);
        -- entity_type -> entity, user_id -> actor_id, changes -> payload (indeksy zostają pod starymi nazwami)
        CREATE INDEX IF NOT EXISTS idx_audit_log_entity ON audit_log(entity_type, entity_id);
        CREATE INDEX IF NOT EXISTS idx_audit_log_user_id ON audit_log(user_id);
        CREATE INDEX IF NOT EXISTS idx_audit_log_changes ON audit_log USING gin(changes);
    END IF;
END $$;

-- =====================================================
-- CIRCULAR REFERENCE CONSTRAINT
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_projects_current_active_version') THEN
        ALTER TABLE projects
            ADD CONSTRAINT fk_projects_current_active_version
            FOREIGN KEY (current_active_version_id)
            REFERENCES project_versions(id) ON DELETE SET NULL;
    END IF;
END $$;

-- =====================================================
-- FUNCTIONS
-- =====================================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- TRIGGERS
-- =====================================================

CREATE OR REPLACE TRIGGER trg_tenants_updated_at
BEFORE UPDATE ON tenants
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE OR REPLACE TRIGGER trg_projects_updated_at
BEFORE UPDATE ON projects
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE OR REPLACE TRIGGER trg_project_versions_updated_at
BEFORE UPDATE ON project_versions
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE OR REPLACE TRIGGER trg_mappings_updated_at
BEFORE UPDATE ON mappings
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE OR REPLACE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE OR REPLACE TRIGGER trg_app_settings_updated_at
BEFORE UPDATE ON app_settings
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- INITIAL DATA
-- =====================================================

-- Default tenants
INSERT INTO tenants (id, name, description, is_active)
VALUES
    ('00000000-0000-0000-0000-000000000001', 'NL', 'Netherlands tenant', true),
    ('00000000-0000-0000-0000-000000000002', 'PL', 'Poland tenant', true),
    ('00000000-0000-0000-0000-000000000003', 'DE', 'Germany tenant', true)
ON CONFLICT (name) DO NOTHING;

-- Default users (kolumna full_name -> display_name w 002; seed tylko na bazie sprzed 002)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'tag_name') THEN
        INSERT INTO users (id, username, email, full_name, is_active)
        VALUES
            ('10000000-0000-0000-0000-000000000001', 'admin', 'admin@d2creator.com', 'System Administrator', true),
            ('10000000-0000-0000-0000-000000000002', 'testuser', 'test@d2creator.com', 'Test User', true)
        ON CONFLICT (username) DO NOTHING;
    END IF;
END $$;

-- Default settings
INSERT INTO app_settings (key, value, value_type, category, description, is_system)
VALUES
    ('app.version', '1.0.0', 'string', 'system', 'Application version', true),
    ('app.maintenance_mode', 'false', 'boolean', 'system', 'Maintenance mode flag', false),
    ('document.default_retention_days', '90', 'number', 'document', 'Default retention period in days', false),
    ('document.max_file_size_mb', '50', 'number', 'document', 'Maximum file upload size in MB', false)
ON CONFLICT (key) DO NOTHING;

-- =====================================================
-- COMMENTS
-- =====================================================

COMMENT ON TABLE tenants IS 'Multi-tenant organization data';
COMMENT ON TABLE projects IS 'Projects with metadata stored in JSONB';
COMMENT ON TABLE project_versions IS 'Versioned project data with step_data in JSONB';
COMMENT ON TABLE files IS 'Template files and attachments';
COMMENT ON TABLE audit_log IS 'Audit log for tracking all entity changes';
COMMENT ON TABLE app_settings IS 'Application configuration settings';

COMMENT ON COLUMN projects.metadata IS 'Flexible metadata: ApplicationName, ProcessName, ProcessOwner, BusinessLineId, ProcessVersion, ProcessStep, StatusId, RecipientId';

-- kolumna changes -> payload w 002 (komentarz zostaje przy kolumnie po zmianie nazwy)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'mappings' AND column_name = 'tag_name') THEN
        COMMENT ON COLUMN audit_log.changes IS 'JSONB field containing before/after values';
    END IF;
END $$;

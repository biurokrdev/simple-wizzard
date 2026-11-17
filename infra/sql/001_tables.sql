-- 001 — Struktura bazy D2 Creator: tabele, klucze, indeksy.
-- PostgreSQL 15+. Wymaga istniejącego schematu `cr` (000) i uprawnień CREATE w tym schemacie.
--
-- Zasady:
--   * każdy obiekt ma jawny prefiks `cr.` — skrypt nie zależy od search_path i nie dotyka `public`,
--   * brak rozszerzeń; identyfikatory (UUID) nadaje aplikacja — kolumny `id` nie mają wartości domyślnej,
--   * wszystkie znaczniki czasu to TIMESTAMPTZ,
--   * statusy i typy wyliczeniowe to VARCHAR — wartości pilnuje aplikacja (EF Core HasConversion<string>),
--   * brak funkcji, triggerów i bloków DO — same zwykłe instrukcje zakończone średnikiem,
--     `updated_at` ustawia aplikacja,
--   * skrypt można uruchomić ponownie (IF NOT EXISTS).

-- =====================================================
-- ZESPOŁY I UŻYTKOWNICY
-- =====================================================

-- Zespół (tenant)
CREATE TABLE IF NOT EXISTS cr.tenants (
    id          UUID         NOT NULL,
    name        VARCHAR(200) NOT NULL,
    description TEXT,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    metadata    JSONB,
    created_by  UUID,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMPTZ,

    CONSTRAINT pk_tenants PRIMARY KEY (id),
    CONSTRAINT uq_tenants_name UNIQUE (name)
);

-- Członek zespołu, identyfikowany po corporate key
CREATE TABLE IF NOT EXISTS cr.tenant_members (
    id            UUID         NOT NULL,
    tenant_id     UUID         NOT NULL,
    corporate_key VARCHAR(255) NOT NULL,
    added_by      UUID         NOT NULL,
    added_at      TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_blocked    BOOLEAN      NOT NULL DEFAULT FALSE,          -- zablokowany: bez dostępu do projektów zespołu
    access        VARCHAR(20)  NOT NULL DEFAULT 'ReadWrite',    -- 'ReadWrite' | 'ReadOnly'

    CONSTRAINT pk_tenant_members PRIMARY KEY (id),
    CONSTRAINT uq_tenant_corporate_key UNIQUE (tenant_id, corporate_key),
    CONSTRAINT fk_tenant_members_tenant FOREIGN KEY (tenant_id)
        REFERENCES cr.tenants (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS cr.users (
    id           UUID         NOT NULL,
    tenant_id    UUID,
    username     VARCHAR(100) NOT NULL,
    email        VARCHAR(200) NOT NULL,
    display_name VARCHAR(200),
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   TIMESTAMPTZ,

    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT uq_users_username UNIQUE (username),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT chk_users_email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'),
    CONSTRAINT fk_users_tenant FOREIGN KEY (tenant_id)
        REFERENCES cr.tenants (id) ON DELETE CASCADE
);

-- =====================================================
-- PROJEKTY I WERSJE
-- =====================================================

-- Projekt (pismo). tenant_id NULL = projekt prywatny; właściciel w metadata -> 'OwnerCorporateKey'.
CREATE TABLE IF NOT EXISTS cr.projects (
    id                        UUID         NOT NULL,
    tenant_id                 UUID,
    source                    VARCHAR(200) NOT NULL,
    name                      VARCHAR(500) NOT NULL,
    description               TEXT,
    metadata                  JSONB        DEFAULT '{}'::jsonb,
    retention_days            INTEGER      NOT NULL DEFAULT 90,
    default_template_name     VARCHAR(500),
    current_active_version_id UUID,        -- FK dodawany niżej (zależność cykliczna z project_versions)
    created_by                UUID,
    created_at                TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at                TIMESTAMPTZ,

    CONSTRAINT pk_projects PRIMARY KEY (id),
    CONSTRAINT uq_projects_source UNIQUE (source),
    CONSTRAINT chk_projects_source_length CHECK (LENGTH(source) >= 1),
    CONSTRAINT chk_projects_name_length CHECK (LENGTH(name) >= 1),
    CONSTRAINT chk_projects_retention_days CHECK (retention_days BETWEEN 1 AND 3650),
    CONSTRAINT fk_projects_tenant FOREIGN KEY (tenant_id)
        REFERENCES cr.tenants (id) ON DELETE SET NULL,
    CONSTRAINT fk_projects_created_by FOREIGN KEY (created_by)
        REFERENCES cr.users (id) ON DELETE SET NULL
);

-- Wersja projektu. Przepływ statusu: Draft -> PendingApproval -> Approved | RequiresChanges.
CREATE TABLE IF NOT EXISTS cr.project_versions (
    id                    UUID         NOT NULL,
    project_id            UUID         NOT NULL,
    major                 INTEGER      NOT NULL DEFAULT 1,
    minor                 INTEGER      NOT NULL DEFAULT 0,
    version_tag           VARCHAR(50)  NOT NULL,
    is_active             BOOLEAN      NOT NULL DEFAULT FALSE,
    status                VARCHAR(50)  NOT NULL DEFAULT 'Draft',
    step_data             JSONB,                    -- dane kroków kreatora
    notes                 TEXT,
    -- przegląd / akceptacja
    submitted_at          TIMESTAMPTZ,
    reviewed_at           TIMESTAMPTZ,
    reviewed_by           VARCHAR(255),
    review_comment        TEXT,
    review_notes          JSONB,                    -- uwagi recenzenta: klucz kroku kreatora -> treść
    -- paczka wdrożeniowa wysłana do aplikacji docelowej
    delivered_at          TIMESTAMPTZ,
    delivered_to          VARCHAR(500),
    delivered_package_sha VARCHAR(64),
    created_by            UUID,
    created_at            TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at            TIMESTAMPTZ,

    CONSTRAINT pk_project_versions PRIMARY KEY (id),
    CONSTRAINT uq_project_version UNIQUE (project_id, major, minor),
    CONSTRAINT chk_project_versions_numbers CHECK (major >= 0 AND minor >= 0),
    CONSTRAINT fk_project_versions_project FOREIGN KEY (project_id)
        REFERENCES cr.projects (id) ON DELETE CASCADE,
    CONSTRAINT fk_project_versions_created_by FOREIGN KEY (created_by)
        REFERENCES cr.users (id) ON DELETE SET NULL
);

-- Zależność cykliczna projects <-> project_versions: FK można dodać dopiero po utworzeniu obu tabel.
ALTER TABLE cr.projects
    DROP CONSTRAINT IF EXISTS fk_projects_current_active_version;

ALTER TABLE cr.projects
    ADD CONSTRAINT fk_projects_current_active_version FOREIGN KEY (current_active_version_id)
        REFERENCES cr.project_versions (id) ON DELETE SET NULL;

-- =====================================================
-- ZAWARTOŚĆ WERSJI
-- =====================================================

-- Pliki szablonów i załączniki
CREATE TABLE IF NOT EXISTS cr.files (
    id                 UUID          NOT NULL,
    project_version_id UUID          NOT NULL,
    filename           VARCHAR(500)  NOT NULL,
    content            BYTEA,
    storage_ref        VARCHAR(1000),
    content_type       VARCHAR(200),
    size               BIGINT,
    sha256             VARCHAR(64),
    created_at         TIMESTAMPTZ   NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_files PRIMARY KEY (id),
    CONSTRAINT chk_files_filename_length CHECK (LENGTH(filename) >= 1),
    CONSTRAINT chk_files_size_positive CHECK (size IS NULL OR size > 0),
    CONSTRAINT fk_files_project_version FOREIGN KEY (project_version_id)
        REFERENCES cr.project_versions (id) ON DELETE CASCADE
);

-- Wynik parsowania pliku szablonu
CREATE TABLE IF NOT EXISTS cr.parse_results (
    id                UUID        NOT NULL,
    file_id           UUID        NOT NULL,
    parse_json        JSONB,
    sample_request    JSONB,
    validation_errors JSONB,
    parsed_at         TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_parse_results PRIMARY KEY (id),
    CONSTRAINT fk_parse_results_file FOREIGN KEY (file_id)
        REFERENCES cr.files (id) ON DELETE CASCADE
);

-- Mapowanie tagu szablonu na wartość
CREATE TABLE IF NOT EXISTS cr.mappings (
    id                 UUID         NOT NULL,
    project_version_id UUID         NOT NULL,
    tag_name           VARCHAR(200) NOT NULL,
    mapping_type       VARCHAR(100) NOT NULL,
    mapping_json       JSONB        NOT NULL,
    sort_order         INTEGER,
    created_by         UUID,
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_mappings PRIMARY KEY (id),
    CONSTRAINT uq_project_version_tag UNIQUE (project_version_id, tag_name),
    CONSTRAINT chk_mappings_tag_name_length CHECK (LENGTH(tag_name) >= 1),
    CONSTRAINT fk_mappings_project_version FOREIGN KEY (project_version_id)
        REFERENCES cr.project_versions (id) ON DELETE CASCADE,
    CONSTRAINT fk_mappings_created_by FOREIGN KEY (created_by)
        REFERENCES cr.users (id) ON DELETE SET NULL
);

-- Worker: konfiguracja przetwarzania pola szablonu
CREATE TABLE IF NOT EXISTS cr.workers (
    id                 UUID         NOT NULL,
    project_version_id UUID         NOT NULL,
    name               VARCHAR(255) NOT NULL,
    worker_type        VARCHAR(100) NOT NULL,       -- = worker_profiles.key
    config_json        JSONB        NOT NULL,
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at         TIMESTAMPTZ,

    CONSTRAINT pk_workers PRIMARY KEY (id),
    CONSTRAINT fk_workers_project_version FOREIGN KEY (project_version_id)
        REFERENCES cr.project_versions (id) ON DELETE CASCADE
);

-- Akceptacja wersji
CREATE TABLE IF NOT EXISTS cr.approvals (
    id                 UUID        NOT NULL,
    project_version_id UUID        NOT NULL,
    approver_id        UUID        NOT NULL,
    status             VARCHAR(50) NOT NULL DEFAULT 'Requested',
    comment            TEXT,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_approvals PRIMARY KEY (id),
    CONSTRAINT fk_approvals_project_version FOREIGN KEY (project_version_id)
        REFERENCES cr.project_versions (id) ON DELETE CASCADE,
    CONSTRAINT fk_approvals_approver FOREIGN KEY (approver_id)
        REFERENCES cr.users (id) ON DELETE RESTRICT
);

-- =====================================================
-- SŁOWNIKI, USTAWIENIA, AUDYT
-- =====================================================

-- Katalog typów workerów dostępnych w kreatorze
CREATE TABLE IF NOT EXISTS cr.worker_profiles (
    id                   UUID         NOT NULL,
    key                  VARCHAR(100) NOT NULL,                      -- = workers.worker_type
    display_name         VARCHAR(200) NOT NULL,
    description          TEXT,
    default_config_json  JSONB        NOT NULL DEFAULT '{}'::jsonb,  -- token {name} = nazwa elementu
    is_visible           BOOLEAN      NOT NULL DEFAULT FALSE,        -- nowe typy z synchronizacji są ukryte
    sort_order           INTEGER      NOT NULL DEFAULT 0,
    source               VARCHAR(20)  NOT NULL DEFAULT 'Synced',     -- 'Synced' | 'Manual'
    is_missing_in_source BOOLEAN      NOT NULL DEFAULT FALSE,
    last_synced_at       TIMESTAMPTZ,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at           TIMESTAMPTZ,

    CONSTRAINT pk_worker_profiles PRIMARY KEY (id),
    CONSTRAINT uq_worker_profiles_key UNIQUE (key)
);

-- Ustawienia aplikacji
CREATE TABLE IF NOT EXISTS cr.app_settings (
    id           UUID         NOT NULL,
    key          VARCHAR(200) NOT NULL,
    value        TEXT,
    value_type   VARCHAR(50)  NOT NULL DEFAULT 'string',
    category     VARCHAR(100),
    description  TEXT,
    is_encrypted BOOLEAN      NOT NULL DEFAULT FALSE,
    is_system    BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   TIMESTAMPTZ,

    CONSTRAINT pk_app_settings PRIMARY KEY (id),
    CONSTRAINT uq_app_settings_key UNIQUE (key),
    CONSTRAINT chk_app_settings_key_format CHECK (key ~ '^[a-zA-Z0-9._-]+$'),
    CONSTRAINT chk_app_settings_value_type CHECK (value_type IN ('string', 'number', 'boolean', 'json'))
);

-- Dziennik zmian encji
CREATE TABLE IF NOT EXISTS cr.audit_log (
    id         UUID         NOT NULL,
    entity     VARCHAR(100) NOT NULL,
    entity_id  UUID,
    action     VARCHAR(50)  NOT NULL,
    actor_id   UUID,
    payload    JSONB,                               -- wartości przed/po zmianie
    created_at TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_audit_log PRIMARY KEY (id),
    CONSTRAINT fk_audit_log_actor FOREIGN KEY (actor_id)
        REFERENCES cr.users (id) ON DELETE SET NULL
);

-- =====================================================
-- INDEKSY
-- (kolumny objęte PRIMARY KEY / UNIQUE mają indeks z samego ograniczenia)
-- =====================================================

CREATE INDEX IF NOT EXISTS idx_tenants_is_active ON cr.tenants (is_active);

CREATE INDEX IF NOT EXISTS idx_tenant_members_corporate_key ON cr.tenant_members (corporate_key);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_tenant_username ON cr.users (tenant_id, username);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON cr.users (is_active);

CREATE INDEX IF NOT EXISTS idx_projects_tenant_id ON cr.projects (tenant_id);
CREATE INDEX IF NOT EXISTS idx_projects_created_by ON cr.projects (created_by);
CREATE INDEX IF NOT EXISTS idx_projects_created_at ON cr.projects (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_projects_metadata ON cr.projects USING gin (metadata);

CREATE INDEX IF NOT EXISTS idx_project_versions_project_id_created_at ON cr.project_versions (project_id, created_at);
CREATE INDEX IF NOT EXISTS idx_project_versions_status ON cr.project_versions (status);
CREATE INDEX IF NOT EXISTS idx_project_versions_created_at ON cr.project_versions (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_project_versions_step_data ON cr.project_versions USING gin (step_data);

CREATE INDEX IF NOT EXISTS idx_files_project_version_id ON cr.files (project_version_id);
CREATE INDEX IF NOT EXISTS idx_files_filename ON cr.files (filename);
CREATE INDEX IF NOT EXISTS idx_files_sha256 ON cr.files (sha256);
CREATE INDEX IF NOT EXISTS idx_files_created_at ON cr.files (created_at DESC);

CREATE INDEX IF NOT EXISTS idx_parse_results_file_id ON cr.parse_results (file_id);

CREATE INDEX IF NOT EXISTS idx_mappings_tag_name ON cr.mappings (tag_name);

CREATE INDEX IF NOT EXISTS idx_workers_project_version ON cr.workers (project_version_id);
CREATE INDEX IF NOT EXISTS idx_workers_config_json ON cr.workers USING gin (config_json);

CREATE INDEX IF NOT EXISTS idx_approvals_project_version_id ON cr.approvals (project_version_id);
CREATE INDEX IF NOT EXISTS idx_approvals_approver_id ON cr.approvals (approver_id);
CREATE INDEX IF NOT EXISTS idx_approvals_status ON cr.approvals (status);

CREATE INDEX IF NOT EXISTS idx_worker_profiles_visible ON cr.worker_profiles (is_visible, sort_order);

CREATE INDEX IF NOT EXISTS idx_app_settings_category ON cr.app_settings (category);

CREATE INDEX IF NOT EXISTS idx_audit_log_entity ON cr.audit_log (entity, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_actor_id ON cr.audit_log (actor_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_action ON cr.audit_log (action);
CREATE INDEX IF NOT EXISTS idx_audit_log_created_at ON cr.audit_log (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_payload ON cr.audit_log USING gin (payload);

-- =====================================================
-- KOMENTARZE
-- =====================================================

COMMENT ON TABLE cr.tenants          IS 'Zespoły (tenanty)';
COMMENT ON TABLE cr.tenant_members   IS 'Członkowie zespołów identyfikowani po corporate key';
COMMENT ON TABLE cr.projects         IS 'Projekty (pisma); metadane w JSONB';
COMMENT ON TABLE cr.project_versions IS 'Wersje projektów; dane kroków kreatora w step_data (JSONB)';
COMMENT ON TABLE cr.files            IS 'Pliki szablonów i załączniki';
COMMENT ON TABLE cr.workers          IS 'Workery wersji: konfiguracja przetwarzania pól szablonu';
COMMENT ON TABLE cr.worker_profiles  IS 'Katalog typów workerów dostępnych w kreatorze';
COMMENT ON TABLE cr.app_settings     IS 'Ustawienia aplikacji';
COMMENT ON TABLE cr.audit_log        IS 'Dziennik zmian encji';

COMMENT ON COLUMN cr.projects.tenant_id IS 'Zespół projektu; NULL = projekt prywatny';
COMMENT ON COLUMN cr.projects.metadata  IS 'Metadane: ApplicationName, ProcessName, ProcessOwner, BusinessLineId, ProcessVersion, ProcessStep, StatusId, RecipientId, OwnerCorporateKey';

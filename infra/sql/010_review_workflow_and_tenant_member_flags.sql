-- 010 — Przegląd/akceptacja wersji (BR-017) + flagi członków zespołów (BR-018).
-- Idempotentny (`ADD COLUMN IF NOT EXISTS`).

-- Wersja pisma zgłoszona do publikacji: kto i kiedy zgłosił, decyzja administratora i komentarz
-- (status przepływu jest w `project_versions.status`: Draft → PendingApproval → Approved | RequiresChanges).
ALTER TABLE project_versions
    ADD COLUMN IF NOT EXISTS submitted_at   TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS reviewed_at    TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS reviewed_by    VARCHAR(255),
    ADD COLUMN IF NOT EXISTS review_comment TEXT;

CREATE INDEX IF NOT EXISTS idx_project_versions_status ON project_versions(status);

-- Członek zespołu: blokada (bez dostępu do projektów zespołu) i poziom dostępu (ReadWrite | ReadOnly).
ALTER TABLE tenant_members
    ADD COLUMN IF NOT EXISTS is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS access     VARCHAR(20) NOT NULL DEFAULT 'ReadWrite';

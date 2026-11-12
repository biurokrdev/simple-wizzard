-- 011 — Uwagi recenzenta do kroków kreatora (BR-017, przegląd krok po kroku).
-- Idempotentny (`ADD COLUMN IF NOT EXISTS`).

-- { "documentDescription": "…", "templateAndSchema": "…", "configuration": "…", "summary": "…" }
-- Zapisywane w trakcie przeglądu (PUT /api/v1/approvals/{versionId}/notes), czyszczone przy ponownym zgłoszeniu.
ALTER TABLE project_versions
    ADD COLUMN IF NOT EXISTS review_notes JSONB;

-- Właściciel projektu prywatnego (bez zespołu) jest zapisywany w istniejącej kolumnie JSONB
-- `projects.metadata` jako klucz "OwnerCorporateKey" — bez zmiany schematu. Projekty prywatne bez właściciela
-- (utworzone przed tą zmianą) są widoczne tylko w module administracyjnym. Jeśli mają wrócić na listę
-- konkretnego użytkownika, przypisz je ręcznie (przykład dla loginu TEMP_USER — wykonaj świadomie):
-- UPDATE projects
--    SET metadata = jsonb_set(COALESCE(metadata, '{}'::jsonb), '{OwnerCorporateKey}', '"TEMP_USER"')
--  WHERE tenant_id IS NULL AND (metadata ->> 'OwnerCorporateKey') IS NULL;

-- 013 — Paczka wdrożeniowa wersji pisma (BR-022, 2026-11-08): kiedy, dokąd i jaki ZIP (SHA-256) wysłano
-- do aplikacji docelowej (POST /api/v1/projects/{p}/versions/{v}/package/publish). Idempotentny.
ALTER TABLE project_versions
    ADD COLUMN IF NOT EXISTS delivered_at           TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS delivered_to           VARCHAR(500),
    ADD COLUMN IF NOT EXISTS delivered_package_sha  VARCHAR(64);

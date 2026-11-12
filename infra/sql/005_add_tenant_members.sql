-- 005 — Tabela `tenant_members` (członkowie zespołu po corporate_key) oraz `tenants.created_by`.
-- Idempotentny (`IF NOT EXISTS`).
-- Przeniesione z changesetu Liquibase tenant-members:1.

-- Add created_by column to tenants table
ALTER TABLE tenants
ADD COLUMN IF NOT EXISTS created_by UUID;

-- Create tenant_members table
CREATE TABLE IF NOT EXISTS tenant_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    corporate_key VARCHAR(255) NOT NULL,
    added_by UUID NOT NULL,
    added_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_tenant_members_tenant
        FOREIGN KEY (tenant_id)
        REFERENCES tenants(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_tenant_corporate_key
        UNIQUE (tenant_id, corporate_key)
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_tenant_members_tenant_id
    ON tenant_members(tenant_id);

CREATE INDEX IF NOT EXISTS idx_tenant_members_corporate_key
    ON tenant_members(corporate_key);

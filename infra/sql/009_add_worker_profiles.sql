-- 009 — Tabela `worker_profiles`: katalog typów workerów dla kreatora (BR-016).
-- Profil = typ workera z aplikacji docelowej (albo dodany ręcznie) + opis + domyślna konfiguracja JSON
-- + decyzja administratora, czy typ jest widoczny dla użytkowników kreatora.
-- Idempotentny (`IF NOT EXISTS`, `ON CONFLICT DO NOTHING`).

CREATE TABLE IF NOT EXISTS worker_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(100) NOT NULL,                       -- = workers.worker_type (np. 'Podstaw wartość')
    display_name VARCHAR(200) NOT NULL,
    description TEXT,
    default_config_json JSONB NOT NULL DEFAULT '{}'::jsonb,   -- token {name} = nazwa elementu
    is_visible BOOLEAN NOT NULL DEFAULT FALSE,       -- nowe typy z synchronizacji są ukryte
    sort_order INTEGER NOT NULL DEFAULT 0,
    source VARCHAR(20) NOT NULL DEFAULT 'Synced',    -- 'Synced' | 'Manual'
    is_missing_in_source BOOLEAN NOT NULL DEFAULT FALSE,
    last_synced_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,

    CONSTRAINT uq_worker_profiles_key UNIQUE (key)
);

CREATE INDEX IF NOT EXISTS idx_worker_profiles_visible ON worker_profiles(is_visible, sort_order);

-- Dwa typy, które kreator generuje sam (BR-010/BR-011) — widoczne od razu, żeby dialog mapowania działał jak dotąd.
INSERT INTO worker_profiles (id, key, display_name, description, default_config_json, is_visible, sort_order, source)
VALUES
    (uuid_generate_v4(), 'Podstaw wartość', 'Podstaw wartość',
     'Podmienia znacznik <%…%> na wartość z modelu danych (formModel).',
     '{"search_for":"<%{name}%>","replacement_property_expression":"formModel.{name}"}'::jsonb, TRUE, 10, 'Synced'),
    (uuid_generate_v4(), 'Stan', 'Stan',
     'Pokazuje lub ukrywa kontrolkę tekstową zależnie od warunku logicznego.',
     '{"tag_name":"{name}","conditional_expression":"formModel.{name}"}'::jsonb, TRUE, 20, 'Synced')
ON CONFLICT (key) DO NOTHING;

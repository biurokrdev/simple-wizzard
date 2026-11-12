-- 012 — Profil workera „Powtórz blok” widoczny w kreatorze (rozpoznawanie rodzaju formantu w szablonie, 2026-11-08).
-- Generator workerów tworzy ten typ dla formantów rozpoznanych jako sekcja powtarzana / lista / tabela
-- (GenerateWorkersCommandHandler). Idempotentny: ON CONFLICT (key) DO NOTHING — jeśli administrator już ma ten profil
-- (np. po synchronizacji z aplikacją docelową), jego widoczność i konfiguracja zostają bez zmian.
INSERT INTO worker_profiles (id, key, display_name, description, default_config_json, is_visible, sort_order, source)
VALUES
    (uuid_generate_v4(), 'Powtórz blok', 'Powtórz blok',
     'Powiela obszar oznaczony tagiem dla każdego elementu kolekcji z modelu danych (sekcja powtarzana, lista, wiersze tabeli).',
     '{"tag_name":"{name}","collection_expression":"formModel.{name}","repeat_scope":"block"}'::jsonb, TRUE, 30, 'Synced')
ON CONFLICT (key) DO NOTHING;

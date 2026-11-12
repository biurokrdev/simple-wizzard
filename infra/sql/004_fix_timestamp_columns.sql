-- 004 — Kolumny czasowe z `timestamp` na `timestamp with time zone` (wartości interpretowane jako UTC).
-- Idempotentny: konwersja wykonuje się TYLKO dla kolumn, które nadal są `timestamp without time zone`.
-- Bez tego warunku ponowne uruchomienie `USING col AT TIME ZONE 'UTC'` na kolumnie już `timestamptz`
-- przesunęłoby wartości o offset strefy sesji.
-- Przeniesione z changesetu Liquibase d2creator:004-fix-timestamp-columns.

DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            ('project_versions', 'created_at'),
            ('project_versions', 'updated_at'),
            ('projects',         'created_at'),
            ('projects',         'updated_at'),
            ('tenants',          'created_at'),
            ('tenants',          'updated_at'),
            ('users',            'created_at'),
            ('users',            'updated_at'),
            ('files',            'created_at'),
            ('approvals',        'created_at'),
            ('parse_results',    'parsed_at'),
            ('audit_log',        'created_at')
        ) AS t(table_name, column_name)
    LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.columns c
            WHERE c.table_schema = 'public'
              AND c.table_name   = r.table_name
              AND c.column_name  = r.column_name
              AND c.data_type    = 'timestamp without time zone'
        ) THEN
            EXECUTE format(
                'ALTER TABLE %I ALTER COLUMN %I TYPE TIMESTAMP WITH TIME ZONE USING %I AT TIME ZONE ''UTC''',
                r.table_name, r.column_name, r.column_name
            );
        END IF;
    END LOOP;
END $$;

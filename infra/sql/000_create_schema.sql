-- 000 — Schemat `cr`.
-- Wszystkie obiekty D2 Creator żyją w schemacie `cr`. Schemat `public` nie jest używany.
--
-- Ten skrypt wymaga uprawnienia CREATE na bazie danych. Jeżeli schemat `cr` został już założony
-- przez administratora, pomiń ten plik i uruchom od razu 001.

CREATE SCHEMA IF NOT EXISTS cr;

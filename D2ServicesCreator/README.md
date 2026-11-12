# D2ServicesCreator — API dla systemów zewnętrznych

Minimalne, osobno hostowane API D2 Creator przeznaczone dla **aplikacji zewnętrznych** (app-to-app),
nie dla GUI. Odpowiednik `D2ServicesViewerEditor` z D2 ViewerEditor.

Dlaczego osobny proces: `D2ApiCreator` (`/api/v1`) jest API dla kreatora — pozwala tworzyć projekty,
wgrywać szablony, generować workery, zarządzać zespołami. System zewnętrzny, który ma tylko
**włączać / wyłączać wersję pisma**, nie powinien dostawać takiej powierzchni. Services API korzysta
z tych samych warstw `Application` / `Infrastructure` / `Domain` (ten sam kod, ta sama baza), ale
wystawia wyłącznie trzy operacje i wymaga klucza `X-Api-Key`.

## Endpointy (`/api/v1`)

| Metoda | Ścieżka | Co robi | Odpowiedzi |
|---|---|---|---|
| GET | `/Projects/{projectId}` | Status projektu: `isActive`, `activeVersionId`, `activeVersionTag`, `versions[]` (bez `stepData`, metadanych, retencji) | 200, 401, 404 |
| POST | `/Projects/{projectId}/versions/{versionId}/activate` | Ustawia wersję jako aktywną (poprzednia aktywna → `Inactive`) | 200, 400, 401, 404 |
| POST | `/Projects/{projectId}/versions/{versionId}/deactivate` | Wyłącza wersję; projekt zostaje **bez** aktywnej wersji (brak domyślnego fallbacku). Idempotentne | 200, 400, 401, 404 |
| GET | `/health` | Health check (bez klucza) | 200 |

Błędy są zwracane jako RFC 7807 `application/problem+json` (bez stack trace, z `traceId`).
Enumy (`status`) serializowane jako nazwy: `Draft`, `Active`, `Inactive`, …

## Uwierzytelnianie

Każde wywołanie poza `/health` i `/swagger` wymaga nagłówka `X-Api-Key` równego `Secrets:ApiKey`
(w kontenerze: `Secrets__ApiKey`). **Bez skonfigurowanego klucza aplikacja nie wystartuje.**
Klucz i connection string ustawiasz lokalnie w `appsettings.Development.json` (plik poza repo —
skopiuj `appsettings.Development.json.example` i uzupełnij wartości) albo przez zmienne środowiskowe
`Secrets__ApiKey` / `ConnectionStrings__DefaultConnection`.

## Uruchomienie lokalne

```bash
# baza (+ ewentualnie API GUI) — patrz infra/README.md
cd infra && podman compose up -d postgres

# Services API — http://localhost:5002, Swagger: /swagger
cd ../D2ServicesCreator/D2ServicesCreator.Api
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5002" dotnet run --no-launch-profile

curl -s http://localhost:5002/health
curl -s -H "X-Api-Key: <twój-klucz>" http://localhost:5002/api/v1/Projects/<projectId>
```

W kontenerze: serwis `services-api` w `infra/podman-compose.yml` (port **5002**), obraz budowany
z kontekstu **katalogu głównego repo** (`D2ServicesCreator/D2ServicesCreator.Api/Dockerfile`).

## Build i testy

```bash
cd D2ServicesCreator
dotnet build D2ServicesCreator.sln
dotnet test D2ServicesCreator.Tests.Unit/D2ServicesCreator.Tests.Unit.csproj
```

Komenda `DeactivateProjectVersionCommand` (używana przez `/deactivate`) żyje we współdzielonej
warstwie `D2ApiCreator.Application` i ma testy w `D2ApiCreator.Tests.Unit`.

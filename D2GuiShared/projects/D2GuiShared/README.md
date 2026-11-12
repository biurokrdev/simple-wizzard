# d2-gui-shared (D2GuiShared)

Wspólny shell GUI aplikacji D2 (Doc2) dla Angular 22 (standalone + Signals). Wygląd 1:1 z D2 ViewerEditor
(`importer/D2GuiViewerEditor`): nagłówek `dashboard-header`, stopka-pasek statusu `editor-footer`, cienki baner
środowiska (`env-banner`), baner offline (`offline-banner`).

Aplikacje (Wizzard = `D2GuiCreator`, Edytor = `D2GuiViewerEditor`) **konfigurują** shell przez
`provideD2Gui()` i **rozszerzają** go własnymi źródłami danych; nie kopiują już HTML/SCSS nagłówka ani stopki.

## Eksportowane klocki

| Element | Selektor | Rola |
|---|---|---|
| `D2AppShellComponent` | `d2-app-shell` | `100vh`: banery + nagłówek u góry, przewijany `<main>` z `<router-outlet>`, stopka **zawsze widoczna** na dole; komponent trasy nadrzędnej |
| `D2HeaderComponent` | `d2-header` | logo + nazwa (link home); `<ng-content>` na akcje/nawigację po prawej |
| `D2FooterComponent` | `d2-footer` | pasek statusu: „Doc2 © rok Doc2 \| tagline" (lewo), `ŚRODOWISKO \| Wersja: <build> \| <data>` (środek, puste pozycje znikają), `<ng-content>` (prawo — np. autozapis, zoom) |
| `D2GlobalBannersComponent` | `d2-global-banners` | środowisko nad offline, w normalnym flow |
| `D2EnvironmentBannerComponent` | `d2-environment-banner` | „Wersja Local / DEV / UAT / TST / PRE"; ukryty na PRD |
| `D2OfflineBannerComponent` | `d2-offline-banner` | żółty pasek, gdy API niedostępne; zamykany |
| `provideD2Gui(config, ...features)` | — | konfiguracja + źródła danych |
| `withEnvironmentSource(Type)` | — | własny serwis z sygnałem `environment` (np. z health-checka API) |
| `withConnectionStatus(Type)` | — | własny serwis z sygnałem `isApiConnected` |
| `withBuildInfo(Type)` | — | własny serwis z sygnałami `buildNumber`, `buildDate` (zwykle z health-checka API) |
| `resolveD2EnvironmentBanner()` | — | czysta funkcja mapująca nazwę środowiska na wariant banera |
| `D2GridComponent` | `d2-grid` | kontrolka listy: wyszukiwarka (domyślnie ukryta, `[searchable]="true"`), panel filtrów, filtry kolumnowe, sortowanie, strony, zaznaczanie, szkielet ładowania, szczegóły wiersza, kliknięcie w wiersz |
| `D2GridCellDef` | `ng-template[d2GridCell]` | własny szablon komórki kolumny (`let-row`, `let-column`, `let-index`) |
| `D2GridDetailDef` | `ng-template[d2GridDetail]` | własny panel szczegółów wiersza rozwijany przyciskiem „∨” (np. pełny JSON) |
| `GridColumn`, `GridLabels`, `GridState`… | — | typy kontrolki; czyste funkcje danych (`filterRows`, `sortRows`, `paginate`, `normalizeText`…) też są eksportowane |
| `D2ThemeService` | — | aktywna paleta (`theme()`), `setTheme(name)`, lista `themes`; wpisuje `--d2-color-*` na `<html>`, pamięta wybór w localStorage |
| `D2_THEMES`, `D2Theme`, `D2ThemePalette` | — | wbudowane palety (orange, violet, teal, graphite) i typy do definiowania własnych |
| `D2ThemeSwitcherComponent` | `d2-theme-switcher` | select z paletami; shell pokazuje go przy `showThemeSwitcher: true` |
| `styles/_theme.scss` | `@use 'd2-gui-shared/styles/theme'` | zmienne `:root` (kolory domyślne + rozmiary pól/paneli) i klasy `.d2-page*`, `.d2-panel`, `.d2-field*` |

## Użycie w aplikacji

```ts
// app.config.ts
import { provideD2Gui, withBuildInfo, withConnectionStatus } from 'd2-gui-shared';
import { environment } from '../environments/environment';
import { ApiConnectionService } from './core/services/api-connection.service';

providers: [
  provideD2Gui(
    {
      appName: 'Doc2',
      logoSrc: 'logo.svg',                      // asset aplikacji (public/logo.svg)
      homeLink: '/projects',
      homeLinkTitle: 'Wróć do listy projektów',
      footerTagline: 'Kreator parametryzacji szablonów',
      environmentName: environment.name,        // 'local' | 'dev' | 'uat' | 'preprd' | 'prd'
      offlineMessage: 'Brak połączenia z API — aplikacja nie może nawiązać połączenia z serwerem',
      // buildNumber / buildDate: statycznie, albo przez withBuildInfo() z API
    },
    withConnectionStatus(ApiConnectionService), // `readonly isApiConnected: Signal<boolean>`
    withBuildInfo(ApiConnectionService),        // `readonly buildNumber/buildDate: Signal<string | null>`
  ),
]
```

```ts
// app.routes.ts
import { D2AppShellComponent } from 'd2-gui-shared';

export const APP_ROUTES: Routes = [
  { path: '', component: D2AppShellComponent, children: [/* trasy aplikacji */] },
];
```

Health-check API, z którego aplikacja bierze dane do stopki, ma kształt jak w D2 ViewerEditor:
`{ status, environment, buildNumber, buildDate, timestamp }`.

Własny nagłówek z nawigacją lub własna zawartość prawej części stopki: zamiast `d2-app-shell` złóż shell
z `<d2-global-banners>`, `<d2-header>…</d2-header>`, przewijanego `<main>` i `<d2-footer>…</d2-footer>`.

## Build i konsumpcja

```bash
# w workspace D2GuiShared
npm run build            # → dist/D2GuiShared
npm test

# w aplikacji (lokalnie, przez symlink `file:`)
npm install ../../D2GuiShared/dist/D2GuiShared
```

Zależność `file:` to symlink do `dist/` — po każdym `npm run build` biblioteki aplikacja widzi nową
wersję, ale **dev-server Vite trzyma prebundlowaną kopię paczki w `.angular/cache`** (skutek `preserveSymlinks`).
Po przebudowie biblioteki w aplikacji: `npx ng cache clean`, potem restart `ng serve` — inaczej zobaczysz
`does not provide an export named '…'` dla nowych eksportów. Aplikacja potrzebuje `preserveSymlinks: true` (tsconfig i
angular.json), inaczej typy `Signal<T>` z dwóch kopii `@angular/core` są niezgodne.
Docelowo: publikacja do rejestru npm (GitHub Packages) i zwykła wersja w `package.json`.

## Motyw i palety

Kolory semantyczne żyją w zmiennych CSS `--d2-color-*`, które `D2ThemeService` wpisuje na `<html>` z wybranej
palety (`D2Theme`). Rozmiary pól i paneli (`--d2-control-height: 48px`, `--d2-panel-radius: 16px`, …) są
wspólne dla palet i siedzą w `styles/_theme.scss`.

```scss
// styles.scss aplikacji
@use 'd2-gui-shared/styles/theme';
```

```ts
provideD2Gui({
  theme: 'orange',              // start; wybór użytkownika z localStorage ma pierwszeństwo
  showThemeSwitcher: true,      // select w nagłówku (do wyłączenia, gdy paleta wynika z linii biznesowej)
  themes: [{                    // własna paleta linii biznesowej — ta sama nazwa nadpisuje wbudowaną
    name: 'bank-x', label: 'Bank X', palette: { ...D2_THEMES[0].palette, primary: '#123456', primaryHover: '#0f2a45', primaryLight: '#dde6ef' },
  }],
})
```

W komponentach używaj zmiennych (`var(--d2-color-primary)`) albo klas `.d2-field__control`, `.d2-panel`;
nie licz odcieni funkcjami Sass — hover/light są w palecie.

## Kontrolka listy `d2-grid`

Przeniesiona 1:1 z projektu `grid-forever` (`src/app/ui-grid`, Angular 20) z prefiksem `d2`; w D2 doszły
szablon szczegółów wiersza (`d2GridDetail`) i kliknięcie w wiersz (`rowClickable` / `rowClick`).
Wygląd: pomarańczowe linie i lupy, fioletowe akcje — jak na listach D2. Motyw przez zmienne CSS
(`--d2-grid-accent`, `--d2-grid-primary`, …) ustawione na `:host`.

```ts
columns: GridColumn<Worker>[] = [
  { key: 'name', header: 'Element', sortable: true, filterable: true },
  { key: 'workerType', header: 'Typ workera', sortable: true, filterable: true, width: '150px' },
  { key: 'configJson', header: 'Mapowanie', filterable: true },
  { key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, align: 'right' },
];
```

```html
<d2-grid [columns]="columns" [rows]="workers()" [trackBy]="trackWorker" [loading]="loading()" [pageSize]="10"
         [labels]="{ expandRow: 'Pokaż JSON konfiguracji' }" caption="Lista mapowań">
  <button d2GridActions type="button" (click)="add()">+ Dodaj worker</button>

  <ng-template d2GridCell="configJson" let-worker>
    <app-worker-mapping-preview [workerType]="worker.workerType" [configJson]="worker.configJson" />
  </ng-template>
  <ng-template d2GridCell="actions" let-worker>
    <button type="button" (click)="edit(worker)">Edytuj</button>
  </ng-template>

  <!-- przycisk „∨” w każdym wierszu rozwija ten szablon -->
  <ng-template d2GridDetail let-worker>
    <pre>{{ worker.configJson }}</pre>
  </ng-template>
</d2-grid>
```

Zasady kontrolki (nie mutuje danych, wyszukiwarka i filtry kolumnowe wykluczają się, zaznaczenie i
rozwinięcia pamiętane po `trackBy`, zmiana kryteriów wraca na stronę 1) — patrz komentarze w
`src/lib/grid/grid.component.ts` i testy (`grid.component.spec.ts`, 109 testów).

## Zasady

- Style komponentów są samowystarczalne (konkretne kolory jak w Edytorze), bez zależności od tokenów SCSS aplikacji.
- Nie dodawaj tu logiki domenowej ani HTTP — biblioteka dostaje dane przez `D2EnvironmentSource` / `D2ConnectionStatusSource` / `D2BuildInfoSource`.
- Zmiana wyglądu nagłówka/stopki = zmiana tutaj + `npm run build` + podbicie `version` w `projects/D2GuiShared/package.json`.

## Changelog

- **0.4.0** — motyw: `D2ThemeService`, `D2_THEMES` (orange/violet/teal/graphite), `d2-theme-switcher`, arkusz `styles/_theme.scss` (kolory + rozmiary pól/paneli, klasy `.d2-page*`/`.d2-panel`/`.d2-field*`); `d2-grid` i shell czytają kolory z motywu; `D2GuiConfig.theme/themes/showThemeSwitcher`.
- **0.3.0** — kontrolka listy `d2-grid` (+ `d2GridCell`, `d2GridDetail`) przeniesiona z `grid-forever/ui-grid`; nowe: szablon szczegółów wiersza, `rowClickable`/`rowClick`.
- **0.2.0** — stopka jako pasek statusu (środowisko, `Wersja: …`, data buildu; `withBuildInfo()`, pola `buildNumber`/`buildDate` w konfiguracji); shell `100vh` z przewijanym `<main>` — stopka zawsze widoczna.
- **0.1.0** — shell, nagłówek, stopka, baner środowiska, baner offline, `provideD2Gui()`.

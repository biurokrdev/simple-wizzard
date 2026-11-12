import {
  EnvironmentProviders,
  InjectionToken,
  Provider,
  Signal,
  Type,
  computed,
  inject,
  makeEnvironmentProviders,
  signal,
} from '@angular/core';
import type { D2Theme } from '../theme/d2-theme.model';

/**
 * Konfiguracja shella D2GuiShared. Każda aplikacja (Wizzard, Edytor…) podaje własne wartości
 * przez `provideD2Gui({...})`; brakujące pola dostają wartości domyślne z `D2_GUI_DEFAULT_CONFIG`.
 */
export interface D2GuiConfig {
  /** Nazwa aplikacji w nagłówku i stopce (domyślnie „Doc2"). */
  appName: string;
  /** Ścieżka do logo (asset aplikacji, np. `public/logo.svg` → `logo.svg`). */
  logoSrc: string;
  /** `routerLink` logo/nazwy w nagłówku (np. `/projects`). */
  homeLink: string | readonly unknown[];
  /** Tytuł linku „home" (atrybut `title`). */
  homeLinkTitle: string;
  /** Tekst po separatorze w stopce (np. „Kreator parametryzacji szablonów"). */
  footerTagline: string;
  /**
   * Statyczna nazwa środowiska (`LOCAL`, `DEV`, `UAT`, `PRE`, `PRD`…). Używana przez domyślne
   * `D2EnvironmentSource` (baner + stopka); `null`/`PRD` = ukryte. Aplikacja może podmienić źródło
   * przez `withEnvironmentSource()` (np. nazwa środowiska z health-checka API).
   */
  environmentName: string | null;
  /** Statyczny numer buildu do stopki („Wersja: …"). `null` = ukryty. Podmienialne przez `withBuildInfo()`. */
  buildNumber: string | null;
  /** Statyczna data buildu do stopki. `null` = ukryta. Podmienialne przez `withBuildInfo()`. */
  buildDate: string | null;
  /** Treść banera offline (gdy `D2ConnectionStatusSource.isApiConnected()` === false). */
  offlineMessage: string;
  /**
   * Początkowa paleta (`D2_THEMES` albo własna z `themes`). `null` = „orange”. Wybór użytkownika
   * zapamiętany w localStorage ma pierwszeństwo. Patrz `D2ThemeService`.
   */
  theme: string | null;
  /** Dodatkowe palety aplikacji (np. per linia biznesowa); ta sama nazwa nadpisuje wbudowaną. */
  themes: readonly D2Theme[];
  /** Czy nagłówek shella ma pokazywać przełącznik motywu (`<d2-theme-switcher>`). */
  showThemeSwitcher: boolean;
}

export const D2_GUI_DEFAULT_CONFIG: D2GuiConfig = {
  appName: 'Doc2',
  logoSrc: 'logo.svg',
  homeLink: '/',
  homeLinkTitle: 'Wróć do strony głównej',
  footerTagline: '',
  environmentName: null,
  buildNumber: null,
  buildDate: null,
  offlineMessage: 'Niedostępność serwera. Zawartość tylko do odczytu!',
  theme: null,
  themes: [],
  showThemeSwitcher: false,
};

export const D2_GUI_CONFIG = new InjectionToken<D2GuiConfig>('D2_GUI_CONFIG', {
  providedIn: 'root',
  factory: () => D2_GUI_DEFAULT_CONFIG,
});

/**
 * Źródło nazwy środowiska dla `<d2-environment-banner>` i stopki. Domyślna implementacja czyta
 * `D2GuiConfig.environmentName`; aplikacja może wskazać własny serwis (`withEnvironmentSource`),
 * byle wystawiał sygnał `environment`.
 */
export abstract class D2EnvironmentSource {
  abstract readonly environment: Signal<string | null | undefined>;
}

/**
 * Źródło stanu połączenia z API dla `<d2-offline-banner>`. Domyślnie „zawsze połączono";
 * aplikacja podaje własny serwis health-checka przez `withConnectionStatus`.
 */
export abstract class D2ConnectionStatusSource {
  abstract readonly isApiConnected: Signal<boolean>;
}

/**
 * Źródło danych o buildzie dla stopki („Wersja: …", data). Domyślnie z `D2GuiConfig`;
 * aplikacja zwykle podaje serwis czytający health-check API (`withBuildInfo`).
 * Puste/`null` wartości ukrywają odpowiednią pozycję w stopce.
 */
export abstract class D2BuildInfoSource {
  abstract readonly buildNumber: Signal<string | null | undefined>;
  abstract readonly buildDate: Signal<string | null | undefined>;
}

class StaticEnvironmentSource implements D2EnvironmentSource {
  private readonly config = inject(D2_GUI_CONFIG);
  readonly environment = computed(() => this.config.environmentName);
}

class AlwaysConnectedSource implements D2ConnectionStatusSource {
  readonly isApiConnected = signal(true).asReadonly();
}

class StaticBuildInfoSource implements D2BuildInfoSource {
  private readonly config = inject(D2_GUI_CONFIG);
  readonly buildNumber = computed(() => this.config.buildNumber);
  readonly buildDate = computed(() => this.config.buildDate);
}

/** Rozszerzenie konfiguracji — wynik `withEnvironmentSource()` / `withConnectionStatus()` / `withBuildInfo()`. */
export interface D2GuiFeature {
  readonly kind: 'environmentSource' | 'connectionStatus' | 'buildInfo';
  readonly providers: Provider[];
}

/** Podmienia źródło nazwy środowiska na istniejący serwis aplikacji (`useExisting`). */
export function withEnvironmentSource(source: Type<D2EnvironmentSource>): D2GuiFeature {
  return {
    kind: 'environmentSource',
    providers: [{ provide: D2EnvironmentSource, useExisting: source }],
  };
}

/** Podmienia źródło stanu połączenia z API na istniejący serwis aplikacji (`useExisting`). */
export function withConnectionStatus(source: Type<D2ConnectionStatusSource>): D2GuiFeature {
  return {
    kind: 'connectionStatus',
    providers: [{ provide: D2ConnectionStatusSource, useExisting: source }],
  };
}

/** Podmienia źródło danych o buildzie (numer, data) na istniejący serwis aplikacji (`useExisting`). */
export function withBuildInfo(source: Type<D2BuildInfoSource>): D2GuiFeature {
  return {
    kind: 'buildInfo',
    providers: [{ provide: D2BuildInfoSource, useExisting: source }],
  };
}

/**
 * Rejestruje konfigurację shella D2GuiShared w `ApplicationConfig.providers`.
 *
 * ```ts
 * provideD2Gui(
 *   { homeLink: '/projects', footerTagline: 'Kreator parametryzacji szablonów', environmentName: 'LOCAL' },
 *   withConnectionStatus(ApiConnectionService),
 *   withBuildInfo(ApiConnectionService),
 * )
 * ```
 */
export function provideD2Gui(
  config: Partial<D2GuiConfig> = {},
  ...features: D2GuiFeature[]
): EnvironmentProviders {
  const hasFeature = (kind: D2GuiFeature['kind']) => features.some((f) => f.kind === kind);

  return makeEnvironmentProviders([
    { provide: D2_GUI_CONFIG, useValue: { ...D2_GUI_DEFAULT_CONFIG, ...config } },
    ...(hasFeature('environmentSource')
      ? []
      : [{ provide: D2EnvironmentSource, useClass: StaticEnvironmentSource }]),
    ...(hasFeature('connectionStatus')
      ? []
      : [{ provide: D2ConnectionStatusSource, useClass: AlwaysConnectedSource }]),
    ...(hasFeature('buildInfo') ? [] : [{ provide: D2BuildInfoSource, useClass: StaticBuildInfoSource }]),
    ...features.flatMap((f) => f.providers),
  ]);
}

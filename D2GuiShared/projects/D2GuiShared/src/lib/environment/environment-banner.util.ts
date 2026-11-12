export type D2EnvironmentBannerVariant = 'local' | 'dev' | 'test' | 'pre' | 'unknown';

export interface D2EnvironmentBannerView {
  label: string;
  variant: D2EnvironmentBannerVariant;
}

// Nazwy produkcyjne nigdy nie pokazują banera (reguła: ukryty na PROD domyślnie).
const HIDDEN_ENVIRONMENTS = new Set(['PROD', 'PRD', 'PRODUCTION']);

// UAT i TST dzielą jasnożółty wariant „test", ale mają osobne etykiety.
const KNOWN_ENVIRONMENTS: Record<string, D2EnvironmentBannerView> = {
  LOCAL: { label: 'Wersja Local', variant: 'local' },
  DEV: { label: 'Wersja DEV', variant: 'dev' },
  DEVELOPMENT: { label: 'Wersja DEV', variant: 'dev' },
  UAT: { label: 'Wersja UAT', variant: 'test' },
  TST: { label: 'Wersja TST', variant: 'test' },
  TEST: { label: 'Wersja TST', variant: 'test' },
  PRE: { label: 'Wersja PRE', variant: 'pre' },
  PREPRD: { label: 'Wersja PRE', variant: 'pre' },
  PREPROD: { label: 'Wersja PRE', variant: 'pre' },
  PREPRODUCTION: { label: 'Wersja PRE', variant: 'pre' },
};

/**
 * Mapuje surową nazwę środowiska (dowolna wielkość liter) na widok banera albo `null`,
 * gdy baner ma być ukryty (produkcja / pusta nazwa). Nieznane nazwy dostają neutralny
 * wariant, żeby błędna konfiguracja była widoczna, a nie cicho zgubiona.
 */
export function resolveD2EnvironmentBanner(
  environmentName: string | null | undefined,
): D2EnvironmentBannerView | null {
  const raw = (environmentName ?? '').trim();
  const normalized = raw.toUpperCase();

  if (!normalized || HIDDEN_ENVIRONMENTS.has(normalized)) {
    return null;
  }

  return KNOWN_ENVIRONMENTS[normalized] ?? { label: `Wersja ${raw}`, variant: 'unknown' };
}

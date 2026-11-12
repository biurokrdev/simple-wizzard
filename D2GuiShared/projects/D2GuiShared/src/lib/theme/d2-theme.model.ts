/**
 * Motyw (paleta) aplikacji D2. Każda paleta to komplet kolorów semantycznych; `D2ThemeService`
 * wpisuje je jako zmienne CSS `--d2-color-*` na `<html>`, a style aplikacji (tokeny SCSS, `d2-grid`,
 * shell) czytają wyłącznie te zmienne. Dzięki temu kreator może wyglądać inaczej dla różnych linii
 * biznesowych bez zmian w komponentach — wystarczy wybrać albo dopisać paletę.
 *
 * Rozmiary (wysokość pól, promienie, odstępy paneli) są wspólne dla wszystkich palet i siedzą
 * w arkuszu `styles/_theme.scss` (`--d2-control-height`, `--d2-panel-radius`, …).
 */
export interface D2ThemePalette {
  /** Kolor główny: akcje podstawowe, linki, aktywny krok, linie tabel. */
  readonly primary: string;
  readonly primaryHover: string;
  /** Jasny odcień koloru głównego: podświetlenie wiersza, tła „miękkie”. */
  readonly primaryLight: string;
  /** Kolor akcentu: przyciski drugorzędne w listach, elementy aktywne grida. */
  readonly accent: string;
  readonly accentHover: string;
  readonly accentLight: string;
  /** Kolor drugorzędny (ciemny akcent, np. „Wstecz”). */
  readonly secondary: string;
  readonly secondaryHover: string;
  readonly secondaryLight: string;
  /** Nagłówki stron i etykiety pól. */
  readonly heading: string;
  /** Tekst podstawowy. */
  readonly text: string;
  /** Tekst pomocniczy. */
  readonly muted: string;
  /** Linie i ramki subtelne. */
  readonly border: string;
  /** Ramki pól formularzy i mocniejsze separatory. */
  readonly borderStrong: string;
  /** Tło strony (pod panelami). */
  readonly pageBg: string;
  /** Tło paneli, pól, tabel. */
  readonly surface: string;
  /** Tło wyciszone (pola wyłączone, pasek statusu). */
  readonly surfaceMuted: string;
  /** Obwódka fokusa (półprzezroczysty odcień koloru głównego). */
  readonly focus: string;
  readonly danger: string;
  readonly dangerLight: string;
  readonly success: string;
  readonly successLight: string;
  readonly warning: string;
  readonly warningLight: string;
}

export interface D2Theme {
  /** Identyfikator (atrybut `data-d2-theme`, klucz w localStorage). */
  readonly name: string;
  /** Etykieta w przełączniku motywów. */
  readonly label: string;
  /** Dla kogo / po co (np. linia biznesowa). */
  readonly description?: string;
  readonly palette: D2ThemePalette;
}

const NEUTRALS = {
  heading: '#1c1c1c',
  text: '#333333',
  muted: '#6b6b6b',
  border: '#e3e3e3',
  borderStrong: '#c9c9c9',
  surface: '#ffffff',
  surfaceMuted: '#f5f5f5',
  danger: '#d0021b',
  dangerLight: '#fde7e9',
  success: '#1e8700',
  successLight: '#dcfce7',
  warning: '#b45309',
  warningLight: '#fef3c7',
} as const;

/** Palety wbudowane. Aplikacja może dołożyć własne przez `provideD2Gui({ themes: [...] })`. */
export const D2_THEMES: readonly D2Theme[] = [
  {
    name: 'orange',
    label: 'Doc2 (pomarańcz)',
    description: 'Domyślny motyw D2: pomarańczowe akcje, fioletowe akcenty list.',
    palette: {
      ...NEUTRALS,
      primary: '#ff6200',
      primaryHover: '#cc4e00',
      primaryLight: '#fde3cf',
      accent: '#4b4a9b',
      accentHover: '#3b3a7e',
      accentLight: '#edecf6',
      secondary: '#4d0020',
      secondaryHover: '#340016',
      secondaryLight: '#ffe5f5',
      pageBg: '#f7f3ef',
      focus: 'rgba(255, 98, 0, 0.25)',
    },
  },
  {
    name: 'violet',
    label: 'Business Banking (fiolet)',
    description: 'Fioletowe akcje główne, pomarańcz jako akcent.',
    palette: {
      ...NEUTRALS,
      primary: '#4b4a9b',
      primaryHover: '#3b3a7e',
      primaryLight: '#e6e5f4',
      accent: '#ff6200',
      accentHover: '#cc4e00',
      accentLight: '#fde3cf',
      secondary: '#1c1c1c',
      secondaryHover: '#000000',
      secondaryLight: '#ececec',
      pageBg: '#f3f2f8',
      focus: 'rgba(75, 74, 155, 0.25)',
    },
  },
  {
    name: 'teal',
    label: 'Wholesale Banking (morski)',
    description: 'Morska zieleń jako kolor główny, bursztyn jako akcent.',
    palette: {
      ...NEUTRALS,
      primary: '#0f766e',
      primaryHover: '#0b5b55',
      primaryLight: '#d5f0ec',
      accent: '#d97706',
      accentHover: '#b45309',
      accentLight: '#fcebd0',
      secondary: '#134e4a',
      secondaryHover: '#0d3835',
      secondaryLight: '#e0f2f1',
      pageBg: '#f1f6f5',
      focus: 'rgba(15, 118, 110, 0.25)',
    },
  },
  {
    name: 'graphite',
    label: 'Neutralny (grafit)',
    description: 'Monochromatyczny, pomarańcz tylko w akcentach.',
    palette: {
      ...NEUTRALS,
      primary: '#1c1c1c',
      primaryHover: '#000000',
      primaryLight: '#e8e8e8',
      accent: '#ff6200',
      accentHover: '#cc4e00',
      accentLight: '#fde3cf',
      secondary: '#4b4a9b',
      secondaryHover: '#3b3a7e',
      secondaryLight: '#edecf6',
      pageBg: '#f4f4f4',
      focus: 'rgba(28, 28, 28, 0.2)',
    },
  },
];

export const D2_DEFAULT_THEME_NAME = 'orange';

/** Nazwa zmiennej CSS dla klucza palety: `primaryHover` → `--d2-color-primary-hover`. */
export function d2ThemeCssVariable(key: keyof D2ThemePalette): string {
  return `--d2-color-${key.replace(/[A-Z]/g, (letter) => `-${letter.toLowerCase()}`)}`;
}

/** Komplet zmiennych CSS dla palety (klucz → wartość). */
export function d2ThemeCssVariables(theme: D2Theme): Record<string, string> {
  const variables: Record<string, string> = {};
  for (const [key, value] of Object.entries(theme.palette) as [keyof D2ThemePalette, string][]) {
    variables[d2ThemeCssVariable(key)] = value;
  }
  return variables;
}

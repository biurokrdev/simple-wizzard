/**
 * Publiczny model danych kontrolki `d2-grid`.
 *
 * Cały moduł jest czysto deklaratywny - nie zawiera logiki ani zależności do
 * Angulara, dzięki czemu można go używać (i testować) niezależnie od widoku.
 */

/** Kierunek sortowania pojedynczej kolumny. */
export type SortDirection = 'asc' | 'desc';

/** Aktualnie wybrane sortowanie: klucz kolumny + kierunek. */
export interface GridSort {
  readonly key: string;
  readonly direction: SortDirection;
}

/** Wyrównanie zawartości komórek w kolumnie. */
export type GridAlign = 'left' | 'center' | 'right';

/**
 * Definicja jednej kolumny.
 *
 * @typeParam T - typ pojedynczego wiersza danych.
 */
export interface GridColumn<T> {
  /** Unikalny klucz kolumny. Domyślnie służy też do odczytu pola z wiersza. */
  readonly key: string;

  /** Nagłówek widoczny dla użytkownika. */
  readonly header: string;

  /**
   * Surowa wartość komórki - używana do sortowania.
   * Domyślnie `row[key]`.
   */
  readonly value?: (row: T) => unknown;

  /**
   * Tekst prezentowany w komórce - używany też przy filtrowaniu i wyszukiwaniu.
   * Domyślnie sformatowana wartość zwrócona przez `value`.
   */
  readonly format?: (value: unknown, row: T) => string;

  /** Czy nagłówek kolumny pozwala na sortowanie. Domyślnie `false`. */
  readonly sortable?: boolean;

  /** Czy kolumna ma własne pole filtra pod nagłówkiem. Domyślnie `false`. */
  readonly filterable?: boolean;

  /** Czy kolumna jest brana pod uwagę przy wyszukiwaniu globalnym. Domyślnie `true`. */
  readonly searchable?: boolean;

  /**
   * Czy kolumna może zostać schowana do panelu szczegółów wiersza, gdy tabela
   * nie mieści się na ekranie. Domyślnie `false`.
   */
  readonly collapsible?: boolean;

  /** Szerokość kolumny (dowolna wartość CSS, np. `'120px'`, `'10%'`). */
  readonly width?: string;

  /** Wyrównanie zawartości. Domyślnie `'left'`. */
  readonly align?: GridAlign;

  /** Ukrywa tekst nagłówka (przydatne dla kolumn z ikonami / akcjami). */
  readonly headerHidden?: boolean;

  /** Własny komparator wartości - wygrywa z domyślnym porównaniem. */
  readonly compare?: (a: unknown, b: unknown) => number;
}

/** Pojedyncza opcja listy rozwijanej w panelu filtrów. */
export interface GridFilterOption {
  /** Wartość porównywana z tekstem komórki (dokładne dopasowanie). */
  readonly value: string;
  /** Etykieta widoczna na liście. */
  readonly label: string;
}

/** Pole panelu filtrów - lista rozwijana nad tabelą. */
export interface GridFilterField {
  /** Klucz kolumny, której dotyczy filtr. */
  readonly key: string;
  /** Etykieta nad listą. */
  readonly label: string;
  /** Dostępne wartości. Pusty wybór oznacza brak filtra. */
  readonly options: readonly GridFilterOption[];
}

/** Warunki zawężania listy. */
export interface GridFilterCriteria {
  /** Fraza wyszukiwania globalnego (dopasowanie "zawiera"). */
  readonly search: string;
  /** Filtry kolumnowe spod nagłówków (dopasowanie "zawiera"). */
  readonly filters: Readonly<Record<string, string>>;
  /** Wybory z panelu filtrów (dopasowanie dokładne). */
  readonly fieldFilters: Readonly<Record<string, string>>;
}

/** Pełny stan interakcji użytkownika z gridem. */
export interface GridState extends GridFilterCriteria {
  /** Wybrane sortowanie lub `null`, gdy dane są w kolejności źródłowej. */
  readonly sort: GridSort | null;
  /** Numer aktualnej strony, liczony od 1. */
  readonly page: number;
  /** Liczba wierszy na stronie. */
  readonly pageSize: number;
}

/** Wynik stronicowania. */
export interface GridPage<T> {
  /** Wiersze bieżącej strony. */
  readonly items: readonly T[];
  /** Numer bieżącej strony (od 1, po przycięciu do dostępnego zakresu). */
  readonly page: number;
  /** Liczba stron (minimum 1, nawet dla pustej listy). */
  readonly pageCount: number;
  /** Liczba wszystkich wierszy po filtrowaniu. */
  readonly total: number;
  /** Numer pierwszego wiersza na stronie (od 1); `0` dla pustej listy. */
  readonly from: number;
  /** Numer ostatniego wiersza na stronie; `0` dla pustej listy. */
  readonly to: number;
}

/** Element paska stronicowania: numer strony albo wielokropek. */
export type PageToken = number | 'ellipsis';

/**
 * Tryb chowania kolumn oznaczonych jako `collapsible`:
 * - `auto` - decyduje szerokość kontrolki,
 * - `always` / `never` - wymuszenie trybu przez aplikację (przydatne w testach).
 */
export type GridCollapseMode = 'auto' | 'always' | 'never';

/**
 * Tryb zaznaczania wierszy:
 * - `none` - bez zaznaczania (domyślnie),
 * - `single` - jeden wiersz naraz,
 * - `multiple` - wiele wierszy + zaznaczanie całej strony w nagłówku.
 */
export type GridSelectionMode = 'none' | 'single' | 'multiple';

/** Teksty interfejsu - w całości nadpisywalne przez aplikację. */
export interface GridLabels {
  /** Placeholder i etykieta pola wyszukiwania globalnego. */
  readonly search: string;
  /** Etykieta pola filtra kolumny. Wspiera parametr `{column}`. */
  readonly filterColumn: string;
  /** Etykieta przycisku czyszczenia filtra kolumny. Wspiera parametr `{column}`. */
  readonly clearColumnFilter: string;
  /** Przycisk rozwijający panel filtrów. */
  readonly showFilters: string;
  /** Przycisk zwijający panel filtrów. */
  readonly hideFilters: string;
  /** Przycisk czyszczący filtry. Wspiera parametr `{count}`. */
  readonly clearFilters: string;
  /** Pusta opcja listy filtra (brak zawężenia). */
  readonly anyOption: string;
  /** Komunikat dla pustej listy. */
  readonly empty: string;
  /** Komunikat w trakcie ładowania danych. */
  readonly loading: string;
  /** Etykieta przycisku rozwijającego szczegóły wiersza. */
  readonly expandRow: string;
  /** Etykieta przycisku zwijającego szczegóły wiersza. */
  readonly collapseRow: string;
  /** Etykieta selecta z liczbą wierszy na stronie. */
  readonly pageSize: string;
  /** Podsumowanie zakresu. Wspiera parametry `{from}`, `{to}`, `{total}`. */
  readonly summary: string;
  /** Etykieta przycisku poprzedniej strony. */
  readonly previousPage: string;
  /** Etykieta przycisku następnej strony. */
  readonly nextPage: string;
  /** Etykieta przycisku konkretnej strony. Wspiera parametr `{page}`. */
  readonly page: string;
  /** Etykieta przycisku sortowania. Wspiera parametr `{column}`. */
  readonly sortBy: string;
  /** Etykieta nawigacji po stronach (dla czytników ekranu). */
  readonly pagination: string;
  /** Etykieta pola zaznaczania wiersza. */
  readonly selectRow: string;
  /** Etykieta pola zaznaczania wszystkich wierszy strony. */
  readonly selectAll: string;
  /** Podsumowanie zaznaczenia. Wspiera parametr `{count}`. */
  readonly selectedCount: string;
}

/** Domyślne (polskie) teksty kontrolki. */
export const DEFAULT_GRID_LABELS: GridLabels = {
  search: 'Szukaj na liście',
  filterColumn: 'Filtruj kolumnę {column}',
  clearColumnFilter: 'Wyczyść filtr kolumny {column}',
  showFilters: 'Pokaż filtry',
  hideFilters: 'Ukryj filtry',
  clearFilters: 'Wyczyść filtry ({count})',
  anyOption: 'Wszystkie',
  empty: 'Brak danych do wyświetlenia',
  loading: 'Trwa ładowanie danych...',
  expandRow: 'Pokaż szczegóły wiersza',
  collapseRow: 'Ukryj szczegóły wiersza',
  pageSize: 'Wyświetlaj',
  summary: 'Wyświetlono {from}-{to} z {total}',
  previousPage: 'Poprzednia strona',
  nextPage: 'Następna strona',
  page: 'Strona {page}',
  sortBy: 'Sortuj według kolumny {column}',
  pagination: 'Stronicowanie',
  selectRow: 'Zaznacz wiersz',
  selectAll: 'Zaznacz wszystkie wiersze na stronie',
  selectedCount: 'Zaznaczono: {count}',
};

/**
 * Podstawia parametry w tekście etykiety, np.
 * `formatLabel('Strona {page}', { page: 3 }) === 'Strona 3'`.
 */
export function formatLabel(template: string, params: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (match, key: string) =>
    key in params ? String(params[key]) : match,
  );
}

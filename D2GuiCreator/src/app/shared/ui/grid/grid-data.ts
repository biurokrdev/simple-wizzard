/**
 * Logika danych grida: odczyt komórek, wyszukiwanie, filtrowanie, sortowanie
 * i stronicowanie.
 *
 * Wszystkie funkcje są czyste (nie mutują wejścia, nie mają stanu), dlatego
 * komponent może je składać w sygnałach, a testy sprawdzać bez udziału DOM.
 */
import { GridColumn, GridFilterCriteria, GridPage, GridSort, PageToken } from './grid.model';

/** Kolator używany do porównywania tekstów - polska kolejność, cyfry naturalnie. */
const COLLATOR = new Intl.Collator('pl', { numeric: true, sensitivity: 'base' });

/** Znaki, których NFD nie rozkłada na literę bazową + znak diakrytyczny. */
const SPECIAL_LETTERS: Record<string, string> = { ł: 'l', Ł: 'L' };

/** Zwraca surową wartość komórki (do sortowania i własnych formatów). */
export function cellValue<T>(column: GridColumn<T>, row: T): unknown {
  if (column.value) {
    return column.value(row);
  }
  return (row as Record<string, unknown>)[column.key];
}

/** Zwraca tekst komórki (do wyświetlenia, wyszukiwania i filtrowania). */
export function cellText<T>(column: GridColumn<T>, row: T): string {
  const value = cellValue(column, row);
  return column.format ? column.format(value, row) : formatValue(value);
}

/** Domyślne formatowanie wartości na tekst. */
export function formatValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }
  if (value instanceof Date) {
    return value.toISOString().slice(0, 10);
  }
  return String(value);
}

/**
 * Normalizuje tekst do porównań "user friendly": małe litery bez znaków
 * diakrytycznych, przycięte białe znaki. Dzięki temu `zaklad` znajdzie
 * `Zakład`.
 */
export function normalizeText(text: string): string {
  return text
    .replace(/[łŁ]/g, (letter) => SPECIAL_LETTERS[letter] ?? letter)
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();
}

/** Czy `haystack` zawiera `needle` (z pominięciem wielkości liter i diakrytyków). */
export function includesText(haystack: string, needle: string): boolean {
  const term = normalizeText(needle);
  return term === '' || normalizeText(haystack).includes(term);
}

/** Czy wiersz pasuje do frazy wyszukiwania globalnego. */
export function matchesSearch<T>(row: T, columns: readonly GridColumn<T>[], search: string): boolean {
  if (normalizeText(search) === '') {
    return true;
  }
  return columns
    .filter((column) => column.searchable !== false)
    .some((column) => includesText(cellText(column, row), search));
}

/** Czy wiersz pasuje do wszystkich aktywnych filtrów kolumnowych. */
export function matchesFilters<T>(
  row: T,
  columns: readonly GridColumn<T>[],
  filters: Readonly<Record<string, string>>,
): boolean {
  return Object.entries(filters).every(([key, term]) => {
    if (normalizeText(term) === '') {
      return true;
    }
    const column = columns.find((candidate) => candidate.key === key);
    return column ? includesText(cellText(column, row), term) : true;
  });
}

/**
 * Czy wiersz pasuje do wyborów z panelu filtrów.
 *
 * W odróżnieniu od filtrów kolumnowych ("zawiera") tutaj wymagane jest
 * dokładne dopasowanie - listy rozwijane podają konkretne wartości.
 */
export function matchesFieldFilters<T>(
  row: T,
  columns: readonly GridColumn<T>[],
  values: Readonly<Record<string, string>>,
): boolean {
  return Object.entries(values).every(([key, value]) => {
    if (normalizeText(value) === '') {
      return true;
    }
    const column = columns.find((candidate) => candidate.key === key);
    return column ? normalizeText(cellText(column, row)) === normalizeText(value) : true;
  });
}

/** Zwraca wiersze spełniające jednocześnie wszystkie warunki zawężania. */
export function filterRows<T>(
  rows: readonly T[],
  columns: readonly GridColumn<T>[],
  criteria: GridFilterCriteria,
): T[] {
  return rows.filter(
    (row) =>
      matchesSearch(row, columns, criteria.search) &&
      matchesFilters(row, columns, criteria.filters) &&
      matchesFieldFilters(row, columns, criteria.fieldFilters),
  );
}

/** Liczba aktywnych (niepustych) warunków zawężania - bez frazy wyszukiwania. */
export function countActiveFilters(criteria: GridFilterCriteria): number {
  return [...Object.values(criteria.filters), ...Object.values(criteria.fieldFilters)].filter(
    (value) => normalizeText(value) !== '',
  ).length;
}

/**
 * Porównuje dwie wartości komórek.
 *
 * Wartości puste (`null`, `undefined`, `''`) trafiają zawsze na koniec przy
 * sortowaniu rosnącym (a więc na początek przy malejącym).
 */
export function compareValues(a: unknown, b: unknown): number {
  const aEmpty = isEmpty(a);
  const bEmpty = isEmpty(b);
  if (aEmpty || bEmpty) {
    return aEmpty && bEmpty ? 0 : aEmpty ? 1 : -1;
  }
  if (typeof a === 'number' && typeof b === 'number') {
    return a - b;
  }
  if (typeof a === 'boolean' && typeof b === 'boolean') {
    return Number(a) - Number(b);
  }
  if (a instanceof Date && b instanceof Date) {
    return a.getTime() - b.getTime();
  }
  return COLLATOR.compare(formatValue(a), formatValue(b));
}

function isEmpty(value: unknown): boolean {
  return value === null || value === undefined || value === '';
}

/**
 * Zwraca nową, posortowaną tablicę wierszy. Sortowanie jest stabilne, więc
 * wiersze o równych wartościach zachowują kolejność źródłową.
 */
export function sortRows<T>(
  rows: readonly T[],
  columns: readonly GridColumn<T>[],
  sort: GridSort | null,
): T[] {
  const column = sort ? columns.find((candidate) => candidate.key === sort.key) : undefined;
  if (!sort || !column) {
    return [...rows];
  }
  const direction = sort.direction === 'asc' ? 1 : -1;
  const compare = column.compare ?? compareValues;
  return [...rows].sort(
    (a, b) => direction * compare(cellValue(column, a), cellValue(column, b)),
  );
}

/**
 * Wycina stronę z listy wierszy. Numer strony jest przycinany do dostępnego
 * zakresu, więc wywołanie ze stroną 99 dla 2 stron zwróci stronę 2.
 */
export function paginate<T>(rows: readonly T[], page: number, pageSize: number): GridPage<T> {
  const total = rows.length;
  const size = Math.max(1, Math.trunc(pageSize));
  const pageCount = Math.max(1, Math.ceil(total / size));
  const current = clamp(Math.trunc(page), 1, pageCount);
  const start = (current - 1) * size;
  const items = rows.slice(start, start + size);
  return {
    items,
    page: current,
    pageCount,
    total,
    from: items.length === 0 ? 0 : start + 1,
    to: items.length === 0 ? 0 : start + items.length,
  };
}

/** Przycina liczbę do zadanego zakresu. */
export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/**
 * Buduje listę przycisków stronicowania: pierwsza i ostatnia strona są zawsze
 * widoczne, wokół bieżącej strony powstaje okno, a przerwy zastępuje
 * `'ellipsis'`.
 *
 * @param maxButtons - maksymalna liczba przycisków z numerami (minimum 5).
 */
export function pageTokens(page: number, pageCount: number, maxButtons = 5): PageToken[] {
  const pages = Math.max(1, Math.trunc(pageCount));
  const limit = Math.max(5, Math.trunc(maxButtons));
  const current = clamp(Math.trunc(page), 1, pages);

  if (pages <= limit) {
    return range(1, pages);
  }

  const sideCount = Math.floor((limit - 3) / 2);
  const start = clamp(current - sideCount, 2, pages - limit + 2);
  const end = start + limit - 3;

  return [
    1,
    ...(start > 2 ? (['ellipsis'] as PageToken[]) : []),
    ...range(start, end),
    ...(end < pages - 1 ? (['ellipsis'] as PageToken[]) : []),
    pages,
  ];
}

function range(from: number, to: number): number[] {
  return Array.from({ length: Math.max(0, to - from + 1) }, (_, index) => from + index);
}

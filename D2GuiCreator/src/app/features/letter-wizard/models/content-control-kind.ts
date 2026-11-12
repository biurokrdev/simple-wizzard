/**
 * Rodzaje formantów (content controls) rozpoznawane przez API w szablonie DOCX (`extract-placeholders`,
 * pole `contentControlDetails`) i ich etykiety w GUI. Słownik `contentControls` (nazwa → typ w schemacie:
 * `boolean` dla wariantu, `array` dla reszty) zostaje dla zgodności — starsze zapisy mają tylko `boolean`.
 */
export type ContentControlKind = 'variant' | 'repeat' | 'list' | 'table';
export type ContentControlListType = 'bullet' | 'numbered';
export type ContentControlLevel = 'block' | 'inline' | 'row' | 'cell';

/** Kolumna wiersza powielanego w arkuszu XLSX (`TemplateColumnDto`). */
export interface TemplateColumn {
  cell: string;
  placeholder: string;
  /** Właściwość elementu listy (`item.kwota`); null = lista wartości prostych (`item`). */
  property: string | null;
  header: string | null;
}

/** Luźny znacznik w arkuszu XLSX (`PlaceholderDetailDto`): etykieta z komórki obok i adres. */
export interface PlaceholderDetail {
  placeholder: string;
  label: string | null;
  sheet: string | null;
  cell: string | null;
}

/** Szczegóły formantu z API (`ContentControlDetailDto`). */
export interface ContentControlDetail {
  name: string;
  kind: ContentControlKind;
  listType: ContentControlListType | null;
  location: 'MainDocument' | 'Header' | 'Footer' | string;
  level: ContentControlLevel | string;
  preview: string | null;
  paragraphCount: number;
  containsTable: boolean;
  containsList: boolean;
  isRepeating: boolean;
  parent: string | null;
  suggestedWorkerType: string;
  alias: string | null;
  /** XLSX: arkusz, zakres wiersza powielanego, nagłówek i kolumny (null dla DOCX). */
  sheet?: string | null;
  range?: string | null;
  headerCells?: string[] | null;
  columns?: TemplateColumn[] | null;
}

/** Wiersz tabeli „Znalezione elementy w dokumencie” (krok 2, podsumowanie, szczegóły projektu). */
export interface DetectedElementRow {
  /** Znacznik `<%nazwa%>` albo nazwa formantu. */
  name: string;
  /** Typ w schemacie danych: `placeholder` | `boolean` | `array` (zgodny ze starszymi zapisami). */
  type: string;
  /** Rodzaj dla odznaki: `placeholder` | `variant` | `repeat` | `list` | `table`. */
  kind: 'placeholder' | ContentControlKind;
  /** Etykieta po polsku. */
  label: string;
  /** Proponowany worker. */
  workerType: string;
  location: string;
  preview: string | null;
  parent: string | null;
  level: string | null;
  /** XLSX: kolumny wiersza powielanego (tabela) albo adres komórki luźnego znacznika. */
  columns?: TemplateColumn[] | null;
  cell?: string | null;
}

export const CONTENT_CONTROL_KIND_LABELS: Record<ContentControlKind, string> = {
  variant: 'Fragment wariantowy',
  repeat: 'Fragment powtarzany',
  list: 'Lista',
  table: 'Tabela'
};

export const CONTENT_CONTROL_KIND_HINTS: Record<ContentControlKind, string> = {
  variant: 'Pokazywany albo usuwany zależnie od warunku — worker „Stan” (lub „Usuń obszar”).',
  repeat: 'Powielany dla każdego elementu listy z modelu danych — worker „Powtórz blok”.',
  list: 'Lista punktowana lub numerowana — jedna pozycja na element listy z danych.',
  table: 'Tabela — wiersz powielany dla każdego elementu listy z danych.'
};

const LOCATION_LABELS: Record<string, string> = {
  MainDocument: 'treść',
  Header: 'nagłówek',
  Footer: 'stopka'
};

export function locationLabel(location: string | null | undefined): string {
  return location ? (LOCATION_LABELS[location] ?? location) : 'treść';
}

/** Etykieta rodzaju formantu — listy z rodzajem numeracji, tabela z arkusza z liczbą kolumn. */
export function contentControlKindLabel(detail: Pick<ContentControlDetail, 'kind' | 'listType'> & { columns?: TemplateColumn[] | null }): string {
  if (detail.kind === 'table' && detail.columns && detail.columns.length > 0) {
    return detail.columns.length === 1 ? 'Wiersz powielany' : `Tabela (${detail.columns.length} kolumn)`;
  }
  if (detail.kind === 'list') {
    return detail.listType === 'bullet' ? 'Lista punktowana' : detail.listType === 'numbered' ? 'Lista numerowana' : 'Lista';
  }
  return CONTENT_CONTROL_KIND_LABELS[detail.kind] ?? detail.kind;
}

/** Etykieta po typie ze schematu (dla zapisów bez szczegółów). */
export function elementTypeLabel(type: string): string {
  switch (type) {
    case 'placeholder':
      return 'Znacznik tekstowy';
    case 'boolean':
      return CONTENT_CONTROL_KIND_LABELS.variant;
    case 'array':
      return CONTENT_CONTROL_KIND_LABELS.repeat;
    default:
      return type;
  }
}

/** Klasa odznaki (`styles/_grid.scss`: `.d2-type-badge--*`). */
export function elementBadgeClass(kind: DetectedElementRow['kind'] | string): string {
  return `d2-type-badge--${kind}`;
}

/**
 * Składa wiersze tabeli elementów z wyniku analizy szablonu (albo z `detectedElements` zapisanych w `step_data`).
 * Szczegóły są opcjonalne — bez nich rodzaj wynika z typu w schemacie (`boolean` → wariant, `array` → powtarzany).
 */
export function buildDetectedElementRows(
  placeholders: readonly string[] | null | undefined,
  contentControls: Record<string, string> | null | undefined,
  details?: readonly ContentControlDetail[] | null,
  placeholderDetails?: readonly PlaceholderDetail[] | null
): DetectedElementRow[] {
  const rows: DetectedElementRow[] = [];
  const placeholderByName = new Map((placeholderDetails ?? []).map((d) => [d.placeholder, d]));
  for (const p of placeholders ?? []) {
    const name = p.startsWith('<%') ? p : `<%${p}%>`;
    const detail = placeholderByName.get(name);
    rows.push({
      name,
      type: 'placeholder',
      kind: 'placeholder',
      label: elementTypeLabel('placeholder'),
      workerType: 'Podstaw wartość',
      location: detail?.sheet ?? '',
      // XLSX: etykieta z komórki obok („Koszt”) i adres komórki
      preview: detail ? [detail.label, detail.cell].filter(Boolean).join(' · ') || null : null,
      parent: null,
      level: null,
      cell: detail?.cell ?? null
    });
  }
  const byName = new Map((details ?? []).map((d) => [d.name.toLowerCase(), d]));
  for (const [name, type] of Object.entries(contentControls ?? {})) {
    const detail = byName.get(name.toLowerCase());
    const kind: DetectedElementRow['kind'] = detail?.kind ?? (type === 'array' ? 'repeat' : 'variant');
    rows.push({
      name,
      type: String(type),
      kind,
      label: detail ? contentControlKindLabel(detail) : elementTypeLabel(String(type)),
      workerType: detail?.suggestedWorkerType ?? (kind === 'variant' ? 'Stan' : 'Powtórz blok'),
      location: locationLabel(detail?.location),
      preview: detail?.preview ?? null,
      parent: detail?.parent ?? null,
      level: detail?.level ?? null,
      columns: detail?.columns ?? null
    });
  }
  return rows;
}

/** Zliczenie rodzajów do komunikatu po wczytaniu szablonu. */
export function summarizeContentControlKinds(details: readonly ContentControlDetail[]): string[] {
  const counts = new Map<string, number>();
  for (const d of details) {
    const label = contentControlKindLabel(d);
    counts.set(label, (counts.get(label) ?? 0) + 1);
  }
  return [...counts.entries()].map(([label, count]) => `${label}: ${count}`);
}

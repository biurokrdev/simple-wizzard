/**
 * Czytelna reprezentacja konfiguracji workera (`workers.config_json`) do podglądu w tabelach i dialogu.
 *
 * Klucze w JSON występują w dwóch konwencjach: backend (`GenerateWorkersCommandHandler`, BR-010) zapisuje
 * `search_for` / `replacement_property_expression` / `tag_name` / `conditional_expression` (podkreślenia),
 * a starsze zapisy z GUI używały myślników (`search-for`). Parser czyta obie; zapis z GUI od 2026-11-08
 * ujednolicono na podkreślenia (jak backend).
 */

export type WorkerConfigJson = Record<string, unknown>;

export interface WorkerMappingView {
  /** `substitution` = „Podstaw wartość", `visibility` = „Stan", `repeat` = „Powtórz blok", `unknown` = nieznany typ / nieczytelny JSON. */
  kind: 'substitution' | 'visibility' | 'repeat' | 'unknown';
  /** Lewa strona: znacznik w dokumencie (`<%nazwa%>`) albo nazwa kontrolki tekstowej. */
  source: string | null;
  /** Prawa strona: ścieżka w modelu danych rozbita na segmenty (`formModel.a.b` → `['formModel','a','b']`). */
  targetPath: string[];
  /** Surowe wyrażenie prawej strony (gdy nie jest prostą ścieżką `a.b.c`, pokazujemy je w całości). */
  targetExpression: string | null;
  /** Czy `targetExpression` da się pokazać jako łańcuch segmentów. */
  targetIsPath: boolean;
  /** Wartość domyślna (tylko „Podstaw wartość"). */
  defaultValue: string | null;
  /** „Stan": czy element ma być ukryty przy fałszywym warunku. */
  hideIfFalse: boolean | null;
  /** „Powtórz blok": zakres powielania (`block` | `paragraph` | `row`). */
  repeatScope: string | null;
  /** XLSX: liczba kolumn wiersza powielanego (`columns`) albo null. */
  repeatColumns: number | null;
  /** XLSX: arkusz!komórka luźnego znacznika i etykieta z komórki obok. */
  cellRef: string | null;
  label: string | null;
  /** Czy JSON dał się sparsować. */
  parsed: boolean;
  /** Ładnie sformatowany JSON do rozwijanego podglądu (albo surowy tekst, gdy nie da się sparsować). */
  prettyJson: string;
}

export const WORKER_TYPE_SUBSTITUTION = 'Podstaw wartość';
export const WORKER_TYPE_VISIBILITY = 'Stan';
export const WORKER_TYPE_REPEAT = 'Powtórz blok';

export const REPEAT_SCOPE_LABELS: Record<string, string> = {
  block: 'cały blok',
  paragraph: 'akapit (pozycja listy)',
  row: 'wiersz tabeli'
};

const PATH_SEGMENT = /^[A-Za-z_$][\w$]*$/;

/** Czyta klucz w obu konwencjach (`search_for` i `search-for`). */
export function readConfigKey(config: WorkerConfigJson, snakeKey: string): unknown {
  if (snakeKey in config) {
    return config[snakeKey];
  }
  const kebab = snakeKey.replace(/_/g, '-');
  return kebab in config ? config[kebab] : undefined;
}

export function parseWorkerConfigJson(configJson: string | null | undefined): WorkerConfigJson | null {
  if (!configJson || !configJson.trim()) {
    return null;
  }
  try {
    const parsed: unknown = JSON.parse(configJson);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? (parsed as WorkerConfigJson) : null;
  } catch {
    return null;
  }
}

export function prettyPrintJson(configJson: string | null | undefined): string {
  const parsed = parseWorkerConfigJson(configJson);
  return parsed ? JSON.stringify(parsed, null, 2) : (configJson ?? '');
}

/** `formModel.ojciec.penia.nazwa_potoczna` → segmenty; `null`, gdy to nie jest prosta ścieżka. */
export function splitPropertyPath(expression: string | null): string[] | null {
  if (!expression) {
    return null;
  }
  const segments = expression.trim().split('.');
  return segments.length > 0 && segments.every((s) => PATH_SEGMENT.test(s)) ? segments : null;
}

function asString(value: unknown): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  const text = typeof value === 'string' ? value : String(value);
  return text.trim() ? text : null;
}

/** `Arkusz!B3` z pól `sheet` i `cell` (albo `range`) konfiguracji XLSX; null, gdy brak. */
function cellReference(config: WorkerConfigJson, cellKey = 'cell'): string | null {
  const sheet = asString(readConfigKey(config, 'sheet'));
  const cell = asString(readConfigKey(config, cellKey));
  if (!sheet && !cell) {
    return null;
  }
  return sheet && cell ? `${sheet}!${cell}` : (sheet ?? cell);
}

function asBoolean(value: unknown): boolean | null {
  if (typeof value === 'boolean') {
    return value;
  }
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase();
    if (normalized === 'true') return true;
    if (normalized === 'false') return false;
  }
  return null;
}

/** Buduje widok mapowania dla tabeli i dialogu. Nigdy nie rzuca — nieczytelny JSON daje `kind: 'unknown'`. */
export function buildWorkerMappingView(workerType: string, configJson: string | null | undefined): WorkerMappingView {
  const config = parseWorkerConfigJson(configJson);
  const prettyJson = prettyPrintJson(configJson);

  const base: WorkerMappingView = {
    kind: 'unknown',
    source: null,
    targetPath: [],
    targetExpression: null,
    targetIsPath: false,
    defaultValue: null,
    hideIfFalse: null,
    repeatScope: null,
    repeatColumns: null,
    cellRef: null,
    label: null,
    parsed: config !== null,
    prettyJson,
  };

  if (!config) {
    return base;
  }

  if (workerType === WORKER_TYPE_SUBSTITUTION) {
    const expression = asString(readConfigKey(config, 'replacement_property_expression'));
    const path = splitPropertyPath(expression);
    return {
      ...base,
      kind: 'substitution',
      source: asString(readConfigKey(config, 'search_for')),
      targetExpression: expression,
      targetPath: path ?? [],
      targetIsPath: path !== null,
      defaultValue: asString(readConfigKey(config, 'default_value')),
      cellRef: cellReference(config),
      label: asString(readConfigKey(config, 'label')),
    };
  }

  if (workerType === WORKER_TYPE_VISIBILITY) {
    const expression = asString(readConfigKey(config, 'conditional_expression'));
    const path = splitPropertyPath(expression);
    return {
      ...base,
      kind: 'visibility',
      source: asString(readConfigKey(config, 'tag_name')),
      targetExpression: expression,
      targetPath: path ?? [],
      targetIsPath: path !== null,
      hideIfFalse: asBoolean(readConfigKey(config, 'hide_if_false')),
    };
  }

  if (workerType === WORKER_TYPE_REPEAT) {
    const expression = asString(readConfigKey(config, 'collection_expression'));
    const path = splitPropertyPath(expression);
    return {
      ...base,
      kind: 'repeat',
      source: asString(readConfigKey(config, 'tag_name')),
      targetExpression: expression,
      targetPath: path ?? [],
      targetIsPath: path !== null,
      repeatScope: asString(readConfigKey(config, 'repeat_scope')),
      repeatColumns: Array.isArray(readConfigKey(config, 'columns')) ? (readConfigKey(config, 'columns') as unknown[]).length : null,
      cellRef: cellReference(config, 'range'),
    };
  }

  return base;
}

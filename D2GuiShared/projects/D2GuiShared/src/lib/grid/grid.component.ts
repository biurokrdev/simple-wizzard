import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  TemplateRef,
  afterNextRender,
  computed,
  contentChild,
  contentChildren,
  inject,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';

import { GridCellContext, D2GridCellDef } from './grid-cell-def';
import { D2GridDetailDef, GridDetailContext } from './grid-detail-def';
import { cellText, countActiveFilters, filterRows, pageTokens, paginate, sortRows } from './grid-data';
import {
  DEFAULT_GRID_LABELS,
  GridCollapseMode,
  GridColumn,
  GridFilterCriteria,
  GridFilterField,
  GridLabels,
  GridSelectionMode,
  GridSort,
  GridState,
  SortDirection,
  formatLabel,
} from './grid.model';

/**
 * Uniwersalna kontrolka listy (grid) z wyszukiwaniem, panelem filtrów, filtrami
 * kolumnowymi, sortowaniem, stronicowaniem i szkieletem ładowania.
 *
 * Przepływ danych jest jednokierunkowy i w całości oparty o sygnały:
 *
 * ```
 * rows() -> filteredRows() -> sortedRows() -> currentPage() -> pageRows()
 * ```
 *
 * Każdy krok to czysta funkcja z `grid-data.ts`, więc zachowanie grida da się
 * testować bez DOM, a komponent odpowiada wyłącznie za prezentację i zbieranie
 * akcji użytkownika.
 *
 * Gdy tabela nie mieści się na ekranie, kolumny oznaczone jako `collapsible`
 * chowają się do rozwijanego panelu szczegółów wiersza.
 *
 * ```html
 * <d2-grid [columns]="columns" [rows]="rows" [loading]="loading()">
 *   <button d2GridActions type="button">Pobierz listę</button>
 *   <ng-template d2GridCell="actions" let-row>
 *     <button type="button" (click)="show(row)">Wyświetl</button>
 *   </ng-template>
 *   <ng-template d2GridDetail let-row>
 *     <pre>{{ row.configJson }}</pre>
 *   </ng-template>
 * </d2-grid>
 * ```
 *
 * Kontrolka pochodzi z projektu `grid-forever` (`ui-grid`) i została przeniesiona
 * do D2GuiShared z prefiksem `d2`; w D2 dodano szablon szczegółów wiersza
 * (`d2GridDetail`) oraz kliknięcie w wiersz (`rowClickable` / `rowClick`).
 */
@Component({
  selector: 'd2-grid',
  imports: [NgTemplateOutlet],
  templateUrl: './grid.component.html',
  styleUrl: './grid.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'd2-grid' },
})
export class D2GridComponent<T> {
  // --- Wejścia -------------------------------------------------------------

  /** Definicje kolumn - kolejność w tablicy jest kolejnością na ekranie. */
  readonly columns = input.required<readonly GridColumn<T>[]>();

  /** Komplet danych. Grid sam filtruje, sortuje i dzieli je na strony. */
  readonly rows = input.required<readonly T[]>();

  /**
   * Identyfikator wiersza dla `@for ... track`. Domyślnie sam obiekt wiersza.
   *
   * Ten sam identyfikator zapamiętuje zaznaczenie i rozwinięte szczegóły, więc
   * przy korzystaniu z tych funkcji musi być niezależny od indeksu wiersza
   * (np. `(wycena) => wycena.id`).
   */
  readonly trackBy = input<(row: T, index: number) => unknown>((row) => row);

  /** Początkowa liczba wierszy na stronie. */
  readonly pageSize = input(10);

  /** Wartości do wyboru w selekcie "Wyświetlaj". */
  readonly pageSizeOptions = input<readonly number[]>([10, 25, 50, 100]);

  /**
   * Czy pokazywać pole wyszukiwania globalnego nad tabelą. Domyślnie wyłączone -
   * aplikacja włącza je świadomie dla konkretnej listy (`[searchable]="true"`).
   */
  readonly searchable = input(false);

  /** Czy pokazywać wiersz filtrów pod nagłówkami kolumn. */
  readonly showColumnFilters = input(true);

  /** Listy rozwijane panelu filtrów. Pusta tablica ukrywa cały panel. */
  readonly filterFields = input<readonly GridFilterField[]>([]);

  /** Sortowanie ustawione przy starcie (można też zmienić z zewnątrz). */
  readonly initialSort = input<GridSort | null>(null);

  /** Pokazuje szkielet ładowania zamiast danych. */
  readonly loading = input(false);

  /** Liczba wierszy szkieletu; `null` = tyle, ile mieści strona. */
  readonly skeletonRows = input<number | null>(null);

  /** Tryb zaznaczania wierszy: bez zaznaczania, jeden wiersz albo wiele. */
  readonly selection = input<GridSelectionMode>('none');

  /** Tryb chowania kolumn `collapsible` do panelu szczegółów. */
  readonly collapse = input<GridCollapseMode>('auto');

  /** Szerokość kontrolki (px), poniżej której chowane są kolumny `collapsible`. */
  readonly collapseWidth = input(1100);

  /** Komunikat dla pustej listy; puste = tekst domyślny z `labels`. */
  readonly emptyMessage = input('');

  /** Nadpisanie wybranych tekstów interfejsu. */
  readonly labels = input<Partial<GridLabels>>({});

  /** Opis tabeli dla czytników ekranu (element `<caption>`). */
  readonly caption = input('');

  /**
   * Czy kliknięcie w wiersz (poza przyciskami i polami formularza) ma zgłaszać
   * `rowClick`. Wiersze dostają wtedy kursor wskazujący.
   */
  readonly rowClickable = input(false);

  // --- Wyjścia -------------------------------------------------------------

  /** Emitowane po każdej zmianie stanu (fraza, filtry, sortowanie, strona). */
  readonly stateChange = output<GridState>();

  /** Emitowane po każdej zmianie zaznaczenia - lista zaznaczonych wierszy. */
  readonly selectionChange = output<readonly T[]>();

  /** Emitowane po kliknięciu w wiersz, gdy `rowClickable` jest włączone. */
  readonly rowClick = output<T>();

  // --- Szablony komórek ----------------------------------------------------

  private readonly cellDefs = contentChildren(D2GridCellDef);

  private readonly cellTemplates = computed(() => {
    const templates = new Map<string, TemplateRef<GridCellContext<T>>>();
    for (const cellDef of this.cellDefs()) {
      templates.set(cellDef.d2GridCell(), cellDef.template);
    }
    return templates;
  });

  // --- Szablon szczegółów wiersza ------------------------------------------

  private readonly detailDef = contentChild(D2GridDetailDef);

  /** Czy aplikacja podała własny szablon szczegółów wiersza (`d2GridDetail`). */
  readonly hasDetailTemplate = computed(() => this.detailDef() !== undefined);

  // --- Stan wewnętrzny -----------------------------------------------------

  /** Fraza wyszukiwania globalnego. */
  private readonly search = signal('');

  /** Filtry kolumnowe: klucz kolumny -> szukany tekst. */
  private readonly filters = signal<Record<string, string>>({});

  /** Wybory z panelu filtrów: klucz kolumny -> dokładna wartość. */
  private readonly fieldFilters = signal<Record<string, string>>({});

  /** Czy panel filtrów jest rozwinięty (domyślnie tak, gdy są jakieś pola). */
  protected readonly filtersVisible = linkedSignal(() => this.filterFields().length > 0);

  /** Aktualne sortowanie; resetuje się, gdy zmieni się `initialSort`. */
  private readonly sort = linkedSignal<GridSort | null>(() => this.initialSort());

  /** Aktualny rozmiar strony; resetuje się, gdy zmieni się `pageSize`. */
  private readonly activePageSize = linkedSignal(() => this.pageSize());

  /** Strona wybrana przez użytkownika (przycinana do dostępnego zakresu). */
  private readonly requestedPage = signal(1);

  /** Identyfikatory wierszy z rozwiniętym panelem szczegółów. */
  private readonly expandedRows = signal<ReadonlySet<unknown>>(new Set());

  /** Identyfikatory zaznaczonych wierszy (przeżywają zmianę strony i filtrów). */
  private readonly selectedIds = signal<ReadonlySet<unknown>>(new Set());

  /** Zmierzona szerokość kontrolki - decyduje o chowaniu kolumn. */
  private readonly hostWidth = signal(0);

  // --- Dane pochodne -------------------------------------------------------

  /** Warunki zawężania listy w jednym miejscu. */
  private readonly criteria = computed<GridFilterCriteria>(() => ({
    search: this.search(),
    filters: this.filters(),
    fieldFilters: this.fieldFilters(),
  }));

  /** Wiersze po wyszukiwaniu globalnym, panelu filtrów i filtrach kolumnowych. */
  readonly filteredRows = computed(() => filterRows(this.rows(), this.columns(), this.criteria()));

  /** Wiersze po filtrowaniu i sortowaniu. */
  readonly sortedRows = computed(() => sortRows(this.filteredRows(), this.columns(), this.sort()));

  /** Bieżąca strona wraz z metadanymi (zakres, liczba stron, suma wierszy). */
  readonly currentPage = computed(() =>
    paginate(this.sortedRows(), this.requestedPage(), this.activePageSize()),
  );

  /** Wiersze widoczne na ekranie. */
  readonly pageRows = computed(() => this.currentPage().items);

  /** Pełny, aktualny stan kontrolki. */
  readonly state = computed<GridState>(() => ({
    search: this.search(),
    filters: { ...this.filters() },
    fieldFilters: { ...this.fieldFilters() },
    sort: this.sort(),
    page: this.currentPage().page,
    pageSize: this.activePageSize(),
  }));

  /** Kolumny, które wolno schować do panelu szczegółów. */
  private readonly collapsibleColumns = computed(() =>
    this.columns().filter((column) => column.collapsible),
  );

  /**
   * Czy kolumny `collapsible` są aktualnie schowane - a więc czy wiersze mają
   * co pokazać w panelu szczegółów. Publiczne, bo aplikacja może udostępnić
   * rozwijanie szczegółów także z własnego menu wiersza.
   */
  readonly collapsed = computed(() => {
    if (this.collapsibleColumns().length === 0) {
      return false;
    }
    switch (this.collapse()) {
      case 'always':
        return true;
      case 'never':
        return false;
      default:
        return this.hostWidth() > 0 && this.hostWidth() < this.collapseWidth();
    }
  });

  /** Kolumny pokazywane w tabeli. */
  readonly visibleColumns = computed(() =>
    this.collapsed() ? this.columns().filter((column) => !column.collapsible) : this.columns(),
  );

  /** Kolumny przeniesione do panelu szczegółów wiersza. */
  protected readonly detailColumns = computed(() =>
    this.collapsed() ? this.collapsibleColumns() : [],
  );

  /**
   * Czy wiersze mają przycisk rozwijania szczegółów: są schowane kolumny albo
   * aplikacja podała własny szablon `d2GridDetail`.
   */
  readonly expandable = computed(() => this.collapsed() || this.hasDetailTemplate());

  /** Czy tabela ma kolumnę z polami zaznaczania. */
  readonly selectable = computed(() => this.selection() !== 'none');

  /** Zaznaczone wiersze - w kolejności ze zbioru danych. */
  readonly selectedRows = computed(() =>
    this.rows().filter((row, index) => this.selectedIds().has(this.trackRow(index, row))),
  );

  /** Liczba zaznaczonych wierszy. */
  readonly selectedCount = computed(() => this.selectedRows().length);

  /** Czy wszystkie wiersze bieżącej strony są zaznaczone. */
  protected readonly allOnPageSelected = computed(() => {
    const rows = this.pageRows();
    return rows.length > 0 && rows.every((row, index) => this.isRowSelected(row, index));
  });

  /** Czy zaznaczona jest część wierszy strony (pole "trzeciego stanu"). */
  protected readonly someOnPageSelected = computed(
    () =>
      !this.allOnPageSelected() &&
      this.pageRows().some((row, index) => this.isRowSelected(row, index)),
  );

  /** Podsumowanie zaznaczenia, np. "Zaznaczono: 105". */
  protected readonly selectionSummary = computed(() =>
    formatLabel(this.texts().selectedCount, { count: this.selectedCount() }),
  );

  /** Liczba kolumn tabeli razem z kolumnami zaznaczania i szczegółów. */
  protected readonly columnCount = computed(
    () =>
      this.visibleColumns().length + (this.expandable() ? 1 : 0) + (this.selectable() ? 1 : 0),
  );

  /** Teksty interfejsu: domyślne, nadpisane tym, co poda aplikacja. */
  protected readonly texts = computed<GridLabels>(() => ({
    ...DEFAULT_GRID_LABELS,
    ...this.labels(),
  }));

  /** Przyciski stronicowania (numery stron i wielokropki). */
  protected readonly pageButtons = computed(() =>
    pageTokens(this.currentPage().page, this.currentPage().pageCount),
  );

  /** Podsumowanie w stopce, np. "Wyświetlono 1-10 z 21". */
  protected readonly summary = computed(() => {
    const page = this.currentPage();
    return formatLabel(this.texts().summary, {
      from: page.from,
      to: page.to,
      total: page.total,
    });
  });

  /** Liczba aktywnych filtrów (panel + kolumny), bez frazy wyszukiwania. */
  protected readonly activeFilterCount = computed(() => countActiveFilters(this.criteria()));

  /** Etykieta przycisku czyszczenia filtrów wraz z licznikiem. */
  protected readonly clearFiltersLabel = computed(() =>
    formatLabel(this.texts().clearFilters, { count: this.activeFilterCount() }),
  );

  /**
   * Wyszukiwarka globalna i filtry kolumnowe wykluczają się - użytkownik zawęża
   * listę albo jednym, albo drugim sposobem. Aktywne narzędzie blokuje to drugie,
   * dzięki czemu nie da się ustawić dwóch sprzecznych zawężeń naraz.
   */
  readonly columnFiltersLocked = computed(() => this.search().trim() !== '');

  /** Czy pole wyszukiwania globalnego jest zablokowane przez filtr kolumnowy. */
  readonly searchLocked = computed(() =>
    Object.values(this.filters()).some((wartosc) => wartosc.trim() !== ''),
  );

  /** Wiersze szkieletu pokazywane w trakcie ładowania. */
  protected readonly skeletonPlaceholders = computed(() =>
    Array.from({ length: this.skeletonRows() ?? this.activePageSize() }, (_, index) => index),
  );

  /** Komunikat pustej listy - własny albo domyślny. */
  protected readonly emptyText = computed(() => this.emptyMessage() || this.texts().empty);

  constructor() {
    this.observeWidth();
  }

  // --- Akcje użytkownika ---------------------------------------------------

  /**
   * Ustawia frazę wyszukiwania globalnego i wraca na pierwszą stronę.
   * Ignorowane, gdy aktywny jest filtr kolumnowy.
   */
  setSearch(term: string): void {
    if (this.searchLocked()) {
      return;
    }
    this.search.set(term);
    this.afterCriteriaChange();
  }

  /**
   * Ustawia filtr pojedynczej kolumny i wraca na pierwszą stronę.
   * Ignorowane, gdy filtry są zablokowane przez wyszukiwarkę globalną.
   */
  setColumnFilter(key: string, term: string): void {
    if (this.columnFiltersLocked()) {
      return;
    }
    this.filters.update((filters) => ({ ...filters, [key]: term }));
    this.afterCriteriaChange();
  }

  /** Aktualna wartość filtra kolumny. */
  columnFilter(key: string): string {
    return this.filters()[key] ?? '';
  }

  /** Czyści filtr pojedynczej kolumny (przycisk "✕" w polu). */
  clearColumnFilter(key: string): void {
    this.filters.update((filters) => ({ ...filters, [key]: '' }));
    this.afterCriteriaChange();
  }

  /** Ustawia wybór z panelu filtrów i wraca na pierwszą stronę. */
  setFieldFilter(key: string, value: string): void {
    this.fieldFilters.update((filters) => ({ ...filters, [key]: value }));
    this.afterCriteriaChange();
  }

  /** Aktualna wartość pola panelu filtrów. */
  fieldFilter(key: string): string {
    return this.fieldFilters()[key] ?? '';
  }

  /** Rozwija/zwija panel filtrów. */
  toggleFilters(): void {
    this.filtersVisible.update((widoczne) => !widoczne);
  }

  /** Czyści panel filtrów i filtry kolumnowe (fraza pozostaje bez zmian). */
  clearFilters(): void {
    this.filters.set({});
    this.fieldFilters.set({});
    this.afterCriteriaChange();
  }

  /**
   * Przełącza sortowanie kolumny w cyklu: rosnąco -> malejąco -> brak.
   * Kolumny bez `sortable` są ignorowane.
   */
  toggleSort(column: GridColumn<T>): void {
    if (!column.sortable) {
      return;
    }
    this.sort.set(nextSort(this.sort(), column.key));
    this.requestedPage.set(1);
    this.emitState();
  }

  /** Kierunek sortowania kolumny albo `null`, gdy nie jest sortowana. */
  sortDirection(column: GridColumn<T>): SortDirection | null {
    const sort = this.sort();
    return sort && sort.key === column.key ? sort.direction : null;
  }

  /** Przechodzi na wskazaną stronę (numer jest przycinany do zakresu). */
  goToPage(page: number): void {
    this.requestedPage.set(Math.max(1, Math.trunc(page)));
    this.emitState();
  }

  /** Poprzednia strona (bez efektu na pierwszej). */
  previousPage(): void {
    this.goToPage(this.currentPage().page - 1);
  }

  /** Następna strona (bez efektu na ostatniej). */
  nextPage(): void {
    this.goToPage(this.currentPage().page + 1);
  }

  /** Zmienia liczbę wierszy na stronie i wraca na pierwszą stronę. */
  setPageSize(size: number): void {
    this.activePageSize.set(Math.max(1, Math.trunc(size)));
    this.requestedPage.set(1);
    this.emitState();
  }

  /** Rozwija lub zwija panel szczegółów wiersza. */
  toggleRow(row: T, index: number): void {
    const id = this.trackRow(index, row);
    this.expandedRows.update((rozwiniete) => {
      const kopia = new Set(rozwiniete);
      if (!kopia.delete(id)) {
        kopia.add(id);
      }
      return kopia;
    });
  }

  /** Czy wiersz jest zaznaczony. */
  isRowSelected(row: T, index: number): boolean {
    return this.selectedIds().has(this.trackRow(index, row));
  }

  /**
   * Zaznacza lub odznacza wiersz. W trybie `single` zaznaczenie przechodzi na
   * kliknięty wiersz, w trybie `multiple` dokłada się do poprzednich.
   */
  toggleRowSelection(row: T, index: number): void {
    if (!this.selectable()) {
      return;
    }
    const id = this.trackRow(index, row);
    this.selectedIds.update((zaznaczone) => {
      if (this.selection() === 'single') {
        return zaznaczone.has(id) ? new Set() : new Set([id]);
      }
      const kopia = new Set(zaznaczone);
      if (!kopia.delete(id)) {
        kopia.add(id);
      }
      return kopia;
    });
    this.selectionChange.emit(this.selectedRows());
  }

  /** Zaznacza albo odznacza wszystkie wiersze bieżącej strony (tryb `multiple`). */
  togglePageSelection(): void {
    if (this.selection() !== 'multiple') {
      return;
    }
    const idsStrony = this.pageRows().map((row, index) => this.trackRow(index, row));
    const zaznaczWszystkie = !this.allOnPageSelected();
    this.selectedIds.update((zaznaczone) => {
      const kopia = new Set(zaznaczone);
      for (const id of idsStrony) {
        if (zaznaczWszystkie) {
          kopia.add(id);
        } else {
          kopia.delete(id);
        }
      }
      return kopia;
    });
    this.selectionChange.emit(this.selectedRows());
  }

  /** Czyści zaznaczenie. */
  clearSelection(): void {
    this.selectedIds.set(new Set());
    this.selectionChange.emit(this.selectedRows());
  }

  /** Czy wiersz ma rozwinięty panel szczegółów. */
  isRowExpanded(row: T, index: number): boolean {
    return this.expandedRows().has(this.trackRow(index, row));
  }

  /** Czyści frazę, filtry i sortowanie oraz wraca na pierwszą stronę. */
  reset(): void {
    this.search.set('');
    this.filters.set({});
    this.fieldFilters.set({});
    this.sort.set(this.initialSort());
    this.activePageSize.set(this.pageSize());
    this.expandedRows.set(new Set());
    this.selectedIds.set(new Set());
    this.requestedPage.set(1);
    this.emitState();
  }

  // --- Pomocnicze dla szablonu --------------------------------------------

  /** Tekst komórki wyliczony z definicji kolumny. */
  protected text(column: GridColumn<T>, row: T): string {
    return cellText(column, row);
  }

  /** Szablon własnej komórki dla kolumny albo `undefined`. */
  protected cellTemplate(key: string): TemplateRef<GridCellContext<T>> | undefined {
    return this.cellTemplates().get(key);
  }

  /** Kontekst przekazywany do szablonu komórki. */
  protected cellContext(row: T, column: GridColumn<T>, index: number): GridCellContext<T> {
    return { $implicit: row, row, column, index };
  }

  /** Własny szablon szczegółów wiersza albo `undefined`. */
  protected detailTemplate(): TemplateRef<GridDetailContext<T>> | undefined {
    return this.detailDef()?.template;
  }

  /** Kontekst przekazywany do szablonu szczegółów wiersza. */
  protected detailContext(row: T, index: number): GridDetailContext<T> {
    return { $implicit: row, row, index };
  }

  /**
   * Kliknięcie w wiersz. Kliknięcia w przyciski, linki i pola formularza
   * (np. akcje wiersza, pola zaznaczania) nie są traktowane jako wybór wiersza.
   */
  protected onRowClick(event: MouseEvent, row: T): void {
    if (!this.rowClickable()) {
      return;
    }
    const target = event.target as HTMLElement | null;
    if (target?.closest('button, a, input, select, textarea, label')) {
      return;
    }
    this.rowClick.emit(row);
  }

  /** Identyfikator wiersza dla `@for ... track`. */
  trackRow(index: number, row: T): unknown {
    return this.trackBy()(row, index);
  }

  /** Wartość `aria-sort` nagłówka kolumny. */
  protected ariaSort(column: GridColumn<T>): 'ascending' | 'descending' | 'none' | null {
    if (!column.sortable) {
      return null;
    }
    const direction = this.sortDirection(column);
    return direction === 'asc' ? 'ascending' : direction === 'desc' ? 'descending' : 'none';
  }

  /** Etykieta z podstawionym parametrem `{column}`. */
  protected columnLabel(template: string, column: GridColumn<T>): string {
    return formatLabel(template, { column: column.header });
  }

  /** Etykieta przycisku strony. */
  protected pageLabel(page: number): string {
    return formatLabel(this.texts().page, { page });
  }

  /** Odczytuje wartość z pola formularza (upraszcza szablon). */
  protected inputValue(event: Event): string {
    return (event.target as HTMLInputElement | HTMLSelectElement).value;
  }

  // --- Wewnętrzne ----------------------------------------------------------

  /** Po każdej zmianie warunków wracamy na stronę 1 i zgłaszamy stan. */
  private afterCriteriaChange(): void {
    this.requestedPage.set(1);
    this.emitState();
  }

  private emitState(): void {
    this.stateChange.emit(this.state());
  }

  /**
   * Śledzi szerokość kontrolki, aby w trybie `auto` decydować o chowaniu
   * kolumn. Obserwator jest zakładany dopiero po pierwszym renderze i sprzątany
   * razem z komponentem.
   */
  private observeWidth(): void {
    if (typeof ResizeObserver === 'undefined') {
      return;
    }
    const host = inject(ElementRef<HTMLElement>).nativeElement;
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      const observer = new ResizeObserver(([entry]) =>
        this.hostWidth.set(entry.contentRect.width),
      );
      observer.observe(host);
      destroyRef.onDestroy(() => observer.disconnect());
    });
  }
}

/**
 * Kolejny stan sortowania po kliknięciu w nagłówek:
 * inna kolumna -> rosnąco, rosnąco -> malejąco, malejąco -> brak sortowania.
 */
function nextSort(current: GridSort | null, key: string): GridSort | null {
  if (!current || current.key !== key) {
    return { key, direction: 'asc' };
  }
  return current.direction === 'asc' ? { key, direction: 'desc' } : null;
}

import { Directive, TemplateRef, inject, input } from '@angular/core';

import { GridColumn } from './grid.model';

/** Kontekst dostępny w szablonie komórki (`let-row`, `let-column`, `let-i`). */
export interface GridCellContext<T> {
  /** Wiersz - dostępny również jako `let-row="row"`. */
  readonly $implicit: T;
  /** Wiersz danych. */
  readonly row: T;
  /** Definicja kolumny, dla której renderowana jest komórka. */
  readonly column: GridColumn<T>;
  /** Indeks wiersza na bieżącej stronie (od 0). */
  readonly index: number;
}

/**
 * Szablon własnej zawartości komórki dla wskazanej kolumny.
 *
 * ```html
 * <d2-grid [columns]="columns" [rows]="rows">
 *   <ng-template d2GridCell="status" let-row let-column="column">
 *     <span class="badge">{{ row.status }}</span>
 *   </ng-template>
 * </d2-grid>
 * ```
 *
 * Kontekst jest celowo typowany jako `any` - klucz kolumny to zwykły string,
 * więc Angular nie ma z czego wywnioskować typu wiersza w szablonie.
 */
@Directive({
  selector: 'ng-template[d2GridCell]',
})
export class D2GridCellDef<T = any> {
  /** Klucz kolumny, której komórki mają korzystać z tego szablonu. */
  readonly d2GridCell = input.required<string>();

  /** Szablon wstrzykiwany przez Angulara dla hosta `ng-template`. */
  readonly template = inject<TemplateRef<GridCellContext<T>>>(TemplateRef);

  /** Pozwala Angularowi typować zmienne `let-...` w szablonie. */
  static ngTemplateContextGuard<T>(
    directive: D2GridCellDef<T>,
    context: unknown,
  ): context is GridCellContext<T> {
    return true;
  }
}

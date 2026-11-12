import { Directive, TemplateRef, inject } from '@angular/core';

/** Kontekst szablonu szczegółów wiersza (`let-row`, `let-index="index"`). */
export interface GridDetailContext<T> {
  /** Wiersz - dostępny również jako `let-row="row"`. */
  readonly $implicit: T;
  /** Wiersz danych. */
  readonly row: T;
  /** Indeks wiersza na bieżącej stronie (od 0). */
  readonly index: number;
}

/**
 * Własny panel szczegółów wiersza - rozwijany przyciskiem „∨" na końcu wiersza.
 *
 * Gdy aplikacja poda ten szablon, przycisk rozwijania jest widoczny zawsze
 * (nie tylko po schowaniu kolumn `collapsible`), a panel pokazuje schowane
 * kolumny i pod nimi treść szablonu - np. pełny JSON konfiguracji.
 *
 * ```html
 * <d2-grid [columns]="columns" [rows]="rows">
 *   <ng-template d2GridDetail let-row>
 *     <pre>{{ row.configJson }}</pre>
 *   </ng-template>
 * </d2-grid>
 * ```
 */
@Directive({
  selector: 'ng-template[d2GridDetail]',
})
export class D2GridDetailDef<T = any> {
  /** Szablon wstrzykiwany przez Angulara dla hosta `ng-template`. */
  readonly template = inject<TemplateRef<GridDetailContext<T>>>(TemplateRef);

  /** Pozwala Angularowi typować zmienne `let-...` w szablonie. */
  static ngTemplateContextGuard<T>(
    directive: D2GridDetailDef<T>,
    context: unknown,
  ): context is GridDetailContext<T> {
    return true;
  }
}

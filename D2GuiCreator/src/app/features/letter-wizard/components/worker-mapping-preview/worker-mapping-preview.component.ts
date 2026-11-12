import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { REPEAT_SCOPE_LABELS, buildWorkerMappingView } from '../../models/worker-config.util';

/**
 * Graficzny podgląd mapowania workera zamiast surowego JSON-a:
 *  - „Podstaw wartość":  `<%znacznik%>`  →  formModel › ojciec › nazwa   (+ „domyślnie: …")
 *  - „Stan":             [kontrolka]     widoczna gdy  formModel › warunek  (+ „ukryj gdy fałsz")
 * Nieznany typ albo nieczytelny JSON → skrócony surowy tekst z ostrzeżeniem.
 * Pełny JSON pokazuje rodzic (przycisk „JSON" w wierszu tabeli / sekcja w dialogu).
 */
@Component({
  selector: 'app-worker-mapping-preview',
  standalone: true,
  imports: [NgTemplateOutlet],
  templateUrl: './worker-mapping-preview.component.html',
  styleUrl: './worker-mapping-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerMappingPreviewComponent {
  readonly workerType = input.required<string>();
  readonly configJson = input<string | null | undefined>('');
  /** Zwarty wariant (tabela) vs. pełny (dialog). */
  readonly compact = input(true);

  protected readonly view = computed(() => buildWorkerMappingView(this.workerType(), this.configJson()));
  protected readonly scopeLabel = computed(() => {
    const scope = this.view().repeatScope;
    return scope ? (REPEAT_SCOPE_LABELS[scope] ?? scope) : null;
  });

  protected readonly rawPreview = computed(() => {
    const raw = (this.configJson() ?? '').replace(/\s+/g, ' ').trim();
    return raw.length > 120 ? `${raw.slice(0, 117)}…` : raw;
  });
}

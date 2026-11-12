import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { D2ConnectionStatusSource, D2_GUI_CONFIG } from '../config/d2-gui.config';

/**
 * Pasek informujący o braku połączenia z API. Widoczny, gdy `D2ConnectionStatusSource.isApiConnected()`
 * jest `false`. Można go zamknąć (X) — pojawi się ponownie po odzyskaniu i ponownej utracie połączenia.
 * Styl 1:1 z D2 ViewerEditor (`offline-banner`).
 */
@Component({
  selector: 'd2-offline-banner',
  standalone: true,
  templateUrl: './offline-banner.component.html',
  styleUrl: './offline-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2OfflineBannerComponent {
  private readonly source = inject(D2ConnectionStatusSource);
  private readonly dismissed = signal(false);

  protected readonly message = inject(D2_GUI_CONFIG).offlineMessage;
  protected readonly isVisible = computed(() => !this.source.isApiConnected() && !this.dismissed());

  constructor() {
    // Powrót połączenia kasuje „zamknięcie", żeby kolejna awaria znów pokazała pasek.
    effect(() => {
      if (this.source.isApiConnected()) {
        this.dismissed.set(false);
      }
    });
  }

  protected dismiss(): void {
    this.dismissed.set(true);
  }
}

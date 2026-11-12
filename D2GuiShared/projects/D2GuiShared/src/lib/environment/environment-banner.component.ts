import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { D2EnvironmentSource } from '../config/d2-gui.config';
import { resolveD2EnvironmentBanner } from './environment-banner.util';

/**
 * Cienki, pełnokolorowy pasek z nazwą środowiska („Wersja Local", „Wersja DEV"…).
 * Styl 1:1 z D2 ViewerEditor. Ukryty na produkcji. Nazwa środowiska z `D2EnvironmentSource`.
 */
@Component({
  selector: 'd2-environment-banner',
  standalone: true,
  templateUrl: './environment-banner.component.html',
  styleUrl: './environment-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2EnvironmentBannerComponent {
  private readonly source = inject(D2EnvironmentSource);

  protected readonly view = computed(() => resolveD2EnvironmentBanner(this.source.environment()));
}

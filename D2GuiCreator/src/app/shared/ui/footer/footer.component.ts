import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { D2BuildInfoSource, D2EnvironmentSource, D2_GUI_CONFIG } from '../config/d2-gui.config';
import { resolveD2EnvironmentBanner } from '../environment/environment-banner.util';

/**
 * Stopka-pasek statusu, zawsze widoczna na dole shella. Układ i dane jak w stopce edytora dokumentów
 * D2 ViewerEditor (`editor-footer`): po lewej „Doc2 © rok Doc2 | tagline", na środku środowisko (poza PRD),
 * „Wersja: <buildNumber>" i data buildu, po prawej treść aplikacji przez `<ng-content>` (np. autozapis, zoom).
 * Puste wartości (brak środowiska / buildu) po prostu znikają razem ze swoimi separatorami.
 */
@Component({
  selector: 'd2-footer',
  standalone: true,
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2FooterComponent {
  private readonly config = inject(D2_GUI_CONFIG);
  private readonly environmentSource = inject(D2EnvironmentSource);
  private readonly buildInfo = inject(D2BuildInfoSource);

  readonly appName = input(this.config.appName);
  readonly tagline = input(this.config.footerTagline);
  readonly currentYear = new Date().getFullYear();

  /** Nazwa środowiska wielkimi literami; `null` na produkcji i przy braku nazwy (ta sama reguła co baner). */
  protected readonly environmentLabel = computed(() => {
    const raw = (this.environmentSource.environment() ?? '').trim();
    return resolveD2EnvironmentBanner(raw) ? raw.toUpperCase() : null;
  });

  protected readonly buildNumber = computed(() => nonEmpty(this.buildInfo.buildNumber()));
  protected readonly buildDate = computed(() => nonEmpty(this.buildInfo.buildDate()));
  protected readonly hasBuildInfo = computed(() => this.buildNumber() !== null || this.buildDate() !== null);
}

function nonEmpty(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed ? trimmed : null;
}

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { D2EnvironmentBannerComponent } from '../environment/environment-banner.component';
import { D2OfflineBannerComponent } from '../offline/offline-banner.component';

/**
 * Kontener globalnych banerów nad nagłówkiem: środowisko na górze, offline bezpośrednio pod nim.
 * Siedzi w normalnym flow, więc layout rezerwuje jego wysokość (nic nie nachodzi na treść).
 */
@Component({
  selector: 'd2-global-banners',
  standalone: true,
  imports: [D2EnvironmentBannerComponent, D2OfflineBannerComponent],
  template: `
    <d2-environment-banner />
    <d2-offline-banner />
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2GlobalBannersComponent {}

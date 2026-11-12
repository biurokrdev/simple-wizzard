import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { D2_GUI_CONFIG } from '../config/d2-gui.config';

/**
 * Nagłówek aplikacji 1:1 z D2 ViewerEditor (`dashboard-header`): biały pasek 52px z dolną linią,
 * logo + nazwa jako link „home". Treść po prawej (nawigacja, akcje) przez `<ng-content>`.
 * Wartości domyślne z `D2_GUI_CONFIG`; inputy pozwalają je nadpisać per użycie.
 */
@Component({
  selector: 'd2-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2HeaderComponent {
  private readonly config = inject(D2_GUI_CONFIG);

  readonly appName = input(this.config.appName);
  readonly logoSrc = input(this.config.logoSrc);
  readonly homeLink = input(this.config.homeLink);
  readonly homeLinkTitle = input(this.config.homeLinkTitle);
}

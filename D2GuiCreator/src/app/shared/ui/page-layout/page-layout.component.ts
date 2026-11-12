
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-page-layout',
  standalone: true,
  imports: [],
  templateUrl: './page-layout.component.html',
  styleUrls: ['./page-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PageLayoutComponent {
  @Input() title = '';
  @Input() subtitle?: string;
}

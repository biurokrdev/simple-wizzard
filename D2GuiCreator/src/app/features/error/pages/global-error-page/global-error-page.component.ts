
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-global-error-page',
  standalone: true,
  imports: [ButtonComponent],
  templateUrl: './global-error-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GlobalErrorPageComponent {
  constructor(private readonly router: Router) {}

  goToProjects(): void {
    this.router.navigate(['/projects']);
  }
}

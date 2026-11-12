
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [ButtonComponent],
  templateUrl: './not-found-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotFoundPageComponent {
  constructor(private readonly router: Router) {}

  goToProjects(): void {
    this.router.navigate(['/projects']);
  }
}

import { ErrorHandler, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../services/notification.service';

@Injectable()
export class GlobalAppErrorHandler implements ErrorHandler {
  constructor(
    private readonly router: Router,
    private readonly notifier: NotificationService
  ) {}

  handleError(error: unknown): void {
    console.error('Unhandled application error', error);
    this.notifier.showError(
      'Unexpected error occurred. Please try again later.'
    );
    this.router.navigate(['/error/global']);
  }
}

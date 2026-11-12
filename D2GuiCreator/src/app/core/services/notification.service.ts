import { Injectable } from '@angular/core';

export type NotificationType = 'info' | 'success' | 'warning' | 'error';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  showInfo(message: string): void {
    console.info('[INFO]', message);
    window.alert(message);
  }

  showSuccess(message: string): void {
    console.log('[SUCCESS]', message);
    window.alert(message);
  }

  showWarning(message: string): void {
    console.warn('[WARNING]', message);
    window.alert(message);
  }

  showError(message: string): void {
    console.error('[ERROR]', message);
    window.alert(message);
  }
}

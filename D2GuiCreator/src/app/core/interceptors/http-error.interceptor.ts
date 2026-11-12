import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { NotificationService } from '../services/notification.service';
import { ApiConnectionService } from '../services/api-connection.service';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly notifier: NotificationService,
    private readonly router: Router,
    private readonly apiConnectionService: ApiConnectionService
  ) {}

  intercept(
    req: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 0) {
          // Błąd połączenia - oznacz jako rozłączone tylko dla requestów do API
          if (req.url.includes('/api/') || req.url.includes('/health')) {
            this.apiConnectionService.markAsDisconnected();
          }
          this.notifier.showError('API is not reachable at the moment.');
        } else if (error.status === 404) {
          this.router.navigate(['/error/not-found']);
        } else if (error.status >= 500) {
          this.router.navigate(['/error/global']);
        } else {
          this.notifier.showError('Request failed. Please try again.');
        }

        return throwError(() => error);
      })
    );
  }
}

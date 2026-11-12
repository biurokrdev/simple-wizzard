import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Wejście do części administracyjnej tylko dla roli `Admin`. Do czasu prawdziwego uwierzytelniania (R-03)
 * rola pochodzi z użytkownika mock w `AuthService` — to blokada w GUI, nie w API.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isAdmin()) {
    return true;
  }
  return inject(Router).createUrlTree(['/projects']);
};

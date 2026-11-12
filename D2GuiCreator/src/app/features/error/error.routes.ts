import { Routes } from '@angular/router';

export const ERROR_ROUTES: Routes = [
  {
    path: 'global',
    loadComponent: () =>
      import('./pages/global-error-page/global-error-page.component')
        .then(c => c.GlobalErrorPageComponent)
  },
  {
    path: 'not-found',
    loadComponent: () =>
      import('./pages/not-found-page/not-found-page.component')
        .then(c => c.NotFoundPageComponent)
  }
];

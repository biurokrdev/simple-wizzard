import { Routes } from '@angular/router';
import { D2AppShellComponent } from 'd2-gui-shared';
import { adminGuard } from './core/guards/admin.guard';

export const APP_ROUTES: Routes = [
  {
    path: '',
    // Globalny shell (banery + nagłówek + stopka) z biblioteki D2GuiShared; konfiguracja w app.config.ts (provideD2Gui).
    component: D2AppShellComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'projects'
      },
      {
        path: 'projects',
        loadChildren: () =>
          import('./features/projects/projects.routes').then(
            m => m.PROJECTS_ROUTES
          )
      },
      {
        path: 'teams',
        loadComponent: () =>
          import('./features/tenants/pages/my-teams-page/my-teams-page.component').then(
            m => m.MyTeamsPageComponent
          )
      },
      {
        // Część administracyjna (profile workerów, BR-016) — tylko rola Admin (adminGuard; R-03: rola z mocka)
        path: 'admin',
        canActivate: [adminGuard],
        loadChildren: () =>
          import('./features/admin/admin.routes').then(
            m => m.ADMIN_ROUTES
          )
      },
      {
        path: 'error',
        loadChildren: () =>
          import('./features/error/error.routes').then(
            m => m.ERROR_ROUTES
          )
      },
      {
        path: '**',
        redirectTo: 'error/not-found'
      }
    ]
  }
];

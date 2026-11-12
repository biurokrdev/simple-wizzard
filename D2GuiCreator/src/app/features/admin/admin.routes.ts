import { Routes } from '@angular/router';
import { AdminShellComponent } from './layout/admin-shell/admin-shell.component';

/** Część administracyjna (`/admin`, tylko rola `Admin` — `adminGuard` w `app.routes.ts`). */
export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'projects',
        loadComponent: () => import('./pages/admin-projects-page/admin-projects-page.component').then(c => c.AdminProjectsPageComponent)
      },
      {
        path: 'approvals',
        loadComponent: () => import('./pages/admin-approvals-page/admin-approvals-page.component').then(c => c.AdminApprovalsPageComponent)
      },
      {
        path: 'tenants',
        loadComponent: () => import('./pages/admin-tenants-page/admin-tenants-page.component').then(c => c.AdminTenantsPageComponent)
      },
      {
        path: 'worker-profiles',
        loadComponent: () => import('./pages/worker-profiles-page/worker-profiles-page.component').then(c => c.WorkerProfilesPageComponent)
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/admin-dashboard-page/admin-dashboard-page.component').then(c => c.AdminDashboardPageComponent)
      }
    ]
  }
];

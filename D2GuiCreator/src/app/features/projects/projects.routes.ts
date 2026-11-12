import { Routes } from '@angular/router';

export const PROJECTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/project-list-page/project-list-page.component').then(
        c => c.ProjectListPageComponent
      )
  },
  {
    path: ':projectId',
    children: [
      {
        path: '',
        loadComponent: () =>
          import(
            './pages/project-details-page/project-details-page.component'
          ).then(c => c.ProjectDetailsPageComponent)
      },
      {
        path: 'wizard',
        loadChildren: () =>
          import('../letter-wizard/letter-wizard.routes').then(
            m => m.LETTER_WIZARD_ROUTES
          )
      }
    ]
  }
];

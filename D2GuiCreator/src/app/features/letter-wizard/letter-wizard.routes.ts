import { Routes } from '@angular/router';

export const LETTER_WIZARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/letter-wizard-page/letter-wizard-page.component').then(
        c => c.LetterWizardPageComponent
      )
  }
];
